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

using System;
using System.Collections.Generic;
using System.Text;

namespace XamU.SGL.Extensions.MarkdownDeep
{
    internal class SpanFormatter : StringScanner
    {
        // Constructor
        // A reference to the owning markdown object is passed incase
        // we need to check for formatting options
        public SpanFormatter(Markdown m)
        {
            m_Markdown = m;
        }

        internal void FormatParagraph(StringBuilder dest, string str, int start, int len)
        {
            // Parse the string into a list of tokens
            Tokenize(str, start, len);

            // Titled image?
            if (m_Tokens.Count == 1 
                && m_Markdown.HtmlClassTitledImages != null 
                && m_Tokens[0].type == TokenType.img)
            {
                // Grab the link info
                LinkInfo li = (LinkInfo)m_Tokens[0].data;

                // Render the figure opening
                // Xamu: changed to figure
                dest.Append("<figure class=\"");
                dest.Append(m_Markdown.HtmlClassTitledImages);
                dest.Append("\">\n");

                // Render the img
                m_Markdown.RenderingTitledImage = true;
                Render(dest, str);
                m_Markdown.RenderingTitledImage = false;
                dest.Append("\n");

                // Render the title
                if (!string.IsNullOrEmpty(li.LinkDefinition.title))
                {
                    if (m_Markdown.HtmlClassFigureCaption != null)
                        dest.AppendFormat("<figcaption class=\"{0}\">", m_Markdown.HtmlClassFigureCaption);
                    else
                        dest.Append("<figcaption>");
                    Utils.SmartHtmlEncodeAmpsAndAngles(dest, li.LinkDefinition.title);
                    dest.Append("</figcaption>\n");
                }
                
                dest.Append("</figure>\n");
            }
            else
            {
                bool addParagraphTags = m_Tokens[0].type == TokenType.Text;

                // Render the paragraph
                if (addParagraphTags)
                {
                    dest.Append("<p>");
                    Render(dest, str);
                    dest.Append("</p>\n");
                }
                else
                {
                    Render(dest, str);
                    dest.Append("\n");
                }
            }
        }

        internal void Format(StringBuilder dest, string str)
        {
            Format(dest, str, 0, str.Length);
        }

        // Format a range in an input string and write it to the destination string builder.
        internal void Format(StringBuilder dest, string str, int start, int len)
        {
            // Parse the string into a list of tokens
            Tokenize(str, start, len);

            // Render all tokens
            Render(dest, str);
        }

        internal void FormatPlain(StringBuilder dest, string str, int start, int len)
        {
            // Parse the string into a list of tokens
            Tokenize(str, start, len);

            // Render all tokens
            RenderPlain(dest, str);
        }

        // Format a string and return it as a new string
        // (used in formatting the text of links)
        internal string Format(string str)
        {
            var dest = new StringBuilder();
            Format(dest, str, 0, str.Length);
            return dest.ToString();
        }

        internal string MakeID(string str)
        {
            return MakeID(str, 0, str.Length);
        }

        internal string MakeID(string str, int start, int len)
        {
            // Parse the string into a list of tokens
            Tokenize(str, start, len);
    
            var sb = new StringBuilder();

            foreach (var t in m_Tokens)
            {
                switch (t.type)
                {
                    case TokenType.Text:
                        sb.Append(str, t.startOffset, t.length);
                        break;

                    case TokenType.link:
                        LinkInfo li = (LinkInfo)t.data;
                        sb.Append(li.LinkText);
                        break;
                }
            }

            // Now clean it using the same rules as pandoc
            base.Reset(sb.ToString());

            // Skip everything up to the first letter
            while (!eof)
            {
                if (Char.IsLetter(current))
                    break;
                SkipForward(1);
            }

            // Process all characters
            sb.Length = 0;
            while (!eof)
            {
                char ch = current;
                if (char.IsLetterOrDigit(ch) || ch=='_' || ch=='-' || ch=='.')
                    sb.Append(Char.ToLower(ch));
                else if (ch == ' ')
                    sb.Append("-");
                else if (IsLineEnd(ch))
                {
                    sb.Append("-");
                    SkipEol();
                    continue;
                }

                SkipForward(1);
            }

            return sb.ToString();
        }

