using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace SerilogAnalyzer
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(SerilogAnalyzerCodeRefactoringProvider)), Shared]
    public class SerilogAnalyzerCodeRefactoringProvider : CodeRefactoringProvider
    {
        private static string[] InterestingProperties = { "Destructure", "Enrich", "Filter", "MinimumLevel", "ReadFrom", "WriteTo" };
        private static string[] LogLevels = { "Debug", "Error", "Fatal", "Information", "Verbose", "Warning" };

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

            // Find the node at the selection.
            var node = root.FindNode(context.Span);

            // Arcane code for finding the starting node in a fluent syntax - waiting to blow up
            var descendants = node.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().LastOrDefault()?.DescendantNodesAndSelf().ToList();
            if (descendants == null)
            {
                return;
            }

            // From all the syntax in the fluent configuration, grab the properties (WriteTo, ...)
            var configurationProperties = descendants.OfType<MemberAccessExpressionSyntax>().Where(x => InterestingProperties.Contains(x.Name.ToString())).ToList();

            var firstProperty = configurationProperties.FirstOrDefault();
            if (firstProperty == null)
            {
                return;
            }

            LoggerConfiguration configuration = GetLoggerConfigurationFromSyntax(context, semanticModel, configurationProperties);
            if (configuration.Enrich.Count == 0 && configuration.EnrichWithProperty.Count == 0 && configuration.MinimumLevel == null && configuration.MinimumLevelOverrides.Count == 0 && configuration.WriteTo.Count == 0)
            {
                return;
            }

            if (configuration.Enrich.Count > 0 || configuration.EnrichWithProperty.Count > 0 || configuration.MinimumLevel != null || configuration.WriteTo.Count > 0)
            {
                context.RegisterRefactoring(CodeAction.Create("Show <appSettings> config", c => InsertConfigurationComment(context.Document, firstProperty, root, configuration, GetAppSettingsConfiguration, c)));
            }

            context.RegisterRefactoring(CodeAction.Create("Show appsettings.json config", c => InsertConfigurationComment(context.Document, firstProperty, root, configuration, GetAppSettingsJsonConfiguration, c)));
        }

        private static LoggerConfiguration GetLoggerConfigurationFromSyntax(CodeRefactoringContext context, SemanticModel semanticModel, List<MemberAccessExpressionSyntax> configurationProperties)
        {
            var configuration = new LoggerConfiguration();
            foreach (var property in configurationProperties)
            {
                var invokedMethod = property.Ancestors().FirstOrDefault() as MemberAccessExpressionSyntax;

                if (String.IsNullOrEmpty(invokedMethod?.Name?.ToString()))
                    continue;

                string configAction = property.Name.ToString();
                if (configAction == "MinimumLevel")
                {
                    string value;
                    var logLevel = invokedMethod.Name.ToString();
                    if (logLevel == "Is")
                    {
                        // Ask roslyn what's the constant argument value passed to this method
                        var argument = (invokedMethod?.Parent as InvocationExpressionSyntax)?.ArgumentList?.Arguments.FirstOrDefault();
                        if (argument == null)
                            continue;

                        var parameter = RoslynHelper.DetermineParameter(argument, semanticModel, false, context.CancellationToken);
                        if (parameter == null)
                            continue;

                        var accessExpression = argument?.Expression as MemberAccessExpressionSyntax;
                        if (accessExpression == null)
                            continue;

                        var constValue = semanticModel.GetConstantValue(accessExpression, context.CancellationToken);
                        if (!constValue.HasValue)
                            continue;

                        long enumIntegralValue;
                        try
                        {
                            enumIntegralValue = Convert.ToInt64(constValue.Value);
                        }
                        catch
                        {
                            continue;
                        }

                        // Roslyn returns enum constant values as integers, convert it back to the enum member name
                        var enumMember = parameter.Type.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(x => Convert.ToInt64(x.ConstantValue) == enumIntegralValue);
                        value = enumMember.Name;
                    }
                    else if (logLevel == "Override")
                    {
                        var arguments = (invokedMethod?.Parent as InvocationExpressionSyntax)?.ArgumentList?.Arguments ?? default(SeparatedSyntaxList<ArgumentSyntax>);

                        string key = null;
                        string level = null;
                        foreach (var argument in arguments)
                        {
                            var parameter = RoslynHelper.DetermineParameter(argument, semanticModel, false, context.CancellationToken);
                            if (parameter == null)
                                continue;

                            if (parameter.Name == "source")
                            {
                                var constValue = semanticModel.GetConstantValue(argument.Expression, context.CancellationToken);
                                if (!constValue.HasValue)
                                    continue;

                                key = constValue.Value?.ToString();
                            }
                            else if (parameter.Name == "minimumLevel")
                            {
                                var constValue = semanticModel.GetConstantValue(argument.Expression, context.CancellationToken);
                                if (!constValue.HasValue)
                                    continue;

                                var enumMember = parameter.Type.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(x => Convert.ToInt64(x.ConstantValue) == Convert.ToInt64(constValue.Value));
                                level = enumMember.Name;
                            }
                        }

                        if (key != null && level != null)
                        {
                            configuration.MinimumLevelOverrides[key] = level;
                        }
                        continue;
                    }
                    else if (LogLevels.Contains(logLevel))
                    {
                        value = logLevel;
                    }
                    else
                    {
                        continue;
                    }

                    configuration.MinimumLevel = value;
                }
                else if (configAction == "Enrich")
                {
                    if (invokedMethod.Name.ToString() == "WithProperty")
                    {
                        var arguments = (invokedMethod?.Parent as InvocationExpressionSyntax)?.ArgumentList?.Arguments ?? default(SeparatedSyntaxList<ArgumentSyntax>);

                        string key = null;
                        string value = null;
                        foreach (var argument in arguments)
                        {
                            var parameter = RoslynHelper.DetermineParameter(argument, semanticModel, false, context.CancellationToken);
                            if (parameter == null)
                                continue;

                            if (parameter.Name == "name")
                            {
                                var constValue = semanticModel.GetConstantValue(argument.Expression, context.CancellationToken);
                                if (!constValue.HasValue)
                                    continue;

                                key = constValue.Value?.ToString();
                            }
                            else if (parameter.Name == "value")
                            {
                                var constValue = semanticModel.GetConstantValue(argument.Expression, context.CancellationToken);
                                if (!constValue.HasValue)
                                    continue;

                                value = constValue.Value?.ToString();
                            }
                        }

                        if (key != null && value != null)
                        {
                            configuration.EnrichWithProperty[key] = value;
                        }
                    }
                    else if (((invokedMethod.Name as GenericNameSyntax)?.Identifier.ToString() ?? invokedMethod.Name.ToString()) == "With")
                    {
                        // configuration cannot express Enrich.With(new SomeEnricher()) or Enrich.With<SomeEnricher>()
                        continue;
                    }
                    else
                    {
                        var method = GetExtensibleMethod(semanticModel, invokedMethod, context.CancellationToken);
                        if (method != null)
                        {
                            configuration.Enrich.Add(method);
                        }
                    }
                }
                else if (configAction == "WriteTo")
                {
                    var method = GetExtensibleMethod(semanticModel, invokedMethod, context.CancellationToken);
                    if (method != null)
                    {
                        configuration.WriteTo.Add(method);
                    }
                }
            }

            return configuration;
        }

        private static ExtensibleMethod GetExtensibleMethod(SemanticModel semanticModel, MemberAccessExpressionSyntax invokedMethod, CancellationToken cancellationToken)
        {
            var method = new ExtensibleMethod
            {
                AssemblyName = semanticModel.GetSymbolInfo(invokedMethod).Symbol?.ContainingAssembly?.Name,
                MethodName = invokedMethod.Name.ToString()
            };

            if (String.IsNullOrEmpty(method.AssemblyName))
                return null;

            var arguments = (invokedMethod?.Parent as InvocationExpressionSyntax)?.ArgumentList?.Arguments ?? default(SeparatedSyntaxList<ArgumentSyntax>);
            foreach (var argument in arguments)
            {
                var parameter = RoslynHelper.DetermineParameter(argument, semanticModel, false, cancellationToken);
                if (parameter == null)
                    continue;

                if (parameter.Type?.TypeKind == TypeKind.Interface)
                {
                    var objectCreation = argument.Expression as ObjectCreationExpressionSyntax;
                    // check if there are explicit arguments which are unsupported
                    if (objectCreation.ArgumentList?.Arguments.Count > 0)
                        continue;

                    var typeInfo = semanticModel.GetTypeInfo(objectCreation).Type as INamedTypeSymbol;
                    if (typeInfo == null)
                        continue;

                    // generate the assembly qualified name for usage with Type.GetType(string)
                    string name = GetAssemblyQualifiedTypeName(typeInfo);
                    method.Arguments[parameter.Name] = name;
                    continue;
                }

                var constValue = semanticModel.GetConstantValue(argument.Expression, cancellationToken);
                if (!constValue.HasValue)
                    continue;

                string value = null;
                if (parameter.Type.TypeKind == TypeKind.Enum)
                {
                    var enumMember = parameter.Type.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(x => Convert.ToInt64(x.ConstantValue) == Convert.ToInt64(constValue.Value));
                    value = enumMember.Name;
                }
                else
                {
                    value = constValue.Value?.ToString();
                }

                method.Arguments[parameter.Name] = value;
            }

            return method;
        }

        private static string GetAssemblyQualifiedTypeName(ITypeSymbol type)
        {
            var display = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
            string name = type.ToDisplayString(display);

            var namedType = type as INamedTypeSymbol;
            if (namedType?.TypeArguments.Length > 0)
            {
                name += "`" + namedType.Arity;
                name += "[" + String.Join(", ", namedType.TypeArguments.Select(x => "[" + GetAssemblyQualifiedTypeName(x) + "]")) + "]";
            }

            name += ", " + type.ContainingAssembly.ToString();

            return name;
        }

        private Task<Document> InsertConfigurationComment(Document document, MemberAccessExpressionSyntax firstProperty, SyntaxNode root, LoggerConfiguration configuration, Func<LoggerConfiguration, string> generateConfig, CancellationToken cancellationToken)
        {
            var trivia = SyntaxFactory.ParseTrailingTrivia(generateConfig(configuration));

            var statement = firstProperty.AncestorsAndSelf().OfType<StatementSyntax>().FirstOrDefault();
            var newStatement = statement.WithLeadingTrivia(statement.GetLeadingTrivia().InsertRange(0, trivia)).WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(statement, newStatement);
            var newDocument = document.WithSyntaxRoot(newRoot);

            return Task.FromResult(newDocument);
        }

        private static string GetAppSettingsConfiguration(LoggerConfiguration configuration)
        {
            var configEntries = new List<XElement>();

            if (configuration.MinimumLevel != null)
            {
                AddEntry(configEntries, "serilog:minimum-level", configuration.MinimumLevel);
            }

            foreach (var kvp in configuration.EnrichWithProperty)
            {
                AddEntry(configEntries, "serilog:enrich:with-property:" + kvp.Key, kvp.Value);
            }

            foreach (var enrichment in configuration.Enrich)
            {
                ConvertExtensibleMethod(configEntries, enrichment, "serilog:enrich");
            }

            foreach (var writeTo in configuration.WriteTo)
            {
                ConvertExtensibleMethod(configEntries, writeTo, "serilog:write-to");
            }

            var usedSuffixes = new HashSet<string>();
            foreach (var usedAssembly in configuration.Enrich.Concat(configuration.WriteTo).Select(x => x.AssemblyName).Distinct())
            {
                if (usedAssembly != "Serilog")
                {
                    var parts = usedAssembly.Split('.');
                    var suffix = parts.Last();
                    if (!usedSuffixes.Add(suffix))
                        suffix = String.Join("", parts);

                    AddEntry(configEntries, $"serilog:using:{suffix}", usedAssembly);
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine("/*");
            foreach (var entry in configEntries)
            {
                sb.AppendLine(entry.ToString());
            }
            sb.AppendLine("*/");
            return sb.ToString();
        }

        private static void ConvertExtensibleMethod(List<XElement> configEntries, ExtensibleMethod enrichment, string prefix)
        {
            string key = $"{prefix}:{enrichment.MethodName}";

            if (!enrichment.Arguments.Any())
            {
                AddEntry(configEntries, key, null);
                return;
            }

            foreach (var argument in enrichment.Arguments)
            {
                AddEntry(configEntries, $"{key}.{argument.Key}", argument.Value);
            }
        }

        private static void AddEntry(List<XElement> configEntries, string key, string value)
        {
            if (value != null)
            {
                configEntries.Add(new XElement("add", new XAttribute("key", key), new XAttribute("value", value)));
            }
            else
            {
                configEntries.Add(new XElement("add", new XAttribute("key", key)));
            }
        }

        private static string GetAppSettingsJsonConfiguration(LoggerConfiguration configuration)
        {
            var sb = new StringBuilder();
            sb.AppendLine("/*");
            sb.AppendLine(@"""Serilog"": {");

            bool needsComma = false;

            // technically it's not correct to abuse roslyn to escape the string literals for us but csharp strings look very much like js string literals so...
            string usings = String.Join(", ", configuration.Enrich.Concat(configuration.WriteTo).Select(x => x.AssemblyName).Distinct().Select(x => SyntaxFactory.Literal(x).ToString()));
            if (!String.IsNullOrEmpty(usings))
            {
                sb.AppendFormat(@"  ""Using"": [{0}]", usings);
                needsComma = true;
            }

            if (configuration.MinimumLevel != null || configuration.MinimumLevelOverrides.Any())
            {
                FinishPreviousLine(sb, ref needsComma);
                if (!configuration.MinimumLevelOverrides.Any())
                {
                    sb.AppendFormat(@"  ""MinimumLevel"": {0}", SyntaxFactory.Literal(configuration.MinimumLevel).ToString());
                }
                else
                {
                    sb.AppendLine(@"  ""MinimumLevel"": {");

                    if (configuration.MinimumLevel != null)
                    {
                        sb.AppendFormat(@"    ""Default"": {0},", SyntaxFactory.Literal(configuration.MinimumLevel).ToString());
                        sb.AppendLine();
                    }

                    sb.AppendLine(@"    ""Override"": {");
                    int remaining = configuration.MinimumLevelOverrides.Count;
                    foreach (var levelOverride in configuration.MinimumLevelOverrides)
                    {
                        sb.AppendFormat("      {0}: {1}", SyntaxFactory.Literal(levelOverride.Key).ToString(), SyntaxFactory.Literal(levelOverride.Value).ToString());

                        if (--remaining > 0)
                        {
                            sb.AppendLine(",");
                        }
                        else
                        {
                            sb.AppendLine();
                        }
                    }
                    sb.AppendLine(@"    }");

                    sb.Append(@"  }");
                }
                needsComma = true;
            }

            if (configuration.WriteTo.Any())
            {
                FinishPreviousLine(sb, ref needsComma);
                WriteMethodCalls(configuration.WriteTo, sb, "WriteTo");

                needsComma = true;
            }

            if (configuration.Enrich.Any())
            {
                FinishPreviousLine(sb, ref needsComma);
                WriteMethodCalls(configuration.Enrich, sb, "Enrich");

                needsComma = true;
            }

            if (configuration.EnrichWithProperty.Any())
            {
                FinishPreviousLine(sb, ref needsComma);
                sb.AppendLine(@"  ""Properties"": {");

                int remaining = configuration.EnrichWithProperty.Count;
                foreach (var property in configuration.EnrichWithProperty)
                {
                    sb.AppendFormat("    {0}: {1}", SyntaxFactory.Literal(property.Key).ToString(), SyntaxFactory.Literal(property.Value).ToString());

                    if (--remaining > 0)
                    {
                        sb.AppendLine(",");
                    }
                    else
                    {
                        sb.AppendLine();
                    }
                }
                sb.Append(@"  }");
                needsComma = true;
            }

            sb.AppendLine();
            sb.AppendLine(@"}");
            sb.AppendLine("*/");
            return sb.ToString();
        }

        private static void WriteMethodCalls(List<ExtensibleMethod> methods, StringBuilder sb, string methodKind)
        {
            sb.AppendFormat(@"  ""{0}"": [", methodKind);

            if (methods.All(x => !x.Arguments.Any()))
            {
                sb.Append(String.Join(", ", methods.Select(x => SyntaxFactory.Literal(x.MethodName).ToString())));
                sb.Append("]");
            }
            else
            {
                sb.AppendLine();
                bool writeToNeedsComma = false;
                foreach (var writeTo in methods)
                {
                    FinishPreviousLine(sb, ref writeToNeedsComma);

                    sb.AppendFormat(@"    {{ ""Name"": {0}", SyntaxFactory.Literal(writeTo.MethodName).ToString());
                    if (writeTo.Arguments.Any())
                    {
                        sb.Append(@", ""Args"": { ");

                        int remaining = writeTo.Arguments.Count;
                        foreach (var argument in writeTo.Arguments)
                        {
                            sb.AppendFormat("{0}: ", SyntaxFactory.Literal(argument.Key).ToString());
                            if (argument.Value == null)
                            {
                                sb.Append("null");
                            }
                            else
                            {
                                sb.Append(SyntaxFactory.Literal(argument.Value).ToString());
                            }
                            if (--remaining > 0)
                            {
                                sb.Append(", ");
                            }
                        }
                        sb.Append(" }");
                    }
                    sb.Append(" }");

                    writeToNeedsComma = true;
                }
                sb.AppendLine();
                sb.Append(@"  ]");
            }
        }

        private static void FinishPreviousLine(StringBuilder sb, ref bool needsComma)
        {
            if (needsComma)
            {
                sb.AppendLine(",");
                needsComma = false;
            }
        }
    }
}