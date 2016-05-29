// Copyright 2016 Robin Sue
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

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
                Id = "Serilog001",
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
                Id = "Serilog001",
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
            string src = GetTemplateTestSource("Hello {Name:$} to the World");

            var expected = new DiagnosticResult
            {
                Id = "Serilog002",
                Message = String.Format("Error while parsing MessageTemplate: {0}", "Found invalid character '$' in property format"),
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 7, 39)
                }
            };

            VerifyCSharpDiagnostic(src, expected);
        }

        [TestMethod]
        public void TestTemplateWithInvalidFormat()
        {
            string src = GetTemplateTestSource("Hello {Name:$} to the World");

            var expected = new DiagnosticResult
            {
                Id = "Serilog002",
                Message = String.Format("Error while parsing MessageTemplate: {0}", "Found invalid character '$' in property format"),
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 7, 39)
                }
            };

            VerifyCSharpDiagnostic(src, expected);
        }

        [TestMethod]
        public void TestTemplateWithInvalidAlignment()
        {
            string src = GetTemplateTestSource("Hello {Name,b} to the World");

            var expected = new DiagnosticResult
            {
                Id = "Serilog002",
                Message = String.Format("Error while parsing MessageTemplate: {0}", "Found invalid character 'b' in property alignment"),
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 7, 39)
                }
            };

            VerifyCSharpDiagnostic(src, expected);
        }

        [TestMethod]
        public void TestTemplateWithAlignmentAndInvalidFormat()
        {
            string src = GetTemplateTestSource("Hello {Name,1:$} to the World");

            var expected = new DiagnosticResult
            {
                Id = "Serilog002",
                Message = String.Format("Error while parsing MessageTemplate: {0}", "Found invalid character '$' in property format"),
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 7, 41)
                }
            };

            VerifyCSharpDiagnostic(src, expected);
        }

        [TestMethod]
        public void TestTemplateWithMissingAlignment()
        {
            string src = GetTemplateTestSource("Hello {Name,} to the World");

            var expected = new DiagnosticResult
            {
                Id = "Serilog002",
                Message = String.Format("Error while parsing MessageTemplate: {0}", "Found alignment specifier without alignment"),
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 7, 38)
                }
            };

            VerifyCSharpDiagnostic(src, expected);
        }

        [TestMethod]
        public void TestTemplateWithZeroAlignment()
        {
            string src = GetTemplateTestSource("Hello {Name,0} to the World");

            var expected = new DiagnosticResult
            {
                Id = "Serilog002",
                Message = String.Format("Error while parsing MessageTemplate: {0}", "Found zero size alignment"),
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 7, 39)
                }
            };

            VerifyCSharpDiagnostic(src, expected);
        }

        [TestMethod]
        public void TestTemplateWithInvalidNegativeAlignment()
        {
            string src = GetTemplateTestSource("Hello {Name,1-} to the World");

            var expected = new DiagnosticResult
            {
                Id = "Serilog002",
                Message = String.Format("Error while parsing MessageTemplate: {0}", "'-' character must be the first in alignment"),
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 7, 40)
                }
            };

            VerifyCSharpDiagnostic(src, expected);
        }

        [TestMethod]
        public void TestTemplateWithUnclosedBrace()
        {
            string src = GetTemplateTestSource("Hello {Name to the World");

            var expected = new DiagnosticResult
            {
                Id = "Serilog002",
                Message = String.Format("Error while parsing MessageTemplate: {0}", "Encountered end of messageTemplate while parsing property"),
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 7, 33)
                }
            };

            VerifyCSharpDiagnostic(src, expected);
        }

        [TestMethod]
        public void TestTemplateWithoutName()
        {
            string src = GetTemplateTestSource("Hello {} the World");

            var expected = new DiagnosticResult
            {
                Id = "Serilog002",
                Message = String.Format("Error while parsing MessageTemplate: {0}", "Found property without name"),
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 7, 33)
                }
            };

            VerifyCSharpDiagnostic(src, expected);
        }

        [TestMethod]
        public void TestTemplateWithInvalidName()
        {
            string src = GetTemplateTestSource("Hello {§} the World");

            var expected = new DiagnosticResult
            {
                Id = "Serilog002",
                Message = String.Format("Error while parsing MessageTemplate: {0}", "Found invalid character '§' in property"),
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 7, 34)
                }
            };

            VerifyCSharpDiagnostic(src, expected);
        }

        [TestMethod]
        public void TestTemplateWithDestructuringButMissingName()
        {
            string src = GetTemplateTestSource("Hello {@} the World");

            var expected = new DiagnosticResult
            {
                Id = "Serilog002",
                Message = String.Format("Error while parsing MessageTemplate: {0}", "Found property with destructuring hint but without name"),
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 7, 33)
                }
            };

            VerifyCSharpDiagnostic(src, expected);
        }

        [TestMethod]
        public void TestTemplateWithDestructuringButInvalidName()
        {
            string src = GetTemplateTestSource("Hello {@ } the World");

            var expected = new DiagnosticResult
            {
                Id = "Serilog002",
                Message = String.Format("Error while parsing MessageTemplate: {0}", "Found invalid character ' ' in property name"),
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 7, 35)
                }
            };

            VerifyCSharpDiagnostic(src, expected);
        }

        private string GetTemplateTestSource(string line)
        {
            return $@"
class Program
{{
    static void Main()
    {{
        Serilog.ILogger test = null;
        test.Information(""{line}"", ""tester"");
    }}
}}";
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