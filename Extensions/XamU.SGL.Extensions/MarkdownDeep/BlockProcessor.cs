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

using System;
using System.Collections.Generic;
using System.Text;

namespace XamU.SGL.Extensions.MarkdownDeep
{
    public class BlockProcessor : StringScanner
    {
        public BlockProcessor(Markdown m, bool markdownInHtml)
        {
            m_markdown = m;
            m_bMarkdownInHtml = markdownInHtml;
            m_parentType = BlockType.Blank;
        }

        internal BlockProcessor(Markdown m, bool markdownInHtml, BlockType parentType)
        {
            m_markdown = m;
            m_bMarkdownInHtml = markdownInHtml;
            m_parentType = parentType;
        }

        internal List<Block> Process(string str)
        {
            return ScanLines(str);
        }

        internal List<Block> ScanLines(string str)
        {
            // Reset string scanner
            Reset(str);
            return ScanLines();
        }

        internal List<Block> ScanLines(string str, int start, int len)
        {
            Reset(str, start, len);
            return ScanLines();
        }

        internal bool StartTable(TableSpec spec, List<Block> lines)
        {
            // Mustn't have more than 1 preceding line
            if (lines.Count > 1)
                return false;

            // Rewind, parse the header row then fast forward back to current pos
            if (lines.Count == 1)
            {
                int savepos = position;
                position = lines[0].lineStart;
                spec.Headers = spec.ParseRow(this);
                if (spec.Headers == null)
                    return false;
                position = savepos;
                lines.Clear();
            }

            // Parse all rows
            while (true)
            {
                int savepos = position;

                var row=spec.ParseRow(this);
                if (row!=null)
                {
                    spec.Rows.Add(row);
                    continue;
                }

                position = savepos;
                break;
            }

            return true;
        }

