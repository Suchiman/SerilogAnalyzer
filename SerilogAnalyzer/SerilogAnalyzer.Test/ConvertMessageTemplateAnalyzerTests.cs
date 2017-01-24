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
    public class ConvertMessageTemplateAnalyzerTests : CodeFixVerifier
    {
        [TestMethod]
        public void TestStringFormat()
        {
            string src = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        Log.Verbose(System.String.Format(""Hello {0}"", ""World""));
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "Serilog004",
                Message = String.Format("MessageTemplate argument {0} is not constant", @"System.String.Format(""Hello {0}"", ""World"")"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 8, 21, 42)
                }
            };
            VerifyCSharpDiagnostic(src, expected);

            var fixtest = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        Log.Verbose(""Hello {V}"", ""World"");
    }
}";
            VerifyCSharpFix(src, fixtest);
        }

        [TestMethod]
        public void TestStringFormatMultipleArguments()
        {
            string src = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        Log.Verbose(System.String.Format(""Hello {0} to {1}"", ""Name"", ""World""));
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "Serilog004",
                Message = String.Format("MessageTemplate argument {0} is not constant", @"System.String.Format(""Hello {0} to {1}"", ""Name"", ""World"")"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 8, 21, 57)
                }
            };
            VerifyCSharpDiagnostic(src, expected);

            var fixtest = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        Log.Verbose(""Hello {V} to {V1}"", ""Name"", ""World"");
    }
}";
            VerifyCSharpFix(src, fixtest);
        }

        [TestMethod]
        public void TestUsingStaticStringFormat()
        {
            string src = @"
using static System.String;
using Serilog;

class TypeName
{
    public static void Test()
    {
        Log.Verbose(Format(""Hello {0}"", ""World""));
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "Serilog004",
                Message = String.Format("MessageTemplate argument {0} is not constant", @"Format(""Hello {0}"", ""World"")"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 9, 21, 28)
                }
            };
            VerifyCSharpDiagnostic(src, expected);

            var fixtest = @"
using static System.String;
using Serilog;

class TypeName
{
    public static void Test()
    {
        Log.Verbose(""Hello {V}"", ""World"");
    }
}";
            VerifyCSharpFix(src, fixtest, allowNewCompilerDiagnostics: true);
        }

        [TestMethod]
        public void TestInterpolatedString()
        {
            string src = @"
using static System.String;
using Serilog;

class TypeName
{
    public static void Test()
    {
        Log.Verbose($""Hello {""World""}"");
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "Serilog004",
                Message = String.Format("MessageTemplate argument {0} is not constant", @"$""Hello {""World""}"""),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 9, 21, 18)
                }
            };
            VerifyCSharpDiagnostic(src, expected);

            var fixtest = @"
using static System.String;
using Serilog;

