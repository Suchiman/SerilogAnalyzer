// Copyright 2017 Robin Sue
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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;

namespace SerilogAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CorrectLoggerContextCodeFixProvider)), Shared]
    public class CorrectLoggerContextCodeFixProvider : CodeFixProvider
    {
        private const string title = "Use correct context for logger";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(SerilogAnalyzerAnalyzer.UseCorrectContextualLoggerDiagnosticId); }
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

            var declaration = root.FindNode(diagnosticSpan) as TypeSyntax;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedSolution: c => UseCorrectType(context.Document, declaration, c),
                    equivalenceKey: title),
                diagnostic);
        }

        private async Task<Solution> UseCorrectType(Document document, TypeSyntax syntax, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var argumentList = syntax.Ancestors().OfType<TypeDeclarationSyntax>().First();
            var symbol = semanticModel.GetDeclaredSymbol(argumentList, cancellationToken);

            var identifier = SyntaxFactory.ParseName(symbol.ToString()).WithAdditionalAnnotations(Simplifier.Annotation);
            root = root.ReplaceNode(syntax, identifier);
            document = document.WithSyntaxRoot(root);

            return document.Project.Solution;
        }
    }
}