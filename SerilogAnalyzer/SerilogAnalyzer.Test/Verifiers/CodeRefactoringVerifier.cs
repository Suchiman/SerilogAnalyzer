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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestHelper
{
    /// <summary>
    /// Superclass of all Unit tests made for code refactorings.
    /// Contains methods used to verify correctness of code refactorings
    /// </summary>
    public abstract class CodeRefactoringVerifier : DiagnosticVerifier
    {
        /// <summary>
        /// Returns the CodeRefactoring being tested (C#) - to be implemented in non-abstract class
        /// </summary>
        /// <returns>The CodeRefactoringProvider to be used for CSharp code</returns>
        protected virtual CodeRefactoringProvider GetCSharpCodeRefactoringProvider()
        {
            return null;
        }

        /// <summary>
        /// Returns the CodeRefactoring being tested (VB) - to be implemented in non-abstract class
        /// </summary>
        /// <returns>The CodeRefactoringProvider to be used for VisualBasic code</returns>
        protected virtual CodeRefactoringProvider GetBasicCodeRefactoringProvider()
        {
            return null;
        }

        /// <summary>
        /// Called to test a C# CodeRefactoring when applied on the inputted string as a source
        /// </summary>
        /// <param name="oldSource">A class in the form of a string with [| |] selection markings before the CodeRefactoring was applied to it</param>
        /// <param name="newSource">A class in the form of a string after the CodeRefactoring was applied to it</param>
        /// <param name="codeRefactoringTitle">Titel determining which CodeRefactoring to apply if there are multiple</param>
        protected void VerifyCSharpRefactoring(string oldSource, string newSource, string codeRefactoringTitle = null)
        {
            VerifyRefactoring(LanguageNames.CSharp, GetCSharpCodeRefactoringProvider(), oldSource, newSource, codeRefactoringTitle);
        }

        /// <summary>
        /// Called to test a VB CodeRefactoring when applied on the inputted string as a source
        /// </summary>
        /// <param name="oldSource">A class in the form of a string with [| |] selection markings before the CodeRefactoring was applied to it</param>
        /// <param name="newSource">A class in the form of a string after the CodeRefactoring was applied to it</param>
        /// <param name="codeRefactoringTitle">Titel determining which CodeRefactoring to apply if there are multiple</param>
        protected void VerifyBasicRefactoring(string oldSource, string newSource, string codeRefactoringTitle = null)
        {
            VerifyRefactoring(LanguageNames.VisualBasic, GetBasicCodeRefactoringProvider(), oldSource, newSource, codeRefactoringTitle);
        }

        /// <summary>
        /// General verifier for code refactorings.
        /// Creates a Document from the source string and applies the relevant code refactorings to the marked selection.
        /// Then gets the string after the code refactoring is applied and compares it with the expected result.
        /// </summary>
        /// <param name="language">The language the source code is in</param>
        /// <param name="codeRefactoringProvider">The code refactoring to be applied to the code</param>
        /// <param name="oldSource">A class in the form of a string with [| |] selection markings before the CodeRefactoring was applied to it</param>
        /// <param name="newSource">A class in the form of a string after the CodeRefactoring was applied to it</param>
        /// <param name="codeRefactoringTitle">Titel determining which CodeRefactoring to apply if there are multiple</param>
        private void VerifyRefactoring(string language, CodeRefactoringProvider codeRefactoringProvider, string oldSource, string newSource, string codeRefactoringTitle)
        {
            TextSpan span;
            if (!TryGetCodeAndSpanFromMarkup(oldSource, out oldSource, out span))
            {
                throw new ArgumentException("There are no selection markings in the code", nameof(oldSource));
            }

            var document = CreateDocument(oldSource, language);

            var actions = new List<CodeAction>();
            var context = new CodeRefactoringContext(document, span, (a) => actions.Add(a), CancellationToken.None);
            codeRefactoringProvider.ComputeRefactoringsAsync(context).Wait();

            if (actions.Any())
            {
                if (codeRefactoringTitle != null)
                {
                    CodeAction codeAction = actions.FirstOrDefault(x => x.Title == codeRefactoringTitle);
                    if (codeAction != null)
                    {
                        document = ApplyCodeAction(document, codeAction);
                    }
                }
                else
                {
                    document = ApplyCodeAction(document, actions.First());
                }
            }

            //after applying all of the code actions, compare the resulting string to the inputted one
            var actual = GetStringFromDocument(document);
            Assert.AreEqual(newSource, actual);
        }

        /// <summary>
        /// Gets the selection from marked code as a TextSpan and the code without markings
        /// </summary>
        /// <param name="markupCode">Code marked with [| |]</param>
        /// <param name="code">Code without markings</param>
        /// <param name="span">TextSpan representing the marked code</param>
        /// <returns></returns>
        public static bool TryGetCodeAndSpanFromMarkup(string markupCode, out string code, out TextSpan span)
        {
            code = null;
            span = default(TextSpan);

            var builder = new StringBuilder();

            var start = markupCode.IndexOf("[|", StringComparison.Ordinal);
            if (start < 0)
            {
                return false;
            }

            builder.Append(markupCode.Substring(0, start));

            var end = markupCode.IndexOf("|]", StringComparison.Ordinal);
            if (end < 0)
            {
                return false;
            }

            builder.Append(markupCode.Substring(start + 2, end - start - 2));
            builder.Append(markupCode.Substring(end + 2));

            code = builder.ToString();
            span = TextSpan.FromBounds(start, end - 2);

            return true;
        }
    }
}