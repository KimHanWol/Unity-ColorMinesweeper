using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ColorMinesweeper.Core;

// 사용법: dotnet run --project Tools/StageTool -- <명령> [--force]
//   validate          모든 스테이지를 읽고 추론만으로 풀리는지, 난이도 순서가 맞는지 검사한다. 오류가 있으면 종료 코드 1.
//   generate          givens 가 비어 있거나 막히는 스테이지의 givens 를 생성해 파일에 저장한다.
//   generate --force  모든 스테이지의 givens 를 다시 생성한다.
//   sort              난이도 점수 순으로 파일 이름 앞 번호(001_...)를 다시 매긴다. 게임은 파일 이름 순서로 스테이지를 낸다.
static class Program
{
    /// <summary>앞 스테이지 점수가 이보다 더 높으면 순서가 뒤집혔다고 경고한다.</summary>
    const float OrderTolerance = 2f;

    sealed class Item
    {
        public string Path;
        public Stage Stage;
        public StageReport Report;
    }

    static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        string mode = args.Length > 0 ? args[0] : "validate";
        bool force = args.Contains("--force");
        string dir = FindStagesDir();
        if (dir == null)
        {
            Console.Error.WriteLine("Assets/Resources/Stages 폴더를 찾지 못했습니다.");
            return 2;
        }

        int failures = 0;
        var items = new List<Item>();
        foreach (string path in Directory.GetFiles(dir, "*.json").OrderBy(p => p, StringComparer.Ordinal))
        {
            string file = Path.GetFileName(path);
            Stage stage;
            try
            {
                stage = StageSerializer.Parse(File.ReadAllText(path));
            }
            catch (StageFormatException e)
            {
                Console.WriteLine($"✗ {file}: {e.Message}");
                failures++;
                continue;
            }

            if (mode == "generate" && (force || stage.Givens.Length == 0 || !new Solver(stage).Solve(stage.Givens).Solved))
            {
                stage.Givens = GivensGenerator.Generate(stage);
                File.WriteAllText(path, StageSerializer.ToJson(stage), new UTF8Encoding(false));
                Console.WriteLine($"  {file}: givens 생성 → [{string.Join(", ", stage.Givens)}]");
            }

            var report = new StageReport(stage);
            items.Add(new Item { Path = path, Stage = stage, Report = report });
            if (!report.IsPlayable)
            {
                failures++;
            }
        }

        if (mode == "sort")
        {
            Sort(dir, items);
            return failures > 0 ? 1 : 0;
        }

        for (int i = 0; i < items.Count; i++)
        {
            Item item = items[i];
            Print(item);
            if (i > 0 && IsOutOfOrder(items[i - 1].Report.Difficulty, item.Report.Difficulty))
            {
                Console.WriteLine($"    경고: 앞 스테이지({Path.GetFileName(items[i - 1].Path)})보다 쉽습니다. `sort` 로 순서를 다시 매기세요.");
            }
        }

        var tiers = items.GroupBy(i => i.Report.Difficulty.TierName).Select(g => $"{g.Key} {g.Count()}");
        Console.WriteLine($"\n{items.Count}개 스테이지: {string.Join(", ", tiers)}");
        return failures > 0 ? 1 : 0;
    }

    /// <summary>앞 스테이지 점수가 눈에 띄게 높으면 순서가 뒤집힌 것(비슷한 점수끼리의 순서는 신경 쓰지 않는다).</summary>
    static bool IsOutOfOrder(Difficulty previous, Difficulty current)
    {
        if (previous.PictureColors != current.PictureColors)
        {
            return previous.PictureColors > current.PictureColors;
        }

        if (previous.Hardest != current.Hardest)
        {
            return previous.Hardest > current.Hardest;
        }

        if (previous.Cells != current.Cells)
        {
            return previous.Cells > current.Cells;
        }

        return previous.Total - current.Total > OrderTolerance;
    }

    static void Print(Item item)
    {
        StageReport report = item.Report;
        Console.WriteLine($"{(report.IsPlayable ? "✓" : "✗")} {Path.GetFileName(item.Path)} ({item.Stage.Name}): {report.Summary()}");
        foreach (string error in report.Errors)
        {
            Console.WriteLine("    오류: " + error);
        }

        foreach (string warning in report.Warnings)
        {
            Console.WriteLine("    경고: " + warning);
        }
    }

    /// <summary>점수 순서대로 001_, 002_ ... 를 붙인다. id 는 그대로라 저장된 진행(별점)은 유지된다.</summary>
    static void Sort(string dir, List<Item> items)
    {
        List<Item> ordered = items.OrderBy(i => i.Report.Difficulty).ThenBy(i => i.Stage.Id, StringComparer.Ordinal).ToList();
        var temp = new List<(string from, string to)>();
        for (int i = 0; i < ordered.Count; i++)
        {
            string name = Regex.Replace(Path.GetFileName(ordered[i].Path), @"^\d+_", "");
            string target = Path.Combine(dir, $"{i + 1:000}_{name}");
            string staging = ordered[i].Path + ".sorting";
            File.Move(ordered[i].Path, staging);
            temp.Add((staging, target));
            MoveMeta(ordered[i].Path, staging);
        }

        foreach ((string from, string to) in temp)
        {
            File.Move(from, to);
            MoveMeta(from, to);
        }

        for (int i = 0; i < ordered.Count; i++)
        {
            Console.WriteLine($"{i + 1:000} {ordered[i].Stage.Name,-8} {ordered[i].Report.Difficulty}");
        }
    }

    /// <summary>Unity 가 만든 .meta 가 있으면 같이 옮긴다(GUID 유지).</summary>
    static void MoveMeta(string from, string to)
    {
        if (File.Exists(from + ".meta"))
        {
            File.Move(from + ".meta", to + ".meta");
        }
    }

    static string FindStagesDir()
    {
        for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir != null; dir = dir.Parent)
        {
            string candidate = Path.Combine(dir.FullName, "Assets", "Resources", "Stages");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
