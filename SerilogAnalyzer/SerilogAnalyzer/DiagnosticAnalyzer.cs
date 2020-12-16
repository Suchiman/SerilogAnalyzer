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
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace SerilogAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SerilogAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string ExceptionDiagnosticId = "Serilog001";
        private static readonly LocalizableString ExceptionTitle = new LocalizableResourceString(nameof(Resources.ExceptionAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString ExceptionMessageFormat = new LocalizableResourceString(nameof(Resources.ExceptionAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString ExceptionDescription = new LocalizableResourceString(nameof(Resources.ExceptionAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private static DiagnosticDescriptor ExceptionRule = new DiagnosticDescriptor(ExceptionDiagnosticId, ExceptionTitle, ExceptionMessageFormat, "CodeQuality", DiagnosticSeverity.Warning, isEnabledByDefault: true, description: ExceptionDescription);

        public const string TemplateDiagnosticId = "Serilog002";
        private static readonly LocalizableString TemplateTitle = new LocalizableResourceString(nameof(Resources.TemplateAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString TemplateMessageFormat = new LocalizableResourceString(nameof(Resources.TemplateAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString TemplateDescription = new LocalizableResourceString(nameof(Resources.TemplateAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private static DiagnosticDescriptor TemplateRule = new DiagnosticDescriptor(TemplateDiagnosticId, TemplateTitle, TemplateMessageFormat, "CodeQuality", DiagnosticSeverity.Error, isEnabledByDefault: true, description: TemplateDescription);

        public const string PropertyBindingDiagnosticId = "Serilog003";
        private static readonly LocalizableString PropertyBindingTitle = new LocalizableResourceString(nameof(Resources.PropertyBindingAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString PropertyBindingMessageFormat = new LocalizableResourceString(nameof(Resources.PropertyBindingAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString PropertyBindingDescription = new LocalizableResourceString(nameof(Resources.PropertyBindingAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private static DiagnosticDescriptor PropertyBindingRule = new DiagnosticDescriptor(PropertyBindingDiagnosticId, PropertyBindingTitle, PropertyBindingMessageFormat, "CodeQuality", DiagnosticSeverity.Error, isEnabledByDefault: true, description: PropertyBindingDescription);

        public const string ConstantMessageTemplateDiagnosticId = "Serilog004";
        private static readonly LocalizableString ConstantMessageTemplateTitle = new LocalizableResourceString(nameof(Resources.ConstantMessageTemplateAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString ConstantMessageTemplateMessageFormat = new LocalizableResourceString(nameof(Resources.ConstantMessageTemplateAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString ConstantMessageTemplateDescription = new LocalizableResourceString(nameof(Resources.ConstantMessageTemplateAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private static DiagnosticDescriptor ConstantMessageTemplateRule = new DiagnosticDescriptor(ConstantMessageTemplateDiagnosticId, ConstantMessageTemplateTitle, ConstantMessageTemplateMessageFormat, "CodeQuality", DiagnosticSeverity.Warning, isEnabledByDefault: true, description: ConstantMessageTemplateDescription);

        public const string UniquePropertyNameDiagnosticId = "Serilog005";
        private static readonly LocalizableString UniquePropertyNameTitle = new LocalizableResourceString(nameof(Resources.UniquePropertyNameAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString UniquePropertyNameMessageFormat = new LocalizableResourceString(nameof(Resources.UniquePropertyNameAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString UniquePropertyNameDescription = new LocalizableResourceString(nameof(Resources.UniquePropertyNameAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private static DiagnosticDescriptor UniquePropertyNameRule = new DiagnosticDescriptor(UniquePropertyNameDiagnosticId, UniquePropertyNameTitle, UniquePropertyNameMessageFormat, "CodeQuality", DiagnosticSeverity.Error, isEnabledByDefault: true, description: UniquePropertyNameDescription);

        public const string PascalPropertyNameDiagnosticId = "Serilog006";
        private static readonly LocalizableString PascalPropertyNameTitle = new LocalizableResourceString(nameof(Resources.PascalPropertyNameAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString PascalPropertyNameMessageFormat = new LocalizableResourceString(nameof(Resources.PascalPropertyNameAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString PascalPropertyNameDescription = new LocalizableResourceString(nameof(Resources.PascalPropertyNameAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private static DiagnosticDescriptor PascalPropertyNameRule = new DiagnosticDescriptor(PascalPropertyNameDiagnosticId, PascalPropertyNameTitle, PascalPropertyNameMessageFormat, "CodeQuality", DiagnosticSeverity.Warning, isEnabledByDefault: true, description: PascalPropertyNameDescription);

        public const string DestructureAnonymousObjectsDiagnosticId = "Serilog007";
        private static readonly LocalizableString DestructureAnonymousObjectsTitle = new LocalizableResourceString(nameof(Resources.DestructureAnonymousObjectsAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString DestructureAnonymousObjectsMessageFormat = new LocalizableResourceString(nameof(Resources.DestructureAnonymousObjectsAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString DestructureAnonymousObjectsDescription = new LocalizableResourceString(nameof(Resources.DestructureAnonymousObjectsAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private static DiagnosticDescriptor DestructureAnonymousObjectsRule = new DiagnosticDescriptor(DestructureAnonymousObjectsDiagnosticId, DestructureAnonymousObjectsTitle, DestructureAnonymousObjectsMessageFormat, "CodeQuality", DiagnosticSeverity.Warning, isEnabledByDefault: true, description: DestructureAnonymousObjectsDescription);

        public const string UseCorrectContextualLoggerDiagnosticId = "Serilog008";
        private static readonly LocalizableString UseCorrectContextualLoggerTitle = new LocalizableResourceString(nameof(Resources.UseCorrectContextualLoggerAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString UseCorrectContextualLoggerMessageFormat = new LocalizableResourceString(nameof(Resources.UseCorrectContextualLoggerAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString UseCorrectContextualLoggerDescription = new LocalizableResourceString(nameof(Resources.UseCorrectContextualLoggerAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private static DiagnosticDescriptor UseCorrectContextualLoggerRule = new DiagnosticDescriptor(UseCorrectContextualLoggerDiagnosticId, UseCorrectContextualLoggerTitle, UseCorrectContextualLoggerMessageFormat, "CodeQuality", DiagnosticSeverity.Warning, isEnabledByDefault: true, description: UseCorrectContextualLoggerDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ExceptionRule, TemplateRule, PropertyBindingRule, ConstantMessageTemplateRule, UniquePropertyNameRule, PascalPropertyNameRule, DestructureAnonymousObjectsRule, UseCorrectContextualLoggerRule);

        protected virtual string ILogger => "Serilog.ILogger";
        protected virtual string ForContext => "ForContext";
        protected virtual string LoggerMethodAttribute => "Serilog.Core.MessageTemplateFormatMethodAttribute";

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            var invocation = context.Node as InvocationExpressionSyntax;
            var info = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
            var method = info.Symbol as IMethodSymbol;
            if (method == null)
            {
                return;
            }

            // is serilog even present in the compilation?
            var messageTemplateAttribute = context.SemanticModel.Compilation.GetTypeByMetadataName(LoggerMethodAttribute);
            if (messageTemplateAttribute == null)
            {
                return;
            }

            // check if ForContext<T> / ForContext(typeof(T)) calls use the containing type as T
            if (method.Name == ForContext && method.ReturnType.ToString() == ILogger)
            {
                CheckForContextCorrectness(ref context, invocation, method);
            }

            // is it a serilog logging method?
            var attributes = method.GetAttributes();
            var attributeData = attributes.FirstOrDefault(x => x.AttributeClass == messageTemplateAttribute);
            if (attributeData == null)
            {
                return;
            }

            string messageTemplateName = attributeData.ConstructorArguments.First().Value as string;

            // check for errors in the MessageTemplate
            var arguments = default(List<SourceArgument>);
            var properties = new List<PropertyToken>();
            var hasErrors = false;
            var literalSpan = default(TextSpan);
            var exactPositions = true;
            var stringText = default(string);
            var invocationArguments = invocation.ArgumentList.Arguments;
            foreach (var argument in invocationArguments)
            {
                var parameter = RoslynHelper.DetermineParameter(argument, context.SemanticModel, true, context.CancellationToken);
                if (parameter.Name == messageTemplateName)
                {
                    string messageTemplate;

                    // is it a simple string literal?
                    if (argument.Expression is LiteralExpressionSyntax literal)
                    {
                        stringText = literal.Token.Text;
                        exactPositions = true;

                        messageTemplate = literal.Token.ValueText;
                    }
                    else
                    {
                        // can we at least get a computed constant value for it?
                        var constantValue = context.SemanticModel.GetConstantValue(argument.Expression, context.CancellationToken);
                        if (!constantValue.HasValue || !(constantValue.Value is string constString))
                        {
                            INamedTypeSymbol StringType() => context.SemanticModel.Compilation.GetTypeByMetadataName("System.String");
                            if (context.SemanticModel.GetSymbolInfo(argument.Expression, context.CancellationToken).Symbol is IFieldSymbol field && field.Name == "Empty" && field.Type == StringType())
                            {
                                constString = "";
                            }
                            else
                            {
                                context.ReportDiagnostic(Diagnostic.Create(ConstantMessageTemplateRule, argument.Expression.GetLocation(), argument.Expression.ToString()));
                                continue;
                            }
                        }

                        // we can't map positions back from the computed string into the real positions
                        exactPositions = false;
                        messageTemplate = constString;
                    }

                    literalSpan = argument.Expression.GetLocation().SourceSpan;

                    var messageTemplateDiagnostics = AnalyzingMessageTemplateParser.Analyze(messageTemplate);
                    foreach (var templateDiagnostic in messageTemplateDiagnostics)
                    {
                        if (templateDiagnostic is PropertyToken property)
                        {
                            properties.Add(property);
                            continue;
                        }

                        if (templateDiagnostic is MessageTemplateDiagnostic diagnostic)
                        {
                            hasErrors = true;
                            ReportDiagnostic(ref context, ref literalSpan, stringText, exactPositions, TemplateRule, diagnostic);
                        }
                    }

                    var messageTemplateArgumentIndex = invocationArguments.IndexOf(argument);
                    arguments = invocationArguments.Skip(messageTemplateArgumentIndex + 1).Select(x =>
                    {
                        var location = x.GetLocation().SourceSpan;
                        return new SourceArgument { Argument = x, StartIndex = location.Start, Length = location.Length };
                    }).ToList();

                    break;
                }
            }

            // do properties match up?
            if (!hasErrors && literalSpan != default(TextSpan) && (arguments.Count > 0 || properties.Count > 0))
            {
                var diagnostics = PropertyBindingAnalyzer.AnalyzeProperties(properties, arguments);
                foreach (var diagnostic in diagnostics)
                {
                    ReportDiagnostic(ref context, ref literalSpan, stringText, exactPositions, PropertyBindingRule, diagnostic);
                }

                // check that all anonymous objects have destructuring hints in the message template
                if (arguments.Count == properties.Count)
                {
                    for (int i = 0; i < arguments.Count; i++)
                    {
                        var argument = arguments[i];
                        var argumentInfo = context.SemanticModel.GetTypeInfo(argument.Argument.Expression, context.CancellationToken);
                        if (argumentInfo.Type?.IsAnonymousType ?? false)
                        {
                            var property = properties[i];
                            if (!property.RawText.StartsWith("{@", StringComparison.Ordinal))
                            {
                                ReportDiagnostic(ref context, ref literalSpan, stringText, exactPositions, DestructureAnonymousObjectsRule, new MessageTemplateDiagnostic(property.StartIndex, property.Length, property.PropertyName));
                            }
                        }
                    }
                }

                // are there duplicate property names?
                var usedNames = new HashSet<string>();
                foreach (var property in properties)
                {
                    if (!property.IsPositional && !usedNames.Add(property.PropertyName))
                    {
                        ReportDiagnostic(ref context, ref literalSpan, stringText, exactPositions, UniquePropertyNameRule, new MessageTemplateDiagnostic(property.StartIndex, property.Length, property.PropertyName));
                    }

                    var firstCharacter = property.PropertyName[0];
                    if (!Char.IsDigit(firstCharacter) && !Char.IsUpper(firstCharacter))
                    {
                        ReportDiagnostic(ref context, ref literalSpan, stringText, exactPositions, PascalPropertyNameRule, new MessageTemplateDiagnostic(property.StartIndex, property.Length, property.PropertyName));
                    }
                }
            }

            // is this an overload where the exception argument is used?
            var exception = context.SemanticModel.Compilation.GetTypeByMetadataName("System.Exception");
            if (HasConventionalExceptionParameter(method))
            {
                return;
            }

            // is there an overload with the exception argument?
            if (!method.ContainingType.GetMembers().OfType<IMethodSymbol>().Any(x => x.Name == method.Name && HasConventionalExceptionParameter(x)))
            {
                return;
            }

            // check wether any of the format arguments is an exception
            foreach (var argument in invocationArguments)
            {
                var arginfo = context.SemanticModel.GetTypeInfo(argument.Expression);
                if (IsException(exception, arginfo.Type))
                {
                    context.ReportDiagnostic(Diagnostic.Create(ExceptionRule, argument.GetLocation(), argument.Expression.ToFullString()));
                }
            }

            // Check if there is an Exception parameter at position 1 (position 2 for static extension method invocations)?
            bool HasConventionalExceptionParameter(IMethodSymbol methodSymbol)
            {
                return methodSymbol.Parameters.FirstOrDefault()?.Type == exception ||
                       methodSymbol.IsExtensionMethod && methodSymbol.Parameters.Skip(1).FirstOrDefault()?.Type == exception;
            }
        }

        private void CheckForContextCorrectness(ref SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation, IMethodSymbol method)
        {
            // is this really a field / property?
            var decl = invocation.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault();
            if (!(decl is PropertyDeclarationSyntax || decl is FieldDeclarationSyntax))
            {
                return;
            }

            ITypeSymbol contextType = null;

            // extract T from ForContext<T>
            if (method.IsGenericMethod && method.TypeArguments.Length == 1)
            {
                contextType = method.TypeArguments[0];
            }
            // or extract T from ForContext(typeof(T))
            else if (method.Parameters.Length == 1 & method.Parameters[0].Type.ToString() == "System.Type")
            {
                if (invocation.ArgumentList.Arguments.FirstOrDefault().Expression is TypeOfExpressionSyntax type && context.SemanticModel.GetTypeInfo(type.Type).Type is ITypeSymbol tsymbol)
                {
                    contextType = tsymbol;
                }
            }

            // if there's no T...
            if (contextType == null)
            {
                return;
            }

            // find the type this field / property is contained in
            var declaringTypeSyntax = invocation.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
            if (declaringTypeSyntax != null && context.SemanticModel.GetDeclaredSymbol(declaringTypeSyntax) is INamedTypeSymbol declaringType && !declaringType.Equals(contextType))
            {
                // if there are multiple field / properties of ILogger, we can't be certain, so do nothing
                if (declaringType.GetMembers().Count(x => (x as IPropertySymbol)?.Type.ToString() == ILogger || (x as IFieldSymbol)?.Type.ToString() == ILogger) > 1)
                {
                    return;
                }

                // get the location of T to report on
                Location location;
                if (method.IsGenericMethod && invocation.Expression is MemberAccessExpressionSyntax member && member.Name is GenericNameSyntax generic)
                {
                    location = generic.TypeArgumentList.Arguments.First().GetLocation();
                }
                else
                {
                    location = (invocation.ArgumentList.Arguments.First().Expression as TypeOfExpressionSyntax).Type.GetLocation();
                }

                // get the name of the logger variable
                string loggerName = null;
                var declaringMember = invocation.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault();
                if (declaringMember is PropertyDeclarationSyntax property)
                {
                    loggerName = property.Identifier.ToString();
                }
                else if (declaringMember is FieldDeclarationSyntax field && field.Declaration.Variables.FirstOrDefault() is VariableDeclaratorSyntax fieldVariable)
                {
                    loggerName = fieldVariable.Identifier.ToString();
                }

                string correctMethod = method.IsGenericMethod ? $"ForContext<{declaringType}>()" : $"ForContext(typeof({declaringType}))";
                string incorrectMethod = method.IsGenericMethod ? $"ForContext<{contextType}>()" : $"ForContext(typeof({contextType}))";

                context.ReportDiagnostic(Diagnostic.Create(UseCorrectContextualLoggerRule, location, loggerName, correctMethod, incorrectMethod));
            }
        }

        private static void ReportDiagnostic(ref SyntaxNodeAnalysisContext context, ref TextSpan literalSpan, string stringText, bool exactPositions, DiagnosticDescriptor rule, MessageTemplateDiagnostic diagnostic)
        {
            TextSpan textSpan;
            if (diagnostic.MustBeRemapped)
            {
                if (!exactPositions)
                {
                    textSpan = literalSpan;
                }
                else
                {
                    int remappedStart = GetPositionInLiteral(stringText, diagnostic.StartIndex);
                    int remappedEnd = GetPositionInLiteral(stringText, diagnostic.StartIndex + diagnostic.Length);
                    textSpan = new TextSpan(literalSpan.Start + remappedStart, remappedEnd - remappedStart);
                }
            }
            else
            {
                textSpan = new TextSpan(diagnostic.StartIndex, diagnostic.Length);
            }
            var sourceLocation = Location.Create(context.Node.SyntaxTree, textSpan);
            context.ReportDiagnostic(Diagnostic.Create(rule, sourceLocation, diagnostic.Diagnostic));
        }

        /// <summary>
        /// Remaps a string position into the position in a string literal
        /// </summary>
        /// <param name="literal">The literal string as string</param>
        /// <param name="unescapedPosition">The position in the non literal string</param>
        /// <returns></returns>
        public static int GetPositionInLiteral(string literal, int unescapedPosition)
        {
            if (literal[0] == '@')
            {
                for (int i = 2; i < literal.Length; i++)
                {
                    char c = literal[i];

                    if (c == '"' && i + 1 < literal.Length && literal[i + 1] == '"')
                    {
                        i++;
                    }
                    unescapedPosition--;

                    if (unescapedPosition == -1)
                    {
                        return i;
                    }
                }
            }
            else
            {
                for (int i = 1; i < literal.Length; i++)
                {
                    char c = literal[i];

                    if (c == '\\' && i + 1 < literal.Length)
                    {
                        c = literal[++i];
                        if (c == 'x' || c == 'u' || c == 'U')
                        {
                            int max = Math.Min((c == 'U' ? 8 : 4) + i + 1, literal.Length);
                            for (i++; i < max; i++)
                            {
                                c = literal[i];
                                if (!IsHexDigit(c))
                                {
                                    break;
                                }
                            }
                            i--;
                        }
                    }
                    unescapedPosition--;

                    if (unescapedPosition == -1)
                    {
                        return i;
                    }
                }
            }

            return unescapedPosition;
        }

        /// <summary>
        /// Returns true if the Unicode character is a hexadecimal digit.
        /// </summary>
        /// <param name="c">The Unicode character.</param>
        /// <returns>true if the character is a hexadecimal digit 0-9, A-F, a-f.</returns>
        internal static bool IsHexDigit(char c)
        {
            return (c >= '0' && c <= '9') ||
                   (c >= 'A' && c <= 'F') ||
                   (c >= 'a' && c <= 'f');
        }

        private static bool IsException(ITypeSymbol exceptionSymbol, ITypeSymbol type)
        {
            for (ITypeSymbol symbol = type; symbol != null; symbol = symbol.BaseType)
            {
                if (symbol == exceptionSymbol)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
