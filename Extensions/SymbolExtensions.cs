using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Nrk.SourceGenerator.Common.Node;
using static Nrk.SourceGenerator.Common.Node.GenerationNode;

namespace Nrk.SourceGenerator.Common.Extensions;

public static class SymbolExtensions
{
    extension(INamedTypeSymbol symbol)
    {
        public string GetQualifiedName() => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        public bool IsInterfaceImplemented(INamedTypeSymbol? interfaceType)
        {
            if (interfaceType == null) return false;
        
            return symbol.TypeKind != TypeKind.Interface &&
                   symbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, interfaceType));
        }

        public FileGenerationNode GenPartialClassFile(Func<BlockGenerationNode, BlockGenerationNode> func)
        {
            var accessibility = symbol.DeclaredAccessibility == Accessibility.Internal ? "internal" : "public";
            var classNode = ClassNode(symbol.Name, accessibility, "partial");
            classNode = func(classNode);

            var fileNode = FileNode()
                .AddGeneratedNotes()
                .AddNode(classNode);

            if (symbol.ContainingNamespace.IsGlobalNamespace)
                fileNode.WithGlobalNamespace();
            else
                fileNode.WithNamespace(symbol.ContainingNamespace.ToDisplayString());
        
            return fileNode;
        }
    }
    
    public static string BuildPrimaryConstructorForwarding(
        this IMethodSymbol constructor,
        string className,
        bool alwaysEmitParentheses = false)
    {
        var format = SymbolDisplayFormat.FullyQualifiedFormat;
        var baseType = constructor.ContainingType.ToDisplayString(format);
        
        if (constructor.Parameters.IsEmpty && !alwaysEmitParentheses)
            return $"{EscapeIdentifier(className)} : {baseType}";

        var parameterTypeFormat = format.AddMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

        var parameters = new List<string>(constructor.Parameters.Length);
        var arguments = new List<string>(constructor.Parameters.Length);

        foreach (var parameter in constructor.Parameters)
        {
            var name = EscapeIdentifier(parameter.Name);
            var type = parameter.Type.ToDisplayString(parameterTypeFormat);

            string declarationModifier = string.Empty;
            string argumentModifier = string.Empty;

            switch (parameter.RefKind)
            {
                case RefKind.Ref:
                    declarationModifier = "ref ";
                    argumentModifier = "ref ";
                    break;

                case RefKind.Out:
                    declarationModifier = "out ";
                    argumentModifier = "out ";
                    break;

                case RefKind.In:
                    declarationModifier = "in ";
                    argumentModifier = "in ";
                    break;

                case RefKind.RefReadOnlyParameter:
                    declarationModifier = "ref readonly ";
                    argumentModifier = "in ";
                    break;
            }

            if (parameter.IsParams)
                declarationModifier = "params " + declarationModifier;

            parameters.Add($"{declarationModifier}{type} {name}");
            arguments.Add($"{argumentModifier}{name}");
        }

        return $"{EscapeIdentifier(className)}({string.Join(", ", parameters)}) : " +
               $"{baseType}({string.Join(", ", arguments)})";
        
        static string EscapeIdentifier(string name) 
            => SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None ? name : "@" + name;
    }
}
