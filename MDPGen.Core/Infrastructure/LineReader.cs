using System;
using System.Diagnostics;
using System.IO;

namespace MDPGen.Core.Infrastructure
{
    /// <summary>
    /// A simple StringReader that supports forward and backward traversal
    /// </summary>

    [DebuggerDisplay("{CurrentPos} : {Remainder}")]
    public sealed class LineReader : TextReader
    {
        private int currentPos;
        private string input;
        private int length;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="input">String to wrap</param>
        public LineReader(string input)
        {
            this.input = input ?? throw new ArgumentNullException(nameof(input));
            this.length = input.Length;
        }

        /// <summary>
        /// Close the reader
        /// </summary>
        public override void Close()
        {
            base.Close();
            this.Dispose(true);
        }

        /// <summary>
        /// Dispose/Close the reader
        /// </summary>
        /// <param name="disposing">True if this is a managed dispose</param>
        protected override void Dispose(bool disposing)
        {
            this.input = null;
            this.currentPos = 0;
            this.length = 0;
            base.Dispose(disposing);
        }

        /// <summary>
        /// Return if the specified character is a space/tab.
        /// </summary>
        /// <param name="ch">Character</param>
        /// <returns>True/False</returns>
        private static bool IsLineSpace(char ch)
        {
            return ch == ' ' || ch == '\t';
        }

        /// <summary>
        /// Return if the specified character is space/tab/EOL
        /// </summary>
        /// <param name="ch">Character</param>
        /// <returns>True/False</returns>
        private static bool IsLineSpaceOrEol(char ch)
        {
            return IsLineSpace(ch) || ch == '\r' || ch == '\n';
        }

        /// <summary>
        /// Peek the next character in the string
        /// </summary>
        /// <returns>Value or -1 if at the end</returns>
        public override int Peek()
        {
            if (this.input == null)
                throw new ObjectDisposedException(nameof(LineReader));

            return !this.IsEof ? this.input[this.currentPos] : -1;
        }

        /// <summary>
        /// Peek the next line in the string
        /// </summary>
        /// <returns>Next full line or null</returns>
        public string PeekLine()
        {
            if (this.input == null)
                throw new ObjectDisposedException(nameof(LineReader));

            int pos = currentPos;
            string line = this.ReadLine();
            currentPos = pos;
            return line;
        }

        /// <summary>
        /// Read the next character and adjust the current position.
        /// </summary>
        /// <returns>Value or -1 if at the end</returns>
        public override int Read()
        {
            if (this.input == null)
                throw new ObjectDisposedException(nameof(LineReader));

            return (this.currentPos == this.length) ? -1 : input[this.currentPos++];
        }

        /// <summary>
        /// Read values from the string into a specified buffer
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="index">Position</param>
        /// <param name="count">Count</param>
        /// <returns># bytes read</returns>
        public override int Read(char[] buffer, int index, int count)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Read a line from the string
        /// </summary>
        /// <returns></returns>
        public override string ReadLine()
        {
            if (this.input == null)
                throw new ObjectDisposedException(nameof(LineReader));

            int i = currentPos;
            while (i < input.Length)
            {
                char ch = input[i];
                if (ch == '\r' || ch == '\n')
                {
                    string result = input.Substring(currentPos, i - currentPos);
                    currentPos = i+1;
                    if (ch == '\r' && currentPos < input.Length && input[currentPos] == '\n')
                        currentPos++;
                    return result;
                }
                i++;
            }

            if (i > currentPos)
            {
                string result = input.Substring(currentPos, i - currentPos);
                currentPos = i;
                return result;
            }

            return null;
        }
    
        /// <summary>
        /// Read the rest of the string
        /// </summary>
        /// <returns></returns>
        public override string ReadToEnd()
        {
            if (this.input == null)
                throw new ObjectDisposedException(nameof(LineReader));

            string s = currentPos == 0 ? input : input.Substring(currentPos, input.Length - currentPos);
            currentPos = input.Length;
            return s;
        }

