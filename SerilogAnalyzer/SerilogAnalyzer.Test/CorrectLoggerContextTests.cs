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
    public class CorrectLoggerContextTests : CodeFixVerifier
    {
        [TestMethod]
        public void TestNoCode()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void TestCorrectContextGeneric()
        {
            var test = @"
    using Serilog;

    namespace ConsoleApplication1
    {
        class A
        {
            private static readonly ILogger Logger = Logger.ForContext<A>();
        }

        class B {}
    }";
            
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void TestWrongContextGeneric()
        {
            var test = @"
    using Serilog;

    namespace ConsoleApplication1
    {
        class A
        {
            private static readonly ILogger Logger = Logger.ForContext<B>();
        }

        class B {}
    }";
            
            var expected007 = new DiagnosticResult
            {
                Id = "Serilog008",
                Message = String.Format("Logger '{0}' should use {1} instead of {2}", "Logger", "ForContext<ConsoleApplication1.A>()", "ForContext<ConsoleApplication1.B>()"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 8, 72, 1)
                }
            };

            VerifyCSharpDiagnostic(test, expected007);

            var fixtest = @"
    using Serilog;

    namespace ConsoleApplication1
    {
        class A
        {
            private static readonly ILogger Logger = Logger.ForContext<A>();
        }

        class B {}
    }";
            VerifyCSharpFix(test, fixtest);
        }

        [TestMethod]
        public void TestCorrectContextTypeof()
        {
            var test = @"
    using Serilog;

    namespace ConsoleApplication1
    {
        class A
        {
            private static readonly ILogger Logger = Logger.ForContext(typeof(A));
        }

        class B {}
    }";
            
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void TestWrongContextTypeof()
        {
            var test = @"
    using Serilog;

    namespace ConsoleApplication1
    {
        class A
        {
            private static readonly ILogger Logger = Logger.ForContext(typeof(B));
        }

        class B {}
    }";

            var expected007 = new DiagnosticResult
            {
                Id = "Serilog008",
                Message = String.Format("Logger '{0}' should use {1} instead of {2}", "Logger", "ForContext(typeof(ConsoleApplication1.A))", "ForContext(typeof(ConsoleApplication1.B))"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 8, 79, 1)
                }
            };

            VerifyCSharpDiagnostic(test, expected007);

            var fixtest = @"
    using Serilog;

    namespace ConsoleApplication1
    {
        class A
        {
            private static readonly ILogger Logger = Logger.ForContext(typeof(A));
        }

        class B {}
    }";
            VerifyCSharpFix(test, fixtest);
        }

        [TestMethod]
        public void TestDoesntTriggerInMethod()
        {
            var test = @"
    using Serilog;

    namespace ConsoleApplication1
    {
        class A
        {
            void Main()
            {
                ILogger Logger = Logger.ForContext(typeof(B));
            }
        }

        class B {}
    }";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void TestDoesntTriggerOnMultipleLoggers()
        {
            var test = @"
    using Serilog;

    namespace ConsoleApplication1
    {
        class A
        {
            private static readonly ILogger Logger1 = Logger.ForContext(typeof(B));
            private static readonly ILogger Logger2 = Logger.ForContext(typeof(C));
        }

        class B {}
        class C {}
    }";

            VerifyCSharpDiagnostic(test);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new CorrectLoggerContextCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new SerilogAnalyzerAnalyzer();
        }
    }
}