        // Render a list of tokens to a destination string builder.
        private void Render(StringBuilder sb, string str)
        {
            foreach (Token t in m_Tokens)
            {
                switch (t.type)
                {
                    case TokenType.Text:
                        // Append encoded text
                        m_Markdown.HtmlEncode(sb, str, t.startOffset, t.length);
                        break;

                    case TokenType.HtmlTag:
                        // Append html as is
                        Utils.SmartHtmlEncodeAmps(sb, str, t.startOffset, t.length);
                        break;

                    case TokenType.Html:
                    case TokenType.opening_mark:
                    case TokenType.closing_mark:
                    case TokenType.internal_mark:
                        // Append html as is
                        sb.Append(str, t.startOffset, t.length);
                        break;

                    case TokenType.br:
                        sb.Append("<br />\n");
                        break;

                    case TokenType.open_em:
                        sb.Append("<em>");
                        break;

                    case TokenType.close_em:
                        sb.Append("</em>");
                        break;

                    case TokenType.open_strong:
                        sb.Append("<strong>");
                        break;

                    case TokenType.close_strong:
                        sb.Append("</strong>");
                        break;

                    // XamU: mark support
                    case TokenType.mark_span:
                        sb.Append("<mark>");
                        m_Markdown.HtmlEncode(sb, str, t.startOffset, t.length);
                        sb.Append("</mark>");
                        break;

                    case TokenType.code_span:
                        sb.Append("<code>");
                        m_Markdown.HtmlEncode(sb, str, t.startOffset, t.length);
                        sb.Append("</code>");
                        break;

                    case TokenType.link:
                    {
                        LinkInfo li = (LinkInfo)t.data;
                        var sf = new SpanFormatter(m_Markdown) {DisableLinks = true};
                        li.LinkDefinition.RenderLink(m_Markdown, sb, sf.Format(li.LinkText), li.Attributes);
                        break;
                    }

                    case TokenType.img:
                    {
                        LinkInfo li = (LinkInfo)t.data;
                        li.LinkDefinition.RenderImg(m_Markdown, sb, li.LinkText, li.Attributes);
                        break;
                    }

                    case TokenType.footnote:
                    {
                        FootnoteReference r=(FootnoteReference)t.data;
                        sb.Append("<sup id=\"fnref:");
                        sb.Append(r.id);
                        sb.Append("\"><a href=\"#fn:");
                        sb.Append(r.id);
                        sb.Append("\" rel=\"footnote\">");
                        sb.Append(r.index + 1);
                        sb.Append("</a></sup>");
                        break;
                    }

                    case TokenType.abbreviation:
                    {
                        Abbreviation a = (Abbreviation)t.data;
                        sb.Append("<abbr");
                        if (!string.IsNullOrEmpty(a.Title))
                        {
                            sb.Append(" title=\"");
                            m_Markdown.HtmlEncode(sb, a.Title, 0, a.Title.Length);
                            sb.Append("\"");
                        }
                        sb.Append(">");
                        m_Markdown.HtmlEncode(sb, a.Abbr, 0, a.Abbr.Length);
                        sb.Append("</abbr>");
                        break;
                    }
                }
            }
        }

        // Render a list of tokens to a destinatino string builder.
        private void RenderPlain(StringBuilder sb, string str)
        {
            foreach (Token t in m_Tokens)
            {
                switch (t.type)
                {
                    case TokenType.Text:
                        sb.Append(str, t.startOffset, t.length);
                        break;

                    case TokenType.HtmlTag:
                        break;

                    case TokenType.Html:
                    case TokenType.opening_mark:
                    case TokenType.closing_mark:
                    case TokenType.internal_mark:
                        break;

                    case TokenType.br:
                        break;

                    case TokenType.open_em:
                    case TokenType.close_em:
                    case TokenType.open_strong:
                    case TokenType.close_strong:
                        break;

                    case TokenType.mark_span:
                    case TokenType.code_span:
                        sb.Append(str, t.startOffset, t.length);
                        break;

                    case TokenType.link:
                        {
                            LinkInfo li = (LinkInfo)t.data;
                            sb.Append(li.LinkText);
                            break;
                        }

                    case TokenType.img:
                        {
                            LinkInfo li = (LinkInfo)t.data;
                            sb.Append(li.LinkText);
                            break;
                        }

                    case TokenType.footnote:
                    case TokenType.abbreviation:
                        break;
                }
            }
        }

