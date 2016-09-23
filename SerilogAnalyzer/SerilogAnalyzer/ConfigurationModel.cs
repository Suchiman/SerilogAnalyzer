using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SerilogAnalyzer
{
    class LoggerConfiguration
    {
        public string MinimumLevel { get; set; }
        public Dictionary<string, string> MinimumLevelOverrides { get; set; } = new Dictionary<string, string>();
        public List<ExtensibleMethod> WriteTo { get; set; } = new List<ExtensibleMethod>();
        public List<ExtensibleMethod> AuditTo { get; set; } = new List<ExtensibleMethod>();
        public List<ExtensibleMethod> Enrich { get; set; } = new List<ExtensibleMethod>();
        public Dictionary<string, string> EnrichWithProperty { get; set; } = new Dictionary<string, string>();

        public List<string> ErrorLog { get; set; } = new List<string>();
        public bool HasParsingErrors => ErrorLog.Count > 0;

        public void AddError(string message, CSharpSyntaxNode syntax)
        {
            ErrorLog.Add($"{FormatLineSpan(syntax.GetLocation().GetMappedLineSpan())}: `{syntax}` -> {message}");
        }

        public void AddNonConstantError(CSharpSyntaxNode syntax)
        {
            ErrorLog.Add($"{FormatLineSpan(syntax.GetLocation().GetMappedLineSpan())}: `{syntax}` -> Can't statically determine value of expression");
        }

        private static string FormatLineSpan(FileLinePositionSpan span)
        {
            return $"{span.Path}: ({span.StartLinePosition.Line + 1},{span.StartLinePosition.Character})-({span.EndLinePosition.Line + 1},{span.EndLinePosition.Character})";
        }
    }

    class ExtensibleMethod
    {
        public string AssemblyName { get; set; }
        public string MethodName { get; set; }
        public Dictionary<string, string> Arguments { get; set; } = new Dictionary<string, string>();
    }
}
