using System;

namespace SerilogAnalyzer
{
    public static class Extensions
    {
        public static string NullWhenWhitespace(this string source)
        {
            return String.IsNullOrWhiteSpace(source) ? null : source;
        }
    }
}