        // Scan the input string, creating tokens for anything special 
        public void Tokenize(string str, int start, int len)
        {
            // Prepare
            base.Reset(str, start, len);
            m_Tokens.Clear();

            List<Token> emphasis_marks = null;

            List<Abbreviation> Abbreviations = m_Markdown.AbbreviationList;

            bool ExtraMode = m_Markdown.ExtraMode;

            // Scan string
            int start_text_token = position;
            while (!eof)
            {
                int end_text_token=position;

                // Work out token
                Token token = null;
                switch (current)
                {
                    case '*':
                    case '_':

                        // Create emphasis mark
                        token = CreateEmphasisMark();

                        if (token != null)
                        {
                            // Store marks in a separate list the we'll resolve later
                            switch (token.type)
                            {
                                case TokenType.internal_mark:
                                case TokenType.opening_mark:
                                case TokenType.closing_mark:
                                    if (emphasis_marks == null)
                                    {
                                        emphasis_marks = new List<Token>();
                                    }
                                    emphasis_marks.Add(token);
                                    break;
                            }
                        }
                        break;
                    // XamU: mark tags
                    case '=':
                        token = ProcessMarkSpan();
                        break;

                    case '`':
                        token = ProcessCodeSpan();
                        break;

                    case '[':
                    case '!':
                    {
                        // Process link reference
                        int linkpos = position;
                        token = ProcessLinkOrImageOrFootnote();

                        // Rewind if invalid syntax
                        // (the '[' or '!' will be treated as a regular character and processed below)
                        if (token == null)
                            position = linkpos;
                        break;
                    }

                    case '<':
                    {
                        // Is it a valid html tag?
                        int save = position;
                        HtmlTag tag = HtmlTag.Parse(this);
                        if (tag != null)
                        {
                            if (!m_Markdown.SafeMode || tag.IsSafe())
                            {
                                // Yes, create a token for it
                                token = new Token(TokenType.HtmlTag, save, position - save);
                                if (m_Markdown.GithubStyleLineBreaks && !tag.closing) {
                                    if (current == '\n') {
                                        position++;
                                    }                                    
                                }
                            }
                            else
                            {
                                // No, rewrite and encode it
                                position = save;
                            }
                        }
                        else
                        {
                            // No, rewind and check if it's a valid autolink eg: <google.com>
                            position = save;
                            token = ProcessAutoLink();

                            if (token == null)
                                position = save;
                        }
                        break;
                    }

                    case '&':
                    {
                        // Is it a valid html entity
                        int save=position;
                        string unused=null;
                        if (SkipHtmlEntity(ref unused))
                        {
                            // Yes, create a token for it
                            token = new Token(TokenType.Html, save, position - save);
                        }

                        break;
                    }

                    case ' ':
                    {
                        // Check for double space at end of a line
                        if (CharAtOffset(1)==' ' && IsLineEnd(CharAtOffset(2)))
                        {
                            // Yes, skip it
                            SkipForward(2);

                            // Don't put br's at the end of a paragraph
                            if (!m_Markdown.GithubStyleLineBreaks && !eof)
                            {
                                SkipEol();
                                token = new Token(TokenType.br, end_text_token, 0);
                            }
                        }
                        break;
                    }

                    case '\n':
                    {
                        if (m_Markdown.GithubStyleLineBreaks)
                        {
                            SkipEol();
                            token = new Token(TokenType.br, end_text_token, 0);
                        }
                        break;
                    }

                    case '\\':
                    {
                        // Check followed by an escapable character
                        if (Utils.IsEscapableChar(CharAtOffset(1), ExtraMode))
                        {
                            token = new Token(TokenType.Text, position + 1, 1);
                            SkipForward(2);
                        }
                        break;
                    }
                }

                // Look for abbreviations.
                if (token == null && Abbreviations!=null && !Char.IsLetterOrDigit(CharAtOffset(-1)))
                {
                    var savepos = position;
                    foreach (var abbr in Abbreviations)
                    {
                        if (SkipString(abbr.Abbr) && !Char.IsLetterOrDigit(current))
                        {
                            token = new Token(TokenType.abbreviation, abbr);
                            break;
                        }

                        position = savepos;
                    }

                }

                // If token found, append any preceeding text and the new token to the token list
                if (token!=null)
                {
                    // Create a token for everything up to the special character
                    if (end_text_token > start_text_token)
                    {
                        m_Tokens.Add(new Token(TokenType.Text, start_text_token, end_text_token-start_text_token));
                    }

                    // Add the new token
                    m_Tokens.Add(token);

                    // Remember where the next text token starts
                    start_text_token=position;
                }
                else
                {
                    // Skip a single character and keep looking
                    SkipForward(1);
                }
            }

            // Append a token for any trailing text after the last token.
            if (position > start_text_token)
            {
                m_Tokens.Add(new Token(TokenType.Text, start_text_token, position-start_text_token));
            }

            // Do we need to resolve and emphasis marks?
            if (emphasis_marks != null)
            {
                ResolveEmphasisMarks(m_Tokens, emphasis_marks);
            }

            // Done!
            return;
        }

