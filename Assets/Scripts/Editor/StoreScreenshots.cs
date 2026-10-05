using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ColorMinesweeper.Game;
using UnityEditor;
using UnityEngine;

namespace ColorMinesweeper.EditorTools
{
    /// <summary>
    /// 스토어 스크린샷(1080x1920, 한국어·영어)을 자동으로 찍는다. 플레이 모드에 들어가 화면을 차례로 띄우고,
    /// 판은 솔버 순서대로 칸을 열어 풀던 모습을 만든 뒤 카메라를 그림 파일로 그린다.
    ///
    /// - 에디터: Tools > Pixel Clue > Store Screenshots
    /// - 배치 모드: Unity.exe -batchmode -projectPath . -executeMethod ColorMinesweeper.EditorTools.StoreScreenshots.CaptureAndQuit
    ///
    /// 배치 모드에는 폰 화면이 없어서 <see cref="ScreenInfo.Override"/> 로 화면 크기를 1080x1920 으로 친다.
    /// 결과는 Screenshots/Store/{ko,en}/ 에 쌓인다. 목록에는 앞의 몇 판을 깬 가짜 기록을 보여 주고, 찍는 동안에는 저장 파일을 쓰지 않는다.
    /// </summary>
    [InitializeOnLoad]
    public static class StoreScreenshots
    {
        const string RunningKey = "PixelClue.StoreShots.Running";
        const string QuitKey = "PixelClue.StoreShots.Quit";
        const string LanguageKey = "PixelClue.StoreShots.Language";
        static readonly Vector2Int StoreSize = new Vector2Int(1080, 1920);

        /// <summary>
        /// 찍을 화면 크기. 기본은 스토어 규격(1080x1920). 배치 모드에서 -shotSize 1080x2400 처럼 넘기면 그 크기로 찍어
        /// 길쭉한 폰에서 화면이 잘리지 않는지 확인할 수 있다(결과는 Screenshots/1080x2400/ 에).
        /// </summary>
        static Vector2Int Size
        {
            get
            {
                string[] args = Environment.GetCommandLineArgs();
                int at = Array.IndexOf(args, "-shotSize");
                if (at >= 0 && at + 1 < args.Length)
                {
                    string[] parts = args[at + 1].Split('x');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int w) && int.TryParse(parts[1], out int h))
                    {
                        return new Vector2Int(w, h);
                    }
                }

