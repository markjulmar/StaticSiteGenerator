﻿// 
//   MarkdownDeep - http://www.toptensoftware.com/markdowndeep
//	 Copyright (C) 2010-2011 Topten Software
// 
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this product except in 
//   compliance with the License. You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software distributed under the License is 
//   distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
//   See the License for the specific language governing permissions and limitations under the License.
//

using System.Collections.Generic;
using System.Text;

namespace XamU.SGL.Extensions.MarkdownDeep
{
    internal enum ColumnAlignment
    {
        NA,
        Left,
        Right,
        Center,
    }
    internal class TableSpec
    {
        public bool LeadingBar;
        public bool TrailingBar;

        public List<string> Headers;
        public List<ColumnAlignment> Columns = new List<ColumnAlignment>();
        public List<List<string>> Rows = new List<List<string>>();

        public List<string> ParseRow(StringScanner p)
        {
            p.SkipLinespace();

            if (p.eol)
                return null;		// Blank line ends the table

            bool bAnyBars=LeadingBar;
            if (LeadingBar && !p.SkipChar('|'))
            {
                return null;
            }

            // Create the row
            List<string> row = new List<string>();

            // Parse all columns except the last

            while (!p.eol)
            {
                // Find the next vertical bar
                p.Mark();
                while (!p.eol && p.current != '|')
                    p.SkipEscapableChar(true);

                row.Add(p.Extract().Trim());

                bAnyBars|=p.SkipChar('|');
            }

            // Require at least one bar to continue the table
            if (!bAnyBars)
                return null;

            // Add missing columns
            while (row.Count < Columns.Count)
            {
                row.Add("&nbsp;");
            }

            p.SkipEol();
            return row;
        }

        internal void RenderRow(Markdown m, StringBuilder b, List<string> row, string type)
        {
            // Class is added as final entry in Headers.
            string rowClass = "";
            if (row.Count > Columns.Count)
            {
                rowClass = row.Last();
                rowClass = " class=\"" + rowClass.Substring(1, rowClass.Length - 2).Replace(".","").Trim() + "\"";
                row.RemoveAt(row.Count - 1);
            }

            for (int i=0; i<row.Count; i++)
            {
                b.Append($"\t<{type}{rowClass}");

                if (i < Columns.Count)
                {
                    switch (Columns[i])
                    {
                        case ColumnAlignment.Left:
                            b.Append(" align=\"left\"");
                            break;
                        case ColumnAlignment.Right:
                            b.Append(" align=\"right\"");
                            break;
                        case ColumnAlignment.Center:
                            b.Append(" align=\"center\"");
                            break;
                    }
                }

                b.Append(">");
                new SpanFormatter(m).Format(b, row[i]);
                b.Append("</");
                b.Append(type);
                b.Append(">\n");
            }
        }
    
        public void Render(Markdown m, StringBuilder b)
        {
            // Class is added as final entry in Headers.
            string tableClass = "";
            if (Headers?.Count > Columns.Count)
            {
                tableClass = Headers.Last();
                if (!string.IsNullOrEmpty(tableClass))
                {
                    tableClass = " class=\"" + tableClass.Substring(1, tableClass.Length - 2).Replace(".", "").Trim() + "\"";
                }
                Headers.RemoveAt(Headers.Count - 1);
            }

            b.Append($"<table{tableClass}>\n");
            if (Headers != null)
            {
                b.Append("<thead>\n<tr>\n");
                RenderRow(m, b, Headers, "th");
                b.Append("</tr>\n</thead>\n");
            }

            b.Append("<tbody>\n");
            foreach (var row in Rows)
            {
                b.Append("<tr>\n");
                RenderRow(m, b, row, "td");
                b.Append("</tr>\n");
            }
            b.Append("</tbody>\n");

            b.Append("</table>\n");
        }

        public static TableSpec Parse(StringScanner p)
        {
            // Leading line space allowed
            p.SkipLinespace();

            // Quick check for typical case
            if (p.current != '|' && p.current != ':' && p.current != '-')
                return null;

            // Don't create the spec until it at least looks like one
            TableSpec spec = null;

            // Leading bar, looks like a table spec
            if (p.SkipChar('|'))
            {
                spec = new TableSpec {LeadingBar = true};
            }

            // Process all columns
            while (true)
            {
                // Parse column spec
                p.SkipLinespace();

                // Must have something in the spec
                if (p.current == '|')
                    return null;

                bool alignLeft = p.SkipChar(':');
                while (p.current == '-')
                    p.SkipForward(1);
                bool alignRight = p.SkipChar(':');
                p.SkipLinespace();

                // Work out column alignment
                ColumnAlignment col = ColumnAlignment.NA;
                if (alignLeft && alignRight)
                    col = ColumnAlignment.Center;
                else if (alignLeft)
                    col = ColumnAlignment.Left;
                else if (alignRight)
                    col = ColumnAlignment.Right;

                if (p.eol)
                {
                    // Not a spec?
                    if (spec == null)
                        return null;

                    // Add the final spec?
                    spec.Columns.Add(col);
                    return spec;
                }

                // We expect a vertical bar
                if (!p.SkipChar('|'))
                    return null;

                // Create the table spec
                if (spec==null)
                    spec=new TableSpec();

                // Add the column
                spec.Columns.Add(col);

                // Check for trailing vertical bar
                p.SkipLinespace();
                if (p.eol)
                {
                    spec.TrailingBar = true;
                    return spec;
                }

                // Next column
            }
        }
    }
}