        private Token ProcessMarkSpan()
        {
            int start = position;

            // Count leading ticks
            int markCount = 0;
            while (SkipChar('='))
            {
                markCount++;
            }

            // Skip optional leading space...
            SkipWhitespace();

            // End?
            if (eof)
                return new Token(TokenType.Text, start, position - start);

            int startofmark = position;

            // Find closing ticks
            if (!Find(Substring(start, markCount)))
                return new Token(TokenType.Text, start, position - start);

            // Save end position before backing up over trailing whitespace
            int endpos = position + markCount;
            while (char.IsWhiteSpace(CharAtOffset(-1)))
                SkipForward(-1);

            // Create the token, move back to the end and we're done
            var ret = new Token(TokenType.mark_span, startofmark, position - startofmark);
            position = endpos;
            return ret;
        }

        static bool IsEmphasisChar(char ch)
        {
            return ch == '_' || ch == '*';
        }

        /*
         * Resolving emphasis tokens is a two part process
         * 
         * 1. Find all valid sequences of * and _ and create `mark` tokens for them
         *		this is done by CreateEmphasisMarks during the initial character scan
         *		done by Tokenize
         *		
         * 2. Looks at all these emphasis marks and tries to pair them up
         *		to make the actual <em> and <strong> tokens
         *		
         * Any unresolved emphasis marks are rendered unaltered as * or _
         */

        // Create emphasis mark for sequences of '*' and '_' (part 1)
        public Token CreateEmphasisMark()
        {
            // Capture current state
            char ch = current;
            char altch = ch == '*' ? '_' : '*';
            int savepos = position;

            // Check for a consecutive sequence of just '_' and '*'
            if (bof || char.IsWhiteSpace(CharAtOffset(-1)))
            {
                while (IsEmphasisChar(current))
                    SkipForward(1);

                if (eof || char.IsWhiteSpace(current))
                {
                    return new Token(TokenType.Html, savepos, position - savepos);
                }

                // Rewind
                position = savepos;
            }

            // Scan backwards and see if we have space before
            while (IsEmphasisChar(CharAtOffset(-1)))
                SkipForward(-1);
            bool bSpaceBefore = bof || char.IsWhiteSpace(CharAtOffset(-1));
            position = savepos;

            // Count how many matching emphasis characters
            while (current == ch)
            {
                SkipForward(1);
            }
            int count=position-savepos;

            // Scan forwards and see if we have space after
            while (IsEmphasisChar(CharAtOffset(1)))
                SkipForward(1);
            bool bSpaceAfter = eof || char.IsWhiteSpace(current);
            position = savepos + count;

            // This should have been stopped by check above
            System.Diagnostics.Debug.Assert(!bSpaceBefore || !bSpaceAfter);

            if (bSpaceBefore)
            {
                return new Token(TokenType.opening_mark, savepos, position - savepos);
            }

            if (bSpaceAfter)
            {
                return new Token(TokenType.closing_mark, savepos, position - savepos);
            }

            if (m_Markdown.ExtraMode && ch == '_' && (Char.IsLetterOrDigit(current)))
                return null;

            return new Token(TokenType.internal_mark, savepos, position - savepos);
        }

