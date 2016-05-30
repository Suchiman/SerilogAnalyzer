// Based on https://github.com/serilog/serilog/blob/023d57c282dcb5ddedee9401c68743a713fa0c8c/src/Serilog/Parsing/PropertyToken.cs
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
using System.Globalization;

namespace SerilogAnalyzer
{
    /// <summary>
    /// A message template token representing a log event property.
    /// </summary>
    class PropertyToken : MessageTemplateToken
    {
        readonly string _rawText;
        readonly int? _position;

        public PropertyToken(int startIndex, string propertyName, string rawText)
            : base(startIndex, rawText.Length)
        {
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));
            if (rawText == null) throw new ArgumentNullException(nameof(rawText));
            PropertyName = propertyName;
            _rawText = rawText;

            int position;
            if (int.TryParse(PropertyName, NumberStyles.None, CultureInfo.InvariantCulture, out position) &&
                position >= 0)
            {
                _position = position;
            }
        }

        /// <summary>
        /// The property name.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// True if the property name is a positional index; otherwise, false.
        /// </summary>
        public bool IsPositional => _position.HasValue;

        /// <summary>
        /// Try to get the integer value represented by the property name.
        /// </summary>
        /// <param name="position">The integer value, if present.</param>
        /// <returns>True if the property is positional, otherwise false.</returns>
        public bool TryGetPositionalValue(out int position)
        {
            if (_position == null)
            {
                position = 0;
                return false;
            }

            position = _position.Value;
            return true;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString() => _rawText;
    }
}
