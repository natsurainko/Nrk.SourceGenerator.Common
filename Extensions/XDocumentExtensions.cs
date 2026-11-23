using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Nrk.SourceGenerator.Common.Extensions;

public static class XDocumentExtensions
{
    public static XElement? GetChild(this XElement parent, string localName)
        => parent.Elements().FirstOrDefault(e => e.Name.LocalName == localName);

    public static IEnumerable<XElement> GetChildren(this XElement parent, string localName)
        => parent.Elements().Where(e => e.Name.LocalName == localName);
}
