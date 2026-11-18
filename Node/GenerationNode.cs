using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nrk.SourceGenerator.Common.Extensions;

namespace Nrk.SourceGenerator.Common.Node;

public interface IGenerationNode
{
    StringBuilder Generate(StringBuilder stringBuilder, int indent = 0);
}

public interface IGenerationBlock
{
    IList<IGenerationNode> Children { get; }
    
    bool WithBlock { get; }
}

public struct LineGenerationNode(string line = "", bool newline = true) : IGenerationNode
{
    private string _line = line;

    public bool Newline { get; set; } = newline;

    public StringBuilder Generate(StringBuilder stringBuilder, int indent = 0)
    {
        if (Newline)
            stringBuilder.AppendLine(GenerationNode.Indent.Repeat(indent) + _line);
        else
            stringBuilder.Append(GenerationNode.Indent.Repeat(indent) + _line);
        
        return stringBuilder;
    }
}

public struct BlockGenerationNode(string header = "", bool expanded = true) : IGenerationNode, IGenerationBlock
{
    private readonly List<IGenerationNode> _children = [];
    
    public IList<IGenerationNode> Children => _children;

    public bool WithBlock => true;
    
    public bool ExpandBlock { get; set; } = expanded;
    
    public string Header { get; set; } = header;
    
    public StringBuilder Generate(StringBuilder stringBuilder, int indent = 0)
    {
        if (ExpandBlock)
        {
            stringBuilder.AppendLine(GenerationNode.Indent.Repeat(indent) + Header);
            stringBuilder.AppendLine(GenerationNode.Indent.Repeat(indent) + "{");
        }
        else
            stringBuilder.Append(GenerationNode.Indent.Repeat(indent) + Header + " { ");

        foreach (var generationNode in _children)
        {
            generationNode.Generate(stringBuilder, ExpandBlock ? indent + 1 : 0);
        }
        
        if (ExpandBlock)
            stringBuilder.AppendLine(GenerationNode.Indent.Repeat(indent) + "}");
        else 
            stringBuilder.AppendLine(" }");
        
        return stringBuilder;
    }
}

public readonly struct SpanGenerationNode(IGenerationBlock block) : IGenerationNode
{
    private readonly int _index = block.Children.Count;
    
    public StringBuilder Generate(StringBuilder stringBuilder, int indent = 0)
    {
        if (_index == block.Children.Count - 1) return stringBuilder;
        if (_index == 0) return stringBuilder;
        if (block.Children[_index + 1] is SpanGenerationNode) return stringBuilder;

        return stringBuilder.AppendLine();
    }
}

public readonly struct UsingLineGenerationNode(string @using) : IGenerationNode, IComparable<UsingLineGenerationNode>
{
    public string Using => @using;

    public StringBuilder Generate(StringBuilder stringBuilder, int indent = 0)
        => stringBuilder.AppendLine(GenerationNode.Indent.Repeat(indent) + $"using { @using};");
    
    public int CompareTo(UsingLineGenerationNode other) => StringComparer.Ordinal.Compare(Using, other.Using);
}

public struct FileGenerationNode() : IGenerationNode, IGenerationBlock
{
    private readonly List<LineGenerationNode> _leadingTriviaLines = [];
    private readonly List<UsingLineGenerationNode> _usingNodes = [];
    private readonly List<IGenerationNode> _childrenNodes = [];
    
    private bool _globalNamespace = true;
    private string _targetNamespace = string.Empty;
    
    public IList<IGenerationNode> Children => _childrenNodes;
    
    public List<UsingLineGenerationNode> UsingNodes => _usingNodes;
    
    public bool WithBlock { get; set; } = true;

    public StringBuilder Generate(StringBuilder stringBuilder, int indent = 0)
    {
        if (_leadingTriviaLines.Count > 0)
        {
            foreach (var triviaLine in _leadingTriviaLines)
                triviaLine.Generate(stringBuilder, indent);

            stringBuilder.AppendLine();
        }
        
        foreach (var usingNode in _usingNodes.OrderBy(x => x.Using, StringComparer.Ordinal))
            usingNode.Generate(stringBuilder, indent);

        stringBuilder.AppendLine();
        
        if (WithBlock)
        {
            stringBuilder.Append("namespace");
            stringBuilder.AppendLine(!_globalNamespace && !string.IsNullOrEmpty(_targetNamespace) 
                ? $" {_targetNamespace}" : string.Empty);
            stringBuilder.AppendLine("{");
            
            foreach (var generationNode in _childrenNodes)
                generationNode.Generate(stringBuilder, indent + 1);
            
            stringBuilder.AppendLine("}");
        }
        else
        {
            if (!_globalNamespace && !string.IsNullOrEmpty(_targetNamespace))
                stringBuilder.AppendLine($"namespace {_targetNamespace};");
            stringBuilder.AppendLine();
            
            foreach (var generationNode in _childrenNodes)
                generationNode.Generate(stringBuilder, indent);
        }
        
        return stringBuilder;
    }

