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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SerilogAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConvertToMessageTemplateCodeFixProvider)), Shared]
    public partial class ConvertToMessageTemplateCodeFixProvider : CodeFixProvider
    {
        private const string title = "Convert to MessageTemplate";
        private const string ConversionName = "SerilogAnalyzer-";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(SerilogAnalyzerAnalyzer.ConstantMessageTemplateDiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var declaration = root.FindNode(diagnosticSpan) as ArgumentSyntax;

            if (declaration.Parent.Parent is InvocationExpressionSyntax logger)
            {
                var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken);
                if (declaration.Expression is InvocationExpressionSyntax inv && semanticModel.GetSymbolInfo(inv.Expression).Symbol is IMethodSymbol symbol && symbol.ToString().StartsWith("string.Format(") && inv.ArgumentList?.Arguments.Count > 0)
                {
                    context.RegisterCodeFix(CodeAction.Create(title, c => ConvertStringFormatToMessageTemplateAsync(context.Document, inv, logger, c), title), diagnostic);
                }
                else if (declaration.Expression is InterpolatedStringExpressionSyntax interpolatedString)
                {
                    context.RegisterCodeFix(CodeAction.Create(title, c => ConvertInterpolationToMessageTemplateAsync(context.Document, interpolatedString, logger, c), title), diagnostic);
                }
                else if (declaration.Expression.DescendantNodesAndSelf().OfType<LiteralExpressionSyntax>().Any())
                {
                    context.RegisterCodeFix(CodeAction.Create(title, c => ConvertStringConcatToMessageTemplateAsync(context.Document, declaration.Expression, logger, c), title), diagnostic);
                }
            }
        }

        private static async Task<Document> InlineFormatAndArgumentsIntoLoggerStatementAsync(Document document, ExpressionSyntax originalTemplateExpression, InvocationExpressionSyntax logger, InterpolatedStringExpressionSyntax format, List<ExpressionSyntax> expressions, CancellationToken cancellationToken)
        {
            var loggerArguments = logger.ArgumentList.Arguments;
            var argumentIndex = loggerArguments.IndexOf(x => x.Expression == originalTemplateExpression);

            var sb = new StringBuilder();
            if (format.StringStartToken.ValueText.Contains("@"))
            {
                sb.Append('@');
            }
            sb.Append('"');

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken);

            var usedNames = new HashSet<string>();
            var argumentExpressions = new List<ExpressionSyntax>();

            int indexFromOriginalLoggingArguments = argumentIndex + 1;
            foreach (var child in format.Contents)
            {
                switch (child)
                {
                    case InterpolatedStringTextSyntax text:
                        sb.Append(text.TextToken.ToString());
                        break;
                    case InterpolationSyntax interpolation:
                        string expressionText = interpolation.Expression.ToString();
                        ExpressionSyntax correspondingArgument = null;
                        string name;
                        if (expressionText.StartsWith(ConversionName, StringComparison.Ordinal) && Int32.TryParse(expressionText.Substring(ConversionName.Length), out int index))
                        {
                            correspondingArgument = expressions.ElementAtOrDefault(index);

                            if (correspondingArgument != null)
                            {
                                name = RoslynHelper.GenerateNameForExpression(semanticModel, correspondingArgument, true).NullWhenWhitespace() ?? "Error";
                            }
                            else // in case this string.format is faulty
                            {
                                correspondingArgument = SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
                                name = "Error";
                            }
                        }
                        else
                        {
                            correspondingArgument = loggerArguments.ElementAtOrDefault(indexFromOriginalLoggingArguments++)?.Expression;
                            if (!String.IsNullOrWhiteSpace(expressionText))
                            {
                                name = expressionText;
                            }
                            else if (correspondingArgument != null)
                            {
                                name = RoslynHelper.GenerateNameForExpression(semanticModel, correspondingArgument, true).NullWhenWhitespace() ?? "Error";
                            }
                            else // in case this string.format is faulty
                            {
                                correspondingArgument = SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
                                name = "Error";
                            }
                        }

                        argumentExpressions.Add(correspondingArgument);

                        sb.Append("{");

                        int attempt = 0;
                        string lastAttempt;
                        while (!usedNames.Add(lastAttempt = (attempt == 0 ? name : name + attempt)))
                        {
                            attempt++;
                        }

                        sb.Append(lastAttempt);

                        if (interpolation.AlignmentClause != null)
                            sb.Append(interpolation.AlignmentClause);

                        if (interpolation.FormatClause != null)
                            sb.Append(interpolation.FormatClause);

                        sb.Append("}");
                        break;
                }
            }

            sb.Append('"');
            var messageTemplate = SyntaxFactory.Argument(SyntaxFactory.ParseExpression(sb.ToString()));

            var seperatedSyntax = loggerArguments.Replace(loggerArguments[argumentIndex], messageTemplate);

            // remove any arguments that we've put into argumentExpressions
            if (indexFromOriginalLoggingArguments > argumentIndex + 1)
            {
                for (int i = Math.Min(indexFromOriginalLoggingArguments, seperatedSyntax.Count) - 1; i > argumentIndex; i--)
                {
                    seperatedSyntax = seperatedSyntax.RemoveAt(i);
                }
            }

            seperatedSyntax = seperatedSyntax.InsertRange(argumentIndex + 1, argumentExpressions.Select(x => SyntaxFactory.Argument(x ?? SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))));

            var newLogger = logger.WithArgumentList(SyntaxFactory.ArgumentList(seperatedSyntax)).WithAdditionalAnnotations(Formatter.Annotation);
            return document.WithSyntaxRoot(syntaxRoot.ReplaceNode(logger, newLogger));
        }
    }
}