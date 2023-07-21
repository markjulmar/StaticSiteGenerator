using System;
using System.Linq;
using System.Text;

namespace MDPGen
{
    /// <summary>
    /// An ASCII progress bar
    /// </summary>
    public class ConsoleProgressBar
    {
        private const int BlockCount = 10;
        private string lastText = string.Empty;

        /// <summary>
        /// Update method to refresh the progress bar state.
        /// </summary>
        /// <param name="value">Current % value</param>
        public void Update(double value)
        {
            if (Console.IsOutputRedirected)
                return;

            lock (Console.Out)
            {
                var progress = (int)(Math.Max(0, Math.Min(1, value / 100)) * BlockCount);
                string text = (progress >= 0)
                    ? $"[{new string('#', progress)}{new string('-', BlockCount - progress)}] {value,3}%"
                    : string.Empty;

                // Backup to the first differing character and add in the new characters
                int len = lastText.Zip(text, (c1, c2) => c1 == c2).TakeWhile(b => b).Count();
                var sb = new StringBuilder();
                sb.Append('\b', lastText.Length - len);
                sb.Append(text.Substring(len));

                // Delete remaining characters
                int overlapCount = lastText.Length - text.Length;
                if (overlapCount > 0)
                {
                    sb.Append(' ', overlapCount);
                    sb.Append('\b', overlapCount);
                }

                Console.Write(sb);
                lastText = text;
            }
        }
    }
}