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
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(ShowConfigCodeRefactoringProvider)), Shared]
    public class ShowConfigCodeRefactoringProvider : CodeRefactoringProvider
    {
        private static string[] InterestingProperties = { "Destructure", "Enrich", "Filter", "MinimumLevel", "ReadFrom", "WriteTo", "AuditTo" };
        private static string[] LogLevels = { "Debug", "Error", "Fatal", "Information", "Verbose", "Warning" };
        private const string NotAConstantReplacementValue = "?";

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
            var configurationProperties = descendants.OfType<MemberAccessExpressionSyntax>().Where(x => InterestingProperties.Contains(x.Name.ToString())).Reverse().ToList();

            var firstProperty = configurationProperties.FirstOrDefault();
            if (firstProperty == null)
            {
                return;
            }

            LoggerConfiguration configuration = GetLoggerConfigurationFromSyntax(context, semanticModel, configurationProperties);

            var appSettingsApplicable = configuration.Enrich.Count > 0 || configuration.EnrichWithProperty.Count > 0 || configuration.MinimumLevel != null || configuration.WriteTo.Count > 0 || configuration.Destructure.Count > 0
                                        || configuration.AuditTo.Count > 0 || configuration.MinimumLevelOverrides.Count > 0 || configuration.Filter.Count > 0 || configuration.MinimumLevelControlledBy != null;

            if (appSettingsApplicable)
            {
                context.RegisterRefactoring(CodeAction.Create("Show <appSettings> config", c => InsertConfigurationComment(context.Document, firstProperty, root, configuration, GetAppSettingsConfiguration, c)));
                context.RegisterRefactoring(CodeAction.Create("Show appsettings.json config", c => InsertConfigurationComment(context.Document, firstProperty, root, configuration, GetAppSettingsJsonConfiguration, c)));
            }
        }

        private static LoggerConfiguration GetLoggerConfigurationFromSyntax(CodeRefactoringContext context, SemanticModel semanticModel, List<MemberAccessExpressionSyntax> configurationProperties)
        {
            var configuration = new LoggerConfiguration();
            foreach (var property in configurationProperties)
            {
                var invokedMethod = property.Ancestors().FirstOrDefault() as MemberAccessExpressionSyntax;

                // Just in case we're looking at syntax that has similiar names to LoggerConfiguration (ReadFrom, WriteTo, ...) but isn't related to Serilog
                if (invokedMethod == null)
                {
                    continue;
                }

                if (String.IsNullOrEmpty(invokedMethod?.Name?.ToString()))
                {
                    configuration.AddError("Failed to get name of method", invokedMethod);
                    continue;
                }

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
                        {
                            configuration.AddError("Can't get parameter value for MinimumLevel.Is(...)", invokedMethod);
                            continue;
                        }

                        var parameter = RoslynHelper.DetermineParameter(argument, semanticModel, false, context.CancellationToken);
                        if (parameter == null)
                        {
                            configuration.AddError("Failed to analyze parameter", argument);
                            continue;
                        }

                        var accessExpression = argument?.Expression as MemberAccessExpressionSyntax;
                        if (accessExpression == null)
                        {
                            configuration.AddError("Failed to analyze parameter", argument);
                            continue;
                        }

                        var constValue = semanticModel.GetConstantValue(accessExpression, context.CancellationToken);
                        if (!constValue.HasValue)
                        {
                            configuration.AddNonConstantError(argument);
                            continue;
                        }

                        long enumIntegralValue;
                        try
                        {
                            enumIntegralValue = Convert.ToInt64(constValue.Value);
                        }
                        catch
                        {
                            configuration.AddError($"Value {constValue.Value} is not within expected range", argument);
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
                            {
                                configuration.AddError("Failed to analyze parameter", argument);
                                continue;
                            }

                            if (parameter.Name == "source")
                            {
                                var constValue = semanticModel.GetConstantValue(argument.Expression, context.CancellationToken);
                                if (!constValue.HasValue)
                                {
                                    configuration.AddNonConstantError(argument.Expression);
                                    value = NotAConstantReplacementValue;
                                    continue;
                                }

                                key = constValue.Value?.ToString();
                            }
                            else if (parameter.Name == "minimumLevel")
                            {
                                var constValue = semanticModel.GetConstantValue(argument.Expression, context.CancellationToken);
                                if (!constValue.HasValue)
                                {
                                    configuration.AddNonConstantError(argument.Expression);
                                    value = NotAConstantReplacementValue;
                                    continue;
                                }

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
                    else if (logLevel == "ControlledBy")
                    {
                        var argument = (invokedMethod?.Parent as InvocationExpressionSyntax)?.ArgumentList?.Arguments.FirstOrDefault();
                        if (argument == null)
                        {
                            configuration.AddError("Can't get parameter value for MinimumLevel.ControlledBy(...)", invokedMethod);
                            continue;
                        }

                        var identifier = argument?.Expression as IdentifierNameSyntax;
                        if (identifier == null)
                        {
                            configuration.AddError("Failed to analyze parameter", argument);
                            continue;
                        }

                        TryAddLoggingLevelSwitch(semanticModel, identifier, configuration, context.CancellationToken);

                        configuration.MinimumLevelControlledBy = "$" + identifier.Identifier.Value;
                        continue;
                    }
                    else if (LogLevels.Contains(logLevel))
                    {
                        value = logLevel;
                    }
                    else
                    {
                        configuration.AddError("Unknown MinimumLevel method", invokedMethod);
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
                            {
                                configuration.AddError("Failed to analyze parameter", argument);
                                continue;
                            }

                            if (parameter.Name == "name")
                            {
                                var constValue = semanticModel.GetConstantValue(argument.Expression, context.CancellationToken);
                                if (!constValue.HasValue)
                                {
                                    configuration.AddNonConstantError(argument.Expression);
                                    value = NotAConstantReplacementValue;
                                    continue;
                                }

                                key = constValue.Value?.ToString();
                            }
                            else if (parameter.Name == "value")
                            {
                                var constValue = semanticModel.GetConstantValue(argument.Expression, context.CancellationToken);
                                if (!constValue.HasValue)
                                {
                                    configuration.AddNonConstantError(argument.Expression);
                                    value = NotAConstantReplacementValue;
                                    continue;
                                }

                                value = constValue.Value?.ToString();
                            }
                        }

                        if (key != null && value != null)
                        {
                            configuration.EnrichWithProperty[key] = value;
                        }
                    }
                    else
                    {
                        AddExtensibleMethod(semanticModel, invokedMethod, configuration, configuration.Enrich.Add, context.CancellationToken);
                    }
                }
                else if (configAction == "Destructure")
                {
                    AddExtensibleMethod(semanticModel, invokedMethod, configuration, configuration.Destructure.Add, context.CancellationToken);
                }
                else if (configAction == "Filter")
                {
                    AddExtensibleMethod(semanticModel, invokedMethod, configuration, configuration.Filter.Add, context.CancellationToken);
                }
                else if (configAction == "WriteTo")
                {
                    AddExtensibleMethod(semanticModel, invokedMethod, configuration, configuration.WriteTo.Add, context.CancellationToken);
                }
                else if (configAction == "AuditTo")
                {
                    AddExtensibleMethod(semanticModel, invokedMethod, configuration, configuration.AuditTo.Add, context.CancellationToken);
                }
            }

            return configuration;
        }

        private static void AddExtensibleMethod(SemanticModel semanticModel, MemberAccessExpressionSyntax invokedMethod, LoggerConfiguration configuration, Action<ExtensibleMethod> addMethod, CancellationToken cancellationToken)
        {
            var methodSymbol = semanticModel.GetSymbolInfo(invokedMethod).Symbol as IMethodSymbol;
            var method = new ExtensibleMethod
            {
                AssemblyName = methodSymbol?.ContainingAssembly?.Name,
                MethodName = invokedMethod.Name.Identifier.ToString()
            };

            if (String.IsNullOrEmpty(method.AssemblyName))
            {
                configuration.AddError("Failed to get semantic informations for this method", invokedMethod);
                return;
            }

            // Check for explicitly given type arguments that are not part of the normal arguments
            if (methodSymbol.TypeArguments.Length > 0 && methodSymbol.TypeArguments.Length == methodSymbol.TypeParameters.Length)
            {
                for (int i = 0; i < methodSymbol.TypeArguments.Length; i++)
                {
                    var typeParamter = methodSymbol.TypeParameters[i];
                    var typeArgument = methodSymbol.TypeArguments[i];

                    if (methodSymbol.Parameters.Any(x => x.Type == typeParamter))
                    {
                        continue;
                    }

                    // Synthesize an System.Type argument if a generic version was used
                    switch (typeParamter.Name)
                    {
                        case "TSink": // WriteTo/AuditTo.Sink<TSink>(...)
                            method.Arguments["sink"] = GetAssemblyQualifiedTypeName(typeArgument);
                            break;
                        case "TEnricher": // Enrich.With<TEnricher>()
                            method.Arguments["enricher"] = GetAssemblyQualifiedTypeName(typeArgument);
                            break;
                        case "TFilter": // Filter.With<TFilter>()
                            method.Arguments["filter"] = GetAssemblyQualifiedTypeName(typeArgument);
                            break;
                        case "TDestructuringPolicy": // Destructure.With<TDestructuringPolicy>()
                            method.Arguments["policy"] = GetAssemblyQualifiedTypeName(typeArgument);
                            break;
                        case "TScalar": // Destructure.AsScalar<TScalar>()
                            method.Arguments["scalarType"] = GetAssemblyQualifiedTypeName(typeArgument);
                            break;
                    }
                }
            }

            var arguments = (invokedMethod?.Parent as InvocationExpressionSyntax)?.ArgumentList?.Arguments ?? default(SeparatedSyntaxList<ArgumentSyntax>);
            foreach (var argument in arguments)
            {
                var parameter = RoslynHelper.DetermineParameter(argument, semanticModel, false, cancellationToken);
                if (parameter == null)
                {
                    configuration.AddError("Failed to analyze parameter", argument);
                    continue;
                }

                string parameterName = parameter.Name;

                // Configuration Surrogates
                if (method.MethodName == "Sink" && parameterName == "logEventSink") // WriteTo/AuditTo.Sink(ILogEventSink logEventSink, ...)
                {
                    parameterName = "sink"; // Sink(this LoggerSinkConfiguration loggerSinkConfiguration, ILogEventSink sink, LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum, LoggingLevelSwitch levelSwitch = null)
                }
                else if (method.MethodName == "With" && parameterName == "filters") // Filter.With(params ILogEventFilter[] filters)
                {
                    parameterName = "filter"; // With(this LoggerFilterConfiguration loggerFilterConfiguration, ILogEventFilter filter)
                }
                else if (method.MethodName == "With" && parameterName == "destructuringPolicies") // Destructure.With(params IDestructuringPolicy[] destructuringPolicies)
                {
                    parameterName = "policy"; // With(this LoggerDestructuringConfiguration loggerDestructuringConfiguration, IDestructuringPolicy policy)
                }
                else if (method.MethodName == "With" && parameterName == "enrichers") // Enrich.With(params ILogEventEnricher[] enrichers)
                {
                    parameterName = "enricher"; // With(this LoggerEnrichmentConfiguration loggerEnrichmentConfiguration, ILogEventEnricher enricher)
                }

                ITypeSymbol type = parameter.Type;
                if (parameter.IsParams && type is IArrayTypeSymbol array)
                {
                    type = array.ElementType;
                }

                if (type.ToString() == "System.Type")
                {
                    method.Arguments[parameterName] = NotAConstantReplacementValue;

                    var typeofExpression = argument.Expression as TypeOfExpressionSyntax;
                    if (typeofExpression == null)
                    {
                        configuration.AddError("I need a typeof(T) expression for Type arguments", argument.Expression);
                        continue;
                    }

                    var typeInfo = semanticModel.GetTypeInfo(typeofExpression.Type).Type as INamedTypeSymbol;
                    if (typeInfo == null)
                    {
                        configuration.AddError("Failed to get semantic informations for typeof expression", typeofExpression);
                        return;
                    }

                    // generate the assembly qualified name for usage with Type.GetType(string)
                    string name = GetAssemblyQualifiedTypeName(typeInfo);
                    method.Arguments[parameterName] = name;
                    continue;
                }
                else if (type.TypeKind == TypeKind.Interface || type.TypeKind == TypeKind.Class && type.IsAbstract)
                {
                    method.Arguments[parameterName] = NotAConstantReplacementValue;

                    var expressionSymbol = semanticModel.GetSymbolInfo(argument.Expression).Symbol;
                    if (expressionSymbol != null && (expressionSymbol.Kind == SymbolKind.Property || expressionSymbol.Kind == SymbolKind.Field))
                    {
                        if (!expressionSymbol.IsStatic)
                        {
                            configuration.AddError("Only static fields and properties can be used", argument.Expression);
                            continue;
                        }

                        if (expressionSymbol.DeclaredAccessibility != Accessibility.Public || expressionSymbol is IPropertySymbol property && property.GetMethod.DeclaredAccessibility != Accessibility.Public)
                        {
                            configuration.AddError("Fields and properties must be public and properties must have public getters", argument.Expression);
                            continue;
                        }

                        method.Arguments[parameterName] = GetAssemblyQualifiedTypeName(expressionSymbol.ContainingType, "::" + expressionSymbol.Name);
                        continue;
                    }

                    var objectCreation = argument.Expression as ObjectCreationExpressionSyntax;
                    if (objectCreation == null)
                    {
                        configuration.AddError("I can only infer types from `new T()` expressions", argument.Expression);
                        continue;
                    }

                    // check if there are explicit arguments which are unsupported
                    if (objectCreation.ArgumentList?.Arguments.Count > 0)
                    {
                        configuration.AddError("The configuration supports only parameterless constructors for interface or abstract type parameters", argument.Expression);
                        continue;
                    }

                    var typeInfo = semanticModel.GetTypeInfo(objectCreation).Type as INamedTypeSymbol;
                    if (typeInfo == null)
                    {
                        configuration.AddError("Failed to get semantic informations for this constructor", objectCreation);
                        return;
                    }

                    // generate the assembly qualified name for usage with Type.GetType(string)
                    string name = GetAssemblyQualifiedTypeName(typeInfo);
                    method.Arguments[parameterName] = name;
                    continue;
                }
                else if (type.ToString() == "Serilog.Core.LoggingLevelSwitch")
                {
                    var identifier = argument?.Expression as IdentifierNameSyntax;
                    if (identifier == null)
                    {
                        configuration.AddError("Failed to analyze parameter", argument);
                        continue;
                    }

                    TryAddLoggingLevelSwitch(semanticModel, identifier, configuration, cancellationToken);

                    method.Arguments[parameterName] = "$" + identifier.Identifier.Value;
                    continue;
                }

                string value = null;
                var constValue = semanticModel.GetConstantValue(argument.Expression, cancellationToken);
                if (!constValue.HasValue)
                {
                    configuration.AddNonConstantError(argument.Expression);
                    value = NotAConstantReplacementValue;
                }
                else
                {
                    if (type.TypeKind == TypeKind.Enum)
                    {
                        var enumMember = type.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(x => Convert.ToInt64(x.ConstantValue) == Convert.ToInt64(constValue.Value));
                        value = enumMember.Name;
                    }
                    else
                    {
                        value = constValue.Value?.ToString();
                    }
                }

                method.Arguments[parameterName] = value;
            }

            addMethod(method);
        }

        private static void TryAddLoggingLevelSwitch(SemanticModel semanticModel, IdentifierNameSyntax identifier, LoggerConfiguration configuration, CancellationToken cancellationToken)
        {
            string name = "$" + identifier.Identifier.Value;
            if (configuration.LevelSwitches.ContainsKey(name))
            {
                return;
            }

            var symbol = semanticModel.GetSymbolInfo(identifier, cancellationToken).Symbol;
            if (symbol == null)
            {
                configuration.AddError("Failed to analyze parameter", identifier);
                return;
            }

            var reference = symbol.DeclaringSyntaxReferences.FirstOrDefault() as SyntaxReference;
            if (reference == null)
            {
                configuration.AddError("Could not find declaration of LoggingLevelSwitch", identifier);
                return;
            }

            var declarator = reference.GetSyntax(cancellationToken) as VariableDeclaratorSyntax;
            if (declarator == null)
            {
                configuration.AddError("Could not find declaration of LoggingLevelSwitch", identifier);
                return;
            }

            var initializer = declarator.Initializer.Value as ObjectCreationExpressionSyntax;
            if (initializer == null)
            {
                configuration.AddError("Could not find initialization of LoggingLevelSwitch", identifier);
                return;
            }

            IParameterSymbol parameter;
            object value;
            var argument = initializer.ArgumentList.Arguments.FirstOrDefault();
            if (argument == null)
            {
                var constructor = semanticModel.GetSymbolInfo(initializer, cancellationToken).Symbol;
                if (constructor == null)
                {
                    configuration.AddError("Could not analyze LoggingLevelSwitch constructor", identifier);
                    return;
                }

                parameter = (symbol as IMethodSymbol)?.Parameters.FirstOrDefault();
                if (parameter == null)
                {
                    configuration.AddError("Could not analyze LoggingLevelSwitch constructor", identifier);
                    return;
                }

                value = parameter.ExplicitDefaultValue;
            }
            else
            {
                parameter = RoslynHelper.DetermineParameter(argument, semanticModel, false, cancellationToken);
                if (parameter == null)
                {
                    configuration.AddError("Failed to analyze parameter", argument);
                    return;
                }

                var constValue = semanticModel.GetConstantValue(argument.Expression, cancellationToken);
                if (!constValue.HasValue)
                {
                    configuration.AddNonConstantError(argument);
                    return;
                }

                value = constValue.Value;
            }

            long enumIntegralValue;
            try
            {
                enumIntegralValue = Convert.ToInt64(value);
            }
            catch
            {
                configuration.AddError($"Value {value} is not within expected range", argument);
                return;
            }

            // Roslyn returns enum constant values as integers, convert it back to the enum member name
            var enumMember = parameter.Type.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(x => Convert.ToInt64(x.ConstantValue) == enumIntegralValue);
            configuration.LevelSwitches.Add(name, enumMember.Name);
        }

        private static string GetAssemblyQualifiedTypeName(ITypeSymbol type, string typeSuffix = null)
        {
            var display = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
            string name = type.ToDisplayString(display);

            var namedType = type as INamedTypeSymbol;
            if (namedType?.TypeArguments.Length > 0)
            {
                name += "`" + namedType.Arity;
                name += "[" + String.Join(", ", namedType.TypeArguments.Select(x => "[" + GetAssemblyQualifiedTypeName(x) + "]")) + "]";
            }

            if (typeSuffix != null)
            {
                name += typeSuffix;
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

            foreach (var kvp in configuration.LevelSwitches)
            {
                AddEntry(configEntries, "serilog:level-switch:" + kvp.Key, kvp.Value);
            }

            if (configuration.MinimumLevel != null)
            {
                AddEntry(configEntries, "serilog:minimum-level", configuration.MinimumLevel);
            }

            if (configuration.MinimumLevelControlledBy != null)
            {
                AddEntry(configEntries, "serilog:minimum-level:controlled-by", configuration.MinimumLevelControlledBy);
            }

            foreach (var kvp in configuration.MinimumLevelOverrides)
            {
                AddEntry(configEntries, "serilog:minimum-level:override:" + kvp.Key, kvp.Value);
            }

            foreach (var kvp in configuration.EnrichWithProperty)
            {
                AddEntry(configEntries, "serilog:enrich:with-property:" + kvp.Key, kvp.Value);
            }

            foreach (var enrichment in configuration.Enrich)
            {
                ConvertExtensibleMethod(configEntries, enrichment, "serilog:enrich");
            }

            foreach (var filter in configuration.Filter)
            {
                ConvertExtensibleMethod(configEntries, filter, "serilog:filter");
            }

            foreach (var destructure in configuration.Destructure)
            {
                ConvertExtensibleMethod(configEntries, destructure, "serilog:destructure");
            }

            foreach (var writeTo in configuration.WriteTo)
            {
                ConvertExtensibleMethod(configEntries, writeTo, "serilog:write-to");
            }

            foreach (var auditTo in configuration.AuditTo)
            {
                ConvertExtensibleMethod(configEntries, auditTo, "serilog:audit-to");
            }

            var usedSuffixes = new HashSet<string>();
            foreach (var usedAssembly in configuration.Enrich.Concat(configuration.WriteTo).Concat(configuration.AuditTo).Select(x => x.AssemblyName).Where(x => x != "Serilog").Distinct())
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
            if (configuration.HasParsingErrors)
            {
                sb.AppendLine("Errors:");
                foreach (var log in configuration.ErrorLog)
                {
                    sb.AppendLine(log);
                }
                if (configuration.ErrorLog.Count > 0)
                {
                    sb.AppendLine();
                }
            }
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
            if (configuration.HasParsingErrors)
            {
                sb.AppendLine("Errors:");
                foreach (var log in configuration.ErrorLog)
                {
                    sb.AppendLine(log);
                }
                sb.AppendLine();
            }
            sb.AppendLine(@"""Serilog"": {");

            bool needsComma = false;

            // technically it's not correct to abuse roslyn to escape the string literals for us but csharp strings look very much like js string literals so...
            string usings = String.Join(", ", configuration.Enrich.Concat(configuration.WriteTo).Concat(configuration.AuditTo).Select(x => x.AssemblyName).Where(x => x != "Serilog").Distinct().Select(x => SyntaxFactory.Literal(x).ToString()));
            if (!String.IsNullOrEmpty(usings))
            {
                sb.AppendFormat(@"  ""Using"": [{0}]", usings);
                needsComma = true;
            }

            if (configuration.LevelSwitches.Count > 0)
            {
                FinishPreviousLine(sb, ref needsComma);

                string switches = String.Join(", ", configuration.LevelSwitches.Select(x => SyntaxFactory.Literal(x.Key).ToString() + ": " + SyntaxFactory.Literal(x.Value).ToString()));
                sb.AppendFormat(@"  ""LevelSwitches"": {{ {0} }}", switches);

                needsComma = true;
            }

            if (configuration.MinimumLevel != null || configuration.MinimumLevelControlledBy != null || configuration.MinimumLevelOverrides.Any())
            {
                FinishPreviousLine(sb, ref needsComma);
                if (!configuration.MinimumLevelOverrides.Any() && configuration.MinimumLevelControlledBy == null)
                {
                    sb.AppendFormat(@"  ""MinimumLevel"": {0}", SyntaxFactory.Literal(configuration.MinimumLevel).ToString());
                }
                else
                {
                    sb.AppendLine(@"  ""MinimumLevel"": {");

                    if (configuration.MinimumLevel != null)
                    {
                        sb.AppendFormat(@"    ""Default"": {0}", SyntaxFactory.Literal(configuration.MinimumLevel).ToString());

                        needsComma = true;
                    }

                    if (configuration.MinimumLevelControlledBy != null)
                    {
                        FinishPreviousLine(sb, ref needsComma);

                        sb.AppendFormat(@"    ""ControlledBy"": {0}", SyntaxFactory.Literal(configuration.MinimumLevelControlledBy).ToString());

                        needsComma = true;
                    }

                    int remaining = configuration.MinimumLevelOverrides.Count;
                    if (remaining > 0)
                    {
                        FinishPreviousLine(sb, ref needsComma);

                        sb.AppendLine(@"    ""Override"": {");
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
                    }
                    else
                    {
                        sb.AppendLine();
                    }

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

            if (configuration.AuditTo.Any())
            {
                FinishPreviousLine(sb, ref needsComma);
                WriteMethodCalls(configuration.AuditTo, sb, "AuditTo");

                needsComma = true;
            }

            if (configuration.Enrich.Any())
            {
                FinishPreviousLine(sb, ref needsComma);
                WriteMethodCalls(configuration.Enrich, sb, "Enrich");

                needsComma = true;
            }

            if (configuration.Destructure.Any())
            {
                FinishPreviousLine(sb, ref needsComma);
                WriteMethodCalls(configuration.Destructure, sb, "Destructure");

                needsComma = true;
            }

            if (configuration.Filter.Any())
            {
                FinishPreviousLine(sb, ref needsComma);
                WriteMethodCalls(configuration.Filter, sb, "Filter");

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