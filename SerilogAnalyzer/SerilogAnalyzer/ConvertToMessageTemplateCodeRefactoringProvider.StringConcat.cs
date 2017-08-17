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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SerilogAnalyzer
{
    public partial class ConvertToMessageTemplateCodeFixProvider
    {
        private async Task<Document> ConvertStringConcatToMessageTemplateAsync(Document document, ExpressionSyntax stringConcat, InvocationExpressionSyntax logger, CancellationToken cancellationToken)
        {
            GetFormatStringAndExpressionsFromStringConcat(stringConcat, out var format, out var expressions);

            return await InlineFormatAndArgumentsIntoLoggerStatementAsync(document, stringConcat, logger, format, expressions, cancellationToken);
        }

        private static void GetFormatStringAndExpressionsFromStringConcat(ExpressionSyntax stringConcat, out InterpolatedStringExpressionSyntax format, out List<ExpressionSyntax> expressions)
        {
            var concatExpressions = new List<ExpressionSyntax>();
            void FindExpressions(ExpressionSyntax exp)
            {
                switch (exp)
                {
                    case BinaryExpressionSyntax binary when binary.OperatorToken.IsKind(SyntaxKind.PlusToken):
                        FindExpressions(binary.Left);
                        FindExpressions(binary.Right);
                        break;
                    case ParenthesizedExpressionSyntax parens:
                        FindExpressions(parens.Expression);
                        break;
                    case LiteralExpressionSyntax literal:
                        concatExpressions.Add(literal);
                        break;
                    default:
                        concatExpressions.Add(exp.Parent is ParenthesizedExpressionSyntax paren ? paren : exp);
                        break;
                }
            }
            FindExpressions(stringConcat);

            var sb = new StringBuilder("$\"");
            var replacements = new List<string>();
            int argumentPosition = 0;
            foreach (var child in concatExpressions)
            {
                switch (child)
                {
                    case LiteralExpressionSyntax literal:
                        sb.Append(literal.Token.ValueText);
                        break;
                    case ExpressionSyntax exp:

                        sb.Append("{");
                        sb.Append(ConversionName);
                        sb.Append(argumentPosition++);
                        sb.Append("}");

                        break;
                }
            }
            sb.Append("\"");

            format = (InterpolatedStringExpressionSyntax)SyntaxFactory.ParseExpression(sb.ToString());
            expressions = concatExpressions.Where(x => !(x is LiteralExpressionSyntax)).ToList();
        }
    }
}