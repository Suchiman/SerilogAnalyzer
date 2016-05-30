// Based on https://github.com/serilog/serilog/blob/e87b99937417700bfb9ba473b017fa104259bba4/src/Serilog/Parsing/MessageTemplateToken.cs
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

namespace SerilogAnalyzer
{
    /// <summary>
    /// An element parsed from a message template string.
    /// </summary>
    abstract class MessageTemplateToken
    {
        /// <summary>
        /// Construct a <see cref="MessageTemplateToken"/>.
        /// </summary>
        /// <param name="startIndex">The token's start index in the template.</param>
        protected MessageTemplateToken(int startIndex, int length)
        {
            StartIndex = startIndex;
            Length = length;
        }

        /// <summary>
        /// The token's start index in the template.
        /// </summary>
        public int StartIndex { get; set; }

        /// <summary>
        /// The token's length.
        /// </summary>
        public int Length { get; set; }
    }
}
