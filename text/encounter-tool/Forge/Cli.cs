namespace Forge;

public static class Cli
{
    /// Expand CLI path args: a file passes straight through; a directory expands to
    /// its `*ext` children (ordinal-sorted). Lets both `forge parse arc/*.enc`
    /// (shell-globbed) and `forge parse arc/` (dir) work the same way. Extension is
    /// matched by suffix so `.enc` never accidentally grabs `.enc.json`.
    public static List<string> ExpandArgs(IEnumerable<string> args, string ext)
    {
        var files = new List<string>();
        foreach (var a in args)
        {
            if (Directory.Exists(a))
                files.AddRange(Directory.EnumerateFiles(a)
                    .Where(f => f.EndsWith(ext, StringComparison.Ordinal))
                    .OrderBy(f => f, StringComparer.Ordinal));
            else
                files.Add(a);
        }
        return files;
    }
}
