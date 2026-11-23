using System;
using System.IO;
using Microsoft.CodeAnalysis;

namespace Nrk.SourceGenerator.Common.Extensions;

public static class AdditionalExtensions
{
    private static StringComparison Comparison { get; } = Path.DirectorySeparatorChar == '\\'
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;
    
    public static bool EndsWithExtension(this AdditionalText additionalText, string extensionName)
        => additionalText.Path.EndsWith(extensionName, Comparison);
}
