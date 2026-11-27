using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Nrk.SourceGenerator.Common.Helpers;

namespace Nrk.SourceGenerator.Common.Extensions;

public static class GeneratorRunningContextExtensions
{
    private static readonly Dictionary<string, string> NumericsSymbolMetadataNames = new()
    {
        { "uint", "System.UInt32" },
        { "int", "System.Int32" },
        { "float", "System.Single" },
        { "float2", "System.Numerics.Vector2"},
        { "float3", "System.Numerics.Vector3"},
        { "float4", "System.Numerics.Vector4"},
        { "matrix4x4", "System.Numerics.Matrix4x4" }
    };

    public static ImmutableDictionary<ITypeSymbol, string> GetNumericsSymbols(this GeneratorRunningContext context)
    {
        return NumericsSymbolMetadataNames
            .ToImmutableDictionary<KeyValuePair<string, string>, ITypeSymbol, string>(
                k => context.GetSymbol(k.Value),
                v => v.Key,
                SymbolEqualityComparer.Default);
    }

    public static ImmutableDictionary<ITypeSymbol, string> GetNumericsSymbols(this Compilation compilation)
    {
        return NumericsSymbolMetadataNames
            .ToImmutableDictionary<KeyValuePair<string, string>, ITypeSymbol, string>(
                k => compilation.GetTypeByMetadataName(k.Value)!,
                v => v.Key,
                SymbolEqualityComparer.Default);
    }
}