        // Split mark token
        public Token SplitMarkToken(List<Token> tokens, List<Token> marks, Token token, int position)
        {
            // Create the new rhs token
            Token tokenRhs = new Token(token.type, token.startOffset + position, token.length - position);

            // Adjust down the length of this token
            token.length = position;

            // Insert the new token into each of the parent collections
            marks.Insert(marks.IndexOf(token) + 1, tokenRhs);
            tokens.Insert(tokens.IndexOf(token) + 1, tokenRhs);

            // Return the new token
            return tokenRhs;
        }

        // Resolve emphasis marks (part 2)
        public void ResolveEmphasisMarks(List<Token> tokens, List<Token> marks)
        {
            bool bContinue = true;
            while (bContinue)
            {
                bContinue = false;
                for (int i = 0; i < marks.Count; i++)
                {
                    // Get the next opening or internal mark
                    Token opening_mark = marks[i];
                    if (opening_mark.type != TokenType.opening_mark && opening_mark.type != TokenType.internal_mark)
                        continue;

                    // Look for a matching closing mark
                    for (int j = i + 1; j < marks.Count; j++)
                    {
                        // Get the next closing or internal mark
                        Token closing_mark = marks[j];
                        if (closing_mark.type != TokenType.closing_mark && closing_mark.type != TokenType.internal_mark)
                            break;

                        // Ignore if different type (ie: `*` vs `_`)
                        if (input[opening_mark.startOffset] != input[closing_mark.startOffset])
                            continue;

                        // strong or em?
                        int style = Math.Min(opening_mark.length, closing_mark.length);

                        // Triple or more on both ends?
                        if (style >= 3)
                        {
                            style = (style % 2)==1 ? 1 : 2;
                        }

                        // Split the opening mark, keeping the RHS
                        if (opening_mark.length > style)
                        {
                            opening_mark = SplitMarkToken(tokens, marks, opening_mark, opening_mark.length - style);
                            i--;
                        }

                        // Split the closing mark, keeping the LHS
                        if (closing_mark.length > style)
                        {
                            SplitMarkToken(tokens, marks, closing_mark, style);
                        }

                        // Connect them
                        opening_mark.type = style == 1 ? TokenType.open_em : TokenType.open_strong;
                        closing_mark.type = style == 1 ? TokenType.close_em : TokenType.close_strong;

                        // Remove the matched marks
                        marks.Remove(opening_mark);
                        marks.Remove(closing_mark);
                        bContinue = true;

                        break;
                    }
                }
            }
        }

