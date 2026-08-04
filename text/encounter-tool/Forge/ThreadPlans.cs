// Per-arc thread definitions, loaded from `_threads.json` beside the arc's bibles.
//
// A thread is an ordered list of choice labels naming one navigable path through the
// arc's enc graph; Thread.Walk replays it to get the beat spine that weave generates
// along. Threads used to be a compiled-in dictionary (Threads.cs, the_villa only),
// which meant authoring content for a new arc required editing source and rebuilding.
// They are content, so they live with the content.
//
// The `note` field carries each thread's COVERAGE INTENT — why the thread exists, which
// otherwise-orphaned beats it is there to sweep up. That reasoning is the hardest part
// to reconstruct later, so it is a first-class field rather than a comment.
namespace Forge;

using System.Text.Json;

public sealed class ThreadPlanFile
{
    /// Thread used when --thread is omitted. Optional when the file defines exactly one.
    public string? Default { get; set; }

    public Dictionary<string, ThreadPlan> Threads { get; set; } = new(StringComparer.Ordinal);
}

public sealed class ThreadPlan
{
    /// Why this thread exists and what it covers. Prose, for humans.
    public string? Note { get; set; }

    /// Ordered choice labels: the pre-'=' option text the thread author types.
    public List<string> Path { get; set; } = [];
}

/// Raised for anything an author can fix by editing `_threads.json`. Commands print the
/// message and exit non-zero; there is no stack trace worth showing.
public sealed class ThreadPlanException(string message) : Exception(message);

public static class ThreadPlans
{
    public const string FileName = "_threads.json";

    private static readonly JsonSerializerOptions Opts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static string PathFor(string arcDir) => System.IO.Path.Combine(arcDir, FileName);

    public static ThreadPlanFile Load(string arcDir)
    {
        var path = PathFor(arcDir);
        if (!File.Exists(path))
            throw new ThreadPlanException(
                $"no {FileName} in '{arcDir}' — author one beside the arc's bibles. "
                + "It maps thread names to ordered choice labels; see the_villa for the shape.");

        ThreadPlanFile? file;
        try
        {
            file = JsonSerializer.Deserialize<ThreadPlanFile>(File.ReadAllText(path), Opts);
        }
        catch (JsonException e)
        {
            throw new ThreadPlanException($"{path}: malformed JSON — {e.Message}");
        }

        if (file is null || file.Threads.Count == 0)
            throw new ThreadPlanException($"{path}: defines no threads");

        foreach (var (name, plan) in file.Threads)
            if (plan.Path.Count == 0)
                throw new ThreadPlanException($"{path}: thread '{name}' has an empty path");

        if (file.Default is not null && !file.Threads.ContainsKey(file.Default))
            throw new ThreadPlanException(
                $"{path}: default '{file.Default}' names no thread (have: {Names(file)})");

        return file;
    }

    /// Load the arc's threads and pick one. `requested` null falls back to the file's
    /// default, or to the only thread when there is exactly one.
    public static (string Name, ThreadPlan Plan) Resolve(string arcDir, string? requested)
    {
        var file = Load(arcDir);

        var name = requested ?? file.Default ?? (file.Threads.Count == 1 ? file.Threads.Keys.First() : null)
            ?? throw new ThreadPlanException(
                $"{PathFor(arcDir)}: no \"default\" and more than one thread — pass --thread (have: {Names(file)})");

        if (!file.Threads.TryGetValue(name, out var plan))
            throw new ThreadPlanException($"unknown thread '{name}' in '{arcDir}' (have: {Names(file)})");

        return (name, plan);
    }

    public static string Names(ThreadPlanFile file) => string.Join(", ", file.Threads.Keys);
}
