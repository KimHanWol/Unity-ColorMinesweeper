using System;
using System.Collections.Generic;

namespace ColorMinesweeper.Core
{
    public sealed class StageColor
    {
        public readonly char Key;
        public readonly Rgb Color;

        public StageColor(char key, Rgb color)
        {
            Key = key;
            Color = color;
        }
    }

    /// <summary>
    /// 한 스테이지의 정답 그림과 단서. 만들어진 뒤에는 <see cref="Givens"/> 외에는 바뀌지 않는다.
    /// </summary>
    public sealed class Stage
    {
        /// <summary>솔버가 칸별 후보 색을 int 비트마스크로 다루므로 색 수에 상한이 있다.</summary>
        public const int MaxColors = 16;
        public const int MaxSize = 40;

        public string Id { get; }
        public string Name { get; }
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<StageColor> Colors { get; }
        public int BackgroundColor { get; }

        /// <summary>처음부터 열려 있는 칸. 비어 있으면 게임이 생성한다.</summary>
        public int[] Givens { get; set; }

        /// <summary>
        /// givens 를 고를 때 플레이어에게 요구할 가장 어려운 추론. 초반 스테이지는 <see cref="Technique.Direct"/>
        /// (JSON "basic")로 두면 열린 칸이 조금 늘어나는 대신 항상 순서대로 풀린다.
        /// </summary>
        public Technique Logic { get; set; } = Technique.Pair;

        readonly int[] pixels;
        readonly int[][] neighbors;
        readonly int[] clues;
        readonly int[] colorTotals;

        public int CellCount => Width * Height;
        public int ColorCount => Colors.Count;

        public Stage(string id, string name, IReadOnlyList<StageColor> colors, int backgroundColor,
            int width, int height, int[] pixels, int[] givens)
        {
            if (colors.Count > MaxColors)
            {
                throw new ArgumentException("too many colors");
            }

            if (pixels.Length != width * height)
            {
                throw new ArgumentException("pixel count does not match size");
            }

            Id = id;
            Name = name;
            Colors = colors;
            BackgroundColor = backgroundColor;
            Width = width;
            Height = height;
            this.pixels = pixels;
            Givens = givens ?? Array.Empty<int>();

            neighbors = new int[CellCount][];
            clues = new int[CellCount * colors.Count];
            colorTotals = new int[colors.Count];
            var buffer = new List<int>(8);
            for (int cell = 0; cell < CellCount; cell++)
            {
                colorTotals[pixels[cell]]++;
                buffer.Clear();
                int x = cell % width;
                int y = cell / width;
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0)
                        {
                            continue;
                        }

                        int nx = x + dx;
                        int ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                        {
                            continue;
                        }

                        int n = ny * width + nx;
                        buffer.Add(n);
                        clues[cell * colors.Count + pixels[n]]++;
                    }
                }

                neighbors[cell] = buffer.ToArray();
            }
        }

        public int ColorAt(int cell)
        {
            return pixels[cell];
        }

        public int ColorAt(int x, int y)
        {
            return pixels[y * Width + x];
        }

        /// <summary>주변 8칸(판 안쪽만)의 인덱스.</summary>
        public int[] Neighbors(int cell)
        {
            return neighbors[cell];
        }

        /// <summary>cell 주변 8칸 중 color 색인 칸의 수.</summary>
        public int Clue(int cell, int color)
        {
            return clues[cell * Colors.Count + color];
        }

        /// <summary>주변에 배경색만 있는 칸. 열리면 주변이 자동으로 펼쳐진다.</summary>
        public bool IsBlank(int cell)
        {
            return Clue(cell, BackgroundColor) == neighbors[cell].Length;
        }

        public int ColorTotal(int color)
        {
            return colorTotals[color];
        }

        /// <summary>
        /// start 를 열고, 열린 칸이 빈 칸이면 주변을 이어서 연다(지뢰찾기의 0 펼침).
        /// 새로 열린 칸을 BFS 순서로 depth 와 함께 result 에 더한다.
        /// </summary>
        public void RevealWithFlood(int start, bool[] revealed, List<RevealedCell> result)
        {
            if (revealed[start])
            {
                return;
            }

            var queue = new Queue<RevealedCell>();
            revealed[start] = true;
            queue.Enqueue(new RevealedCell(start, 0));
            while (queue.Count > 0)
            {
                RevealedCell current = queue.Dequeue();
                result?.Add(current);
                if (!IsBlank(current.Cell))
                {
                    continue;
                }

                foreach (int n in neighbors[current.Cell])
                {
                    if (revealed[n])
                    {
                        continue;
                    }

                    revealed[n] = true;
                    queue.Enqueue(new RevealedCell(n, current.Depth + 1));
                }
            }
        }
    }

    public readonly struct RevealedCell
    {
        public readonly int Cell;

        /// <summary>처음 연 칸으로부터 펼쳐진 거리. 파도처럼 여는 연출의 지연에 쓴다.</summary>
        public readonly int Depth;

        public RevealedCell(int cell, int depth)
        {
            Cell = cell;
            Depth = depth;
        }
    }
}
