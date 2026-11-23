using System;
using System.IO;

namespace Nrk.SourceGenerator.Common.Helpers;

public static class PathHelper
{
    public static StringComparer Comparer { get; } = Path.DirectorySeparatorChar == '\\'
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;
    
    // Accept both separator styles in XML, including on Unix hosts.
    private static string NormalizeSeparators(string path) => path
        .Replace('\\', Path.DirectorySeparatorChar)
        .Replace('/', Path.DirectorySeparatorChar);

    public static string GetFullPath(string path) => Path.GetFullPath(NormalizeSeparators(path));

    public static string ResolvePath(string baseDirectory, string path)
    {
        var normalizedPath = NormalizeSeparators(path);
        return Path.GetFullPath(Path.Combine(GetFullPath(baseDirectory), normalizedPath));
    }
}