        /// <summary>
        /// Change the current position of the reader.
        /// </summary>
        /// <param name="newPos">New position</param>
        public void SetPosition(int newPos)
        {
            if (this.input == null)
                throw new ObjectDisposedException(nameof(LineReader));

            this.currentPos = newPos;
            if (this.currentPos > this.input.Length)
                this.currentPos = this.input.Length;
            else if (this.currentPos < 0)
                this.currentPos = 0;
        }

        /// <summary>
        /// Move the position forward or backward by a known amount
        /// </summary>
        /// <param name="adjustBy">Adjustment amount</param>
        public void Skip(int adjustBy)
        {
            if (this.input == null)
                throw new ObjectDisposedException(nameof(LineReader));

            this.currentPos += adjustBy;
            if (this.currentPos > this.input.Length)
                this.currentPos = this.input.Length;
            else if (this.currentPos < 0)
                this.currentPos = 0;
        }

        /// <summary>
        /// Skip the next line and move the position
        /// </summary>
        public void SkipLine()
        {
            if (this.input == null)
                throw new ObjectDisposedException(nameof(LineReader));

            this.ReadLine();
        }

        /// <summary>
        /// Skip the space/tab characters.
        /// </summary>
        /// <returns>True if we moved by at least one character</returns>
        public bool SkipLinespace()
        {
            if (this.input == null)
                throw new ObjectDisposedException(nameof(LineReader));

            if (!IsLineSpace(this.Current))
                return false;
            this.Skip(1);
            while (IsLineSpace(this.Current))
                this.Skip(1);

            return true;
        }

        /// <summary>
        /// Skip spaces/tabs/EOL characters
        /// </summary>
        /// <returns>True if we moved by at least one character</returns>
        public bool SkipLineSpaceAndEol()
        {
            if (this.input == null)
                throw new ObjectDisposedException(nameof(LineReader));

            if (!IsLineSpaceOrEol(this.Current))
                return false;

            this.Skip(1);
            while (IsLineSpace(this.Current))
                this.Skip(1);
            return true;
        }

        /// <summary>
        /// Skip to the end of the line
        /// </summary>
        public void SkipToEol()
        {
            int len = this.input.Length;
            while (this.currentPos < len)
            {
                switch (this.input[this.currentPos])
                {
                    case '\n':
                        this.currentPos++;
                        if (this.currentPos < len && this.input[this.currentPos] == '\r')
                        {
                            this.currentPos++;
                        }
                        return;

                    case '\r':
                        this.currentPos++;
                        if (this.currentPos < len && this.input[this.currentPos] == '\n')
                        {
                            this.currentPos++;
                        }
                        return;
                }
                this.currentPos++;
            }
        }

        /// <summary>
        /// The current character
        /// </summary>
        public char Current => !this.IsEof ? this.input[this.currentPos] : '\0';

        /// <summary>
        /// Current position
        /// </summary>
        public int CurrentPos => this.currentPos;

        /// <summary>
        /// True if at the beginning of the string
        /// </summary>
        public bool IsBof => this.currentPos == 0;

        /// <summary>
        /// True if at the end of the string
        /// </summary>
        public bool IsEof => this.currentPos == this.length;

        /// <summary>
        /// Index to retrieve a character
        /// </summary>
        /// <param name="pos">Position</param>
        /// <returns>Character</returns>
        public char this[int pos]
        {
            get
            {
                if (this.input == null)
                    throw new ObjectDisposedException(nameof(LineReader));

                if (pos < 0 || pos > this.input.Length)
                    throw new IndexOutOfRangeException();

                return this.input[pos];
            }
        }

        /// <summary>
        /// Returns the full input being handled by this LineReader.
        /// </summary>
        public string Input => input;

        /// <summary>
        /// Returns the remainder of the string
        /// </summary>
        public string Remainder
        {
            get
            {
                if (this.input == null)
                    throw new ObjectDisposedException(nameof(LineReader));

                return this.currentPos != this.input.Length ? this.input.Substring(this.currentPos) : "";
            }
        }

        /// <summary>
        /// Returns true if we are at the end of a line
        /// </summary>
        public bool IsEol => (IsEof || Current == '\r' || Current == '\n');
    }

}