using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ColorMinesweeper.Core;
using ColorMinesweeper.Game;
using UnityEditor;
using UnityEngine;

namespace ColorMinesweeper.EditorTools
{
    /// <summary>
    /// 개발용 스테이지 도구. JSON 을 고치면 바로 그림·형식 오류·풀이 가능 여부를 보여 주고,
    /// 솔버가 어떤 순서로 푸는지 단계별로 재생하며, givens 생성·저장과 시험 플레이를 한 곳에서 한다.
    /// </summary>
    public sealed class StagePreviewWindow : EditorWindow
    {
        const string StagesFolder = "Assets/Resources/Stages";

        enum View
        {
            Picture,
            Start,
            Steps,
        }

        static readonly string[] ViewNames = { "그림", "시작 상태", "풀이 단계" };

        sealed class Entry
        {
            public string Path;
            public string Text;
            public string SavedText;
            public Stage Stage;
            public StageReport Report;
            public string ParseError;

            public bool Dirty => Text != SavedText;
            public string FileName => System.IO.Path.GetFileName(Path);
        }

        readonly List<Entry> entries = new List<Entry>();
        int selected;
        View view = View.Picture;
        int step;
        Vector2 listScroll;
        Vector2 bodyScroll;
        Vector2 jsonScroll;
        int hoverCell = -1;

        [MenuItem("Tools/Pixel Clue/Stage Preview")]
        static void Open()
        {
            GetWindow<StagePreviewWindow>("Stage Preview").Show();
        }

        void OnEnable()
        {
            minSize = new Vector2(760f, 480f);
            Reload();
        }

        void Reload()
        {
            string current = selected >= 0 && selected < entries.Count ? entries[selected].Path : null;
            entries.Clear();
            if (Directory.Exists(StagesFolder))
            {
                foreach (string path in Directory.GetFiles(StagesFolder, "*.json").OrderBy(p => p, StringComparer.Ordinal))
                {
                    string text = File.ReadAllText(path);
                    var entry = new Entry { Path = path.Replace('\\', '/'), Text = text, SavedText = text };
                    Analyze(entry);
                    entries.Add(entry);
                }
            }

            selected = Mathf.Max(0, entries.FindIndex(e => e.Path == current));
        }

        static void Analyze(Entry entry)
        {
            entry.Stage = null;
            entry.Report = null;
            entry.ParseError = null;
            try
            {
                entry.Stage = StageSerializer.Parse(entry.Text);
                entry.Report = new StageReport(entry.Stage);
            }
            catch (StageFormatException e)
            {
                entry.ParseError = e.Message;
            }
        }