        // Resolve emphasis marks (part 2)
        public void ResolveEmphasisMarks_classic(List<Token> tokens, List<Token> marks)
        {
            // First pass, do <strong>
            for (int i = 0; i < marks.Count; i++)
            { 
                // Get the next opening or internal mark
                Token opening_mark=marks[i];
                if (opening_mark.type!=TokenType.opening_mark && opening_mark.type!=TokenType.internal_mark)
                    continue;
                if (opening_mark.length < 2)
                    continue;

                // Look for a matching closing mark
                for (int j = i + 1; j < marks.Count; j++)
                {
                    // Get the next closing or internal mark
                    Token closing_mark = marks[j];
                    if (closing_mark.type != TokenType.closing_mark && closing_mark.type!=TokenType.internal_mark)
                        continue;

                    // Ignore if different type (ie: `*` vs `_`)
                    if (input[opening_mark.startOffset] != input[closing_mark.startOffset])
                        continue;

                    // Must be at least two
                    if (closing_mark.length < 2)
                        continue;

                    // Split the opening mark, keeping the LHS
                    if (opening_mark.length > 2)
                    {
                        SplitMarkToken(tokens, marks, opening_mark, 2);
                    }

                    // Split the closing mark, keeping the RHS
                    if (closing_mark.length > 2)
                    {
                        closing_mark=SplitMarkToken(tokens, marks, closing_mark, closing_mark.length-2);
                    }

                    // Connect them
                    opening_mark.type = TokenType.open_strong;
                    closing_mark.type = TokenType.close_strong;

                    // Continue after the closing mark
                    i = marks.IndexOf(closing_mark);
                    break;
                }
            }

            // Second pass, do <em>
            for (int i = 0; i < marks.Count; i++)
            {
                // Get the next opening or internal mark
                Token opening_mark = marks[i];
                if (opening_mark.type != TokenType.opening_mark && opening_mark.type != TokenType.internal_mark)
                    continue;

                // Look for a matching closing mark
                for (int j = i + 1; j < marks.Count; j++)
                {
                    // Get the next closing or internal mark
                    Token closing_mark = marks[j];
                    if (closing_mark.type != TokenType.closing_mark && closing_mark.type != TokenType.internal_mark)
                        continue;

                    // Ignore if different type (ie: `*` vs `_`)
                    if (input[opening_mark.startOffset] != input[closing_mark.startOffset])
                        continue;

                    // Split the opening mark, keeping the LHS
                    if (opening_mark.length > 1)
                    {
                        SplitMarkToken(tokens, marks, opening_mark, 1);
                    }

                    // Split the closing mark, keeping the RHS
                    if (closing_mark.length > 1)
                    {
                        closing_mark = SplitMarkToken(tokens, marks, closing_mark, closing_mark.length - 1);
                    }

                    // Connect them
                    opening_mark.type = TokenType.open_em;
                    closing_mark.type = TokenType.close_em;

                    // Continue after the closing mark
                    i = marks.IndexOf(closing_mark);
                    break;
                }
            }
        }

        // Process auto links eg: <google.com>
        Token ProcessAutoLink()
        {
            if (DisableLinks)
                return null;

            // Skip the angle bracket and remember the start
            SkipForward(1);
            Mark();

            bool ExtraMode = m_Markdown.ExtraMode;

            // Allow anything up to the closing angle, watch for escapable characters
            while (!eof)
            {
                char ch = current;

                // No whitespace allowed
                if (char.IsWhiteSpace(ch))
                    break;

                // End found?
                if (ch == '>')
                {
                    string url = Utils.UnescapeString(Extract(), ExtraMode);

                    LinkInfo li = null;
                    if (Utils.IsEmailAddress(url))
                    {
                        string link_text;
                        if (url.StartsWith("mailto:"))
                        {
                            link_text = url.Substring(7);
                        }
                        else
                        {
                            link_text = url;
                            url = "mailto:" + url;
                        }

                        li = new LinkInfo(new LinkDefinition("auto", url, null), link_text);
                    }
                    else if (Utils.IsWebAddress(url))
                    {
                        li=new LinkInfo(new LinkDefinition("auto", url, null), url);
                    }

                    if (li!=null)
                    {
                        SkipForward(1);
                        return new Token(TokenType.link, li);
                    }

                    return null;
                }

                this.SkipEscapableChar(ExtraMode);
            }

            // Didn't work
            return null;
        }