    public FileGenerationNode WithGlobalNamespace()
    {
        _globalNamespace = true;
        _targetNamespace = string.Empty;
        return this;
    }

    public FileGenerationNode WithNamespace(string namespaceName)
    {
        _globalNamespace = false;
        _targetNamespace = namespaceName;
        return this;
    }
    
    public FileGenerationNode WithFileNamespace()
    {
        WithBlock = false;
        return this;
    }

    public FileGenerationNode WithBlockNamespace()
    {
        WithBlock = true;
        return this;
    }

    public FileGenerationNode AddLeadingTrivia(params string[] lines)
    {
        foreach (var line in lines)
            _leadingTriviaLines.Add(new LineGenerationNode(line));
        
        return this;
    }

    public FileGenerationNode AddGeneratedNotes(bool enableDefaultNotes = true, params string[] lines)
    {
        if (enableDefaultNotes)
        {
            string[] defaultNotes =
            [
                "// <auto-generated/>",
                "// This code was generated by a tool.",
                "// Changes to this file may cause incorrect behavior and will be lost if",
                "// the code is regenerated.",
                "",
                "#nullable enable"
            ];

            AddLeadingTrivia(defaultNotes);
        }
        
        AddLeadingTrivia(lines);

        return this;
    }
    
    public FileGenerationNode AddUsingLines(params string[] lines)
    {
        foreach (var line in lines)
            _usingNodes.Add(new UsingLineGenerationNode(line));
        
        return this;
    }
}

public static partial class GenerationNode
{
    public static BlockGenerationNode ClassNode(string identifier, params string[] keywords)
    {
        string[] sortedKeywords = [.. ClassKeywords.Where(keywords.Contains)];
        string prefix = sortedKeywords.Length > 0 ? $"{string.Join(" ", sortedKeywords)} " : "";
        return new BlockGenerationNode($"{prefix}class {identifier}");
    }
    
    public static BlockGenerationNode StructNode(string identifier, params string[] keywords)
    {
        string[] sortedKeywords = [.. StructKeywords.Where(keywords.Contains)];
        string prefix = sortedKeywords.Length > 0 ? $"{string.Join(" ", sortedKeywords)} " : "";
        return new BlockGenerationNode($"{prefix}struct {identifier}");
    }

    public static BlockGenerationNode MethodNode(string sign, params string[] keywords)
    {
        string[] sortedKeywords = [.. MethodKeywords.Where(keywords.Contains)];
        string prefix = sortedKeywords.Length > 0 ? $"{string.Join(" ", sortedKeywords)} " : "";
        return new BlockGenerationNode($"{prefix}{sign}");
    }

    public static LineGenerationNode MethodExpressionNode(string sign, string expression, params string[] keywords)
    {
        string[] sortedKeywords = [.. MethodKeywords.Where(keywords.Contains)];
        string prefix = sortedKeywords.Length > 0 ? $"{string.Join(" ", sortedKeywords)} " : "";
        return new LineGenerationNode($"{prefix}{sign} => {expression};");
    }

    public static BlockGenerationNode PropertyNode(string identifier, string type, params string[] keywords)
    {
        string[] sortedKeywords = [.. PropertyKeywords.Where(keywords.Contains)];
        string prefix = sortedKeywords.Length > 0 ? $"{string.Join(" ", sortedKeywords)} " : "";
        return BlockNode($"{prefix}{type} {identifier}", false);
    }

    public static BlockGenerationNode PropertyWithAccessorsNode(
        string identifier,
        string type,
        string getter = "get;",
        string setter = "set;",
        params string[] keywords)
    {
        string[] sortedKeywords = [.. PropertyKeywords.Where(keywords.Contains)];
        string prefix = sortedKeywords.Length > 0 ? $"{string.Join(" ", sortedKeywords)} " : "";

        return BlockNode($"{prefix}{type} {identifier}", false)
            .AddNode(LineNode(string.Join(" ", getter, setter), false));
    }

