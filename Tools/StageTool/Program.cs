using System;
using System.IO;
using System.Linq;
using System.Text;
using ColorMinesweeper.Core;

// 사용법: dotnet run --project Tools/StageTool -- validate|generate [--force]
//   validate          모든 스테이지를 읽고 추론만으로 풀리는지 검사한다. 오류가 있으면 종료 코드 1.
//   generate          givens 가 비어 있거나 막히는 스테이지의 givens 를 생성해 파일에 저장한다.
//   generate --force  모든 스테이지의 givens 를 다시 생성한다.
static class Program
{
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
                stage.Givens = GivensGenerator.Generate(stage, XorShiftRandom.SeedFrom(stage.Id));
                File.WriteAllText(path, StageSerializer.ToJson(stage), new UTF8Encoding(false));
                Console.WriteLine($"  {file}: givens 생성 → [{string.Join(", ", stage.Givens)}]");
            }

            var report = new StageReport(stage);
            Console.WriteLine($"{(report.IsPlayable ? "✓" : "✗")} {file} ({stage.Name}): {report.Summary()}");
            foreach (string error in report.Errors)
            {
                Console.WriteLine("    오류: " + error);
            }

            foreach (string warning in report.Warnings)
            {
                Console.WriteLine("    경고: " + warning);
            }

            if (!report.IsPlayable)
            {
                failures++;
            }
        }

        return failures > 0 ? 1 : 0;
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
