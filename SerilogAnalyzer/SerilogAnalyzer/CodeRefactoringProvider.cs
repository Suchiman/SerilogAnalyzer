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
            if (configuration.Enrich.Count == 0 && configuration.EnrichWithProperty.Count == 0 && configuration.MinimumLevel == null && configuration.WriteTo.Count == 0)
            {
                return;
            }

            var action = CodeAction.Create("Show <appSettings> config", c => InsertConfigurationComment(context.Document, firstProperty, root, configuration, GetAppSettingsConfiguration, c));
            context.RegisterRefactoring(action);
        }

        private static LoggerConfiguration GetLoggerConfigurationFromSyntax(CodeRefactoringContext context, SemanticModel semanticModel, List<MemberAccessExpressionSyntax> configurationProperties)
        {
            var configuration = new LoggerConfiguration();
            foreach (var property in configurationProperties)
            {
                var invokedMethod = property.Ancestors().FirstOrDefault() as MemberAccessExpressionSyntax;

                string configAction = property.Name.ToString();
                if (configAction == "MinimumLevel")
                {
                    string value;
                    var logLevel = invokedMethod.Name.ToString();
                    if (logLevel == "Is")
                    {
                        // Ask roslyn what's the constant argument value passed to this method
                        var argument = (invokedMethod?.Parent as InvocationExpressionSyntax)?.ArgumentList?.Arguments.FirstOrDefault();
                        var parameter = RoslynHelper.DetermineParameter(argument, semanticModel, false, context.CancellationToken);
                        var accessExpression = argument?.Expression as MemberAccessExpressionSyntax;
                        var constValue = semanticModel.GetConstantValue(accessExpression, context.CancellationToken);
                        if (!constValue.HasValue)
                        {
                            continue;
                        }

                        // Roslyn returns enum constant values as integers, convert it back to the enum member name
                        var enumMember = parameter.Type.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(x => Convert.ToInt64(x.ConstantValue) == Convert.ToInt64(constValue.Value));
                        value = enumMember.Name;
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
                            if (parameter.Name == "name")
                            {
                                var constValue = semanticModel.GetConstantValue(argument.Expression, context.CancellationToken);
                                if (!constValue.HasValue)
                                {
                                    continue;
                                }
                                key = constValue.Value.ToString();
                            }
                            else if (parameter.Name == "value")
                            {
                                var constValue = semanticModel.GetConstantValue(argument.Expression, context.CancellationToken);
                                if (!constValue.HasValue)
                                {
                                    continue;
                                }
                                value = constValue.Value.ToString();
                            }
                        }

                        if (key != null && value != null)
                        {
                            configuration.EnrichWithProperty[key] = value;
                        }
                    }
                    else
                    {
                        var method = GetExtensibleMethod(semanticModel, invokedMethod, context.CancellationToken);
                        configuration.Enrich.Add(method);
                    }
                }
                else if (configAction == "WriteTo")
                {
                    var method = GetExtensibleMethod(semanticModel, invokedMethod, context.CancellationToken);
                    configuration.WriteTo.Add(method);
                }
            }

            return configuration;
        }

        private static ExtensibleMethod GetExtensibleMethod(SemanticModel semanticModel, MemberAccessExpressionSyntax invokedMethod, CancellationToken cancellationToken)
        {
            var method = new ExtensibleMethod
            {
                AssemblyName = semanticModel.GetSymbolInfo(invokedMethod).Symbol.ContainingAssembly.Name,
                MethodName = invokedMethod.Name.ToString()
            };
            
            var arguments = (invokedMethod?.Parent as InvocationExpressionSyntax)?.ArgumentList?.Arguments ?? default(SeparatedSyntaxList<ArgumentSyntax>);
            foreach (var argument in arguments)
            {
                var parameter = RoslynHelper.DetermineParameter(argument, semanticModel, false, cancellationToken);

                var constValue = semanticModel.GetConstantValue(argument.Expression, cancellationToken);
                if (!constValue.HasValue)
                {
                    continue;
                }

                string value = null;
                if (parameter.Type.TypeKind == TypeKind.Enum)
                {
                    var enumMember = parameter.Type.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(x => Convert.ToInt64(x.ConstantValue) == Convert.ToInt64(constValue.Value));
                    value = enumMember.Name;
                }
                else
                {
                    value = constValue.Value.ToString();
                }

                method.Arguments[parameter.Name] = value;
            }

            return method;
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

            foreach (var usedAssembly in configuration.Enrich.Concat(configuration.WriteTo).Select(x => x.AssemblyName).Distinct())
            {
                if (usedAssembly != "Serilog")
                {
                    AddEntry(configEntries, "serilog:using", usedAssembly);
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
    }
}