        void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawList();
                DrawBody();
            }
        }

        void DrawList()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(230f)))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("새로고침"))
                    {
                        Reload();
                    }

                    if (GUILayout.Button("새 스테이지"))
                    {
                        CreateStage();
                    }
                }

                listScroll = EditorGUILayout.BeginScrollView(listScroll, "box");
                for (int i = 0; i < entries.Count; i++)
                {
                    Entry entry = entries[i];
                    string mark = entry.ParseError != null || (entry.Report != null && !entry.Report.IsPlayable) ? "✗"
                        : entry.Report != null && entry.Report.Warnings.Count > 0 ? "!"
                        : "✓";
                    string label = mark + "  " + entry.FileName + (entry.Dirty ? " *" : "");
                    var style = new GUIStyle(i == selected ? EditorStyles.boldLabel : EditorStyles.label);
                    if (GUILayout.Button(label, style))
                    {
                        selected = i;
                        step = 0;
                        GUI.FocusControl(null);
                    }
                }

                EditorGUILayout.EndScrollView();
                EditorGUILayout.HelpBox("✓ 문제 없음   ! 경고   ✗ 오류\n* 저장 안 된 변경", MessageType.None);
            }
        }

        void DrawBody()
        {
            if (entries.Count == 0)
            {
                EditorGUILayout.HelpBox(StagesFolder + " 에 스테이지 JSON 이 없습니다. '새 스테이지' 로 만드세요.", MessageType.Info);
                return;
            }

            Entry entry = entries[Mathf.Clamp(selected, 0, entries.Count - 1)];
            using (new EditorGUILayout.VerticalScope())
            {
                bodyScroll = EditorGUILayout.BeginScrollView(bodyScroll);
                EditorGUILayout.LabelField(entry.FileName + (entry.Stage != null ? "  —  " + entry.Stage.Name : ""),
                    EditorStyles.largeLabel);

                DrawToolbar(entry);
                if (entry.ParseError != null)
                {
                    EditorGUILayout.HelpBox(entry.ParseError, MessageType.Error);
                }
                else
                {
                    DrawReport(entry);
                    DrawPreview(entry);
                }

                DrawJson(entry);
                EditorGUILayout.EndScrollView();
            }
        }

        void DrawToolbar(Entry entry)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = entry.Dirty;
                if (GUILayout.Button("저장"))
                {
                    Save(entry);
                }

                GUI.enabled = entry.Stage != null;
                if (GUILayout.Button(new GUIContent("givens 생성 후 저장", "추론만으로 풀리게 처음 열어 둘 칸을 다시 고른다")))
                {
                    entry.Stage.Givens = GivensGenerator.Generate(entry.Stage);
                    entry.Text = StageSerializer.ToJson(entry.Stage);
                    Save(entry);
                }

                if (GUILayout.Button(new GUIContent("시험 플레이", "지금 편집 중인 내용 그대로(저장 안 해도) 플레이 모드로 띄운다")))
                {
                    SessionState.SetString(GameApp.PlaytestKey, entry.Text);
                    EditorApplication.EnterPlaymode();
                }

                GUI.enabled = true;
                if (GUILayout.Button("파일 선택", GUILayout.Width(80f)))
                {
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<TextAsset>(entry.Path));
                }
            }
        }

        static void DrawReport(Entry entry)
        {
            StageReport report = entry.Report;
            EditorGUILayout.LabelField(report.Summary(), EditorStyles.miniBoldLabel);
            foreach (string error in report.Errors)
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }

            foreach (string warning in report.Warnings)
            {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }
        }

        void DrawPreview(Entry entry)
        {
            Stage stage = entry.Stage;
            SolveResult result = entry.Report.Result;
            view = (View)GUILayout.Toolbar((int)view, ViewNames);

            int[] revealStep = RevealSteps(stage, entry.Report.Givens, result);
            if (view == View.Steps)
            {
                step = EditorGUILayout.IntSlider("단계", step, 0, result.Waves.Count);
            }

            float available = position.width - 270f;
            float cellSize = Mathf.Clamp(Mathf.Floor(Mathf.Min(available / stage.Width, 420f / stage.Height)), 6f, 36f);
            Rect area = GUILayoutUtility.GetRect(stage.Width * cellSize, stage.Height * cellSize, GUILayout.ExpandWidth(false));
            float gap = cellSize >= 10f ? 1f : 0f;
            var hidden = new Color(0.55f, 0.55f, 0.6f);
            var givenSet = new HashSet<int>(entry.Report.Givens);

            for (int cell = 0; cell < stage.CellCount; cell++)
            {
                int x = cell % stage.Width;
                int y = cell / stage.Width;
                var r = new Rect(area.x + x * cellSize, area.y + y * cellSize, cellSize - gap, cellSize - gap);
                Color color = Theme.ToColor(stage.Colors[stage.ColorAt(cell)].Color);
                bool shown = view == View.Picture ||
                             (view == View.Start && revealStep[cell] == 0) ||
                             (view == View.Steps && revealStep[cell] >= 0 && revealStep[cell] <= step);
                EditorGUI.DrawRect(r, shown ? color : hidden);

                if (view != View.Picture && givenSet.Contains(cell))
                {
                    Outline(r, Color.white, 2f);
                }

                if (view == View.Steps && step > 0 && revealStep[cell] == step)
                {
                    Outline(r, new Color(1f, 0.8f, 0.1f), 2f);
                }

                if (view != View.Picture && revealStep[cell] < 0)
                {
                    Outline(r, Color.red, 1f);
                }
            }

            Event e = Event.current;
            if (e.type == EventType.MouseMove || e.type == EventType.Repaint)
            {
                int previous = hoverCell;
                hoverCell = area.Contains(e.mousePosition)
                    ? Mathf.FloorToInt((e.mousePosition.y - area.y) / cellSize) * stage.Width +
                      Mathf.FloorToInt((e.mousePosition.x - area.x) / cellSize)
                    : -1;
                if (previous != hoverCell)
                {
                    Repaint();
                }
            }

            EditorGUILayout.LabelField(HoverText(stage, revealStep), EditorStyles.helpBox);
            EditorGUILayout.LabelField(
                "흰 테두리: givens   노란 테두리: 이번 단계에 확정된 칸   빨간 테두리: 추론으로 닿지 않는 칸", EditorStyles.miniLabel);
        }

        string HoverText(Stage stage, int[] revealStep)
        {
            if (hoverCell < 0 || hoverCell >= stage.CellCount)
            {
                return "칸에 마우스를 올리면 좌표·색·단서가 보입니다.";
            }

            var sb = new StringBuilder();
            sb.Append("(").Append(hoverCell % stage.Width).Append(", ").Append(hoverCell / stage.Width).Append(")  #")
                .Append(hoverCell).Append("  색 '").Append(stage.Colors[stage.ColorAt(hoverCell)].Key).Append("'  단서: ");
            bool any = false;
            for (int k = 0; k < stage.ColorCount; k++)
            {
                if (k == stage.BackgroundColor || stage.Clue(hoverCell, k) == 0)
                {
                    continue;
                }

                sb.Append(stage.Colors[k].Key).Append('×').Append(stage.Clue(hoverCell, k)).Append("  ");
                any = true;
            }

            if (!any)
            {
                sb.Append("없음(빈 칸 — 열리면 주변이 펼쳐짐)");
            }

            int s = revealStep[hoverCell];
            sb.Append("  |  ").Append(s < 0 ? "추론으로 안 열림" : s == 0 ? "처음부터 열림" : s + "단계에 열림");
            return sb.ToString();
        }

        /// <summary>칸마다 열리는 단계. 0 = 처음부터 열림(givens 와 그 펼침), -1 = 끝까지 안 열림.</summary>
        static int[] RevealSteps(Stage stage, int[] givens, SolveResult result)
        {
            var steps = new int[stage.CellCount];
            for (int i = 0; i < steps.Length; i++)
            {
                steps[i] = -1;
            }

            var initial = new bool[stage.CellCount];
            foreach (int g in givens)
            {
                stage.RevealWithFlood(g, initial, null);
            }

            for (int i = 0; i < initial.Length; i++)
            {
                if (initial[i])
                {
                    steps[i] = 0;
                }
            }

            for (int w = 0; w < result.Waves.Count; w++)
            {
                foreach (int cell in result.Waves[w])
                {
                    if (steps[cell] < 0)
                    {
                        steps[cell] = w + 1;
                    }
                }
            }

            return steps;
        }

        void DrawJson(Entry entry)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("JSON (고치면 바로 미리보기에 반영, 저장해야 파일에 쓰임)", EditorStyles.miniBoldLabel);
            jsonScroll = EditorGUILayout.BeginScrollView(jsonScroll, GUILayout.Height(260f));
            var style = new GUIStyle(EditorStyles.textArea) { font = EditorStyles.standardFont, wordWrap = false };
            string edited = EditorGUILayout.TextArea(entry.Text, style, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            if (edited != entry.Text)
            {
                entry.Text = edited;
                Analyze(entry);
            }
        }

        static void Save(Entry entry)
        {
            File.WriteAllText(entry.Path, entry.Text, new UTF8Encoding(false));
            entry.SavedText = entry.Text;
            AssetDatabase.ImportAsset(entry.Path);
            Analyze(entry);
        }

        void CreateStage()
        {
            Directory.CreateDirectory(StagesFolder);
            int number = entries.Count + 1;
            string id = "stage" + number.ToString("000");
            string path = StagesFolder + "/" + number.ToString("000") + "_" + id + ".json";
            string template = "{\n  \"id\": \"" + id + "\",\n  \"name\": \"새 그림\",\n  \"background\": \".\",\n" +
                              "  \"palette\": [\n    { \"key\": \".\", \"color\": \"#F3EEE4\" },\n" +
                              "    { \"key\": \"A\", \"color\": \"#6C5CE7\" }\n  ],\n  \"pixels\": [\n" +
                              "    \"........\",\n    \"..AAAA..\",\n    \".AAAAAA.\",\n    \".AA..AA.\",\n" +
                              "    \".AAAAAA.\",\n    \"..AAAA..\",\n    \"........\"\n  ],\n  \"givens\": []\n}\n";
            File.WriteAllText(path, template, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(path);
            Reload();
            selected = entries.FindIndex(e => e.Path == path);
        }

        static void Outline(Rect r, Color color, float width)
        {
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, width), color);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - width, r.width, width), color);
            EditorGUI.DrawRect(new Rect(r.x, r.y, width, r.height), color);
            EditorGUI.DrawRect(new Rect(r.xMax - width, r.y, width, r.height), color);
        }
    }
}
