using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Nrk.SourceGenerator.Common.Extensions;

namespace Nrk.SourceGenerator.Common.Helpers;

public class XmlSourceText
{
    public XDocument Document { get; set; } = null!;
    
    public SourceText Source { get; set; } = null!;
    
    public string Path { get; set; } = null!;

    public string Text { get; set; } = null!;

    public static ParseResult Parse(AdditionalText additionalText)
    {
        string path = additionalText.Path;
        SourceText? sourceText = additionalText.GetText();
        
        try
        {
            if (sourceText is null)
                throw new NullReferenceException();
            
            string text = sourceText.ToString();
            if (string.IsNullOrEmpty(text))
                throw new InvalidDataException();
            
            XmlSourceText xmlSourceText = new XmlSourceText
            {
                Source = sourceText,
                Text = text,
                Path = path,
                Document = XDocument.Parse(text, LoadOptions.SetLineInfo)
            };

            return new ParseResult
            {
                Success = true,
                XmlSourceText = xmlSourceText,
                ParseException = null
            };
        }
        catch (Exception e)
        {
            return new ParseResult
            {
                Success = false,
                XmlSourceText = new XmlSourceText
                {
                    Source = sourceText!,
                    Path = path,
                },
                ParseException = e
            };
        }
    }

    public class ParseResult
    {
        public bool Success { get; set; }

        public XmlSourceText XmlSourceText { get; set; } = null!;
    
        public Exception? ParseException { get; set; }

        public Location GetExceptionLocation()
        {
            if (ParseException is XmlException xmlException)
                return LocationExtensions.CreateLocation(XmlSourceText, xmlException);
            
            return Location.Create(XmlSourceText.Path,
                new TextSpan(0, 0),
                new LinePositionSpan(new LinePosition(0, 0), new LinePosition(0, 0)));
        }
    }
}
