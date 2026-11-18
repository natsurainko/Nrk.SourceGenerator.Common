using System;
using System.IO;

namespace Nrk.SourceGenerator.Common.Extensions;

public static class PathHelper
{
    public static StringComparer Comparer { get; } = Path.DirectorySeparatorChar == '\\'
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;
    
    private static string NormalizeSeparators(string path) => path
        .Replace('\\', Path.DirectorySeparatorChar)
        .Replace('/', Path.DirectorySeparatorChar);
}
