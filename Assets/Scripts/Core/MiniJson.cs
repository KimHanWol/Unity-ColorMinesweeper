using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ColorMinesweeper.Core
{
    public sealed class JsonParseException : Exception
    {
        public int Line { get; }
        public int Column { get; }

        public JsonParseException(string message, int line, int column)
            : base(message + " (" + line + "행 " + column + "열)")
        {
            Line = line;
            Column = column;
        }
    }

    /// <summary>
    /// 스테이지 파일용 작은 JSON 파서. JsonUtility 는 Unity 밖(dotnet 테스트)에서 못 쓰고 오류 위치도 알려주지 않아서 직접 둔다.
    /// 객체는 Dictionary&lt;string, object&gt;, 배열은 List&lt;object&gt;, 숫자는 double 로 읽는다.
    /// </summary>
    public static class MiniJson
    {
        public static object Parse(string text)
        {
            var parser = new Parser(text);
            parser.SkipWhitespace();
            object value = parser.ReadValue();
            parser.SkipWhitespace();
            if (!parser.AtEnd)
            {
                throw parser.Error("JSON 뒤에 남는 내용이 있습니다");
            }

            return value;
        }

        public static string Quote(string value)
        {
            var sb = new StringBuilder(value.Length + 2);
            sb.Append('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4"));
                        }
                        else
                        {
                            sb.Append(c);
                        }

                        break;
                }
            }

            sb.Append('"');
            return sb.ToString();
        }

        sealed class Parser
        {
            readonly string text;
            int pos;

            public Parser(string text)
            {
                this.text = text ?? string.Empty;
            }

            public bool AtEnd => pos >= text.Length;

            public JsonParseException Error(string message)
            {
                int line = 1;
                int column = 1;
                for (int i = 0; i < pos && i < text.Length; i++)
                {
                    if (text[i] == '\n')
                    {
                        line++;
                        column = 1;
                    }
                    else
                    {
                        column++;
                    }
                }

                return new JsonParseException(message, line, column);
            }

            public void SkipWhitespace()
            {
                while (!AtEnd && char.IsWhiteSpace(text[pos]))
                {
                    pos++;
                }
            }

            public object ReadValue()
            {
                if (AtEnd)
                {
                    throw Error("값이 와야 하는데 파일이 끝났습니다");
                }

                char c = text[pos];
                switch (c)
                {
                    case '{': return ReadObject();
                    case '[': return ReadArray();
                    case '"': return ReadString();
                    case 't': ReadLiteral("true"); return true;
                    case 'f': ReadLiteral("false"); return false;
                    case 'n': ReadLiteral("null"); return null;
                    default:
                        if (c == '-' || (c >= '0' && c <= '9'))
                        {
                            return ReadNumber();
                        }

                        throw Error("알 수 없는 문자 '" + c + "'");
                }
            }

            Dictionary<string, object> ReadObject()
            {
                var result = new Dictionary<string, object>();
                pos++;
                SkipWhitespace();
                if (!AtEnd && text[pos] == '}')
                {
                    pos++;
                    return result;
                }

                while (true)
                {
                    SkipWhitespace();
                    if (AtEnd || text[pos] != '"')
                    {
                        throw Error("객체의 키는 문자열이어야 합니다");
                    }

                    string key = ReadString();
                    SkipWhitespace();
                    Expect(':');
                    SkipWhitespace();
                    result[key] = ReadValue();
                    SkipWhitespace();
                    if (AtEnd)
                    {
                        throw Error("'}' 가 없습니다");
                    }

                    if (text[pos] == ',')
                    {
                        pos++;
                        continue;
                    }

                    Expect('}');
                    return result;
                }
            }

            List<object> ReadArray()
            {
                var result = new List<object>();
                pos++;
                SkipWhitespace();
                if (!AtEnd && text[pos] == ']')
                {
                    pos++;
                    return result;
                }

                while (true)
                {
                    SkipWhitespace();
                    result.Add(ReadValue());
                    SkipWhitespace();
                    if (AtEnd)
                    {
                        throw Error("']' 가 없습니다");
                    }

                    if (text[pos] == ',')
                    {
                        pos++;
                        continue;
                    }

                    Expect(']');
                    return result;
                }
            }

            string ReadString()
            {
                pos++;
                var sb = new StringBuilder();
                while (true)
                {
                    if (AtEnd)
                    {
                        throw Error("문자열이 닫히지 않았습니다");
                    }

                    char c = text[pos++];
                    if (c == '"')
                    {
                        return sb.ToString();
                    }

                    if (c == '\n')
                    {
                        throw Error("문자열 안에 줄바꿈이 있습니다");
                    }

                    if (c != '\\')
                    {
                        sb.Append(c);
                        continue;
                    }

                    if (AtEnd)
                    {
                        throw Error("문자열이 닫히지 않았습니다");
                    }

                    char e = text[pos++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (pos + 4 > text.Length ||
                                !int.TryParse(text.Substring(pos, 4), NumberStyles.HexNumber,
                                    CultureInfo.InvariantCulture, out int code))
                            {
                                throw Error("잘못된 \\u 이스케이프");
                            }

                            sb.Append((char)code);
                            pos += 4;
                            break;
                        default:
                            throw Error("잘못된 이스케이프 '\\" + e + "'");
                    }
                }
            }

            double ReadNumber()
            {
                int start = pos;
                while (!AtEnd && "+-0123456789.eE".IndexOf(text[pos]) >= 0)
                {
                    pos++;
                }

                string token = text.Substring(start, pos - start);
                if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                {
                    pos = start;
                    throw Error("잘못된 숫자 '" + token + "'");
                }

                return value;
            }

            void ReadLiteral(string literal)
            {
                if (string.CompareOrdinal(text, pos, literal, 0, literal.Length) != 0)
                {
                    throw Error("알 수 없는 값");
                }

                pos += literal.Length;
            }

            void Expect(char c)
            {
                if (AtEnd || text[pos] != c)
                {
                    throw Error("'" + c + "' 가 와야 합니다");
                }

                pos++;
            }
        }
    }
}
