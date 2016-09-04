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

using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace SerilogAnalyzer.Test
{
    [TestClass]
    public class RefactoringTests : CodeRefactoringVerifier
    {
        protected override CodeRefactoringProvider GetCSharpCodeRefactoringProvider()
        {
            return new SerilogAnalyzerCodeRefactoringProvider();
        }

        [TestMethod]
        public void TestMinimumLevelTransformation()
        {
            var test = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        ILogger test = [|new LoggerConfiguration()
            .MinimumLevel.Debug()
            .CreateLogger()|];
    }
}";

            var fixtest = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        /*
        <add key=""serilog:minimum-level"" value=""Debug"" />
        */
        ILogger test = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest);
        }

        [TestMethod]
        public void TestMinimumLevelTransformationUsingIs()
        {
            var test = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        ILogger test = [|new LoggerConfiguration()
            .MinimumLevel.Is(Serilog.Events.LogEventLevel.Debug)
            .CreateLogger()|];
    }
}";

            var fixtest = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        /*
        <add key=""serilog:minimum-level"" value=""Debug"" />
        */
        ILogger test = new LoggerConfiguration()
            .MinimumLevel.Is(Serilog.Events.LogEventLevel.Debug)
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest);
        }

        [TestMethod]
        public void TestEnrichWithProperty()
        {
            var test = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        ILogger test = [|new LoggerConfiguration()
            .Enrich.WithProperty(""AppName"", ""test"")
            .CreateLogger()|];
    }
}";

            var fixtest = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        /*
        <add key=""serilog:enrich:with-property:AppName"" value=""test"" />
        */
        ILogger test = new LoggerConfiguration()
            .Enrich.WithProperty(""AppName"", ""test"")
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest);
        }

        [TestMethod]
        public void TestWriteToLiterate()
        {
            var test = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        ILogger test = [|new LoggerConfiguration()
            .WriteTo.LiterateConsole(Serilog.Events.LogEventLevel.Verbose)
            .CreateLogger()|];
    }
}";

            var fixtest = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        /*
        <add key=""serilog:write-to:LiterateConsole.restrictedToMinimumLevel"" value=""Verbose"" />
        <add key=""serilog:using"" value=""Serilog.Sinks.Literate"" />
        */
        ILogger test = new LoggerConfiguration()
            .WriteTo.LiterateConsole(Serilog.Events.LogEventLevel.Verbose)
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest);
        }

        [TestMethod]
        public void TestComplexSample()
        {
            var test = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        ILogger test = [|new LoggerConfiguration()
            .Enrich.WithProperty(""AppName"", ""test"")
            .MinimumLevel.Is(Serilog.Events.LogEventLevel.Debug)
            .WriteTo.LiterateConsole(outputTemplate: ""test"", restrictedToMinimumLevel: (Serilog.Events.LogEventLevel)2)
            .CreateLogger()|];
    }
}";

            var fixtest = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        /*
        <add key=""serilog:minimum-level"" value=""Debug"" />
        <add key=""serilog:enrich:with-property:AppName"" value=""test"" />
        <add key=""serilog:write-to:LiterateConsole.outputTemplate"" value=""test"" />
        <add key=""serilog:write-to:LiterateConsole.restrictedToMinimumLevel"" value=""Information"" />
        <add key=""serilog:using"" value=""Serilog.Sinks.Literate"" />
        */
        ILogger test = new LoggerConfiguration()
            .Enrich.WithProperty(""AppName"", ""test"")
            .MinimumLevel.Is(Serilog.Events.LogEventLevel.Debug)
            .WriteTo.LiterateConsole(outputTemplate: ""test"", restrictedToMinimumLevel: (Serilog.Events.LogEventLevel)2)
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest);
        }
    }
}
