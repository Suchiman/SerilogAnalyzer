using System.Collections.Generic;

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
    }

    class ExtensibleMethod
    {
        public string AssemblyName { get; set; }
        public string MethodName { get; set; }
        public Dictionary<string, string> Arguments { get; set; } = new Dictionary<string, string>();
    }
}
