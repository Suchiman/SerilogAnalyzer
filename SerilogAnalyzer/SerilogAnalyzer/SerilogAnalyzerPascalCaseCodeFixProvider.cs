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
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SerilogAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SerilogAnalyzerCodeFixProvider)), Shared]
    public class SerilogAnalyzerPascalCaseCodeFixProvider : CodeFixProvider
    {
        private const char stringificationPrefix = '$';
        private const char destructuringPrefix = '@';
        private const string title = "Pascal case the property";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(SerilogAnalyzerAnalyzer.PascalPropertyNameDiagnosticId);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var declaration = root.FindNode(diagnosticSpan);

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedSolution: c => this.PascalCaseTheProperties(context.Document, (ArgumentSyntax)declaration, c),
                    equivalenceKey: title),
                diagnostic);
        }

        private async Task<Solution> PascalCaseTheProperties(Document document, ArgumentSyntax node, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var oldToken = node.GetFirstToken();

            var text = oldToken.ValueText;
            var matches = Regex.Matches(text, @"(?<!{){[@|$]?[a-z]\w+?}");
            foreach (Match match in matches)
            {
                var matchValue = match.Value;
                var chars = matchValue.ToCharArray();

                if (chars[1] == destructuringPrefix || chars[1] == stringificationPrefix)
                {
                    chars[2] = Char.ToUpper(chars[2]);
                    text = text.Replace(matchValue, new string(chars));
                }
                else
                {
                    chars[1] = Char.ToUpper(chars[1]);
                    text = text.Replace(matchValue, new string(chars));
                }
            }

            var newToken = SyntaxFactory.Literal(
                SyntaxFactory.TriviaList(),
                "\"" + text + "\"",
                "\"" + text + "\"",
                SyntaxFactory.TriviaList());
            root = root.ReplaceToken(oldToken, newToken);

            document = document.WithSyntaxRoot(root);
            return document.Project.Solution;
        }
    }
}