                return StoreSize;
            }
        }

        static string OutputRoot => Size == StoreSize
            ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Screenshots", "Store"))
            : Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Screenshots", Size.x + "x" + Size.y));

        sealed class Step
        {
            public float Wait;
            public Action Run;
        }

        static List<Step> steps;
        static int stepIndex;
        static double nextAt;

        static StoreScreenshots()
        {
            if (!SessionState.GetBool(RunningKey, false))
            {
                return;
            }

            // 플레이 모드에 들어가며 스크립트를 다시 불러와도 이어서 찍는다. 게임이 화면을 만들기 전에 크기를 정해 둔다.
            ScreenInfo.Override = Size;
            EditorApplication.update += Tick;
        }

        [MenuItem("Tools/Pixel Clue/Store Screenshots")]
        public static void Capture()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[StoreShots] 플레이 모드를 끄고 다시 실행해 주세요.");
                return;
            }

            SessionState.SetBool(RunningKey, true);
            ScreenInfo.Override = Size;
            EditorApplication.EnterPlaymode();
        }

        /// <summary>배치 모드용. 다 찍으면 Unity 를 끈다.</summary>
        public static void CaptureAndQuit()
        {
            SessionState.SetBool(QuitKey, true);
            Capture();
        }

        /// <summary>목록과 메인 화면에 보일 가짜 기록: 앞의 13판을 깬 것으로(대부분 별 3개, 가끔 2개). 저장 파일은 건드리지 않는다.</summary>
        const int ShownCleared = 13;

        static int ShownStars(string id)
        {
            int index = StageCatalog.IndexOf(id);
            if (index < 0 || index >= ShownCleared)
            {
                return 0;
            }

            return index % 5 == 3 ? 2 : 3;
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying || GameApp.Instance == null || GameApp.Instance.Current == null)
            {
                return;
            }

            // 찍는 동안에는 소리를 끈다. 화면을 한꺼번에 푸는 동작이 효과음을 겹쳐 내고, 배치 모드는 창이 없어도 스피커로 소리가 난다.
            AudioListener.volume = 0f;

            if (steps == null)
            {
                steps = BuildSteps();
                stepIndex = 0;
                nextAt = EditorApplication.timeSinceStartup + 1.5;
            }

            if (EditorApplication.timeSinceStartup < nextAt)
            {
                return;
            }

            if (stepIndex >= steps.Count)
            {
                Finish();
                return;
            }

            Step step = steps[stepIndex++];
            try
            {
                step.Run();
            }
            catch (Exception e)
            {
                Debug.LogError("[StoreShots] " + e);
            }

            nextAt = EditorApplication.timeSinceStartup + step.Wait;
        }

        static List<Step> BuildSteps()
        {
            SessionState.SetInt(LanguageKey, (int)Loc.Current);
            Game.Progress.DebugStars = ShownStars;
            var list = new List<Step>();
            foreach (Language language in new[] { Language.Korean, Language.English })
            {
                string folder = language == Language.Korean ? "ko" : "en";
                list.Add(new Step { Wait = 0.2f, Run = () => Loc.Current = language });

                // 1. 푸는 중(해바라기): 색을 하나 골라 단서 숫자가 보이게.
                AddPlay(list, "sunflower", 0.45f, play => play.DebugSelectColor(FirstPictureColor(play)));
                list.Add(Shot(folder, "01_play"));

                // 2. 완성 화면(더블 아이스크림). 가짜 기록을 쓰는 동안은 완성해도 저장하지 않는다.
                AddPlay(list, "icecream", 1f, null, 6f);
                list.Add(Shot(folder, "02_clear"));

                // 3. 힌트(앵무새): 전구를 누르고 칸을 고르는 모습.
                AddPlay(list, "parrot", 0.6f, play => play.DebugHintMode());
                list.Add(Shot(folder, "03_hint"));

                // 4. 메인 화면, 5. 스테이지 선택.
                list.Add(new Step { Wait = 2.5f, Run = () => GameApp.Instance.ShowTitle() });
                list.Add(Shot(folder, "04_title"));
                if (Size != StoreSize)
                {
                    // 확인용 크기로 찍을 때는 설정 창도 찍어 번역 문구가 잘리지 않는지 본다(스토어에는 올리지 않는다).
                    list.Add(new Step
                    {
                        Wait = 1f,
                        Run = () => SettingsPanel.Open(GameApp.Instance.Current.transform, GameApp.Instance.Ui, 300, null, true),
                    });
                    list.Add(Shot(folder, "07_settings"));
                }

                list.Add(new Step { Wait = 2.5f, Run = () => GameApp.Instance.ShowSelect() });
                list.Add(Shot(folder, "05_select"));

                // 6. 작은 판(오렌지)을 막 풀기 시작한 모습.
                AddPlay(list, "orange", 0.3f, play => play.DebugSelectColor(FirstPictureColor(play)));
                list.Add(Shot(folder, "06_play_small"));
            }

            return list;
        }

        static void AddPlay(List<Step> list, string id, float fraction, Action<PlayScreen> after, float wait = 2f)
        {
            list.Add(new Step { Wait = 1.5f, Run = () => GameApp.Instance.ShowPlay(StageCatalog.IndexOf(id)) });
            list.Add(new Step
            {
                Wait = wait,
                Run = () =>
                {
                    PlayScreen play = Play();
                    play.DebugShowHints(3);
                    play.DebugSolve(fraction);
                    after?.Invoke(play);
                },
            });
        }

        static PlayScreen Play()
        {
            return (PlayScreen)GameApp.Instance.Current;
        }

        /// <summary>아직 칠할 칸이 남은 그림 색(배경이 아닌 색) 중 첫 색.</summary>
        static int FirstPictureColor(PlayScreen play)
        {
            for (int k = 0; k < play.Stage.ColorCount; k++)
            {
                if (k != play.Stage.BackgroundColor && play.Session.Remaining(k) > 0)
                {
                    return k;
                }
            }

            return play.Stage.BackgroundColor;
        }

        static Step Shot(string folder, string name)
        {
            return new Step { Wait = 0.3f, Run = () => Save(Path.Combine(OutputRoot, folder, name + ".png")) };
        }

        /// <summary>켜진 카메라를 깊이 순서대로 한 그림에 그린다(판 → UI).</summary>
        static void Save(string path)
        {
            var target = new RenderTexture(Size.x, Size.y, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            IEnumerable<Camera> cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                .Where(c => c.isActiveAndEnabled)
                .OrderBy(c => c.depth);
            foreach (Camera camera in cameras)
            {
                RenderTexture previous = camera.targetTexture;
                camera.targetTexture = target;
                ScreenInfo.ApplyAspect(camera);
                camera.Render();
                camera.targetTexture = previous;
            }

            RenderTexture active = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(Size.x, Size.y, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, Size.x, Size.y), 0, 0);
            texture.Apply();
            RenderTexture.active = active;

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            Debug.Log("[StoreShots] " + path);
        }

        static void Finish()
        {
            EditorApplication.update -= Tick;
            steps = null;
            Loc.Current = (Language)SessionState.GetInt(LanguageKey, (int)Loc.Current);
            SessionState.EraseBool(RunningKey);
            Game.Progress.DebugStars = null;
            AudioListener.volume = 1f;
            ScreenInfo.Override = null;
            bool quit = SessionState.GetBool(QuitKey, false);
            SessionState.EraseBool(QuitKey);
            Debug.Log("[StoreShots] 다 찍었습니다: " + OutputRoot);
            EditorApplication.ExitPlaymode();
            if (quit)
            {
                EditorApplication.delayCall += () => EditorApplication.Exit(0);
            }
        }
    }
}
