// Based on https://github.com/serilog/serilog/blob/e97b3c028bdb28e4430512b84dc2082e6f98dcc7/src/Serilog/Parameters/PropertyBinder.cs
// Copyright 2013-2015 Serilog Contributors
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

namespace SerilogAnalyzer
{
    static class PropertyBindingAnalyzer
    {
        static readonly List<MessageTemplateDiagnostic> NoProperties = new List<MessageTemplateDiagnostic>(0);

        public static List<MessageTemplateDiagnostic> AnalyzeProperties(List<PropertyToken> propertyTokens, List<SourceArgument> arguments)
        {
            if (propertyTokens.Count > 0)
            {
                var allPositional = true;
                var anyPositional = false;
                foreach (var propertyToken in propertyTokens)
                {
                    if (propertyToken.IsPositional)
                        anyPositional = true;
                    else
                        allPositional = false;
                }

                var diagnostics = new List<MessageTemplateDiagnostic>();
                if (allPositional)
                {
                    AnalyzePositionalProperties(diagnostics, propertyTokens, arguments);
                }
                else
                {
                    if (anyPositional)
                    {
                        foreach (var propertyToken in propertyTokens)
                        {
                            if (propertyToken.IsPositional)
                            {
                                diagnostics.Add(new MessageTemplateDiagnostic(propertyToken.StartIndex, propertyToken.Length, "Positional properties are not allowed, when named properties are being used"));
                            }
                        }
                    }

                    AnalyzeNamedProperties(diagnostics, propertyTokens, arguments);
                }

                return diagnostics;
            }
            else if (arguments.Count > 0)
            {
                var diagnostics = new List<MessageTemplateDiagnostic>();
                foreach (var argument in arguments)
                {
                    diagnostics.Add(new MessageTemplateDiagnostic(argument.StartIndex, argument.Length, "There is no property that corresponds to this argument", false));
                }
                return diagnostics;
            }

            return NoProperties;
        }

        static void AnalyzePositionalProperties(List<MessageTemplateDiagnostic> diagnostics, List<PropertyToken> positionalProperties, List<SourceArgument> arguments)
        {
            int biggestIndexUsed = -1;
            foreach (var property in positionalProperties)
            {
                int position;
                if (property.TryGetPositionalValue(out position))
                {
                    if (position < 0)
                    {
                        diagnostics.Add(new MessageTemplateDiagnostic(property.StartIndex, property.Length, "Positional index cannot be negative"));
                    }

                    if (position >= arguments.Count)
                    {
                        diagnostics.Add(new MessageTemplateDiagnostic(property.StartIndex, property.Length, "There is no argument that corresponds to the positional property " + position.ToString()));
                    }

                    biggestIndexUsed = Math.Max(biggestIndexUsed, position);
                }
                else
                {
                    diagnostics.Add(new MessageTemplateDiagnostic(property.StartIndex, property.Length, "Couldn't get the position of this property while analyzing"));
                }
            }

            for (var i = biggestIndexUsed + 1; i < arguments.Count; ++i)
            {
                diagnostics.Add(new MessageTemplateDiagnostic(arguments[i].StartIndex, arguments[i].Length, "There is no positional property that corresponds to this argument", false));
            }
        }

        static void AnalyzeNamedProperties(List<MessageTemplateDiagnostic> diagnostics, List<PropertyToken> namedProperties, List<SourceArgument> arguments)
        {
            var matchedRun = Math.Min(namedProperties.Count, arguments.Count);

            // could still possibly work when it hits a name of a contextual property but it's better practice to be explicit at the callsite
            for (int i = matchedRun; i < namedProperties.Count; i++)
            {
                var namedProperty = namedProperties[i];
                diagnostics.Add(new MessageTemplateDiagnostic(namedProperty.StartIndex, namedProperty.Length, "There is no argument that corresponds to the named property '" + namedProperty.PropertyName + "'"));
            }

            for (int i = matchedRun; i < arguments.Count; i++)
            {
                var argument = arguments[i];
                diagnostics.Add(new MessageTemplateDiagnostic(argument.StartIndex, argument.Length, "There is no named property that corresponds to this argument", false));
            }
        }
    }
}
