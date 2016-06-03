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
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace SerilogAnalyzer.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {
        [TestMethod]
        public void TestNoCode()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

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

            // Test0.cs(22,49): error Serilog003: Error while binding properties: There is no property that corresponds to this argument
            // Test0.cs(22,49): warning Serilog001: The exception 'ex' should be passed as first argument
            var expected003 = new DiagnosticResult
            {
                Id = "Serilog003",
                Message = String.Format("Error while binding properties: {0}", "There is no property that corresponds to this argument"),
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 22, 49)
                }
            };
            var expected001 = new DiagnosticResult
            {
                Id = "Serilog001",
                Message = String.Format("The exception '{0}' should be passed as first argument", "ex"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 22, 49)
                }
            };

            VerifyCSharpDiagnostic(test, expected003, expected001);

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

            var expected003 = new DiagnosticResult
            {
                Id = "Serilog003",
                Message = String.Format("Error while binding properties: {0}", "There is no property that corresponds to this argument"),
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 22, 49)
                }
            };
            var expected001 = new DiagnosticResult
            {
                Id = "Serilog001",
                Message = String.Format("The exception '{0}' should be passed as first argument", "TestMethod(ex)"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 22, 49)
                }
            };

            VerifyCSharpDiagnostic(test, expected003, expected001);

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

        private string GetTemplateTestSource(string line, params string[] args)
        {
            return $@"
class Program
{{
    static void Main()
    {{
        Serilog.ILogger test = null;
        test.Information(""{line}"", {String.Join(", ", args.Select(x => "\"" + x + "\""))});
    }}
}}";
        }

        [TestMethod]
        public void TestCorrectParameterCount()
        {
            string src = GetTemplateTestSource("{User} did {Action} {Subject}", "tester", "knock over", "a sack of rice");

            VerifyCSharpDiagnostic(src);
        }

        [TestMethod]
        public void TestMoreArgumentsThanNamedProperties()
        {
            string src = GetTemplateTestSource("{User} did {Action}", "tester", "knock over", "a sack of rice");

            var expected = new DiagnosticResult
            {
                Id = "Serilog003",
                Message = String.Format("Error while binding properties: {0}", "There is no named property that corresponds to this argument"),
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 7, 73)
                }
            };
            VerifyCSharpDiagnostic(src, expected);
        }

        [TestMethod]
        public void TestMoreNamedPropertiesThanArguments()
        {
            string src = GetTemplateTestSource("{User} did {Action} {Subject}", "tester", "knock over");

            var expected = new DiagnosticResult
            {
                Id = "Serilog003",
                Message = String.Format("Error while binding properties: {0}", "There is no argument that corresponds to the named property 'Subject'"),
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 7, 47)
                }
            };
            VerifyCSharpDiagnostic(src, expected);
        }

        [TestMethod]
        public void TestBiggerPositionalPropertyThanArguments()
        {
            string src = GetTemplateTestSource("{1}");

            var expected = new DiagnosticResult
            {
                Id = "Serilog003",
                Message = String.Format("Error while binding properties: {0}", "There is no argument that corresponds to the positional property 1"),
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 7, 27)
                }
            };
            VerifyCSharpDiagnostic(src, expected);
        }

        [TestMethod]
        public void TestMoreArgumentsThanPositionalProperties()
        {
            string src = GetTemplateTestSource("{0}", "Mr.", "Tester");

            var expected = new DiagnosticResult
            {
                Id = "Serilog003",
                Message = String.Format("Error while binding properties: {0}", "There is no positional property that corresponds to this argument"),
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 7, 40)
                }
            };
            VerifyCSharpDiagnostic(src, expected);
        }

        [TestMethod]
        public void TestNoPropertiesWithArgument()
        {
            string src = GetTemplateTestSource("");

            var expected = new DiagnosticResult
            {
                Id = "Serilog003",
                Message = String.Format("Error while binding properties: {0}", "There is no property that corresponds to this argument"),
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 7, 30)
                }
            };
            VerifyCSharpDiagnostic(src, expected);
        }

        [TestMethod]
        public void TestPositionalAndNamedMix()
        {
            string src = GetTemplateTestSource("{0} mixed with {Kind} Property", "positional", "named");

            var expected = new DiagnosticResult
            {
                Id = "Serilog003",
                Message = String.Format("Error while binding properties: {0}", "Positional properties are not allowed, when named properties are being used"),
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 7, 27)
                }
            };
            VerifyCSharpDiagnostic(src, expected);
        }

        [TestMethod]
        public void TestNonConstantMessageTemplateWarning()
        {
            string src = @"
    using Serilog;

    class TypeName
    {
        public static void Test()
        {
            var errorMessage = TryToCheckOutOrder();
            Log.Error(errorMessage);
        }

        public static string TryToCheckOutOrder() => ""Something bad happened"";
    }";

            var expected = new DiagnosticResult
            {
                Id = "Serilog004",
                Message = String.Format("MessageTemplate argument {0} is not constant", "errorMessage"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 9, 23)
                }
            };
            VerifyCSharpDiagnostic(src, expected);
        }

        [TestMethod]
        public void TestExactMappingInVerbatimLiteralWithEscapes()
        {
            string testStr = "@\"text \"\"text\"\" text X text\"";
            int remappedLocation = SerilogAnalyzerAnalyzer.GetPositionInLiteral(testStr, GetXPosition(testStr));

            Assert.AreEqual(testStr.IndexOf('X'), remappedLocation);
        }

        [TestMethod]
        public void TestExactMappingInLiteralWithUtf16Escape()
        {
            string testStr = "\"text \\u0000 text X text\"";
            int remappedLocation = SerilogAnalyzerAnalyzer.GetPositionInLiteral(testStr, GetXPosition(testStr));

            Assert.AreEqual(testStr.IndexOf('X'), remappedLocation);
        }

        [TestMethod]
        public void TestExactMappingInLiteralWithUtf16SurrogateEscape()
        {
            string testStr = "\"text \\U00000000 text X text\"";
            int remappedLocation = SerilogAnalyzerAnalyzer.GetPositionInLiteral(testStr, GetXPosition(testStr));

            Assert.AreEqual(testStr.IndexOf('X'), remappedLocation);
        }

        [TestMethod]
        public void TestExactMappingInLiteralWithHexEscape4Chars()
        {
            string testStr = "\"text \\x1111 text X text\"";
            int remappedLocation = SerilogAnalyzerAnalyzer.GetPositionInLiteral(testStr, GetXPosition(testStr));

            Assert.AreEqual(testStr.IndexOf('X'), remappedLocation);
        }

        [TestMethod]
        public void TestExactMappingInLiteralWithHexEscape()
        {
            string testStr = "\"text \\x01 text X text\"";
            int remappedLocation = SerilogAnalyzerAnalyzer.GetPositionInLiteral(testStr, GetXPosition(testStr));

            Assert.AreEqual(testStr.IndexOf('X'), remappedLocation);
        }

        [TestMethod]
        public void TestExactMappingInLiteralWithHexEscapeWithoutSpace()
        {
            string testStr = "\"text \\x1l text X text\"";
            int remappedLocation = SerilogAnalyzerAnalyzer.GetPositionInLiteral(testStr, GetXPosition(testStr));

            Assert.AreEqual(testStr.IndexOf('X'), remappedLocation);
        }

        [TestMethod]
        public void TestExactMappingInLiteralWithTab()
        {
            string testStr = "\"text \\t text X text\"";
            int remappedLocation = SerilogAnalyzerAnalyzer.GetPositionInLiteral(testStr, GetXPosition(testStr));

            Assert.AreEqual(testStr.IndexOf('X'), remappedLocation);
        }

        [TestMethod]
        public void TestExactMappingInLiteralAtTheStartSurrounded()
        {
            string testStr = "\"\\tX\t\"";
            int remappedLocation = SerilogAnalyzerAnalyzer.GetPositionInLiteral(testStr, GetXPosition(testStr));

            Assert.AreEqual(testStr.IndexOf('X'), remappedLocation);
        }

        private static int GetXPosition(string literal)
        {
            var literalExpression = SyntaxFactory.ParseExpression(literal) as LiteralExpressionSyntax;
            return literalExpression.Token.ValueText.IndexOf('X');
        }

        [TestMethod]
        public void TestUniquePropertyNameRule()
        {
            string src = GetTemplateTestSource("{Tester} chats with {Tester}", "tester1", "tester2");

            var expected = new DiagnosticResult
            {
                Id = "Serilog005",
                Message = String.Format("Property name '{0}' is not unique in this MessageTemplate", "Tester"),
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 7, 47)
                }
            };
            VerifyCSharpDiagnostic(src, expected);
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