    public static LineGenerationNode PropertyExpressionNode(
        string identifier,
        string type,
        string expression,
        params string[] keywords)
    {
        string[] sortedKeywords = [.. PropertyKeywords.Where(keywords.Contains)];
        string prefix = sortedKeywords.Length > 0 ? $"{string.Join(" ", sortedKeywords)} " : "";
        return LineNode($"{prefix}{type} {identifier} => {expression};");
    }

    public static LineGenerationNode FieldNode(string identifier, string type, params string[] keywords)
    {
        string[] sortedKeywords = [.. FieldKeywords.Where(keywords.Contains)];
        string prefix = sortedKeywords.Length > 0 ? $"{string.Join(" ", sortedKeywords)} " : "";
        return LineNode($"{prefix}{type} {identifier};");
    }
    
    public static LineGenerationNode FieldExpressionNode(string identifier, string type, string expression, params string[] keywords)
    {
        string[] sortedKeywords = [.. FieldKeywords.Where(keywords.Contains)];
        string prefix = sortedKeywords.Length > 0 ? $"{string.Join(" ", sortedKeywords)} " : "";
        return LineNode($"{prefix}{type} {identifier} = {expression};");
    }

    public static BlockGenerationNode FixedBlockNode(string expression) => BlockNode($"fixed ({expression})");

    public static LineGenerationNode EmptyLineNode() => new("", true);
    
    public static LineGenerationNode LineNode(string line, bool newline = true) => new(line, newline);
    
    public static BlockGenerationNode BlockNode(string header, bool expanded = true) => new(header, expanded);

    public static FileGenerationNode FileNode() => new();
    
    public static TNode AddNode<TNode>(this TNode block, IGenerationNode? node) where TNode : IGenerationBlock
    {
        if (node == null) return block;
        
        block.Children.Add(node);
        return block;
    }

    public static TNode AddNodes<TNode>(this TNode block, params IGenerationNode[] nodes) where TNode : IGenerationBlock
    {
        foreach (var node in nodes)
            block.Children.Add(node);
        
        return block;
    }
    
    public static TNode AddNodes<TNode>(this TNode block, List<IGenerationNode> nodes) where TNode : IGenerationBlock
    {
        foreach (var node in nodes)
            block.Children.Add(node);
        
        return block;
    }
    
    public static TNode AddNodes<TNode>(this TNode block, IEnumerable<IGenerationNode> nodes) where TNode : IGenerationBlock
    {
        foreach (var node in nodes)
            block.Children.Add(node);
        
        return block;
    }

    public static TNode AddLines<TNode>(this TNode block, params string[] lines) where TNode : IGenerationBlock
    {
        foreach (var line in lines)
            block.Children.Add(LineNode(line));
        
        return block;
    }
    
    public static TNode AddEmptyLine<TNode>(this TNode block) where TNode : IGenerationBlock
    {
        block.Children.Add(EmptyLineNode());
        return block;
    }

    public static TNode AddSpanLine<TNode>(this TNode block) where TNode : IGenerationBlock
    {
        block.Children.Add(new SpanGenerationNode(block));
        return block;
    }

    public static string BuildNode(this IGenerationNode node)
    {
        StringBuilder builder = new();
        node.Generate(builder);
        return builder.ToString();
    }
}

public static partial class GenerationNode
{
    public const string Indent = "    ";
    
    private static readonly string[] ClassKeywords =
    [
        "public",
        "internal",
        "protected",
        "private",
        "new",
        "abstract",
        "sealed",
        "static",
        "unsafe",
        "partial"
    ];

    private static readonly string[] StructKeywords =
    [
        "public",
        "internal",
        "protected",
        "private",
        "new",
        "readonly",
        "ref",
        "unsafe",
        "partial"
    ];
    
    private static readonly string[] MethodKeywords =
    [
        "public",
        "protected",
        "internal",
        "private",
        "new",
        "static",
        "virtual",
        "abstract",
        "override",
        "sealed",
        "extern",
        "unsafe",
        "async",
        "partial"
    ];
    
    private static readonly string[] PropertyKeywords =
    [
        "public",
        "protected",
        "internal",
        "private",
        "new",
        "static",
        "virtual",
        "abstract",
        "override",
        "sealed",
        "required",
        "unsafe",
        "partial"
    ];
    
    private static readonly string[] FieldKeywords =
    [
        "public",
        "protected",
        "internal",
        "private",
        "new",
        "static",
        "readonly",
        "volatile",
        "const",
        "unsafe",
        "required"
    ];
}