class TypeName
{
    public static void Test()
    {
        Log.Verbose(""Hello {V}"", ""World"");
    }
}";
            VerifyCSharpFix(src, fixtest);
        }

        [TestMethod]
        public void TestBadStringFormat()
        {
            string src = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        Log.Verbose(System.String.Format(""Hello {0} to {1}"", ""World""));
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "Serilog004",
                Message = String.Format("MessageTemplate argument {0} is not constant", @"System.String.Format(""Hello {0} to {1}"", ""World"")"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 8, 21, 49)
                }
            };
            VerifyCSharpDiagnostic(src, expected);

            var fixtest = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        Log.Verbose(""Hello {V} to {Error}"", ""World"", null);
    }
}";
            VerifyCSharpFix(src, fixtest);
        }

        [TestMethod]
        public void TestStringFormatWithAlignment()
        {
            string src = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        Log.Verbose(System.String.Format(""Hello {0,-10}"", ""World""));
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "Serilog004",
                Message = String.Format("MessageTemplate argument {0} is not constant", @"System.String.Format(""Hello {0,-10}"", ""World"")"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 8, 21, 46)
                }
            };
            VerifyCSharpDiagnostic(src, expected);

            var fixtest = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        Log.Verbose(""Hello {V,-10}"", ""World"");
    }
}";
            VerifyCSharpFix(src, fixtest);
        }

        [TestMethod]
        public void TestStringFormatWithFormat()
        {
            string src = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        Log.Verbose(System.String.Format(""That is {0:C}"", 10));
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "Serilog004",
                Message = String.Format("MessageTemplate argument {0} is not constant", @"System.String.Format(""That is {0:C}"", 10)"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 8, 21, 41)
                }
            };
            VerifyCSharpDiagnostic(src, expected);

            var fixtest = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        Log.Verbose(""That is {V:C}"", 10);
    }
}";
            VerifyCSharpFix(src, fixtest);
        }

        [TestMethod]
        public void TestStringFormatWithAlignmentAndFormat()
        {
            string src = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        Log.Verbose(System.String.Format(""That is {0,-10:C}"", 10));
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "Serilog004",
                Message = String.Format("MessageTemplate argument {0} is not constant", @"System.String.Format(""That is {0,-10:C}"", 10)"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 8, 21, 45)
                }
            };
            VerifyCSharpDiagnostic(src, expected);

            var fixtest = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        Log.Verbose(""That is {V,-10:C}"", 10);
    }
}";
            VerifyCSharpFix(src, fixtest);
        }

        [TestMethod]
        public void TestStringFormatWithAdditionalArgs()
        {
            string src = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        Log.Verbose(System.String.Format(""Hello {{Name}} to {0}"", ""World""), ""Name"");
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "Serilog004",
                Message = String.Format("MessageTemplate argument {0} is not constant", @"System.String.Format(""Hello {{Name}} to {0}"", ""World"")"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 8, 21, 54)
                }
            };
            VerifyCSharpDiagnostic(src, expected);

            var fixtest = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        Log.Verbose(""Hello {Name} to {V}"", ""Name"", ""World"");
    }
}";
            VerifyCSharpFix(src, fixtest);
        }

        [TestMethod]
        public void TestInterpolatedStringWithAdditionalArgs()
        {
            string src = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        Log.Verbose($""Hello {{Name}} to {""World""}"", ""Name"");
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "Serilog004",
                Message = String.Format("MessageTemplate argument {0} is not constant", @"$""Hello {{Name}} to {""World""}"""),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 8, 21, 30)
                }
            };
            VerifyCSharpDiagnostic(src, expected);

            var fixtest = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        Log.Verbose(""Hello {Name} to {V}"", ""Name"", ""World"");
    }
}";
            VerifyCSharpFix(src, fixtest);
        }

        [TestMethod]
        public void TestInterpolatedStringWithMissingAdditionalArgs()
        {
            string src = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        Log.Verbose($""Hello {{Name}} to {""World""}"");
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "Serilog004",
                Message = String.Format("MessageTemplate argument {0} is not constant", @"$""Hello {{Name}} to {""World""}"""),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 8, 21, 30)
                }
            };
            VerifyCSharpDiagnostic(src, expected);

            var fixtest = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        Log.Verbose(""Hello {Name} to {V}"", null, ""World"");
    }
}";
            VerifyCSharpFix(src, fixtest);
        }

        [TestMethod]
        public void TestBrokenInterpolatedStringWithMissingAdditionalArgs()
        {
            string src = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        Log.Verbose($""Hello {{Name}} to {}"");
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "Serilog004",
                Message = String.Format("MessageTemplate argument {0} is not constant", @"$""Hello {{Name}} to {}"""),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 8, 21, 23)
                }
            };
            VerifyCSharpDiagnostic(src, expected);

            var fixtest = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        Log.Verbose(""Hello {Name} to {Error}"", null,);
    }
}";
            VerifyCSharpFix(src, fixtest);
        }

        [TestMethod]
        public void TestStringFormatWithMissingAdditionalArgs()
        {
            string src = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        Log.Verbose(System.String.Format(""Hello {{Name}} to {0}"", ""World""));
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "Serilog004",
                Message = String.Format("MessageTemplate argument {0} is not constant", @"System.String.Format(""Hello {{Name}} to {0}"", ""World"")"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 8, 21, 54)
                }
            };
            VerifyCSharpDiagnostic(src, expected);

            var fixtest = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        Log.Verbose(""Hello {Name} to {V}"", null, ""World"");
    }
}";
            VerifyCSharpFix(src, fixtest);
        }

        [TestMethod]
        public void TestStringFormatWithEverythingMissing()
        {
            string src = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        Log.Verbose(System.String.Format(""Hello {{Name}} to {0}""));
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "Serilog004",
                Message = String.Format("MessageTemplate argument {0} is not constant", @"System.String.Format(""Hello {{Name}} to {0}"")"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 8, 21, 45)
                }
            };
            VerifyCSharpDiagnostic(src, expected);

            var fixtest = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        Log.Verbose(""Hello {Name} to {Error}"", null, null);
    }
}";
            VerifyCSharpFix(src, fixtest);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ConvertToMessageTemplateCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new SerilogAnalyzerAnalyzer();
        }
    }
}