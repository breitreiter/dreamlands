using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace GameServer;

/// <summary>
/// OpenTelemetry wiring. Traces and metrics only — logs are deliberately out of
/// scope (see plans/otel_appsignal.md).
///
/// All exporter configuration comes from the standard OTEL_* environment
/// variables, so the collector endpoint and API key live in Azure app settings /
/// local.settings.json and never in the repo. No endpoint set means no telemetry
/// at all: local runs stay silent and a missing setting degrades to "no data"
/// rather than a boot failure or a per-request stall.
/// </summary>
public static class Telemetry
{
    public const string SourceName = "Dreamlands.GameServer";

    public static readonly ActivitySource Source = new(SourceName);
    public static readonly Meter Meter = new(SourceName);

    /// <summary>
    /// Hits on endpoints nothing legitimate should ever call in production. The
    /// correct value is permanently zero; alert on > 0. Dies with the debug
    /// affordance sweep (see TODO.md → Launch Blockers).
    /// </summary>
    private static readonly Counter<long> DebugEndpointHits =
        Meter.CreateCounter<long>("dreamlands.debug_endpoint.hit");

    public static void RecordDebugEndpointHit(string endpoint) =>
        DebugEndpointHits.Add(1, new KeyValuePair<string, object?>("endpoint", endpoint));

    public static IServiceCollection AddDreamlandsTelemetry(this IServiceCollection services)
    {
        // The single gate. Everything below is skipped when unset.
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")))
            return services;

        // Consumption-plan workers get torn down on idle without a guaranteed flush,
        // so the 5s default would routinely lose the last request before an idle-out —
        // disproportionately the interesting one. Operator can still override.
        if (Environment.GetEnvironmentVariable("OTEL_BSP_SCHEDULE_DELAY") == null)
            Environment.SetEnvironmentVariable("OTEL_BSP_SCHEDULE_DELAY", "2000");

        // GameData is a singleton but isn't built yet, and reading api-version twice
        // is cheaper than reordering startup for it.
        var data = new GameData();
        var environment = data.IsDev ? "development" : "production";

        // AppSignal authenticates by RESOURCE ATTRIBUTE, not by a header — the push
        // API key rides on every payload as appsignal.config.push_api_key. Without it
        // the collector drops everything, silently from our side.
        var pushKey = Environment.GetEnvironmentVariable("APPSIGNAL_PUSH_API_KEY");
        if (string.IsNullOrEmpty(pushKey))
            Console.Error.WriteLine(
                "[telemetry] OTLP endpoint is set but APPSIGNAL_PUSH_API_KEY is not — " +
                "the collector will reject every export.");

        services.AddOpenTelemetry()
            .ConfigureResource(r => r
                .AddService(
                    serviceName: Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "dreamlands-api",
                    serviceVersion: data.ApiVersion)
                // Not on by default: without this, OTEL_RESOURCE_ATTRIBUTES is ignored
                // outright (verified 2026-08-06 — the variable never reached the wire).
                .AddEnvironmentVariableDetector()
                .AddAttributes([
                    new KeyValuePair<string, object>("appsignal.config.push_api_key", pushKey ?? ""),
                    new KeyValuePair<string, object>("appsignal.config.name",
                        Environment.GetEnvironmentVariable("APPSIGNAL_APP_NAME") ?? "dreamlands"),
                    new KeyValuePair<string, object>("appsignal.config.environment", environment),
                    // Drives AppSignal's deploy markers.
                    new KeyValuePair<string, object>("appsignal.config.revision", data.ApiVersion),
                    new KeyValuePair<string, object>("appsignal.config.language_integration", "dotnet"),
                    new KeyValuePair<string, object>("host.name", Environment.MachineName),
                    new KeyValuePair<string, object>("deployment.environment", environment),
                ]))
            // Traces: our own spans are the only server-side traces we get. The
            // isolated worker takes invocations over gRPC from the Functions host, so
            // AspNetCore instrumentation emits NO request spans here — verified
            // 2026-08-06 against a local collector, with and without a span filter,
            // with and without the worker OTel package and host telemetryMode. It is
            // registered for metrics only, below.
            .WithTracing(t => t
                .AddSource(SourceName)
                .AddHttpClientInstrumentation(o =>
                    // The worker's own long-lived gRPC channel to the host is noise.
                    o.FilterHttpRequestMessage = r =>
                        r.RequestUri?.AbsolutePath.Contains("AzureFunctionsRpcMessages") != true))
            .WithMetrics(m => m
                .AddMeter(SourceName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            .UseOtlpExporter();

        // UseOtlpExporter() enables ALL THREE signals — it registers the OTel logging
        // provider whether or not .WithLogging() was called, and 1.17 has no
        // per-signal AddOtlpExporter for tracing to use instead. Verified against a
        // local collector on 2026-08-06: without this line, /v1/logs receives data.
        // We are traces + metrics only (free plan), so mute the provider explicitly.
        // To ship logs later, delete this block — nothing else needs to change.
        services.AddLogging(b => b.AddFilter<OpenTelemetryLoggerProvider>("*", LogLevel.None));

        return services;
    }
}