        internal List<Block> ScanLines()
        {
            // The final set of blocks will be collected here
            var blocks = new List<Block>();

            // The current paragraph/list/codeblock etc will be accumulated here
            // before being collapsed into a block and store in above `blocks` list
            var lines = new List<Block>();

            // Add all blocks
            BlockType PrevBlockType = BlockType.unsafe_html;
            while (!eof)
            {
                // Remember if the previous line was blank
                bool bPreviousBlank = PrevBlockType == BlockType.Blank;

                // Get the next block
                var b = EvaluateLine();
                PrevBlockType = b.BlockType;
                if (PrevBlockType == BlockType.p_span)
                {
                    PrevBlockType = BlockType.p;
                }

                // For dd blocks, we need to know if it was preceeded by a blank line
                // so store that fact as the block's data.
                if (b.BlockType == BlockType.dd)
                {
                    b.Data = bPreviousBlank;
                }


                // SetExt header?
                if (b.BlockType == BlockType.post_h1 || b.BlockType == BlockType.post_h2)
                {
                    if (lines.Count > 0)
                    {
                        // Remove the previous line and collapse the current paragraph
                        var prevline = lines.Pop();
                        CollapseLines(blocks, lines);

                        // If previous line was blank, 
                        if (prevline.BlockType != BlockType.Blank)
                        {
                            // Convert the previous line to a heading and add to block list
                            prevline.RevertToPlain();
                            prevline.BlockType = b.BlockType == BlockType.post_h1 ? BlockType.h1 : BlockType.h2;
                            blocks.Add(prevline);
                            continue;
                        }
                    }

                    // Couldn't apply setext header to a previous line

                    if (b.BlockType == BlockType.post_h1)
                    {
                        // `===` gets converted to normal paragraph
                        b.RevertToPlain();
                        lines.Add(b);
                    }
                    else
                    {
                        // `---` gets converted to hr
                        if (b.contentLen >= 3)
                        {
                            b.BlockType = BlockType.hr;
                            blocks.Add(b);
                        }
                        else
                        {
                            b.RevertToPlain();
                            lines.Add(b);
                        }
                    }

                    continue;
                }


                // Work out the current paragraph type
                BlockType currentBlockType = lines.Count > 0 ? lines[0].BlockType : BlockType.Blank;

                // Starting a table?
                if (b.BlockType == BlockType.table_spec)
                {
                    // Get the table spec, save position
                    TableSpec spec = (TableSpec)b.Data;
                    int savepos = position;
                    if (!StartTable(spec, lines))
                    {
                        // Not a table, revert the tablespec row to plain,
                        // fast forward back to where we were up to and continue
                        // on as if nothing happened
                        position = savepos;
                        b.RevertToPlain();
                    }
                    else
                    {
                        blocks.Add(b);
                        continue;
                    }
                }

                // Process this line
                switch (b.BlockType)
                {
                    case BlockType.Blank:
                        switch (currentBlockType)
                        {
                            case BlockType.Blank:
                                break;

                            case BlockType.p:
                            case BlockType.p_span:
                                CollapseLines(blocks, lines);
                                break;

                            case BlockType.quote:
                            case BlockType.ol_li:
                            case BlockType.ul_li:
                            case BlockType.dd:
                            case BlockType.footnote:
                            case BlockType.indent:
                                lines.Add(b);
                                break;

                            default:
                                System.Diagnostics.Debug.Assert(false);
                                break;
                        }
                        break;

                    case BlockType.p:
                    case BlockType.p_span:
                        switch (currentBlockType)
                        {
                            case BlockType.Blank:
                            case BlockType.p:
                            case BlockType.p_span:
                                lines.Add(b);
                                break;

                            case BlockType.quote:
                            case BlockType.ol_li:
                            case BlockType.ul_li:
                            case BlockType.dd:
                            case BlockType.footnote:
                                var prevline = lines.Last();
                                if (prevline.BlockType == BlockType.Blank)
                                {
                                    CollapseLines(blocks, lines);
                                    lines.Add(b);
                                }
                                else
                                {
                                    lines.Add(b);
                                }
                                break;

                            case BlockType.indent:
                                CollapseLines(blocks, lines);
                                lines.Add(b);
                                break;

                            default:
                                System.Diagnostics.Debug.Assert(false);
                                break;
                        }
                        break;

                    case BlockType.indent:
                        switch (currentBlockType)
                        {
                            case BlockType.Blank:
                                // Start a code block
                                lines.Add(b);
                                break;

                            case BlockType.p:
                            case BlockType.p_span:
                            case BlockType.quote:
                                var prevline = lines.Last();
                                if (prevline.BlockType == BlockType.Blank)
                                {
                                    // Start a code block after a paragraph
                                    CollapseLines(blocks, lines);
                                    lines.Add(b);
                                }
                                else
                                {
                                    // indented line in paragraph, just continue it
                                    b.RevertToPlain();
                                    lines.Add(b);
                                }
                                break;


                            case BlockType.ol_li:
                            case BlockType.ul_li:
                            case BlockType.dd:
                            case BlockType.footnote:
                            case BlockType.indent:
                                lines.Add(b);
                                break;

                            default:
                                System.Diagnostics.Debug.Assert(false);
                                break;
                        }
                        break;

                    case BlockType.quote:
                        if (currentBlockType != BlockType.quote)
                        {
                            CollapseLines(blocks, lines);
                        }
                        lines.Add(b);
                        break;

                    case BlockType.ol_li:
                    case BlockType.ul_li:
                        switch (currentBlockType)
                        {
                            case BlockType.Blank:
                                lines.Add(b);
                                break;

                            case BlockType.p:
                            case BlockType.p_span:
                            case BlockType.quote:
                                var prevline = lines.Last();
                                if (prevline.BlockType == BlockType.Blank || m_parentType==BlockType.ol_li || m_parentType==BlockType.ul_li || m_parentType==BlockType.dd)
                                {
                                    // List starting after blank line after paragraph or quote
                                    CollapseLines(blocks, lines);
                                    lines.Add(b);
                                }
                                else
                                {
                                    // List's can't start in middle of a paragraph
                                    b.RevertToPlain();
                                    lines.Add(b);
                                }
                                break;

                            case BlockType.ol_li:
                            case BlockType.ul_li:
                                if (b.BlockType!=BlockType.ol_li && b.BlockType!=BlockType.ul_li)
                                {
                                    CollapseLines(blocks, lines);
                                }
                                lines.Add(b);
                                break;

                            case BlockType.dd:
                            case BlockType.footnote:
                                if (b.BlockType != currentBlockType)
                                {
                                    CollapseLines(blocks, lines);
                                }
                                lines.Add(b);
                                break;

                            case BlockType.indent:
                                // List after code block
                                CollapseLines(blocks, lines);
                                lines.Add(b);
                                break;
                        }
                        break;

                    case BlockType.dd:
                    case BlockType.footnote:
                        switch (currentBlockType)
                        {
                            case BlockType.Blank:
                            case BlockType.p:
                            case BlockType.p_span:
                            case BlockType.dd:
                            case BlockType.footnote:
                                CollapseLines(blocks, lines);
                                lines.Add(b);
                                break;

                            default:
                                b.RevertToPlain();
                                lines.Add(b);
                                break;
                        }
                        break;

                    default:
                        CollapseLines(blocks, lines);
                        blocks.Add(b);
                        break;
                }
            }

            CollapseLines(blocks, lines);

            if (m_markdown.ExtraMode)
            {
                BuildDefinitionLists(blocks);
            }

            // All children just text?
            bool allSpan = (m_parentType != BlockType.Blank 
                && blocks.TrueForAll(b => b.BlockType == BlockType.p_span));
            
            // Turn p_span into either <p> or text.
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i].BlockType == BlockType.p_span)
                {
                    blocks[i].BlockType = (allSpan) ? BlockType.span : BlockType.p;
                }
            }

            return blocks;
        }

        internal string RenderLines(List<Block> lines)
        {
            var b = new StringBuilder();
            foreach (var l in lines)
            {
                b.Append(l.buf, l.contentStart, l.contentLen);
                b.Append('\n');
            }
            return b.ToString();
        }

        internal void CollapseLines(List<Block> blocks, List<Block> lines)
        {
            // Remove trailing blank lines
            while (lines.Count>0 && lines.Last().BlockType == BlockType.Blank)
            {
                lines.Pop();
            }

            // Quit if empty
            if (lines.Count == 0)
                return;

            // What sort of block?
            switch (lines[0].BlockType)
            {
                case BlockType.p:
                case BlockType.p_span:
                {
                    // Collapse all lines into a single paragraph
                    var para = new Block() {
                        BlockType = lines[0].BlockType,
                        buf = lines[0].buf,
                        contentStart = lines[0].contentStart,
                        contentEnd = lines.Last().contentEnd
                    };
                    blocks.Add(para);
                    lines.Clear();
                    break;
                }

                case BlockType.quote:
                {
                    // Create a new quote block
                    var quote = new Block(BlockType.quote)
                    {
                        children = new BlockProcessor(m_markdown, m_bMarkdownInHtml, BlockType.quote).Process(RenderLines(lines)),
                        // XamU: save data
                        Data = lines[0].Data
                    };
                    lines.Clear();
                    blocks.Add(quote);
                    break;
                }

                case BlockType.ol_li:
                case BlockType.ul_li:
                    blocks.Add(BuildList(lines));
                    break;

                case BlockType.dd:
                    if (blocks.Count > 0)
                    {
                        var prev=blocks[blocks.Count-1];
                        switch (prev.BlockType)
                        {
                            case BlockType.p:
                                prev.BlockType = BlockType.dt;
                                break;

                            case BlockType.dd:
                                break;

                            default:
                                var wrapper = new Block {
                                    BlockType = BlockType.dt,
                                    children = new List<Block> { prev }
                                };
                                blocks.Pop();
                                blocks.Add(wrapper);
                                break;
                        }

                    }
                    blocks.Add(BuildDefinition(lines));
                    break;

                case BlockType.footnote:
                    m_markdown.AddFootnote(BuildFootnote(lines));
                    break;

                case BlockType.indent:
                {
                    if (m_parentType == BlockType.HtmlTag)
                    {
                        var htmlBlock = new Block(BlockType.HtmlTag) {children = new List<Block>(lines)};
                        blocks.Add(htmlBlock);
                    }
                    else
                    {
                        var codeblock = new Block(BlockType.codeblock) { children = new List<Block>(lines) };
                        blocks.Add(codeblock);
                    }
                    lines.Clear();
                    break;
                }
            }
        }


        Block EvaluateLine()
        {
            // Create a block
            var b = new Block {
                // Store line start
                lineStart = position,
                buf = input,

                // Scan the line
                contentStart = position,
                contentLen = -1
            };

            b.BlockType = EvaluateLine(b);
            
            // If end of line not returned, do it automatically
            if (b.contentLen < 0)
            {
                // Move to end of line
                SkipToEol();
                b.contentLen = position - b.contentStart;
            }

            // Setup line length
            b.lineLen=position-b.lineStart;

            // Next line
            SkipEol();

            // Create block
            return b;
        }

        BlockType EvaluateLine(Block b)
        {
            // Empty line?
            if (eol)
                return BlockType.Blank;

            // Save start of line position
            int line_start= position;

            // ## Heading ##		
            char ch=current;
            if (ch == '#')
            {
                // Work out heading level
                int level = 1;
                SkipForward(1);
                while (current == '#')
                {
                    level++;
                    SkipForward(1);
                }

                // Limit of 6
                if (level > 6)
                    level = 6;

                // Skip any whitespace
                SkipLinespace();

                // Save start position
                b.contentStart = position;

                // Jump to end
                SkipToEol();

                // In extra mode, check for a trailing HTML ID
                if (m_markdown.ExtraMode && !m_markdown.SafeMode)
                {
                    int end=position;
                    var attrs = Utils.CollectAttributes(input, b.contentStart, ref end);
                    if (attrs != null)
                    {
                        b.Data = attrs;
                        position = end;
                    }
                }

                // Rewind over trailing hashes
                while (position>b.contentStart && CharAtOffset(-1) == '#')
                {
                    SkipForward(-1);
                }

                // Rewind over trailing spaces
                while (position>b.contentStart && char.IsWhiteSpace(CharAtOffset(-1)))
                {
                    SkipForward(-1);
                }

                // Create the heading block
                b.contentEnd = position;

                SkipToEol();
                return BlockType.h1 + (level - 1);
            }

            // Check for entire line as - or = for setext h1 and h2
            if (ch=='-' || ch=='=')
            {
                // Skip all matching characters
                char chType = ch;
                while (current==chType)
                {
                    SkipForward(1);
                }

                // Trailing whitespace allowed
                SkipLinespace();

                // If not at eol, must have found something other than setext header
                if (eol)
                {
                    return chType == '=' ? BlockType.post_h1 : BlockType.post_h2;
                }

                position = line_start;
            }

            // MarkdownExtra Table row indicator?
            if (m_markdown.ExtraMode)
            {
                TableSpec spec = TableSpec.Parse(this);
                if (spec!=null)
                {
                    b.Data = spec;
                    return BlockType.table_spec;
                }

                position = line_start;
            }

            // Fenced code blocks?
            if (m_markdown.ExtraMode && (ch == '~' || ch=='`'))
            {
                if (ProcessFencedCodeBlock(b))
                    return b.BlockType;

                // Rewind
                position = line_start;
            }

            // Scan the leading whitespace, remembering how many spaces and where the first tab is
            int tabPos = -1;
            int leadingSpaces = 0;
            while (!eol)
            {
                if (current == ' ')
                {
                    if (tabPos < 0)
                        leadingSpaces++;
                }
                else if (current == '\t')
                {
                    if (tabPos < 0)
                        tabPos = position;
                }
                else
                {
                    // Something else, get out
                    break;
                }
                SkipForward(1);
            }

            // Blank line?
            if (eol)
            {
                b.contentEnd = b.contentStart;
                return BlockType.Blank;
            }

            // XamU Ignore so we can embed HTML in HTML with tabs.
            if (m_parentType != BlockType.HtmlTag)
            {
                // 4 leading spaces?
                if (leadingSpaces >= 4)
                {
                    b.contentStart = line_start + 4;
                    return BlockType.indent;
                }

                // Tab in the first 4 characters? 
                if (tabPos >= 0 && tabPos - line_start < 4)
                {
                    b.contentStart = tabPos + 1;
                    return BlockType.indent;
                }
            }

            // Treat start of line as after leading whitespace
            b.contentStart = position;

            // Get the next character
            ch = current;

            // Html block?
            if (ch == '<')
            {
                // Scan html block
                if (ScanHtml(b))
                    return b.BlockType;

                // Rewind
                position = b.contentStart;
                return BlockType.span; // do not wrap in <p>
            }

            // Block quotes start with '>' and have one space or one tab following
            if (ch == '>')
            {
                b.Data = 1;
                // XamU: second '>'? Indicates Info blockquote.
                if (m_markdown.EmitTag != null && CharAtOffset(1) == '>')
                {
                    b.Data = 2;
                    SkipForward(1);

                    // Third '>'? Indicates danger
                    if (CharAtOffset(1) == '>')
                    {
                        b.Data = 3;
                        SkipForward(1);
                    }
                }

                // Block quote followed by space
                if (IsLineSpace(CharAtOffset(1)))
                {
                    // Skip it and create quote block
                    SkipForward(2);
                    b.contentStart = position;
                    return BlockType.quote;
                }

                SkipForward(1);
                b.contentStart = position;
                return BlockType.quote;
            }

            // Horizontal rule - a line consisting of 3 or more '-', '_' or '*' with optional spaces and nothing else
            if (ch == '-' || ch == '_' || ch == '*')
            {
                int count = 0;
                while (!eol)
                {
                    if (current == ch)
                    {
                        count++;
                        SkipForward(1);
                        continue;
                    }

                    if (IsLineSpace(current))
                    {
                        SkipForward(1);
                        continue;
                    }

                    break;
                }

                if (eol && count >= 3)
                {
                    return m_markdown.UserBreaks ? BlockType.user_break : BlockType.hr;
                }

                // Rewind
                position = b.contentStart;
            }

            // Abbreviation definition?
            if (m_markdown.ExtraMode && ch == '*' && CharAtOffset(1) == '[')
            {
                SkipForward(2);
                SkipLinespace();

                Mark();
                while (!eol && current != ']')
                {
                    SkipForward(1);
                }

                var abbr = Extract().Trim();
                if (current == ']' && CharAtOffset(1) == ':' && !string.IsNullOrEmpty(abbr))
                {
                    SkipForward(2);
                    SkipLinespace();

                    Mark();

                    SkipToEol();

                    var title = Extract();

                    m_markdown.AddAbbreviation(abbr, title);

                    return BlockType.Blank;
                }

                position = b.contentStart;
            }

            // Unordered list
            if ((ch == '*' || ch == '+' || ch == '-') && IsLineSpace(CharAtOffset(1)))
            {
                // Skip it
                SkipForward(1);
                SkipLinespace();
                b.contentStart = position;
                return BlockType.ul_li;
            }

            // Definition
            if (ch == ':' && m_markdown.ExtraMode && IsLineSpace(CharAtOffset(1)))
            {
                SkipForward(1);
                SkipLinespace();
                b.contentStart = position;
                return BlockType.dd;
            }

            // Ordered list
            if (char.IsDigit(ch))
            {
                string startingDigit = ch.ToString();

                // Ordered list?  A line starting with one or more digits, followed by a '.' and a space or tab

                // Skip all digits
                SkipForward(1);
                while (char.IsDigit(current))
                {
                    startingDigit += current;
                    SkipForward(1);
                }

                if (SkipChar('.') && SkipLinespace())
                {
                    b.contentStart = position;
                    b.olStartValue = startingDigit;
                    return BlockType.ol_li;
                }

                position=b.contentStart;
            }

            // Reference link definition?
            if (ch == '[')
            {
                // Footnote definition?
                if (m_markdown.ExtraMode && CharAtOffset(1) == '^')
                {
                    var savepos = position;

                    SkipForward(2);

                    string id;
                    if (SkipFootnoteID(out id) && SkipChar(']') && SkipChar(':'))
                    {
                        SkipLinespace();
                        b.contentStart = position;
                        b.Data = id;
                        return BlockType.footnote;
                    }

                    position = savepos;
                }

                // Parse a link definition
                LinkDefinition l = LinkDefinition.ParseLinkDefinition(this, m_markdown.ExtraMode);
                if (l!=null)
                {
                    m_markdown.AddLinkDefinition(l);
                    return BlockType.Blank;
                }
                else
                {
                    return BlockType.p;
                }
            }

            // Nothing special
            return BlockType.p_span;
        }

        internal enum MarkdownInHtmlMode
        {
            NA,			// No markdown attribute on the tag
            Block,		// markdown=1 or markdown=block
            Span,		// markdown=1 or markdown=span
            Deep,		// markdown=deep - recursive block mode
            Off		    // Markdown="something else"
        }

        internal MarkdownInHtmlMode GetMarkdownMode(HtmlTag tag)
        {
            // Get the markdown attribute
            string strMarkdownMode;
            if (!m_markdown.ExtraMode || !tag.attributes.TryGetValue("markdown", out strMarkdownMode))
            {
                return m_bMarkdownInHtml ? MarkdownInHtmlMode.Deep : MarkdownInHtmlMode.NA;
            }

            // Remove it
            tag.attributes.Remove("markdown");

            // Parse mode
            if (strMarkdownMode == "1")
                return (tag.Flags & HtmlTagFlags.ContentAsSpan)!=0 ? MarkdownInHtmlMode.Span : MarkdownInHtmlMode.Block;

            if (strMarkdownMode == "block")
                return MarkdownInHtmlMode.Block;

            if (strMarkdownMode == "deep")
                return MarkdownInHtmlMode.Deep;

            if (strMarkdownMode == "span")
                return MarkdownInHtmlMode.Span;

            return MarkdownInHtmlMode.Off;
        }

        internal bool ProcessMarkdownEnabledHtml(Block b, HtmlTag openingTag, MarkdownInHtmlMode mode)
        {
            // Current position is just after the opening tag

            // Scan until we find matching closing tag
            int inner_pos = position;
            int depth = 1;
            bool bHasUnsafeContent = false;
            while (!eof)
            {
                // Find next angle bracket
                if (!Find('<'))
                    break;

                // Is it a html tag?
                int tagpos = position;
                HtmlTag tag = HtmlTag.Parse(this);
                if (tag == null)
                {
                    // Nope, skip it 
                    SkipForward(1);
                    continue;
                }

                // In markdown off mode, we need to check for unsafe tags
                if (m_markdown.SafeMode && mode == MarkdownInHtmlMode.Off && !bHasUnsafeContent)
                {
                    if (!tag.IsSafe())
                        bHasUnsafeContent = true;
                }

                // Ignore self closing tags
                if (tag.closed)
                    continue;

                // Same tag?
                if (tag.name == openingTag.name)
                {
                    if (tag.closing)
                    {
                        depth--;
                        if (depth == 0)
                        {
                            // End of tag?
                            SkipLinespace();
                            SkipEol();

                            b.BlockType = BlockType.HtmlTag;
                            b.Data = openingTag;
                            b.contentEnd = position;

                            switch (mode)
                            {
                                case MarkdownInHtmlMode.Span:
                                {
                                    var span = new Block {
                                        buf = input,
                                        BlockType = BlockType.span,
                                        contentStart = inner_pos,
                                        contentLen = tagpos - inner_pos
                                    };
                                    b.children = new List<Block> {span};
                                    break;
                                }

                                case MarkdownInHtmlMode.Block:
                                case MarkdownInHtmlMode.Deep:
                                {
                                    // Scan the internal content
                                    var bp = new BlockProcessor(m_markdown, mode == MarkdownInHtmlMode.Deep, BlockType.HtmlTag);
                                    b.children = bp.ScanLines(input, inner_pos, tagpos - inner_pos);
                                    break;
                                }

                                case MarkdownInHtmlMode.Off:
                                {
                                    if (bHasUnsafeContent)
                                    {
                                        b.BlockType = BlockType.unsafe_html;
                                        b.contentEnd = position;
                                    }
                                    else
                                    {
                                        var span = new Block {
                                            buf = input,
                                            BlockType = BlockType.html,
                                            contentStart = inner_pos,
                                            contentLen = tagpos - inner_pos
                                        };
                                        b.children = new List<Block> {span};
                                    }
                                    break;
                                }
                            }


                            return true;
                        }
                    }
                    else
                    {
                        depth++;
                    }
                }
            }

            // Missing closing tag(s).  
            return false;
        }

        // Scan from the current position to the end of the html section
        internal bool ScanHtml(Block b)
        {
            // Remember start of html
            int posStartPiece = this.position;

            // Parse a HTML tag
            HtmlTag openingTag = HtmlTag.Parse(this);
            if (openingTag == null)
                return false;

            // Closing tag?
            if (openingTag.closing)
                return false;

            // Safe mode?
            bool bHasUnsafeContent = m_markdown.SafeMode && !openingTag.IsSafe();

            HtmlTagFlags flags = openingTag.Flags;

            // Is it a block level tag?
            if ((flags & HtmlTagFlags.Block) == 0)
                return false;

            // Closed tag, hr or comment?
            if ((flags & HtmlTagFlags.NoClosing) != 0 || openingTag.closed)
            {
                SkipLinespace();
                SkipEol();

                b.contentEnd = position;
                b.BlockType = bHasUnsafeContent ? BlockType.unsafe_html : BlockType.html;
                return true;
            }

            // Can it also be an inline tag?
            if ((flags & HtmlTagFlags.Inline) != 0)
            {
                // Yes, opening tag must be on a line by itself
                SkipLinespace();
                if (!eol)
                    return false;
            }

            // Head block extraction?
            bool bHeadBlock = m_markdown.ExtractHeadBlocks && string.Compare(openingTag.name, "head", StringComparison.OrdinalIgnoreCase) == 0;
            int headStart = this.position;

            // Work out the markdown mode for this element
            if (!bHeadBlock && m_markdown.ExtraMode)
            {
                MarkdownInHtmlMode markdownMode = this.GetMarkdownMode(openingTag);
                if (markdownMode != MarkdownInHtmlMode.NA)
                {
                    return this.ProcessMarkdownEnabledHtml(b, openingTag, markdownMode);
                }
            }

            List<Block> childBlocks = null;

            // Now capture everything up to the closing tag and put it all in a single HTML block
            int depth = 1;

            while (!eof)
            {
                // Find next angle bracket
                if (!Find('<'))
                    break;

                // Save position of current tag
                int posStartCurrentTag = position;

                // Is it a html tag?
                HtmlTag tag = HtmlTag.Parse(this);
                if (tag == null)
                {
                    // Nope, skip it 
                    SkipForward(1);
                    continue;
                }

                // Safe mode checks
                if (m_markdown.SafeMode && !tag.IsSafe())
                    bHasUnsafeContent = true;

                // Ignore self closing tags
                if (tag.closed)
                    continue;

                // Markdown enabled content?
                if (!bHeadBlock && !tag.closing && m_markdown.ExtraMode && !bHasUnsafeContent)
                {
                    MarkdownInHtmlMode markdownMode = this.GetMarkdownMode(tag);
                    if (markdownMode != MarkdownInHtmlMode.NA)
                    {
                        var markdownBlock = new Block();
                        if (this.ProcessMarkdownEnabledHtml(markdownBlock, tag, markdownMode))
                        {
                            if (childBlocks==null)
                            {
                                childBlocks = new List<Block>();
                            }

                            // Create a block for everything before the markdown tag
                            if (posStartCurrentTag > posStartPiece)
                            {
                                var htmlBlock = new Block {
                                    buf = input,
                                    BlockType = BlockType.html,
                                    contentStart = posStartPiece,
                                    contentLen = posStartCurrentTag - posStartPiece
                                };
                                childBlocks.Add(htmlBlock);
                            }

                            // Add the markdown enabled child block
                            childBlocks.Add(markdownBlock);

                            // Remember start of the next piece
                            posStartPiece = position;

                            continue;
                        }
                    }
                }
                
                // Same tag?
                if (tag.name == openingTag.name)
                {
                    if (tag.closing)
                    {
                        depth--;
                        if (depth == 0)
                        {
                            // End of tag?
                            SkipLinespace();
                            SkipEol();

                            // If anything unsafe detected, just encode the whole block
                            if (bHasUnsafeContent)
                            {
                                b.BlockType = BlockType.unsafe_html;
                                b.contentEnd = position;
                                return true;
                            }

                            // Did we create any child blocks
                            if (childBlocks != null)
                            {
                                // Create a block for the remainder
                                if (position > posStartPiece)
                                {
                                    var htmlBlock = new Block {
                                        buf = input,
                                        BlockType = BlockType.html,
                                        contentStart = posStartPiece,
                                        contentLen = position - posStartPiece
                                    };
                                    childBlocks.Add(htmlBlock);
                                }

                                // Return a composite block
                                b.BlockType = BlockType.Composite;
                                b.contentEnd = position;
                                b.children = childBlocks;
                                return true;
                            }

                            // Extract the head block content
                            if (bHeadBlock)
                            {
                                var content = this.Substring(headStart, posStartCurrentTag - headStart);
                                m_markdown.HeadBlockContent = (m_markdown.HeadBlockContent ?? "") + content.Trim() + "\n";
                                b.BlockType = BlockType.html;
                                b.contentStart = position;
                                b.contentEnd = position;
                                b.lineStart = position;
                                return true;
                            }

                            // Straight html block
                            b.BlockType = BlockType.html;
                            b.contentEnd = position;
                            return true;
                        }
                    }
                    else
                    {
                        depth++;
                    }
                }
            }

            // Rewind to just after the tag
            return false;
        }

        /*
         * Spacing
         * 
         * 1-3 spaces - Promote to indented if more spaces than original item
         * 
         */

        /* 
         * BuildList - build a single <ol> or <ul> list
         */
        private Block BuildList(List<Block> lines)
        {
            // What sort of list are we dealing with
            BlockType listType = lines[0].BlockType;
            string startingDigit = lines[0].olStartValue;
            System.Diagnostics.Debug.Assert(listType == BlockType.ul_li || listType == BlockType.ol_li);

            // Preprocess
            // 1. Collapse all plain lines (ie: handle hardwrapped lines)
            // 2. Promote any unindented lines that have more leading space 
            //    than the original list item to indented, including leading 
            //    special chars
            int leadingSpace = lines[0].leadingSpaces;
            for (int i = 1; i < lines.Count; i++)
            {
                // Join plain paragraphs
                if ((lines[i].BlockType == BlockType.p) &&
                    (lines[i - 1].BlockType == BlockType.p || lines[i - 1].BlockType == BlockType.ul_li || lines[i - 1].BlockType==BlockType.ol_li))
                {
                    lines[i - 1].contentEnd = lines[i].contentEnd;
                    lines.RemoveAt(i);
                    i--;
                    continue;
                }

                if (lines[i].BlockType != BlockType.indent && lines[i].BlockType != BlockType.Blank)
                {
                    int thisLeadingSpace = lines[i].leadingSpaces;
                    if (thisLeadingSpace > leadingSpace)
                    {
                        // Change line to indented, including original leading chars 
                        // (eg: '* ', '>', '1.' etc...)
                        lines[i].BlockType = BlockType.indent;
                        int saveend = lines[i].contentEnd;
                        lines[i].contentStart = lines[i].lineStart + thisLeadingSpace;
                        lines[i].contentEnd = saveend;
                    }
                }
            }


            // Create the wrapping list item
            var list = new Block(listType == BlockType.ul_li ? BlockType.ul : BlockType.ol) {children = new List<Block>()};

            // Process all lines in the range		
            for (int i = 0; i < lines.Count; i++)
            {
                System.Diagnostics.Debug.Assert(lines[i].BlockType == BlockType.ul_li || lines[i].BlockType==BlockType.ol_li);

                // Find start of item, including leading blanks
                int start_of_li = i;
                while (start_of_li > 0 && lines[start_of_li - 1].BlockType == BlockType.Blank)
                    start_of_li--;

                // Find end of the item, including trailing blanks
                int end_of_li = i;
                while (end_of_li < lines.Count - 1 && lines[end_of_li + 1].BlockType != BlockType.ul_li && lines[end_of_li + 1].BlockType != BlockType.ol_li)
                    end_of_li++;

                // Is this a simple or complex list item?
                if (start_of_li == end_of_li)
                {
                    // It's a simple, single line item item
                    System.Diagnostics.Debug.Assert(start_of_li == i);
                    list.children.Add(new Block().CopyFrom(lines[i]));
                }
                else
                {
                    // Build a new string containing all child items
                    bool bAnyBlanks = false;
                    var sb = new StringBuilder();
                    for (int j = start_of_li; j <= end_of_li; j++)
                    {
                        var l = lines[j];
                        sb.Append(l.buf, l.contentStart, l.contentLen);
                        sb.Append('\n');

                        if (lines[j].BlockType == BlockType.Blank)
                        {
                            bAnyBlanks = true;
                        }
                    }

                    // Create the item and process child blocks
                    var item = new Block(BlockType.li)
                    {
                        children = new BlockProcessor(m_markdown, m_bMarkdownInHtml, listType).Process(sb.ToString())
                    };

                    // If no blank lines, change all contained paragraphs to plain text
                    if (!bAnyBlanks)
                    {
                        foreach (var child in item.children)
                        {
                            if (child.BlockType == BlockType.p)
                            {
                                child.BlockType = BlockType.span;
                            }
                        }
                    }

                    // Add the complex item
                    list.children.Add(item);
                }

                // Continue processing from end of li
                i = end_of_li;
            }

            lines.Clear();

            // Continue processing after this item
            list.olStartValue = startingDigit;
            return list;
        }

        /* 
         * BuildDefinition - build a single <dd> item
         */
        private Block BuildDefinition(List<Block> lines)
        {
            // Collapse all plain lines (ie: handle hardwrapped lines)
            for (int i = 1; i < lines.Count; i++)
            {
                // Join plain paragraphs
                if ((lines[i].BlockType == BlockType.p) &&
                    (lines[i - 1].BlockType == BlockType.p || lines[i - 1].BlockType == BlockType.dd))
                {
                    lines[i - 1].contentEnd = lines[i].contentEnd;
                    lines.RemoveAt(i);
                    i--;
                }
            }

            // Single line definition
            bool bPreceededByBlank=(bool)lines[0].Data;
            if (lines.Count==1 && !bPreceededByBlank)
            {
                var ret=lines[0];
                lines.Clear();
                return ret;
            }

            // Build a new string containing all child items
            var sb = new StringBuilder();
            for (int i = 0; i < lines.Count; i++)
            {
                var l = lines[i];
                sb.Append(l.buf, l.contentStart, l.contentLen);
                sb.Append('\n');
            }

            // Create the item and process child blocks
            var item = new Block {
                BlockType = BlockType.dd,
                children = new BlockProcessor(m_markdown, m_bMarkdownInHtml, BlockType.dd).Process(sb.ToString())
            };
            
            lines.Clear();

            // Continue processing after this item
            return item;
        }

        void BuildDefinitionLists(List<Block> blocks)
        {
            Block currentList = null;
            for (int i = 0; i < blocks.Count; i++)
            {
                switch (blocks[i].BlockType)
                {
                    case BlockType.dt:
                    case BlockType.dd:
                        if (currentList==null)
                        {
                            currentList = new Block {
                                BlockType = BlockType.dl,
                                children = new List<Block>()
                            };
                            blocks.Insert(i, currentList);
                            i++;
                        }

                        currentList.children.Add(blocks[i]);
                        blocks.RemoveAt(i);
                        i--;
                        break;

                    default:
                        currentList = null;
                        break;
                }
            }
        }

        private Block BuildFootnote(List<Block> lines)
        {
            // Collapse all plain lines (ie: handle hardwrapped lines)
            for (int i = 1; i < lines.Count; i++)
            {
                // Join plain paragraphs
                if ((lines[i].BlockType == BlockType.p) &&
                    (lines[i - 1].BlockType == BlockType.p || lines[i - 1].BlockType == BlockType.footnote))
                {
                    lines[i - 1].contentEnd = lines[i].contentEnd;
                    lines.RemoveAt(i);
                    i--;
                    continue;
                }
            }

            // Build a new string containing all child items
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < lines.Count; i++)
            {
                var l = lines[i];
                sb.Append(l.buf, l.contentStart, l.contentLen);
                sb.Append('\n');
            }

            // Create the item and process child blocks
            var item = new Block {
                BlockType = BlockType.footnote,
                Data = lines[0].Data,
                children = new BlockProcessor(m_markdown, m_bMarkdownInHtml, BlockType.footnote).Process(sb.ToString())
            };
            
            lines.Clear();

            // Continue processing after this item
            return item;
        }

        bool ProcessFencedCodeBlock(Block b)
        {
            char delim = current;

            // Extract the fence
            Mark();
            while (current == delim)
                SkipForward(1);
            string strFence = Extract();

            // Must be at least 3 long
            if (strFence.Length < 3)
                return false;

            // In extra mode, check for a trailing HTML ID
            if (m_markdown.ExtraMode && !m_markdown.SafeMode)
            {
                SkipToEol();

                int end=position;
                var attrs = Utils.CollectAttributes(input, b.contentStart, ref end);
                if (attrs != null)
                {
                    b.Data = attrs;
                    //position = end;
                }
            }

            // Rest of line must be blank
            SkipLinespace();
            if (!eol)
                return false;

            // Skip the eol and remember start of code
            SkipEol();
            int startCode = position;

            // Find the end fence
            if (!Find(strFence))
                return false;

            // Character before must be a eol char
            if (!IsLineEnd(CharAtOffset(-1)))
                return false;

            int endCode = position;

            // Skip the fence
            SkipForward(strFence.Length);

            // Whitespace allowed at end
            SkipLinespace();
            if (!eol)
                return false;

            // Create the code block
            b.BlockType = BlockType.codeblock;
            b.marker = strFence;
            b.children = new List<Block>();

            // Remove the trailing line end
            if (input[endCode - 1] == '\r' && input[endCode - 2] == '\n')
                endCode -= 2;
            else if (input[endCode - 1] == '\n' && input[endCode - 2] == '\r')
                endCode -= 2;
            else
                endCode--;

            // Create the child block with the entire content
            var child = new Block  {
                BlockType = BlockType.indent,
                buf = input,
                contentStart = startCode,
                contentEnd = endCode
            };
            b.children.Add(child);

            return true;
        }

        Markdown m_markdown;
        BlockType m_parentType;
        bool m_bMarkdownInHtml;
    }
}
