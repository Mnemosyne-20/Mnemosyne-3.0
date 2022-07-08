using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchiveApi.LinkParsing
{

    [Serializable]
    public class ParseException : Exception
    {
        public ParseException() { }
        public ParseException(string message) : base(message) { }
        public ParseException(string message, Exception inner) : base(message, inner) { }
        protected ParseException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    public class ParseWebLinks
    {
        private enum ParsingState
        {
            LinkParsingState,
            LinkValueList,
            LinkValue,
            LinkParam,
            LinkExtension,
            MediaType,
            QuotedMT,
            RelationTypes,
            RelationType,
            RegRelType,
            ExtRelType,
            Cardinal
        };
        static (string, int) ParseQuotedString(string body, int pos)
        {
            if (body[pos] != '"')
            {
                return (string.Empty, pos);
            }
            pos++;
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                if (body[pos] == '\\')
                {
                    sb.Append(body[pos + 1]);
                    pos += 2;
                    continue;
                }
                else if (body[pos] == '"')
                {
                    pos++;
                    return (sb.ToString(), pos);
                }
                sb.Append(body[pos]);
                pos++;
            }
        }
        public static IEnumerable<WebLink> Parse(string body)
        {
            ParsingState state = ParsingState.LinkParsingState;
            body = body.Trim();
            Scanner scanner = new Scanner(body);
            string myURI = string.Empty;
            Dictionary<string, string> dic = new Dictionary<string, string>();

            while (!scanner.IsDone)
            {
                switch (state)
                {
                    case ParsingState.LinkParsingState:
                        scanner.ChewWhitespace();
                        if (!scanner.IsCurrentChar('<'))
                        {
                            throw new ParseException();
                        }
                        scanner.NextChar();
                        state = ParsingState.LinkValueList;
                        break;
                    case ParsingState.LinkValueList:
                        myURI = scanner.MatchUntil('>');
                        scanner.NextChar();
                        scanner.ChewWhitespace();
                        if (scanner.IsCurrentChar(';'))
                        {
                            state = ParsingState.LinkParam;
                            scanner.NextChar();
                            continue;
                        }
                        else if (scanner.IsCurrentChar(','))
                        {
                            state = ParsingState.LinkParsingState;
                            yield return new WebLink(new Uri(myURI.ToString()));
                            myURI = string.Empty;
                            scanner.NextChar();
                            continue;
                        }
                        else
                        {
                            throw new ParseException();
                        }
                    case ParsingState.LinkParam:
                        scanner.ChewWhitespace();
                        if (scanner.IsCurrentChar(','))
                        {
                            state = ParsingState.LinkParsingState;
                            Dictionary<string, string> dictionary = new Dictionary<string, string>(dic);
                            yield return new WebLink(new Uri(myURI.ToString()), dictionary);
                            scanner.NextChar();
                            myURI = string.Empty;
                            dic = new Dictionary<string, string>();
                            continue;
                        }
                        string attr = scanner.MatchUntil('=');
                        scanner.NextChar();

                        switch (attr)
                        {
                            case "rel":
                                if (scanner.IsCurrentChar('"'))
                                {
                                    string quoted = scanner.ParseCustom(ParseQuotedString);
                                    dic.Add(attr, quoted);
                                }
                                else
                                {
                                    string val = scanner.MatchUntil(c => char.IsWhiteSpace(c) || c == ',');
                                    dic.Add(attr, val);
                                }
                                break;
                            case "anchor":
                                break;
                            case "media":
                                break;
                            case "title":
                                break;
                            case "title*":
                                break;
                            case "rt":
                            case "if":

                                break;
                            case "type":
                                if (scanner.IsCurrentChar('"'))
                                {
                                    string mediatype = scanner.ParseCustom(ParseQuotedString);
                                    dic.Add(attr, mediatype);
                                }
                                else
                                {
                                    string mediatype = scanner.MatchUntil(',', ' ');
                                    dic.Add(attr, mediatype);
                                }
                                break;
                            case "sz":
                                break;
                            case "hreflang":
                                break;
                            case "rev":
                                break;
                            default:
                                string value = scanner.ParseCustom(ParseQuotedString);
                                dic.Add(attr, value);
                                break;
                        }
                        if (!scanner.IsDone)
                        {
                            if (!scanner.IsCurrentChar(';') && !scanner.IsCurrentChar(','))
                            {
                                throw new ParseException($"Expected a ';' or ',', got {scanner.CurrentChar}");
                            }
                            else if (scanner.IsCurrentChar(';'))
                            {
                                scanner.NextChar();
                            }
                        }
                        break;
                }
            }
            yield return new WebLink(new Uri(myURI.ToString()), dic);
            yield break;
        }
    }
}
