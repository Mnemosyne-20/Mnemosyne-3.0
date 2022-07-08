using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ArchiveApi.LinkParsing
{
    internal class Scanner
    {
        private readonly string str;
        private int pos;
        public bool IsDone { get => pos == str.Length; }
        public string Underlying { get => str; }
        public int CurrentPosition { get => pos; }
        public char CurrentChar { get => str[CurrentPosition]; }
        public char Next { get => str[CurrentPosition + 1]; }
        public char Prev { get => pos > 0 ? str[CurrentPosition - 1] : str[CurrentPosition]; }
        public Scanner(string str)
        {
            this.str = str;
            pos = 0;
        }
        public string Find(Regex regex)
        {
            return Find(regex, -1);
        }
        public string Find(Regex regex, int len)
        {
            if (pos < str.Length)
            {
                Match m = len < 0 ? regex.Match(str, pos) : regex.Match(str, pos, len);
                if (m.Success)
                {
                    pos = m.Index + m.Length;
                    return m.Value;
                }
            }
            return null;
        }
        public void NextChar()
        {
            pos++;
        }
        public bool IsCurrentChar(char c)
        {
            return c == str[pos];
        }
        public void ChewWhitespace()
        {
            while (char.IsWhiteSpace(str[pos]))
            {
                pos++;
            }
        }
        public string MatchUntil(char c)
        {
            int nextIndex = str[pos..].IndexOf(c) + pos;
            string ret = str[pos..nextIndex];
            pos = nextIndex;
            return ret;
        }
        public string MatchUntil(char c, params char[] extended)
        {
            List<char> chars = new List<char>()
            {
                c
            };
            chars.AddRange(extended);
            int nextIndex = str[pos..].IndexOfAny(chars.ToArray()) + pos;
            string ret = str[pos..nextIndex];
            pos = nextIndex;
            return ret;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stopCharCheck">A function which returns TRUE when you want to stop the search</param>
        /// <returns></returns>
        public string MatchUntil(Func<char, bool> stopCharCheck)
        {
            StringBuilder sb = new StringBuilder();
            while (!stopCharCheck(str[pos]))
            {
                sb.Append(str[pos]);
                pos++;
            }
            return sb.ToString();
        }
        public string ParseCustom(Func<string, int, (string, int)> func)
        {
            (string retStr, pos) = func(str, pos);
            return retStr;
        }
    }
}
