using System.Text;

namespace Nrk.SourceGenerator.Common.Extensions;

public static class StringExtensions
{
    extension(string value)
    {
        public string Repeat(int count)
        {
            StringBuilder sb = new(value.Length * count);
        
            for (int i = 0; i < count; i++)
                sb.Append(value);
        
            return sb.ToString();
        }

        public string ToFieldName() => "_" + value[0].ToString().ToLowerInvariant() + value.Substring(1);
        
        public string ToVariableName() => value[0].ToString().ToLowerInvariant() + value.Substring(1);
    }
}
