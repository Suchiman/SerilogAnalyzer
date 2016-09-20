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
            VerifyCSharpRefactoring(test, fixtest, "Show <appSettings> config");
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
            VerifyCSharpRefactoring(test, fixtest, "Show <appSettings> config");
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
            VerifyCSharpRefactoring(test, fixtest, "Show <appSettings> config");
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
        <add key=""serilog:using:Literate"" value=""Serilog.Sinks.Literate"" />
        */
        ILogger test = new LoggerConfiguration()
            .WriteTo.LiterateConsole(Serilog.Events.LogEventLevel.Verbose)
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest, "Show <appSettings> config");
        }

        [TestMethod]
        public void TestAuditToFile()
        {
            var test = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        ILogger test = [|new LoggerConfiguration()
            .AuditTo.File(""C:\\Path\\Filename"")
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
        <add key=""serilog:audit-to:File.path"" value=""C:\Path\Filename"" />
        <add key=""serilog:using:File"" value=""Serilog.Sinks.File"" />
        */
        ILogger test = new LoggerConfiguration()
            .AuditTo.File(""C:\\Path\\Filename"")
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest, "Show <appSettings> config");
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
        <add key=""serilog:using:Literate"" value=""Serilog.Sinks.Literate"" />
        */
        ILogger test = new LoggerConfiguration()
            .Enrich.WithProperty(""AppName"", ""test"")
            .MinimumLevel.Is(Serilog.Events.LogEventLevel.Debug)
            .WriteTo.LiterateConsole(outputTemplate: ""test"", restrictedToMinimumLevel: (Serilog.Events.LogEventLevel)2)
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest, "Show <appSettings> config");
        }

        [TestMethod]
        public void TestComplexJsonSample()
        {
            var test = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        ILogger test = [|new LoggerConfiguration()
            .Enrich.WithProperty(""AppName"", ""test"")
            .Enrich.FromLogContext()
            .MinimumLevel.Is(Serilog.Events.LogEventLevel.Debug)
            .WriteTo.LiterateConsole(outputTemplate: ""test"", restrictedToMinimumLevel: (Serilog.Events.LogEventLevel)2)
            .AuditTo.File(""C:\\Path\\Filename"")
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
        ""Serilog"": {
          ""Using"": [""Serilog"", ""Serilog.Sinks.Literate"", ""Serilog.Sinks.File""],
          ""MinimumLevel"": ""Debug"",
          ""WriteTo"": [
            { ""Name"": ""LiterateConsole"", ""Args"": { ""outputTemplate"": ""test"", ""restrictedToMinimumLevel"": ""Information"" } }
          ],
          ""AuditTo"": [
            { ""Name"": ""File"", ""Args"": { ""path"": ""C:\\Path\\Filename"" } }
          ],
          ""Enrich"": [""FromLogContext""],
          ""Properties"": {
            ""AppName"": ""test""
          }
        }
        */
        ILogger test = new LoggerConfiguration()
            .Enrich.WithProperty(""AppName"", ""test"")
            .Enrich.FromLogContext()
            .MinimumLevel.Is(Serilog.Events.LogEventLevel.Debug)
            .WriteTo.LiterateConsole(outputTemplate: ""test"", restrictedToMinimumLevel: (Serilog.Events.LogEventLevel)2)
            .AuditTo.File(""C:\\Path\\Filename"")
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest, "Show appsettings.json config");
        }

        [TestMethod]
        public void TestJsonWithOverrides()
        {
            var test = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        ILogger test = [|new LoggerConfiguration()
            .MinimumLevel.Is(Serilog.Events.LogEventLevel.Debug)
            .MinimumLevel.Override(""Microsoft"", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override(""System"", Serilog.Events.LogEventLevel.Fatal)
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
        ""Serilog"": {
          ""MinimumLevel"": {
            ""Default"": ""Debug"",
            ""Override"": {
              ""System"": ""Fatal"",
              ""Microsoft"": ""Warning""
            }
          }
        }
        */
        ILogger test = new LoggerConfiguration()
            .MinimumLevel.Is(Serilog.Events.LogEventLevel.Debug)
            .MinimumLevel.Override(""Microsoft"", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override(""System"", Serilog.Events.LogEventLevel.Fatal)
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest, "Show appsettings.json config");
        }

        [TestMethod]
        public void TestJsonWithOverridesAndNoDefault()
        {
            var test = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        ILogger test = [|new LoggerConfiguration()
            .MinimumLevel.Override(""Microsoft"", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override(""System"", Serilog.Events.LogEventLevel.Fatal)
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
        ""Serilog"": {
          ""MinimumLevel"": {
            ""Override"": {
              ""System"": ""Fatal"",
              ""Microsoft"": ""Warning""
            }
          }
        }
        */
        ILogger test = new LoggerConfiguration()
            .MinimumLevel.Override(""Microsoft"", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override(""System"", Serilog.Events.LogEventLevel.Fatal)
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest, "Show appsettings.json config");
        }

        [TestMethod]
        public void TestXmlWithOverridesAndNoDefault()
        {
            var test = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        ILogger test = [|new LoggerConfiguration()
            .MinimumLevel.Override(""Microsoft"", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override(""System"", Serilog.Events.LogEventLevel.Fatal)
            .CreateLogger()|];
    }
}";

            var fixtest = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        ILogger test = new LoggerConfiguration()
            .MinimumLevel.Override(""Microsoft"", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override(""System"", Serilog.Events.LogEventLevel.Fatal)
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest, "Show <appSettings> config");
        }

        [TestMethod]
        public void TestNullableNullXml()
        {
            var test = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        ILogger test = [|new LoggerConfiguration()
            .WriteTo.RollingFile(""logfile.txt"" retainedFileCountLimit: null)
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
        <add key=""serilog:write-to:RollingFile.pathFormat"" value=""logfile.txt"" />
        <add key=""serilog:write-to:RollingFile.retainedFileCountLimit"" />
        <add key=""serilog:using:RollingFile"" value=""Serilog.Sinks.RollingFile"" />
        */
        ILogger test = new LoggerConfiguration()
            .WriteTo.RollingFile(""logfile.txt"" retainedFileCountLimit: null)
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest, "Show <appSettings> config");
        }

        [TestMethod]
        public void TestNullableNullJson()
        {
            var test = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        ILogger test = [|new LoggerConfiguration()
            .WriteTo.RollingFile(""logfile.txt"" retainedFileCountLimit: null)
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
        ""Serilog"": {
          ""Using"": [""Serilog.Sinks.RollingFile""],
          ""WriteTo"": [
            { ""Name"": ""RollingFile"", ""Args"": { ""pathFormat"": ""logfile.txt"", ""retainedFileCountLimit"": null } }
          ]
        }
        */
        ILogger test = new LoggerConfiguration()
            .WriteTo.RollingFile(""logfile.txt"" retainedFileCountLimit: null)
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest, "Show appsettings.json config");
        }

        [TestMethod]
        public void TestBadCode()
        {
            var test = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        ILogger test = [|new LoggerConfiguration()
            .WriteTo.RollingFile(""logfile.txt""
            .CreateLogger()|];
    }
}";

            var fixtest = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        ILogger test = new LoggerConfiguration()
            .WriteTo.RollingFile(""logfile.txt""
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest, "Show appsettings.json config");
        }

        [TestMethod]
        public void TestBadCode2()
        {
            var test = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        ILogger test = [|new LoggerConfiguration()
            .WriteTo. (""
            .CreateLogger()|];
    }
}";

            var fixtest = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        ILogger test = new LoggerConfiguration()
            .WriteTo. (""
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest, "Show appsettings.json config");
        }

        [TestMethod]
        public void TestBadCode3()
        {
            var test = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        ILogger test = [|new LoggerConfiguration()
            .MinimumLevel.Is (""
            .CreateLogger()|];
    }
}";

            var fixtest = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        ILogger test = new LoggerConfiguration()
            .MinimumLevel.Is (""
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest, "Show appsettings.json config");
        }

        [TestMethod]
        public void TestInterface()
        {
            var test = @"
using Serilog;
using System;

class TypeName
{
    public static void Test()
    {
        ILogger test = [|new LoggerConfiguration()
            .WriteTo.LiterateConsole(formatProvider: new Stuff<string>())
            .CreateLogger()|];
    }
}

internal class Stuff<T> : IFormatProvider
{
    public object GetFormat(Type formatType)
    {
        return null;
    }
}";

            var fixtest = @"
using Serilog;
using System;

class TypeName
{
    public static void Test()
    {
        /*
        ""Serilog"": {
          ""Using"": [""Serilog.Sinks.Literate""],
          ""WriteTo"": [
            { ""Name"": ""LiterateConsole"", ""Args"": { ""formatProvider"": ""Stuff`1[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], TestProject, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" } }
          ]
        }
        */
        ILogger test = new LoggerConfiguration()
            .WriteTo.LiterateConsole(formatProvider: new Stuff<string>())
            .CreateLogger();
    }
}

internal class Stuff<T> : IFormatProvider
{
    public object GetFormat(Type formatType)
    {
        return null;
    }
}";
            VerifyCSharpRefactoring(test, fixtest, "Show appsettings.json config");
        }
    }
}
