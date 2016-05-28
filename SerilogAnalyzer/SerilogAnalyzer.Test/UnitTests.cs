using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using SerilogAnalyzer;
using Serilog;

namespace SerilogAnalyzer.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {
        //No diagnostics expected to show up
        [TestMethod]
        public void TestNoCode()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void TestLocalExceptionInFormatArgs()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using Serilog;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public static void Test()
            {
                ILogger test = null;
                try
                {
                }
                catch (ArgumentException ex)
                {
                    test.Warning(""Hello World"", ex);
                }
            }
        }
    }";

            var expected = new DiagnosticResult
            {
                Id = "SerilogExceptionUsageAnalyzer",
                Message = String.Format("The exception '{0}' should be passed as first argument", "ex"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 22, 49)
                }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using Serilog;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public static void Test()
            {
                ILogger test = null;
                try
                {
                }
                catch (ArgumentException ex)
                {
                    test.Warning(ex, ""Hello World"");
                }
            }
        }
    }";
            VerifyCSharpFix(test, fixtest);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void TestMethodReturningExceptionInFormatArgs()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using Serilog;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public static void Test()
            {
                ILogger test = null;
                try
                {
                }
                catch (ArgumentException ex)
                {
                    test.Warning(""Hello World"", TestMethod(ex));
                }
            }

            public static Exception TestMethod(Exception ex) => ex;
        }
    }";

            var expected = new DiagnosticResult
            {
                Id = "SerilogExceptionUsageAnalyzer",
                Message = String.Format("The exception '{0}' should be passed as first argument", "TestMethod(ex)"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 22, 49)
                }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using Serilog;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public static void Test()
            {
                ILogger test = null;
                try
                {
                }
                catch (ArgumentException ex)
                {
                    test.Warning(TestMethod(ex), ""Hello World"");
                }
            }

            public static Exception TestMethod(Exception ex) => ex;
        }
    }";
            VerifyCSharpFix(test, fixtest);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void TestMissingBraceInTemplate()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using Serilog;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public static void Test()
            {
                ILogger test = null;
                test.Warning(""Hello {Name World"", ""tester"");
            }
        }
    }";

            var expected = new DiagnosticResult
            {
                Id = "SerilogExceptionUsageAnalyzer",
                Message = String.Format("The exception '{0}' should be passed as first argument", "ex"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 22, 49)
                }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void TestCorrectTemplate()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using Serilog;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public static void Test()
            {
                ILogger test = null;
                test.Warning(""Hello {Name} World"", ""tester"");
            }
        }
    }";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void TestTemplateWithErroneousAlignment()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using Serilog;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public static void Test()
            {
                ILogger test = null;
                test.Warning(""Hello {Name,} World"", ""tester"");
            }
        }
    }";

            VerifyCSharpDiagnostic(test);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new SerilogAnalyzerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new SerilogAnalyzerAnalyzer();
        }
    }
}