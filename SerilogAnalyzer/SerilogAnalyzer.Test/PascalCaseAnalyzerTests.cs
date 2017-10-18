// Copyright 2017 Serilog Analyzer Contributors
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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;

namespace SerilogAnalyzer.Test
{
    [TestClass]
    public class PascalCaseAnalyzerTests : CodeFixVerifier
    {
        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new SerilogAnalyzerPascalCaseCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new SerilogAnalyzerAnalyzer();
        }

        [TestMethod]
        public void TestPascalCaseFixForString()
        {
            string src = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        var foo = ""tester"";
        Log.Verbose(""Hello {tester}"", foo));
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "Serilog006",
                Message = String.Format("Property name '{0}' should be pascal case", "tester"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 9, 28, 8)
                }
            };
            VerifyCSharpDiagnostic(src, expected);

            var fixtest = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        var foo = ""tester"";
        Log.Verbose(""Hello {Tester}"", foo));
    }
}";
            VerifyCSharpFix(src, fixtest);
        }

        [TestMethod]
        public void TestPascalCaseFixForSnakeCaseString()
        {
            string src = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        var foo = ""tester"";
        Log.Verbose(""Hello {tester_name}"", foo));
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "Serilog006",
                Message = String.Format("Property name '{0}' should be pascal case", "tester_name"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 9, 28, 13)
                }
            };
            VerifyCSharpDiagnostic(src, expected);

            var fixtest = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        var foo = ""tester"";
        Log.Verbose(""Hello {TesterName}"", foo));
    }
}";
            VerifyCSharpFix(src, fixtest);
        }

        [TestMethod]
        public void TestPascalCaseFixForStringWithEscapes()
        {
            string src = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        var foo = ""tester"";
        Log.Verbose(""Hello \""{tester}\"""", foo));
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "Serilog006",
                Message = String.Format("Property name '{0}' should be pascal case", "tester"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 9, 30, 9)
                }
            };
            VerifyCSharpDiagnostic(src, expected);

            var fixtest = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        var foo = ""tester"";
        Log.Verbose(""Hello \""{Tester}\"""", foo));
    }
}";
            VerifyCSharpFix(src, fixtest);
        }

        [TestMethod]
        public void TestPascalCaseFixForStringWithVerbatimEscapes()
        {
            string src = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        var foo = ""tester"";
        Log.Verbose(@""Hello """"{tester}"""""", foo));
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "Serilog006",
                Message = String.Format("Property name '{0}' should be pascal case", "tester"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 9, 31, 9)
                }
            };
            VerifyCSharpDiagnostic(src, expected);

            var fixtest = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        var foo = ""tester"";
        Log.Verbose(@""Hello """"{Tester}"""""", foo));
    }
}";
            VerifyCSharpFix(src, fixtest);
        }

        [TestMethod]
        public void TestPascalCaseFixForObject()
        {
            string src = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        var foo = ""tester"";
        Log.Verbose(""Hello {@tester}"", foo));
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "Serilog006",
                Message = $"Property name '{"tester"}' should be pascal case",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 9, 28, 9)
                }
            };
            VerifyCSharpDiagnostic(src, expected);

            var fixtest = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        var foo = ""tester"";
        Log.Verbose(""Hello {@Tester}"", foo));
    }
}";
            VerifyCSharpFix(src, fixtest);
        }

        [TestMethod]
        public void TestPascalCaseFixForStringification()
        {
            string src = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        var foo = ""tester"";
        Log.Verbose(""Hello {$tester}"", foo));
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "Serilog006",
                Message = $"Property name '{"tester"}' should be pascal case",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 9, 28, 9)
                }
            };
            VerifyCSharpDiagnostic(src, expected);

            var fixtest = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        var foo = ""tester"";
        Log.Verbose(""Hello {$Tester}"", foo));
    }
}";
            VerifyCSharpFix(src, fixtest);
        }

        [TestMethod]
        public void TestPascalCaseFixForEscapedString()
        {
            string src = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        var foo = ""tester"";
        Log.Verbose(""Hello {{tester}} you will {foo}"", bar));
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "Serilog006",
                Message = $"Property name '{"tester"}' should be pascal case",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 9, 28, 9)
                }
            };

            // should ignore as the variable is escaped
            VerifyCSharpFix(src, src);
        }
    }
}