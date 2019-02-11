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
using System.Collections.Generic;
using System.Linq;

namespace TestHelper
{
    /// <summary>
    /// Diagnostic Producer class with extra methods dealing with applying codefixes
    /// All methods are static
    /// </summary>
    public abstract partial class CodeFixVerifier : DiagnosticVerifier
    {
        /// <summary>
        /// Compare two collections of Diagnostics,and return a list of any new diagnostics that appear only in the second collection.
        /// Note: Considers Diagnostics to be the same if they have the same Ids.  In the case of multiple diagnostics with the same Id in a row,
        /// this method may not necessarily return the new one.
        /// </summary>
        /// <param name="diagnostics">The Diagnostics that existed in the code before the CodeFix was applied</param>
        /// <param name="newDiagnostics">The Diagnostics that exist in the code after the CodeFix was applied</param>
        /// <returns>A list of Diagnostics that only surfaced in the code after the CodeFix was applied</returns>
        private static IEnumerable<Diagnostic> GetNewDiagnostics(IEnumerable<Diagnostic> diagnostics, IEnumerable<Diagnostic> newDiagnostics)
        {
            var oldArray = diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();
            var newArray = newDiagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();

            int oldIndex = 0;
            int newIndex = 0;

            while (newIndex < newArray.Length)
            {
                if (oldIndex < oldArray.Length && oldArray[oldIndex].Id == newArray[newIndex].Id)
                {
                    ++oldIndex;
                    ++newIndex;
                }
                else
                {
                    yield return newArray[newIndex++];
                }
            }
        }

        /// <summary>
        /// Get the existing compiler diagnostics on the inputted document.
        /// </summary>
        /// <param name="document">The Document to run the compiler diagnostic analyzers on</param>
        /// <returns>The compiler diagnostics that were found in the code</returns>
        private static IEnumerable<Diagnostic> GetCompilerDiagnostics(Document document)
        {
            return document.GetSemanticModelAsync().Result.GetDiagnostics();
        }
    }
}

