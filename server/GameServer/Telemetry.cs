using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
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

    // Every label combination is its own metric series, so the only dimensions here
    // are bounded sets: the action verb (the GameAction switch cases) and a fixed
    // outcome vocabulary. Game ids, item ids, coordinates and other user-supplied
    // strings belong on spans, never on these.
    private static readonly Counter<long> Actions =
        Meter.CreateCounter<long>("dreamlands.action.count");

    private static readonly Histogram<double> ActionDuration =
        Meter.CreateHistogram<double>("dreamlands.action.duration", unit: "s");

    private static readonly Counter<long> GamesCreated =
        Meter.CreateCounter<long>("dreamlands.game.created");

    public static void RecordAction(string action, string outcome, double seconds)
    {
        Actions.Add(1, new TagList { { "action", action }, { "outcome", outcome } });
        ActionDuration.Record(seconds, new TagList { { "action", action } });
    }

    public static void RecordGameCreated() => GamesCreated.Add(1);

    /// <summary>
    /// Why the server said no. The code is a stable slug, never the human message —
    /// several of those interpolate user input, which would be unbounded cardinality.
    /// A rising rate here is the signal for client/server drift.
    /// </summary>
    private static readonly Counter<long> Rejections =
        Meter.CreateCounter<long>("dreamlands.rejected");

    public static void RecordRejection(string reasonCode)
    {
        Rejections.Add(1, new TagList { { "reason_code", reasonCode } });
        Activity.Current?.SetTag("dreamlands.reject_reason", reasonCode);
    }

    private static readonly Counter<long> EncountersStarted =
        Meter.CreateCounter<long>("dreamlands.encounter.started");

    /// <summary>
    /// Encounter ids are paths like "plains/tier1/Lost" or "arcs/forest/fugitive/Start";
    /// the leading segment (biome, "arcs", "intro") is a bounded label. The full id is
    /// far too high-cardinality for a metric.
    /// </summary>
    public static void RecordEncounterStarted(string? encounterId)
    {
        var slash = encounterId?.IndexOf('/') ?? -1;
        var kind = slash > 0 ? encounterId![..slash] : "unknown";
        EncountersStarted.Add(1, new TagList { { "kind", kind } });
    }

    private static readonly Counter<long> CombatsStarted =
        Meter.CreateCounter<long>("dreamlands.combat.started");

    private static readonly Counter<long> CombatsEnded =
        Meter.CreateCounter<long>("dreamlands.combat.ended");

    public static void RecordCombatStarted() => CombatsStarted.Add(1);

    public static void RecordCombatEnded(string result) =>
        CombatsEnded.Add(1, new TagList { { "result", result } });

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
            // Phase 4. Pairs with host.json "telemetryMode": "OpenTelemetry": the host
            // emits the invocation span and this makes our spans its children rather
            // than parallel roots. Inert if the host.json flag is ever removed.
            .UseFunctionsWorkerDefaults()
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
                // Cosmos SDK spans (enabled in CosmosGameStore.CreateClient). Wildcard
                // because the SDK pins only its diagnostic namespace — the constant
                // OpenTelemetryAttributeKeys.DiagnosticNamespace is "Azure.Cosmos" —
                // and not the per-operation source names built on top of it.
                .AddSource("Azure.Cosmos*")
                .AddHttpClientInstrumentation(o =>
                {
                    o.FilterHttpRequestMessage = r =>
                    {
                        var uri = r.RequestUri;
                        if (uri == null) return true;
                        // The worker's own gRPC channel to the Functions host, and the
                        // runtime's own storage plumbing (AzureWebJobsStorage polling).
                        // Neither is our code; both would otherwise dominate the traces.
                        if (uri.AbsolutePath.Contains("AzureFunctionsRpcMessages")) return false;
                        // The Cosmos SDK probes the Instance Metadata Service once per
                        // client to tag its telemetry with VM/region info. App Service
                        // blocks outbound traffic to that link-local address, so the
                        // probe always fails (WSAEACCES) on the first Cosmos call of a
                        // cold-started worker. The SDK swallows it; only this
                        // instrumentation made it look like an error.
                        if (uri.Host == "169.254.169.254") return false;
                        return !uri.Host.EndsWith(".core.windows.net", StringComparison.OrdinalIgnoreCase);
                    };
                    // OTel names client spans after the HTTP method alone, so a trace
                    // list reads "GET GET GET". Add the host — bounded, and enough to
                    // tell Cosmos from anything else at a glance. Paths are deliberately
                    // left out: Cosmos URLs embed document ids.
                    o.EnrichWithHttpRequestMessage = (activity, request) =>
                    {
                        if (request.RequestUri is { } uri)
                            activity.DisplayName = $"{request.Method.Method} {uri.Host}";
                    };
                }))
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
