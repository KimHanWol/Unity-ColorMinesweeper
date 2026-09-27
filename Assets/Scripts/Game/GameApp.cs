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
            Ui = UiRoot.Create(transform);
            pointer = gameObject.AddComponent<PointerInput>();
            pointer.UiCamera = Ui.Camera;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            lastSafeArea = Screen.safeArea;
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

            ShowTitle();
        }

        void Update()
        {
            // 안드로이드 뒤로 가기 버튼은 Escape 로 들어온다.
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                current?.OnBack();
            }

            var size = new Vector2Int(Screen.width, Screen.height);
            if (size != lastScreenSize || Screen.safeArea != lastSafeArea)
            {
                lastScreenSize = size;
                lastSafeArea = Screen.safeArea;
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
            if (current is StageSelectScreen)
            {
                ShowSelect();
            }
            else if (current is TitleScreen)
            {
                ShowTitle();
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

        void Open<T>(Action<T> setup) where T : ScreenBase
        {
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