        // Process [link] and ![image] directives
        Token ProcessLinkOrImageOrFootnote()
        {
            // Link or image?
            TokenType token_type = SkipChar('!') ? TokenType.img : TokenType.link;

            // Opening '['
            if (!SkipChar('['))
                return null;

            // Is it a foonote?
            var savepos=position;
            if (m_Markdown.ExtraMode && token_type==TokenType.link && SkipChar('^'))
            {
                SkipLinespace();

                // Parse it
                string id;
                if (SkipFootnoteID(out id) && SkipChar(']'))
                {
                    // Look it up and create footnote reference token
                    int footnote_index = m_Markdown.ClaimFootnote(id);
                    if (footnote_index >= 0)
                    {
                        // Yes it's a footnote
                        return new Token(TokenType.footnote, new FootnoteReference(footnote_index, id));
                    }
                }

                // Rewind
                position = savepos;
            }

            if (DisableLinks && token_type==TokenType.link)
                return null;

            bool ExtraMode = m_Markdown.ExtraMode;

            // Find the closing square bracket, allowing for nesting, watching for 
            // escapable characters
            Mark();
            int depth = 1;
            while (!eof)
            {
                char ch = current;
                if (ch == '[')
                {
                    depth++;
                }
                else if (ch == ']')
                {
                    depth--;
                    if (depth == 0)
                        break;
                }

                this.SkipEscapableChar(ExtraMode);
            }

            // Quit if end
            if (eof)
                return null;

            // Get the link text and unescape it
            string link_text = Utils.UnescapeString(Extract(), ExtraMode);

            // The closing ']'
            SkipForward(1);

            // Save position in case we need to rewind
            savepos = position;

            // Inline links must follow immediately
            if (SkipChar('('))
            {
                // Extract the url and title
                var link_def = LinkDefinition.ParseLinkTarget(this, null, m_Markdown.ExtraMode);
                if (link_def==null)
                    return null;

                // Closing ')'
                SkipWhitespace();
                if (!SkipChar(')'))
                    return null;

                // Xamu: Can have optional class name/id
                // In extra mode, check for a trailing HTML ID
                SpecialAttributes attrs = null;
                if (ExtraMode && !m_Markdown.SafeMode)
                {
                    int endLink = position;
                    SkipToEol();
                    int end=position;
                    attrs = Utils.CollectAttributes(input, endLink, ref end);
                    if (attrs == null)
                    {
                        position = endLink;
                    }
                }

                // Create the token
                return new Token(token_type, new LinkInfo(link_def, link_text, attrs));
            }

            // Optional space or tab
            if (!SkipChar(' '))
                SkipChar('\t');

            // If there's line end, we're allow it and as must line space as we want
            // before the link id.
            if (eol)
            {
                SkipEol();
                SkipLinespace();
            }

            // Reference link?
            string link_id = null;
            if (current == '[')
            {
                // Skip the opening '['
                SkipForward(1);

                // Find the start/end of the id
                Mark();
                if (!Find(']'))
                    return null;

                // Extract the id
                link_id = Extract();

                // Skip closing ']'
                SkipForward(1);
            }
            else
            {
                // Rewind to just after the closing ']'
                position = savepos;
            }

            // Link id not specified?
            if (string.IsNullOrEmpty(link_id))
            {
                // Use the link text (implicit reference link)
                link_id = Utils.NormalizeLineEnds(link_text);

                // If the link text has carriage returns, normalize
                // to spaces
                if (!object.ReferenceEquals(link_id, link_text))
                {
                    while (link_id.Contains(" \n"))
                        link_id = link_id.Replace(" \n", "\n");
                    link_id = link_id.Replace("\n", " ");
                }
            }

            // Find the link definition abort if not defined
            var def = m_Markdown.GetLinkDefinition(link_id);
            if (def == null)
                return null;

            // Create a token
            return new Token(token_type, new LinkInfo(def, link_text));
        }

        // Process a ``` code span ```
        Token ProcessCodeSpan()
        {
            int start = position;

            // Count leading ticks
            int tickcount = 0;
            while (SkipChar('`'))
            {
                tickcount++;
            }

            // Skip optional leading space...
            SkipWhitespace();

            // End?
            if (eof)
                return new Token(TokenType.Text, start, position - start);

            int startofcode = position;

            // Find closing ticks
            if (!Find(Substring(start, tickcount)))
                return new Token(TokenType.Text, start, position - start);

            // Save end position before backing up over trailing whitespace
            int endpos = position + tickcount;
            while (char.IsWhiteSpace(CharAtOffset(-1)))
                SkipForward(-1);

            // Create the token, move back to the end and we're done
            var ret = new Token(TokenType.code_span, startofcode, position - startofcode);
            position = endpos;
            return ret;
        }

        Markdown m_Markdown;
        internal bool DisableLinks;
        List<Token> m_Tokens = new List<Token>();
    }
}
