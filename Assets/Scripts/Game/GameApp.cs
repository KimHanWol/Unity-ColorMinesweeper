using System;
using ColorMinesweeper.Core;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 진입점. 씬에 아무것도 없어도 실행되면 스스로 카메라·입력·화면을 만든다(화면은 코드로 조립한다는 원칙, README 참고).
    /// </summary>
    public sealed class GameApp : MonoBehaviour
    {
        /// <summary>Unity 기본 "UI" 레이어. UI 카메라만 이 레이어를 그린다.</summary>
        public const int UiLayer = 5;

#if UNITY_EDITOR
        /// <summary>에디터의 Stage Preview 창이 시험 플레이할 스테이지 JSON 을 넣어 두는 곳.</summary>
        public const string PlaytestKey = "ColorMinesweeper.PlaytestJson";
#endif

        public static GameApp Instance { get; private set; }

        public UiRoot Ui { get; private set; }
        public BoardCamera BoardCamera { get; private set; }

        PointerInput pointer;
        ScreenBase current;
        TileWipe wipe;

        /// <summary>지금 떠 있는 화면(스토어 스크린샷 도구가 쓴다).</summary>
        public ScreenBase Current => current;
        Vector2Int lastScreenSize;
        Rect lastSafeArea;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<GameApp>() != null)
            {
                return;
            }

            new GameObject("GameApp").AddComponent<GameApp>();
        }

        void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            Input.multiTouchEnabled = true;
            Haptics.EnsurePermissionReference();
            Settings.Apply();

            Sfx.Create(transform);
            Music.Create(transform);
            Settings.Apply();
            BoardCamera = BoardCamera.Create(transform);
            Backdrop.Create(BoardCamera.Camera);
            Ui = UiRoot.Create(transform);
            wipe = TileWipe.Create(transform, Ui);
            pointer = gameObject.AddComponent<PointerInput>();
            pointer.UiCamera = Ui.Camera;
            lastScreenSize = new Vector2Int(ScreenInfo.Width, ScreenInfo.Height);
            lastSafeArea = ScreenInfo.SafeArea;
        }

        void Start()
        {
#if GOOGLE_MOBILE_ADS
            // 광고 SDK 는 첫 화면을 띄우는 동안 뒤에서 준비한다. 준비되기 전에는 자리 표시 광고가 대신한다.
            AdMobAds.Initialize();
#endif
            // 광고 제거 상품 가격을 받아 오고, 예전에 산 기록이 있으면 되살린다.
            RemoveAdsStore.Initialize();
            if (TryStartPlaytest())
            {
                return;
            }

            // 첫 화면은 덮여 있던 타일이 열리며 나타난다.
            Open<TitleScreen>(null, true);
            wipe.OpenFromCovered();
        }

        void Update()
        {
            // 안드로이드 뒤로 가기 버튼은 Escape 로 들어온다.
            if (Input.GetKeyDown(KeyCode.Escape) && !wipe.Busy)
            {
                current?.OnBack();
            }

            var size = new Vector2Int(ScreenInfo.Width, ScreenInfo.Height);
            if (size != lastScreenSize || ScreenInfo.SafeArea != lastSafeArea)
            {
                lastScreenSize = size;
                lastSafeArea = ScreenInfo.SafeArea;
                Ui.Refresh();
                current?.Layout();
            }
        }

        public void ShowTutorial()
        {
            Open<PlayScreen>(screen => screen.Setup(TutorialDirector.CreateStage(), -1, true));
        }

        public void ShowTitle()
        {
            Open<TitleScreen>(null);
        }

        public void ShowSelect()
        {
            Open<StageSelectScreen>(null);
        }

        /// <summary>언어를 바꾼 뒤: 지금 화면(메인·목록)을 새 언어로 다시 그리고 설정 창을 다시 열어 둔다.</summary>
        public void RefreshForLanguage()
        {
            ScreenBase screen = current;
            RefreshStageList();
            if (current != screen)
            {
                SettingsPanel.Open(current.transform, Ui, 300, null, true);
            }
        }

        /// <summary>목록·메인 화면을 보고 있으면 다시 그린다(치트로 잠금·기록이 바뀌었을 때). 플레이 중이면 그대로 둔다.</summary>
        public void RefreshStageList()
        {
            // 같은 화면을 다시 그리는 것이라 전환 연출 없이 바로 바꾼다.
            if (current is StageSelectScreen)
            {
                Open<StageSelectScreen>(null, true);
            }
            else if (current is TitleScreen)
            {
                Open<TitleScreen>(null, true);
            }
        }

        public void ShowPlay(int index)
        {
            ShowPlay(StageCatalog.All[index], index);
        }

        public void ShowPlay(Stage stage, int index)
        {
            Open<PlayScreen>(screen => screen.Setup(stage, index));
        }

        /// <summary>
        /// 화면을 바꾼다. 보통은 타일이 화면을 덮었다가 걷히는 전환(<see cref="TileWipe"/>)을 거치고,
        /// instant 면 바로 바꾼다(첫 화면, 같은 화면 새로 그리기).
        /// </summary>
        void Open<T>(Action<T> setup, bool instant = false) where T : ScreenBase
        {
            if (!instant && current != null)
            {
                wipe.Run(() => Open(setup, true));
                return;
            }

            if (current != null)
            {
                Destroy(current.gameObject);
            }

            Transform root = UiRoot.NewLayerRoot(typeof(T).Name);
            root.SetParent(transform, false);
            var screen = root.gameObject.AddComponent<T>();
            setup?.Invoke(screen);
            screen.Init(this);
            current = screen;
            pointer.Handler = screen;
        }

        bool TryStartPlaytest()
        {
#if UNITY_EDITOR
            string json = UnityEditor.SessionState.GetString(PlaytestKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            UnityEditor.SessionState.EraseString(PlaytestKey);
            try
            {
                ShowPlay(StageSerializer.Parse(json), -1);
                return true;
            }
            catch (StageFormatException e)
            {
                Debug.LogError("[Playtest] " + e.Message);
            }
#endif
            return false;
        }
    }
}
