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

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SerilogAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SerilogAnalyzerCodeFixProvider)), Shared]
    public class SerilogAnalyzerCodeFixProvider : CodeFixProvider
    {
        private const string title = "Make exception the first argument";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(SerilogAnalyzerAnalyzer.ExceptionDiagnosticId); }
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

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedSolution: c => MoveExceptionFirstAsync(context.Document, declaration, c),
                    equivalenceKey: title),
                diagnostic);
        }

        private async Task<Solution> MoveExceptionFirstAsync(Document document, ArgumentSyntax argument, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var argumentList = argument.AncestorsAndSelf().OfType<ArgumentListSyntax>().First();

            var newList = argumentList.Arguments.Remove(argument);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var symbolInfo = semanticModel.GetSymbolInfo(argumentList.Parent);

            if (symbolInfo.Symbol is IMethodSymbol methodSymbol && methodSymbol.IsExtensionMethod && methodSymbol.IsStatic)
            {
                // This is a static method invocation of an extension method, so the first parameter is the
                // extended type itself; hence we insert at the second position
                newList = newList.Insert(1, argument);
            }
            else
            {
                newList = newList.Insert(0, argument);
            }

            root = root.ReplaceNode(argumentList, argumentList.WithArguments(newList));
            document = document.WithSyntaxRoot(root);

            return document.Project.Solution;
        }
    }
}