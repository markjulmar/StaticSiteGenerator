// 
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
    // Some block types are only used during block parsing, some
    // are only used during rendering and some are used during both
    public enum BlockType
    {
        Blank,			// blank line (parse only)
        h1,				// headings (render and parse)
        h2, 
        h3, 
        h4, 
        h5, 
        h6,
        post_h1,		// setext heading lines (parse only)
        post_h2,
        quote,			// block quote (render and parse)
        ol_li,			// list item in an ordered list	(render and parse)
        ul_li,			// list item in an unordered list (render and parse)
        p,				// paragraph 
        p_span,         // paragraph or plain line during parse
        indent,			// an indented line (parse only)
        hr,				// horizontal rule (render and parse)
        user_break,		// user break
        html,			// html content (render and parse)
        unsafe_html,	// unsafe html that should be encoded
        span,			// an undecorated span of text (used for simple list items 
                        //			where content is not wrapped in paragraph tags
        codeblock,		// a code block (render only)
        li,				// a list item (render only)
        ol,				// ordered list (render only)
        ul,				// unordered list (render only)
        HtmlTag,		// Data=(HtmlTag), children = content
        Composite,		// Just a list of child blocks
        table_spec,		// A table row specifier eg:  |---: | ---|	`data` = TableSpec reference
        dd,				// definition (render and parse)	`data` = bool true if blank line before
        dt,				// render only
        dl,				// render only
        footnote,		// footnote definition  eg: [^id]   `data` holds the footnote id
        p_footnote,		// paragraph with footnote return link append.  Return link string is in `data`.
    }

    public class Block
    {
        internal Block()
        {
        }

        internal Block(BlockType type)
        {
            BlockType = type;
        }

        public BlockType BlockType;
        public object Data;
        public string marker;
        public string olStartValue;

        public string Content
        {
            get
            {
                switch (BlockType)
                {
                    case BlockType.codeblock:
                        StringBuilder s = new StringBuilder();
                        foreach (var line in children)
                        {
                            s.Append(line.Content);
                            s.Append('\n');
                        }
                        return s.ToString();
                }


                return buf == null ? null : (contentStart == -1 ? buf : buf.Substring(contentStart, contentLen));
            }
        }

        public int LineStart => lineStart == 0 ? contentStart : lineStart;

        internal void RenderChildren(Markdown m, StringBuilder b)
        {
            foreach (var block in children)
            {
                block.Render(m, b);
            }
        }

        internal void RenderChildrenPlain(Markdown m, StringBuilder b)
        {
            foreach (var block in children)
            {
                block.RenderPlain(m, b);
            }
        }

        internal SpecialAttributes GetSpecialAttributes(Markdown m)
        {
            // Already resolved?
            if (Data is SpecialAttributes)
                return (SpecialAttributes)this.Data;

            // Approach 1 - PHP Markdown Extra style header id
            int end=contentEnd;
            var attrs = Utils.CollectAttributes(buf, contentStart, ref end);
            if (attrs != null)
            {
                contentEnd = end;
            }
            else
            {
                // Approach 2 - pandoc style header id
                string id = m.MakeUniqueHeaderID(buf, contentStart, contentLen);
                if (!string.IsNullOrEmpty(id)) {
                    attrs = new SpecialAttributes() { Id = id };
                }
            }

            this.Data = attrs;
            return attrs;
        }

        internal void Render(Markdown m, StringBuilder b)
        {
            switch (BlockType)
            {
                case BlockType.Blank:
                    return;

                case BlockType.p:
                    new SpanFormatter(m).FormatParagraph(b, buf, contentStart, contentLen);
                    break;

                case BlockType.span:
                    new SpanFormatter(m).Format(b, buf, contentStart, contentLen);
                    b.Append("\n");
                    break;

                case BlockType.h1:
                case BlockType.h2:
                case BlockType.h3:
                case BlockType.h4:
                case BlockType.h5:
                case BlockType.h6:
                    if (m.ExtraMode && !m.SafeMode)
                    {
                        b.Append("<");
                        b.Append(BlockType.ToString());
                        var attrs = GetSpecialAttributes(m);
                        if (attrs != null)
                        {
                            b.Append(attrs);                           
                        }
                        b.Append(">");
                    }
                    else
                    {
                        b.Append("<");
                        b.Append(BlockType.ToString());
                        b.Append(">");
                    }
                    new SpanFormatter(m).Format(b, buf, contentStart, contentLen);
                    b.Append("</");
                    b.Append(BlockType.ToString());
                    b.Append(">\n");
                    break;

                case BlockType.hr:
                    b.Append("<hr />\n");
                    return;

                case BlockType.user_break:
                    return;

                case BlockType.ol_li:
                case BlockType.ul_li:
                    b.Append(m.EmitTag?.Invoke(this, false) ?? "<li>");
                    new SpanFormatter(m).Format(b, buf, contentStart, contentLen);
                    b.Append(m.EmitTag?.Invoke(this, true) ?? "</li>\n");
                    break;

                case BlockType.dd:
                    b.Append(m.EmitTag?.Invoke(this, false) ?? "<dd>");
                    if (children != null)
                    {
                        b.Append("\n");
                        RenderChildren(m, b);
                    }
                    else
                        new SpanFormatter(m).Format(b, buf, contentStart, contentLen);
                    b.Append(m.EmitTag?.Invoke(this, true) ?? "</dd>\n");
                    break;

                case BlockType.dt:
                {
                    if (children == null)
                    {
                        foreach (var l in Content.Split('\n'))
                        {
                            b.Append("<dt>");
                            new SpanFormatter(m).Format(b, l.Trim());
                            b.Append("</dt>\n");
                        }
                    }
                    else
                    {
                        b.Append("<dt>\n");
                        RenderChildren(m, b);
                        b.Append("</dt>\n");
                    }
                    break;
                }

                case BlockType.dl:
                    b.Append(m.EmitTag?.Invoke(this, false) ?? "<dl>\n");
                    RenderChildren(m, b);
                    b.Append(m.EmitTag?.Invoke(this, true) ?? "</dl>\n");
                    return;

                case BlockType.html:
                    b.Append(buf, contentStart, contentLen);
                    return;

                case BlockType.unsafe_html:
                    m.HtmlEncode(b, buf, contentStart, contentLen);
                    return;

                case BlockType.codeblock:
                    if (m.FormatCodeBlock != null)
                    {
                        var attrs = GetSpecialAttributes(m);
                        var sb = new StringBuilder();
                        foreach (var line in children)
                        {
                            m.HtmlEncodeAndConvertTabsToSpaces(sb, line.buf, line.contentStart, line.contentLen);
                            sb.Append("\n");
                        }
                        b.Append(m.FormatCodeBlock(m, this.marker, attrs, sb.ToString()));
                    }
                    else
                    {
                        if (m.ExtraMode && !m.SafeMode)
                        {
                            b.Append("<pre");
                            var attrs = GetSpecialAttributes(m);
                            if (attrs != null)
                                b.Append(attrs);
                            b.Append(">");
                        }
                        else
                        {
                            b.Append("<pre>");
                        }

                        b.Append("<code>");
                        foreach (var line in children)
                        {
                            m.HtmlEncodeAndConvertTabsToSpaces(b, line.buf, line.contentStart, line.contentLen);
                            b.Append("\n");
                        }
                        b.Append("</code></pre>\n\n");
                    }
                    return;

                case BlockType.quote:
                    b.Append(m.EmitTag?.Invoke(this,false) ?? "<blockquote>\n");
                    RenderChildren(m, b);
                    b.Append(m.EmitTag?.Invoke(this, true) ?? "</blockquote>\n");
                    return;

                case BlockType.li:
                    b.Append(m.EmitTag?.Invoke(this, false) ?? "<li>\n");
                    RenderChildren(m, b);
                    b.Append(m.EmitTag?.Invoke(this, true) ?? "</li>\n");
                    return;

                case BlockType.ol:
                    if (olStartValue != null && olStartValue != "1")
                    {
                        b.Append("<ol start=\"" + olStartValue + "\">\n");
                    }
                    else
                    {
                        b.Append("<ol>\n");
                    }
                    RenderChildren(m, b);
                    b.Append("</ol>\n");
                    return;

                case BlockType.ul:
                    b.Append("<ul>\n");
                    RenderChildren(m, b);
                    b.Append("</ul>\n");
                    return;

                case BlockType.HtmlTag:
                    var tag = (HtmlTag)Data;

                    // Prepare special tags
                    var name=tag.name.ToLowerInvariant();
                    if (name == "a")
                    {
                        m.OnPrepareLink(tag);
                    }
                    else if (name == "img")
                    {
                        m.OnPrepareImage(tag, m.RenderingTitledImage);
                    }

                    tag.RenderOpening(b);
                    b.Append("\n");
                    RenderChildren(m, b);
                    tag.RenderClosing(b);
                    b.Append("\n");
                    return;

                case BlockType.Composite:
                case BlockType.footnote:
                    RenderChildren(m, b);
                    return;

                case BlockType.table_spec:
                    ((TableSpec)Data).Render(m, b);
                    break;

                case BlockType.p_footnote:
                    b.Append("<p>");
                    if (contentLen > 0)
                    {
                        new SpanFormatter(m).Format(b, buf, contentStart, contentLen);
                        b.Append("&nbsp;");
                    }
                    b.Append((string)Data);
                    b.Append("</p>\n");
                    break;

                default:
                    b.Append("<" + BlockType.ToString() + ">");
                    new SpanFormatter(m).Format(b, buf, contentStart, contentLen);
                    b.Append("</");
                    b.Append(BlockType.ToString());
                    b.Append(">\n");
                    break;
            }
        }

        internal void RenderPlain(Markdown m, StringBuilder b)
        {
            switch (BlockType)
            {
                case BlockType.Blank:
                    return;

                case BlockType.p:
                case BlockType.span:
                    new SpanFormatter(m).FormatPlain(b, buf, contentStart, contentLen);
                    b.Append(" ");
                    break;

                case BlockType.h1:
                case BlockType.h2:
                case BlockType.h3:
                case BlockType.h4:
                case BlockType.h5:
                case BlockType.h6:
                    new SpanFormatter(m).FormatPlain(b, buf, contentStart, contentLen);
                    b.Append(" - ");
                    break;


                case BlockType.ol_li:
                case BlockType.ul_li:
                    b.Append("* ");
                    new SpanFormatter(m).FormatPlain(b, buf, contentStart, contentLen);
                    b.Append(" ");
                    break;

                case BlockType.dd:
                    if (children != null)
                    {
                        b.Append("\n");
                        RenderChildrenPlain(m, b);
                    }
                    else
                        new SpanFormatter(m).FormatPlain(b, buf, contentStart, contentLen);
                    break;

                case BlockType.dt:
                    {
                        if (children == null)
                        {
                            var f = new SpanFormatter(m);
                            foreach (var l in Content.Split('\n'))
                            {
                                var str = l.Trim();
                                f.FormatPlain(b, str, 0, str.Length);
                            }
                        }
                        else
                        {
                            RenderChildrenPlain(m, b);
                        }
                        break;
                    }

                case BlockType.dl:
                    RenderChildrenPlain(m, b);
                    return;

                case BlockType.codeblock:
                    foreach (var line in children)
                    {
                        b.Append(line.buf, line.contentStart, line.contentLen);
                        b.Append(" ");
                    }
                    return;

                case BlockType.quote:
                case BlockType.li:
                case BlockType.ol:
                case BlockType.ul:
                case BlockType.HtmlTag:
                    RenderChildrenPlain(m, b);
                    return;
            }
        }

        public void RevertToPlain()
        {
            BlockType = BlockType.p;
            contentStart = lineStart;
            contentLen = lineLen;
        }

        public int contentEnd
        {
            get
            {
                return contentStart + contentLen;
            }
            set
            {
                contentLen = value - contentStart;
            }
        }

        // Count the leading spaces on a block
        // Used by list item evaluation to determine indent levels
        // irrespective of indent line type.
        public int leadingSpaces
        {
            get
            {
                int count = 0;
                for (int i = lineStart; i < lineStart + lineLen; i++)
                {
                    if (buf[i] == ' ')
                    {
                        count++;
                    }
                    else
                    {
                        break;
                    }
                }
                return count;
            }
        }

        public override string ToString()
        {
            string c = Content;
            return BlockType + " - " + (c ?? "<null>");
        }

        public Block CopyFrom(Block other)
        {
            BlockType = other.BlockType;
            buf = other.buf;
            contentStart = other.contentStart;
            contentLen = other.contentLen;
            lineStart = other.lineStart;
            lineLen = other.lineLen;
            return this;
        }

        internal string buf;
        internal int contentStart;
        internal int contentLen;
        internal int lineStart;
        internal int lineLen;
        internal List<Block> children;
    }
}
