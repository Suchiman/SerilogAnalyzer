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
            return new ShowConfigCodeRefactoringProvider();
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
          ""Using"": [""Serilog.Sinks.Literate"", ""Serilog.Sinks.File""],
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
        /*
        <add key=""serilog:minimum-level:override:System"" value=""Fatal"" />
        <add key=""serilog:minimum-level:override:Microsoft"" value=""Warning"" />
        */
        ILogger test = new LoggerConfiguration()
            .MinimumLevel.Override(""Microsoft"", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override(""System"", Serilog.Events.LogEventLevel.Fatal)
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest, "Show <appSettings> config");
        }

        [TestMethod]
        public void TestXmlWithOverrides()
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
            .WriteTo.RollingFile(""logfile.txt"")
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
        <add key=""serilog:minimum-level:override:System"" value=""Fatal"" />
        <add key=""serilog:minimum-level:override:Microsoft"" value=""Warning"" />
        <add key=""serilog:write-to:RollingFile.pathFormat"" value=""logfile.txt"" />
        <add key=""serilog:using:RollingFile"" value=""Serilog.Sinks.RollingFile"" />
        */
        ILogger test = new LoggerConfiguration()
            .MinimumLevel.Override(""Microsoft"", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override(""System"", Serilog.Events.LogEventLevel.Fatal)
            .WriteTo.RollingFile(""logfile.txt"")
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

        [TestMethod]
        public void TestEnrichWithEnricher()
        {
            var test = @"
using Serilog;
using Serilog.Core;
using Serilog.Events;

class Enricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory) { }
}

class TypeName
{
    public static void Test()
    {
        ILogger test = [|new LoggerConfiguration()
            .Enrich.With(new Enricher())
            .CreateLogger()|];
    }
}";

            var fixtest = @"
using Serilog;
using Serilog.Core;
using Serilog.Events;

class Enricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory) { }
}

class TypeName
{
    public static void Test()
    {
        /*
        ""Serilog"": {
          ""Enrich"": [
            { ""Name"": ""With"", ""Args"": { ""enricher"": ""Enricher, TestProject, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" } }
          ]
        }
        */
        ILogger test = new LoggerConfiguration()
            .Enrich.With(new Enricher())
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest, "Show appsettings.json config");
        }

        [TestMethod]
        public void TestEnrichWithGenericEnricherXml()
        {
            var test = @"
using Serilog;
using Serilog.Core;
using Serilog.Events;

class Enricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory) { }
}

class TypeName
{
    public static void Test()
    {
        ILogger test = [|new LoggerConfiguration()
            .Enrich.With<Enricher>()
            .CreateLogger()|];
    }
}";

            var fixtest = @"
using Serilog;
using Serilog.Core;
using Serilog.Events;

class Enricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory) { }
}

class TypeName
{
    public static void Test()
    {
        /*
        <add key=""serilog:enrich:With.enricher"" value=""Enricher, TestProject, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
        */
        ILogger test = new LoggerConfiguration()
            .Enrich.With<Enricher>()
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest, "Show <appSettings> config");
        }

        [TestMethod]
        public void TestEnrichWithGenericEnricherJson()
        {
            var test = @"
using Serilog;
using Serilog.Core;
using Serilog.Events;

class Enricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory) { }
}

class TypeName
{
    public static void Test()
    {
        ILogger test = [|new LoggerConfiguration()
            .Enrich.With<Test>()
            .CreateLogger()|];
    }
}";

            var fixtest = @"
using Serilog;
using Serilog.Core;
using Serilog.Events;

class Enricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory) { }
}

