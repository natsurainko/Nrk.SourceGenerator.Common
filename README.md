# Nrk.SourceGenerator.Common

A small set of reusable helpers for C# source generators. The node-generation API composes generated source from files, blocks, and lines instead of relying on ad-hoc string concatenation.

## Node Generation

`IGenerationNode` represents a piece of generated code. Use `GenerationNode` helpers to create file, class, method, property, field, block, and line nodes. Children can be attached with `AddNode`, `AddNodes`, or `AddLines`, and `BuildNode()` renders the complete node tree.

`FileNode()` can add using directives, generated-file notes, and block-scoped or file-scoped namespaces.

## Example

```csharp
using Nrk.SourceGenerator.Common.Node;
using static Nrk.SourceGenerator.Common.Node.GenerationNode;

var file = FileNode()
    .AddGeneratedNotes()
    .AddUsingLines("System")
    .WithNamespace("Demo")
    .WithFileNamespace()
    .AddNode(
        ClassNode("Greeter", "public", "static")
            .AddNode(
                MethodNode("string SayHello()", "public", "static")
                    .AddNode(
                        LineNode("return \"Hello from generated code!\";"))));

string source = file.BuildNode();