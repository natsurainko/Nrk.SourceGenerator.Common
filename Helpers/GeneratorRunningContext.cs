using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Nrk.SourceGenerator.Common.Extensions;

namespace Nrk.SourceGenerator.Common.Helpers;

// Unwinds the current generator output callback after a diagnostic has been
// reported. Catch it with GeneratorRunningContext.Run/TryRun so Roslyn does
// not additionally report a generator failure.
public sealed class GeneratorAbortException : Exception;

public partial class GeneratorRunningContext(SourceProductionContext sourceProductionContext, Compilation compilation)
{
    private readonly Dictionary<string, INamedTypeSymbol> _symbols = new(StringComparer.Ordinal);

    public SourceProductionContext SourceProductionContext { get; } = sourceProductionContext;

    public Compilation Compilation { get; } = compilation;

    public CancellationToken CancellationToken { get; } = sourceProductionContext.CancellationToken;

    public void Report(DiagnosticDescriptor descriptor, Location? location, params object?[]? messageArgs)
        => SourceProductionContext.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageArgs));

    // Resolves a fully qualified name (an optional global:: prefix is allowed)
    // and caches the result for the lifetime of this context. A missing symbol
    // is reported as SGC0001 and aborts the current generation.
    public INamedTypeSymbol GetSymbol(string name)
    {
        var metadataName = name.Trim().TrimGlobalPrefix();

        if (_symbols.TryGetValue(metadataName, out var cached))
            return cached;

        var symbol = Compilation.GetTypeByMetadataName(metadataName);
        if (symbol is null)
        {
            Report(SymbolNotFound, Location.None, metadataName);
            throw new GeneratorAbortException();
        }

        _symbols[metadataName] = symbol;
        return symbol;
    }

    // Runs the generation body and swallows GeneratorAbortException, keeping
    // the diagnostic reported by GetSymbol without failing the generator.
    public void Run(Action action)
    {
        try { action(); }
        catch (GeneratorAbortException) { /*IGNORED*/ }
    }
}

public partial class GeneratorRunningContext
{
#pragma warning disable RS2008 // Analyzer release tracking is not enabled for this helper.
    private static readonly DiagnosticDescriptor SymbolNotFound = new(
        id: "SGC0001",
        title: "C# type not found",
        messageFormat: "The C# type '{0}' could not be resolved in the current compilation",
        category: "SGC",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The name must be a fully qualified C# type name.");
#pragma warning restore RS2008
}

public class GeneratorRunningContext<TValue>(
    SourceProductionContext sourceProductionContext,
    Compilation compilation, TValue value) : GeneratorRunningContext(sourceProductionContext, compilation)
{
    public TValue Value { get; } = value;
}
