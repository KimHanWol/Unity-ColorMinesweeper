using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ColorMinesweeper.Core
{
    public sealed class StageFormatException : Exception
    {
        public StageFormatException(string message) : base(message)
        {
        }
    }

    /// <summary>스테이지 JSON 을 읽고 쓴다. 형식은 README.md 의 "스테이지 형식" 참고.</summary>
    public static class StageSerializer
    {
        public static Stage Parse(string json)
        {
            object root;
            try
            {
                root = MiniJson.Parse(json);
            }
            catch (JsonParseException e)
            {
                throw new StageFormatException("JSON 문법 오류: " + e.Message);
            }

            if (!(root is Dictionary<string, object> obj))
            {
                throw new StageFormatException("최상위가 객체({ ... })여야 합니다");
            }

            string id = RequireString(obj, "id");
            if (id.Length == 0)
            {
                throw new StageFormatException("id 가 비어 있습니다");
            }

            string name = obj.TryGetValue("name", out object nameValue) && nameValue is string n ? n : id;

            var colors = new List<StageColor>();
            var keyToIndex = new Dictionary<char, int>();
            foreach (object entry in RequireArray(obj, "palette"))
            {
                if (!(entry is Dictionary<string, object> color))
                {
                    throw new StageFormatException("palette 의 항목은 { \"key\": ..., \"color\": ... } 형태여야 합니다");
                }

                string key = RequireString(color, "key");
                if (key.Length != 1)
                {
                    throw new StageFormatException("palette key '" + key + "' 는 한 글자여야 합니다");
                }

                string hex = RequireString(color, "color");
                if (!Rgb.TryParseHex(hex, out Rgb rgb))
                {
                    throw new StageFormatException("palette '" + key + "' 의 색 '" + hex + "' 는 #RRGGBB 형식이어야 합니다");
                }

                if (keyToIndex.ContainsKey(key[0]))
                {
                    throw new StageFormatException("palette key '" + key + "' 가 중복됩니다");
                }

                keyToIndex[key[0]] = colors.Count;
                colors.Add(new StageColor(key[0], rgb));
            }

            if (colors.Count < 2)
            {
                throw new StageFormatException("palette 에는 배경색을 포함해 두 색 이상이 있어야 합니다");
            }

            if (colors.Count > Stage.MaxColors)
            {
                throw new StageFormatException("palette 는 최대 " + Stage.MaxColors + "색입니다");
            }

            string background = RequireString(obj, "background");
            if (background.Length != 1 || !keyToIndex.TryGetValue(background[0], out int backgroundIndex))
            {
                throw new StageFormatException("background '" + background + "' 가 palette 에 없습니다");
            }

            List<object> rows = RequireArray(obj, "pixels");
            if (rows.Count == 0)
            {
                throw new StageFormatException("pixels 가 비어 있습니다");
            }

            int height = rows.Count;
            int width = -1;
            int[] pixels = null;
            for (int y = 0; y < height; y++)
            {
                if (!(rows[y] is string row))
                {
                    throw new StageFormatException("pixels " + (y + 1) + "번째 줄이 문자열이 아닙니다");
                }

                if (width < 0)
                {
                    width = row.Length;
                    if (width == 0)
                    {
                        throw new StageFormatException("pixels 첫 줄이 비어 있습니다");
                    }

                    if (width > Stage.MaxSize || height > Stage.MaxSize)
                    {
                        throw new StageFormatException("판은 최대 " + Stage.MaxSize + "x" + Stage.MaxSize + " 입니다");
                    }

                    pixels = new int[width * height];
                }
                else if (row.Length != width)
                {
                    throw new StageFormatException(
                        "pixels " + (y + 1) + "번째 줄 길이가 " + row.Length + " 입니다(첫 줄은 " + width + ")");
                }

                for (int x = 0; x < width; x++)
                {
                    if (!keyToIndex.TryGetValue(row[x], out int colorIndex))
                    {
                        throw new StageFormatException(
                            "pixels " + (y + 1) + "번째 줄 " + (x + 1) + "번째 글자 '" + row[x] + "' 가 palette 에 없습니다");
                    }

                    pixels[y * width + x] = colorIndex;
                }
            }

            var used = new bool[colors.Count];
            foreach (int p in pixels)
            {
                used[p] = true;
            }

            for (int i = 0; i < colors.Count; i++)
            {
                if (!used[i])
                {
                    throw new StageFormatException("palette '" + colors[i].Key + "' 가 그림에 쓰이지 않습니다");
                }
            }

            int[] givens = Array.Empty<int>();
            if (obj.TryGetValue("givens", out object givensValue) && givensValue != null)
            {
                if (!(givensValue is List<object> list))
                {
                    throw new StageFormatException("givens 는 칸 인덱스 배열이어야 합니다");
                }

                var set = new HashSet<int>();
                givens = new int[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    if (!(list[i] is double d) || d != Math.Floor(d) || d < 0 || d >= width * height)
                    {
                        throw new StageFormatException("givens 의 " + (i + 1) + "번째 값이 0~" + (width * height - 1) + " 범위의 정수가 아닙니다");
                    }

                    givens[i] = (int)d;
                    if (!set.Add(givens[i]))
                    {
                        throw new StageFormatException("givens 에 " + givens[i] + " 가 중복됩니다");
                    }
                }
            }

            var stage = new Stage(id, name, colors, backgroundIndex, width, height, pixels, givens);
            if (obj.TryGetValue("logic", out object logicValue) && logicValue != null)
            {
                if (!(logicValue is string logic) || (logic != "basic" && logic != "medium" && logic != "full"))
                {
                    throw new StageFormatException(
                        "logic 은 \"basic\"(순서대로), \"medium\"(제외 기억까지), \"full\"(조합까지) 중 하나여야 합니다");
                }

                stage.Logic = logic == "basic" ? Technique.Direct : logic == "medium" ? Technique.Single : Technique.Pair;
            }

            return stage;
        }

        public static string ToJson(Stage stage)
        {
            var sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append("  \"id\": ").Append(MiniJson.Quote(stage.Id)).Append(",\n");
            sb.Append("  \"name\": ").Append(MiniJson.Quote(stage.Name)).Append(",\n");
            if (stage.Logic != Technique.Pair)
            {
                sb.Append("  \"logic\": ").Append(stage.Logic == Technique.Direct ? "\"basic\"" : "\"medium\"").Append(",\n");
            }

            sb.Append("  \"background\": ")
                .Append(MiniJson.Quote(stage.Colors[stage.BackgroundColor].Key.ToString())).Append(",\n");
            sb.Append("  \"palette\": [\n");
            for (int i = 0; i < stage.ColorCount; i++)
            {
                StageColor color = stage.Colors[i];
                sb.Append("    { \"key\": ").Append(MiniJson.Quote(color.Key.ToString()))
                    .Append(", \"color\": ").Append(MiniJson.Quote(color.Color.ToHex())).Append(" }")
                    .Append(i + 1 < stage.ColorCount ? ",\n" : "\n");
            }

            sb.Append("  ],\n");
            sb.Append("  \"pixels\": [\n");
            for (int y = 0; y < stage.Height; y++)
            {
                var row = new StringBuilder(stage.Width);
                for (int x = 0; x < stage.Width; x++)
                {
                    row.Append(stage.Colors[stage.ColorAt(x, y)].Key);
                }

                sb.Append("    ").Append(MiniJson.Quote(row.ToString())).Append(y + 1 < stage.Height ? ",\n" : "\n");
            }

            sb.Append("  ],\n");
            sb.Append("  \"givens\": [");
            for (int i = 0; i < stage.Givens.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(stage.Givens[i].ToString(CultureInfo.InvariantCulture));
            }

            sb.Append("]\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        static string RequireString(Dictionary<string, object> obj, string key)
        {
            if (!obj.TryGetValue(key, out object value))
            {
                throw new StageFormatException("\"" + key + "\" 가 없습니다");
            }

            if (!(value is string s))
            {
                throw new StageFormatException("\"" + key + "\" 는 문자열이어야 합니다");
            }

            return s;
        }

        static List<object> RequireArray(Dictionary<string, object> obj, string key)
        {
            if (!obj.TryGetValue(key, out object value))
            {
                throw new StageFormatException("\"" + key + "\" 가 없습니다");
            }

            if (!(value is List<object> list))
            {
                throw new StageFormatException("\"" + key + "\" 는 배열이어야 합니다");
            }

            return list;
        }
    }
}