class TypeName
{
    public static void Test()
    {
        /*
        ""Serilog"": {
          ""Enrich"": [
            { ""Name"": ""With"", ""Args"": { ""enricher"": ""Test, TestProject, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" } }
          ]
        }
        */
        ILogger test = new LoggerConfiguration()
            .Enrich.With<Test>()
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest, "Show appsettings.json config");
        }

        [TestMethod]
        public void TestFilterXml()
        {
            var test = @"
using Serilog;
using System;

class TypeName
{
    public static void Test()
    {
        ILogger test = [|new LoggerConfiguration()
            .Filter.ByExcluding(""A = 'A'"")
            .CreateLogger()|];
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
        <add key=""serilog:filter:ByExcluding.expression"" value=""A = 'A'"" />
        */
        ILogger test = new LoggerConfiguration()
            .Filter.ByExcluding(""A = 'A'"")
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest, "Show <appSettings> config");
        }

        [TestMethod]
        public void TestFilterJson()
        {
            var test = @"
using Serilog;
using System;

class TypeName
{
    public static void Test()
    {
        ILogger test = [|new LoggerConfiguration()
            .Filter.ByExcluding(""A = 'A'"")
            .CreateLogger()|];
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
          ""Filter"": [
            { ""Name"": ""ByExcluding"", ""Args"": { ""expression"": ""A = 'A'"" } }
          ]
        }
        */
        ILogger test = new LoggerConfiguration()
            .Filter.ByExcluding(""A = 'A'"")
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest, "Show appsettings.json config");
        }

        [TestMethod]
        public void TestStaticPropertyXml()
        {
            var test = @"
using Serilog;
using System;

class TypeName
{
    public static void Test()
    {
        ILogger test = [|new LoggerConfiguration()
            .WriteTo.Console(theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Literate)
            .CreateLogger()|];
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
        <add key=""serilog:write-to:Console.theme"" value=""Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Literate, Serilog.Sinks.Console, Version=3.1.1.0, Culture=neutral, PublicKeyToken=24c2f752a8e58a10"" />
        <add key=""serilog:using:Console"" value=""Serilog.Sinks.Console"" />
        */
        ILogger test = new LoggerConfiguration()
            .WriteTo.Console(theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Literate)
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest, "Show <appSettings> config");
        }

        [TestMethod]
        public void TestStaticPropertyJson()
        {
            var test = @"
using Serilog;
using System;

class TypeName
{
    public static void Test()
    {
        ILogger test = [|new LoggerConfiguration()
            .WriteTo.Console(theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Literate)
            .CreateLogger()|];
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
          ""Using"": [""Serilog.Sinks.Console""],
          ""WriteTo"": [
            { ""Name"": ""Console"", ""Args"": { ""theme"": ""Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Literate, Serilog.Sinks.Console, Version=3.1.1.0, Culture=neutral, PublicKeyToken=24c2f752a8e58a10"" } }
          ]
        }
        */
        ILogger test = new LoggerConfiguration()
            .WriteTo.Console(theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Literate)
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest, "Show appsettings.json config");
        }

        [TestMethod]
        public void TestMinimumLevelControlledByXml()
        {
            var test = @"
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;

class TypeName
{
    public static void Test()
    {
        var sw = new LoggingLevelSwitch(LogEventLevel.Information);
        ILogger test = [|new LoggerConfiguration()
            .MinimumLevel.ControlledBy(sw)
            .CreateLogger()|];
    }
}";

            var fixtest = @"
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;

class TypeName
{
    public static void Test()
    {
        var sw = new LoggingLevelSwitch(LogEventLevel.Information);
        /*
        <add key=""serilog:level-switch:$sw"" value=""Information"" />
        <add key=""serilog:minimum-level:controlled-by"" value=""$sw"" />
        */
        ILogger test = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(sw)
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest, "Show <appSettings> config");
        }

        [TestMethod]
        public void TestMinimumLevelControlledByJson()
        {
            var test = @"
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;

class TypeName
{
    public static void Test()
    {
        var sw = new LoggingLevelSwitch(LogEventLevel.Information);
        ILogger test = [|new LoggerConfiguration()
            .MinimumLevel.ControlledBy(sw)
            .CreateLogger()|];
    }
}";

            var fixtest = @"
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;

class TypeName
{
    public static void Test()
    {
        var sw = new LoggingLevelSwitch(LogEventLevel.Information);
        /*
        ""Serilog"": {
          ""LevelSwitches"": { ""$sw"": ""Information"" },
          ""MinimumLevel"": {
            ""ControlledBy"": ""$sw""
          }
        }
        */
        ILogger test = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(sw)
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest, "Show appsettings.json config");
        }

        [TestMethod]
        public void TestWriteToConsoleLevelSwitchXml()
        {
            var test = @"
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;

class TypeName
{
    public static void Test()
    {
        var sw = new LoggingLevelSwitch(LogEventLevel.Information);
        ILogger test = [|new LoggerConfiguration()
            .WriteTo.Console(levelSwitch: sw)
            .CreateLogger()|];
    }
}";

            var fixtest = @"
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;

class TypeName
{
    public static void Test()
    {
        var sw = new LoggingLevelSwitch(LogEventLevel.Information);
        /*
        <add key=""serilog:level-switch:$sw"" value=""Information"" />
        <add key=""serilog:write-to:Console.levelSwitch"" value=""$sw"" />
        <add key=""serilog:using:Console"" value=""Serilog.Sinks.Console"" />
        */
        ILogger test = new LoggerConfiguration()
            .WriteTo.Console(levelSwitch: sw)
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest, "Show <appSettings> config");
        }

        [TestMethod]
        public void TestWriteToConsoleLevelSwitchJson()
        {
            var test = @"
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;

class TypeName
{
    public static void Test()
    {
        var sw = new LoggingLevelSwitch(LogEventLevel.Information);
        ILogger test = [|new LoggerConfiguration()
            .WriteTo.Console(levelSwitch: sw)
            .CreateLogger()|];
    }
}";

            var fixtest = @"
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;

class TypeName
{
    public static void Test()
    {
        var sw = new LoggingLevelSwitch(LogEventLevel.Information);
        /*
        ""Serilog"": {
          ""Using"": [""Serilog.Sinks.Console""],
          ""LevelSwitches"": { ""$sw"": ""Information"" },
          ""WriteTo"": [
            { ""Name"": ""Console"", ""Args"": { ""levelSwitch"": ""$sw"" } }
          ]
        }
        */
        ILogger test = new LoggerConfiguration()
            .WriteTo.Console(levelSwitch: sw)
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest, "Show appsettings.json config");
        }

        [TestMethod]
        public void TestDestructureXml()
        {
            var test = @"
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;

public class CustomPolicy : IDestructuringPolicy
{
    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
    {
        result = null;
        return false;
    }
}

class TypeName
{
    public static void Test()
    {
        ILogger test = [|new LoggerConfiguration()
            .Destructure.ToMaximumDepth(maximumDestructuringDepth: 3)
            .Destructure.ToMaximumStringLength(maximumStringLength: 3)
            .Destructure.ToMaximumCollectionCount(maximumCollectionCount: 3)
            .Destructure.AsScalar(typeof(System.Version))
            .Destructure.With(new CustomPolicy())
            .CreateLogger()|];
    }
}";

            var fixtest = @"
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;

public class CustomPolicy : IDestructuringPolicy
{
    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
    {
        result = null;
        return false;
    }
}

class TypeName
{
    public static void Test()
    {
        /*
        <add key=""serilog:destructure:With.policy"" value=""CustomPolicy, TestProject, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
        <add key=""serilog:destructure:AsScalar.scalarType"" value=""System.Version, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" />
        <add key=""serilog:destructure:ToMaximumCollectionCount.maximumCollectionCount"" value=""3"" />
        <add key=""serilog:destructure:ToMaximumStringLength.maximumStringLength"" value=""3"" />
        <add key=""serilog:destructure:ToMaximumDepth.maximumDestructuringDepth"" value=""3"" />
        */
        ILogger test = new LoggerConfiguration()
            .Destructure.ToMaximumDepth(maximumDestructuringDepth: 3)
            .Destructure.ToMaximumStringLength(maximumStringLength: 3)
            .Destructure.ToMaximumCollectionCount(maximumCollectionCount: 3)
            .Destructure.AsScalar(typeof(System.Version))
            .Destructure.With(new CustomPolicy())
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest, "Show <appSettings> config");
        }

        [TestMethod]
        public void TestDestructureJson()
        {
            var test = @"
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;

public class CustomPolicy : IDestructuringPolicy
{
    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
    {
        result = null;
        return false;
    }
}

class TypeName
{
    public static void Test()
    {
        ILogger test = [|new LoggerConfiguration()
            .Destructure.ToMaximumDepth(maximumDestructuringDepth: 3)
            .Destructure.ToMaximumStringLength(maximumStringLength: 3)
            .Destructure.ToMaximumCollectionCount(maximumCollectionCount: 3)
            .Destructure.AsScalar(typeof(System.Version))
            .Destructure.With(new CustomPolicy())
            .CreateLogger()|];
    }
}";

            var fixtest = @"
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;

public class CustomPolicy : IDestructuringPolicy
{
    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
    {
        result = null;
        return false;
    }
}

class TypeName
{
    public static void Test()
    {
        /*
        ""Serilog"": {
          ""Destructure"": [
            { ""Name"": ""With"", ""Args"": { ""policy"": ""CustomPolicy, TestProject, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" } },
            { ""Name"": ""AsScalar"", ""Args"": { ""scalarType"": ""System.Version, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" } },
            { ""Name"": ""ToMaximumCollectionCount"", ""Args"": { ""maximumCollectionCount"": ""3"" } },
            { ""Name"": ""ToMaximumStringLength"", ""Args"": { ""maximumStringLength"": ""3"" } },
            { ""Name"": ""ToMaximumDepth"", ""Args"": { ""maximumDestructuringDepth"": ""3"" } }
          ]
        }
        */
        ILogger test = new LoggerConfiguration()
            .Destructure.ToMaximumDepth(maximumDestructuringDepth: 3)
            .Destructure.ToMaximumStringLength(maximumStringLength: 3)
            .Destructure.ToMaximumCollectionCount(maximumCollectionCount: 3)
            .Destructure.AsScalar(typeof(System.Version))
            .Destructure.With(new CustomPolicy())
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest, "Show appsettings.json config");
        }

        [TestMethod]
        public void TestWriteToRollingFileNonConstant()
        {
            var test = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        ILogger test = [|new LoggerConfiguration()
            .WriteTo.RollingFile(GetPath())
            .CreateLogger()|];
    }

    public static string GetPath() => ""test"";
}";

            var fixtest = @"
using Serilog;

class TypeName
{
    public static void Test()
    {
        /*
        Errors:
        Test0.cs: (9,33)-(9,42): `GetPath()` -> Can't statically determine value of expression

        <add key=""serilog:write-to:RollingFile.pathFormat"" value=""?"" />
        <add key=""serilog:using:RollingFile"" value=""Serilog.Sinks.RollingFile"" />
        */
        ILogger test = new LoggerConfiguration()
            .WriteTo.RollingFile(GetPath())
            .CreateLogger();
    }

    public static string GetPath() => ""test"";
}";
            VerifyCSharpRefactoring(test, fixtest, "Show <appSettings> config");
        }

        [TestMethod]
        public void TestInterfaceNotInlineError()
        {
            var test = @"
using Serilog;
using System;

class TypeName
{
    public static void Test()
    {
        var formatProvider = new Stuff<string>();
        ILogger test = [|new LoggerConfiguration()
            .WriteTo.LiterateConsole(formatProvider: formatProvider)
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
        var formatProvider = new Stuff<string>();
        /*
        Errors:
        Test0.cs: (11,53)-(11,67): `formatProvider` -> I can only infer types from `new T()` expressions

        ""Serilog"": {
          ""Using"": [""Serilog.Sinks.Literate""],
          ""WriteTo"": [
            { ""Name"": ""LiterateConsole"", ""Args"": { ""formatProvider"": ""?"" } }
          ]
        }
        */
        ILogger test = new LoggerConfiguration()
            .WriteTo.LiterateConsole(formatProvider: formatProvider)
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

        [TestMethod]
        public void TestSimiliarNames()
        {
            var test = @"
using Serilog;
using System;

class TypeName
{
    public static void Test()
    {
        int[] testData = { 1, 2, 3 };
        var test = [|testData.Select(x => { var model = new TypeName(); model.ReadFrom(x); return model; })
            .ToList()|];
    }

    public void ReadFrom(int i)
    {

    }
}";

            var fixtest = @"
using Serilog;
using System;

class TypeName
{
    public static void Test()
    {
        int[] testData = { 1, 2, 3 };
        var test = testData.Select(x => { var model = new TypeName(); model.ReadFrom(x); return model; })
            .ToList();
    }

    public void ReadFrom(int i)
    {

    }
}";
            VerifyCSharpRefactoring(test, fixtest);
        }

        [TestMethod]
        public void TestWriteToSinkXml()
        {
            var test = @"
using Serilog;
using Serilog.Core;
using Serilog.Events;

class TestSink : ILogEventSink
{
    public void Emit(LogEvent logEvent)
    {
        throw new System.NotImplementedException();
    }
}

class TypeName
{
    public static void Test()
    {
        ILogger test = [|new LoggerConfiguration()
            .WriteTo.Sink(new TestSink())
            .CreateLogger()|];
    }
}";

            var fixtest = @"
using Serilog;
using Serilog.Core;
using Serilog.Events;

class TestSink : ILogEventSink
{
    public void Emit(LogEvent logEvent)
    {
        throw new System.NotImplementedException();
    }
}

class TypeName
{
    public static void Test()
    {
        /*
        <add key=""serilog:write-to:Sink.sink"" value=""TestSink, TestProject, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
        */
        ILogger test = new LoggerConfiguration()
            .WriteTo.Sink(new TestSink())
            .CreateLogger();
    }
}";
            VerifyCSharpRefactoring(test, fixtest, "Show <appSettings> config");
        }
    }
}
