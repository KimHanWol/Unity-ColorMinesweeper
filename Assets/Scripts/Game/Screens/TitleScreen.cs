using System.Collections.Generic;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 메인 화면. 가운데 작은 판이 하트를 파도처럼 계속 열어 보이며 게임이 무엇인지 보여 주고,
    /// 뒤로는 파스텔 도트가 천천히 떠오른다. 이어하기(다음 안 깬 스테이지)와 스테이지 선택으로 들어간다.
    /// </summary>
    public sealed class TitleScreen : ScreenBase
    {
        const float HeroTile = 0.78f;
        const float LoopSeconds = 5.5f;

        /// <summary>메인 화면 하트. '.' 은 배경, R 빨강, P 분홍(반짝임).</summary>
        static readonly string[] HeroPicture =
        {
            ".RR.RR.",
            "RPRRRRR",
            "RRRRRRR",
            ".RRRRR.",
            "..RRR..",
            "...R...",
        };

        static readonly Color[] FloatColors =
        {
            Theme.Hex(0xFF8FAB), Theme.Hex(0xFFD166), Theme.Hex(0x8ECAE6), Theme.Hex(0x95D5B2), Theme.Hex(0xC77DFF),
        };

        sealed class Floater
        {
            public Transform Transform;
            public float Speed;
            public float Spin;
            public float Sway;
            public float Phase;
        }

        readonly List<Floater> floaters = new List<Floater>();
        readonly List<SpriteRenderer> heroTiles = new List<SpriteRenderer>();
        readonly List<Color> heroColors = new List<Color>();
        readonly List<float> heroDelays = new List<float>();
        Transform hero;
        Transform menu;
        Transform logo;
        Transform settingsButton;
        Mascot mascot;
        float loopTime;

        /// <summary>판이 다 열리는 때(한 바퀴 안의 초). 이때 마스코트가 기뻐한다.</summary>
        const float HeroDoneAt = 1.5f;

        protected override void Build()
        {
            BuildFloaters();
            logo = Draw.Node(transform, "Logo");
            Label.Create(logo, "Title", "Pixel", Theme.Accent, 60, new Vector2(0f, 0.55f), 1.25f, TextAnchor.MiddleCenter, true);
            Label.Create(logo, "Title2", "Clue", Theme.Ink, 60, new Vector2(0f, -0.55f), 1.25f,
                TextAnchor.MiddleCenter, true);
            Label.Create(logo, "Tagline", Loc.T("title.tagline"), Theme.SubInk, 60, new Vector2(0f, -1.55f),
                0.38f)
                .FitWidth(Ui.Safe.width - 0.8f);

            BuildHero();
            BuildMenu();
            settingsButton = UiKit.IconButton(transform, "Settings", Icons.Settings,
                Vector2.zero, 80, () => SettingsPanel.Open(transform, Ui, 300, null, true)).transform;
            Layout();
            PlayEntrance();
        }

        public override void Layout()
        {
            Rect safe = Ui.Safe;
            logo.localPosition = new Vector3(safe.center.x, safe.yMax - 3.4f, 0f);
            hero.localPosition = new Vector3(safe.center.x, safe.center.y + 0.9f, 0f);
            menu.localPosition = new Vector3(safe.center.x, safe.yMin + 3.6f, 0f);
            settingsButton.localPosition = new Vector3(safe.xMax - 1.05f, safe.yMax - 1.05f, 0f);
        }

        void BuildFloaters()
        {
            Transform layer = Draw.Node(transform, "Floaters");
            for (int i = 0; i < 22; i++)
            {
                float size = Random.Range(0.25f, 0.7f);
                SpriteRenderer r = Draw.Sprite(layer, "Pixel", SpriteFactory.RoundedRect(0.25f),
                    Theme.WithAlpha(FloatColors[i % FloatColors.Length], Random.Range(0.25f, 0.5f)), 1,
                    new Vector2(Random.Range(-Ui.Width / 2f, Ui.Width / 2f), Random.Range(-UiRoot.Height / 2f, UiRoot.Height / 2f)),
                    new Vector2(size, size));
                floaters.Add(new Floater
                {
                    Transform = r.transform,
                    Speed = Random.Range(0.25f, 0.7f),
                    Spin = Random.Range(-40f, 40f),
                    Sway = Random.Range(0.1f, 0.4f),
                    Phase = Random.Range(0f, 10f),
                });
            }
        }

        void BuildHero()
        {
            hero = Draw.Node(transform, "Hero");
            int w = HeroPicture[0].Length;
            int h = HeroPicture.Length;
            var plateSize = new Vector2(w * HeroTile + 0.6f, h * HeroTile + 0.6f);
            Draw.Sprite(hero, "Shadow", SpriteFactory.SoftShadow(), Theme.Shadow, 20, new Vector2(0f, -0.25f), plateSize * 1.3f);
            Draw.Panel(hero, "Plate", plateSize, Theme.Plate, 21, Vector2.zero, 0.4f);
            // 마스코트가 판 뒤에서 고개를 내밀고 그림이 열리는 것을 구경한다.
            mascot = Mascot.Create(hero, "Mascot", new Vector2(plateSize.x / 2f - 1.2f, plateSize.y / 2f + 0.5f), 1.3f, 20);
            mascot.PopIn(0.5f);

            var center = new Vector2((w - 1) / 2f, (h - 1) / 2f);
            Color background = Theme.Hex(0xFFF1F3);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    char key = HeroPicture[y][x];
                    Color color = key == 'R' ? Theme.Hex(0xE8505B) : key == 'P' ? Theme.Hex(0xFFB3C1) : background;
                    var position = new Vector2((x - center.x) * HeroTile, (center.y - y) * HeroTile);
                    SpriteRenderer tile = Draw.Sprite(hero, "Tile", SpriteFactory.RaisedTile(), Theme.HiddenTile, 22, position,
                        Vector2.one * HeroTile * 0.9f);
                    heroTiles.Add(tile);
                    heroColors.Add(color);
                    heroDelays.Add(0.35f + Vector2.Distance(new Vector2(x, y), new Vector2(3f, 2.5f)) * 0.12f);
                }
            }
        }

        void BuildMenu()
        {
            menu = Draw.Node(transform, "Menu");
            int next = StageCatalog.NextToPlay();
            int cleared = StageCatalog.ClearedCount();
            bool started = cleared > 0;
            string playText = started ? Loc.F("title.continue", next + 1) : Loc.T("title.start");
            // 튜토리얼을 마치기 전에는 시작하기만 둔다. 목록에서 바로 스테이지에 들어가면 튜토리얼을 건너뛰게 된다.
            bool tutorialDone = TutorialDirector.IsDone;
            UiKit.Button(menu, "Play", playText, Icons.Play, Theme.Accent, Color.white,
                new Vector2(6.4f, 1.5f), new Vector2(0f, tutorialDone ? 1.1f : 0.3f), 70, () =>
                {
                    // 튜토리얼을 끝까지 마치기 전에는 시작하기를 누를 때마다 튜토리얼부터 한다(마치면 1번 스테이지로 이어진다).
                    if (TutorialDirector.IsDone)
                    {
                        App.ShowPlay(next);
                    }
                    else
                    {
                        App.ShowTutorial();
                    }
                });
            if (!tutorialDone)
            {
                return;
            }

            UiKit.Button(menu, "Stages", Loc.T("title.stages"), null, Color.white, Theme.Ink, new Vector2(6.4f, 1.25f),
                new Vector2(0f, -0.55f), 70, () => App.ShowSelect());

            Sprite star = Icons.Star;
            UiKit.IconLabel(menu, star, Theme.Gold, Loc.F("title.progress", cleared, StageCatalog.All.Count), Theme.SubInk, 0.36f,
                0.42f, 70, new Vector2(0f, -1.85f), false, Ui.Safe.width - 0.8f);
        }

        void PlayEntrance()
        {
            Transform l = logo;
            l.localScale = Vector3.zero;
            Tween.Run(l, 0.6f, t => l.localScale = Vector3.one * t, Ease.OutBack, 0.05f);
            Transform m = menu;
            Vector3 menuRest = m.localPosition;
            m.localPosition = menuRest + Vector3.down * 4f;
            Tween.Run(m, 0.55f, t => m.localPosition = Vector3.LerpUnclamped(menuRest + Vector3.down * 4f, menuRest, t),
                Ease.OutBack, 0.25f);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float top = UiRoot.Height / 2f + 1f;
            foreach (Floater f in floaters)
            {
                Vector3 p = f.Transform.localPosition;
                p.y += f.Speed * dt;
                p.x += Mathf.Sin((Time.unscaledTime + f.Phase) * 0.8f) * f.Sway * dt;
                if (p.y > top)
                {
                    p.y = -top;
                    p.x = Random.Range(-Ui.Width / 2f, Ui.Width / 2f);
                }

                f.Transform.localPosition = p;
                f.Transform.Rotate(0f, 0f, f.Spin * dt);
            }

            AnimateHero(dt);
        }

        /// <summary>
        /// 판이 닫힌 타일로 시작해 가운데부터 색이 번지며 열리고, 잠깐 멈췄다가 다시 닫힌다. 효과음은 내지 않는다(메인에서 반복되면 거슬린다).
        /// </summary>
        void AnimateHero(float dt)
        {
            float before = loopTime;
            loopTime = (loopTime + dt) % LoopSeconds;
            if (before < HeroDoneAt && loopTime >= HeroDoneAt)
            {
                mascot.Cheer(false);
            }

            float closeAt = LoopSeconds - 0.6f;
            for (int i = 0; i < heroTiles.Count; i++)
            {
                SpriteRenderer tile = heroTiles[i];
                float local = loopTime - heroDelays[i];
                float scale = HeroTile * 0.9f;
                if (loopTime >= closeAt)
                {
                    float k = Mathf.Clamp01((loopTime - closeAt) / 0.5f);
                    tile.color = Color.Lerp(heroColors[i], Theme.HiddenTile, k);
                    tile.sprite = k >= 1f ? SpriteFactory.RaisedTile() : tile.sprite;
                }
                else if (local >= 0f)
                {
                    float k = Mathf.Clamp01(local / 0.4f);
                    tile.sprite = SpriteFactory.RoundedRect(0.2f);
                    tile.color = Color.Lerp(Theme.HiddenTile, heroColors[i], Mathf.Clamp01(local / 0.2f));
                    scale *= Mathf.LerpUnclamped(0.55f, 1f, Ease.OutElastic(k));
                }
                else
                {
                    tile.sprite = SpriteFactory.RaisedTile();
                    tile.color = Theme.HiddenTile;
                }

                tile.transform.localScale = new Vector3(scale, scale, 1f);
            }

            float bob = Mathf.Sin(Time.unscaledTime * 1.6f) * 0.08f;
            Vector3 hp = hero.localPosition;
            hero.localPosition = new Vector3(hp.x, Ui.Safe.center.y + 0.9f + bob, 0f);
        }
    }
}
