using System;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Nrk.SourceGenerator.Common.Extensions;

public static class LocationExtensions
{
    public static Location CreateLocation(string path, SourceText sourceText, XmlException exception) 
        => CreateLocation(path, sourceText, exception.LineNumber, exception.LinePosition);

    public static Location CreateLocation(string path, SourceText sourceText, IXmlLineInfo info)
    {
        return info.HasLineInfo()
            ? CreateLocation(path, sourceText, info.LineNumber, info.LinePosition)
            : CreateLocation(path, sourceText, 0, 0);
    }
    
    private static Location CreateLocation(string path, SourceText sourceText, int lineNumber, int linePosition)
    {
        if (string.IsNullOrEmpty(path)) return Location.None;
        if (lineNumber <= 0 || linePosition <= 0)
        {
            return Location.Create(path, 
                new TextSpan(0, 0),
                sourceText.Lines.GetLinePositionSpan(new TextSpan(0, 0)));
        }
        
        int lineIndex = Math.Min(lineNumber - 1, sourceText.Lines.Count - 1);
        var line = sourceText.Lines[lineIndex];

        int column = Math.Min(linePosition - 1, line.Span.Length);
        int offset = line.Start + column;
        int length = offset < line.End ? 1 : 0;

        TextSpan span = new(offset, length);
        return Location.Create(path, span, sourceText.Lines.GetLinePositionSpan(span));
    }
}
