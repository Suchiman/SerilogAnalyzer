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
    public class DestructuringHintTests : CodeFixVerifier
    {
        [TestMethod]
        public void TestNoCode()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void TestMissingDestructuringOnAnonymousObject()
        {
            var test = @"
    using Serilog;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public static void Test()
            {
                Log.Warning(""Hello World {Some}"", new { Meh = 42 });
            }
        }
    }";
            
            var expected007 = new DiagnosticResult
            {
                Id = "Serilog007",
                Message = String.Format("Property '{0}' should use destructuring because the argument is an anonymous object", "Some"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 10, 42, 6)
                }
            };

            VerifyCSharpDiagnostic(test, expected007);

            var fixtest = @"
    using Serilog;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public static void Test()
            {
                Log.Warning(""Hello World {@Some}"", new { Meh = 42 });
            }
        }
    }";
            VerifyCSharpFix(test, fixtest);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new DestructuringHintCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new SerilogAnalyzerAnalyzer();
        }
    }
}