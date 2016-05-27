using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SerilogAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SerilogAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SerilogAnalyzer";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.InvocationExpression);
        }


        private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            var invocation = context.Node as InvocationExpressionSyntax;
            var info = context.SemanticModel.GetSymbolInfo(invocation);
            var method = info.Symbol as IMethodSymbol;

            // is it a method on the ILogger interface?
            var ilogger = context.SemanticModel.Compilation.GetTypeByMetadataName("Serilog.ILogger");
            var log = context.SemanticModel.Compilation.GetTypeByMetadataName("Serilog.Log");
            if (method?.ContainingType != ilogger && method?.ContainingType != log)
            {
                return;
            }

            // is this an overload where the exception argument is used?
            var exception = context.SemanticModel.Compilation.GetTypeByMetadataName("System.Exception");
            if (method.Parameters.First().Type == exception)
            {
                return;
            }

            // check wether any of the format arguments is an exception
            foreach (var argument in invocation.ArgumentList.Arguments)
            {
                var arginfo = context.SemanticModel.GetTypeInfo(argument.Expression);
                if (IsException(exception, arginfo.Type))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, argument.GetLocation(), argument.Expression.ToFullString()));
                }
            }
            //var attributes = method.GetAttributes();
            // Find just those named type symbols with names containing lowercase letters.
            //if (namedTypeSymbol.Name.ToCharArray().Any(char.IsLower))
            //{
            //    // For all such symbols, produce a diagnostic.
            //    var diagnostic = Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);

            //    context.ReportDiagnostic(diagnostic);
            //}
        }

        private static bool IsException(ITypeSymbol exception, ITypeSymbol type)
        {
            for (ITypeSymbol symbol = type; symbol != null; symbol = symbol.BaseType)
            {
                if (symbol == exception)
                    return true;
            }
            return false;
        }
    }
}
