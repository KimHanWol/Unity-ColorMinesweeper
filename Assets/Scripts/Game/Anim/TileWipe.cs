using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 화면 전환. 아직 안 열린 칸과 같은 타일이 대각선으로 화면을 덮었다가, 다음 화면 위에서 다시 열리듯 걷힌다.
    /// 게임의 핵심 동작(타일이 열리며 그림이 드러난다)을 화면을 넘길 때도 그대로 쓴다.
    /// </summary>
    public sealed class TileWipe : MonoBehaviour
    {
        const float Cell = 2.2f;
        const int Order = 2000;
        const float StepDelay = 0.008f;
        const float CoverSeconds = 0.14f;
        const float OpenSeconds = 0.2f;

        readonly List<Transform> tiles = new List<Transform>();
        readonly List<float> delays = new List<float>();
        UiRoot ui;
        SpriteRenderer backing;
        UiButton blocker;
        float longestDelay;
        int builtColumns;
        Action pending;
        bool covering;

        /// <summary>덮거나 걷는 중인지. 이 동안에는 화면 입력을 받지 않는다.</summary>
        public bool Busy { get; private set; }

        public static TileWipe Create(Transform parent, UiRoot ui)
        {
            Transform root = UiRoot.NewLayerRoot("TileWipe");
            root.SetParent(parent, false);
            var wipe = root.gameObject.AddComponent<TileWipe>();
            wipe.ui = ui;
            // 둥근 타일 사이로 뒤 화면이 비치지 않게 타일 밑에 깔아 두는 바탕.
            wipe.backing = Draw.Sprite(root, "Backing", SpriteFactory.Square(), Color.clear, Order - 1);
            wipe.blocker = UiButton.Attach(wipe.backing.transform, Vector2.one, Order, null);
            wipe.blocker.Pressable = false;
            wipe.blocker.enabled = false;
            return wipe;
        }

        /// <summary>
        /// 타일로 화면을 덮은 뒤 swap(화면 바꾸기)을 부르고, 이어서 타일을 걷는다.
        /// 덮는 도중에 다시 부르면 마지막에 요청한 화면으로 간다.
        /// </summary>
        public void Run(Action swap)
        {
            pending = swap;
            if (covering)
            {
                return;
            }

            Rebuild();
            Busy = true;
            covering = true;
            blocker.enabled = true;
            Tween.Kill(this);
            Tween.Kill(backing);
            foreach (Transform tile in tiles)
            {
                Tween.Kill(tile);
            }

            for (int i = 0; i < tiles.Count; i++)
            {
                Transform tile = tiles[i];
                Vector3 from = tile.localScale;
                Tween.Run(tile, CoverSeconds, t => tile.localScale = Vector3.LerpUnclamped(from, Vector3.one * Cell, t),
                    Ease.OutBack, delays[i]);
            }

            float coverTime = longestDelay + CoverSeconds;
            Color solid = Color.Lerp(Theme.HiddenTile, Color.black, 0.1f);
            float fromAlpha = backing.color.a;
            Tween.Run(this, coverTime, t => backing.color = Theme.WithAlpha(solid, Mathf.Lerp(fromAlpha, 1f, t * t)), Ease.Linear,
                0f, () =>
                {
                    covering = false;
                    Action action = pending;
                    pending = null;
                    action?.Invoke();
                    Open(solid);
                });
        }

        /// <summary>앱을 켰을 때: 덮인 상태에서 시작해 첫 화면이 타일이 열리듯 나타난다.</summary>
        public void OpenFromCovered()
        {
            Rebuild();
            foreach (Transform tile in tiles)
            {
                tile.localScale = Vector3.one * Cell;
            }

            Busy = true;
            blocker.enabled = true;
            Open(Color.Lerp(Theme.HiddenTile, Color.black, 0.1f));
        }

        void Open(Color solid)
        {
            // 바탕은 타일이 줄어들기 시작할 때 같이 옅어진다(바로 끄면 타일 틈으로 새 화면이 번쩍 비친다).
            Tween.Run(backing, 0.16f, t => backing.color = Theme.WithAlpha(solid, 1f - t), Ease.Linear, 0.05f);
            for (int i = 0; i < tiles.Count; i++)
            {
                Transform tile = tiles[i];
                Tween.Run(tile, OpenSeconds, t => tile.localScale = Vector3.one * (Cell * (1f - t)), Ease.InBack,
                    0.05f + delays[i]);
            }

            Tween.Delay(this, 0.05f + longestDelay + OpenSeconds, () =>
            {
                Busy = false;
                blocker.enabled = false;
            });
        }

        /// <summary>화면 비율이 바뀌었으면(회전, 창 크기) 타일 수를 다시 맞춘다.</summary>
        void Rebuild()
        {
            int columns = Mathf.CeilToInt(ui.Width / Cell) + 1;
            int rows = Mathf.CeilToInt(UiRoot.Height / Cell) + 1;
            backing.transform.localScale = new Vector3(ui.Width + 2f, UiRoot.Height + 2f, 1f);
            blocker.Size = Vector2.one;
            if (columns == builtColumns)
            {
                return;
            }

            builtColumns = columns;
            foreach (Transform tile in tiles)
            {
                Destroy(tile.gameObject);
            }

            tiles.Clear();
            delays.Clear();
            longestDelay = 0f;
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    var position = new Vector2((x - (columns - 1) / 2f) * Cell, ((rows - 1) / 2f - y) * Cell);
                    SpriteRenderer tile = Draw.Sprite(transform, "Tile", SpriteFactory.RaisedTile(), Theme.HiddenTile, Order,
                        position, Vector2.zero);
                    float delay = (x + y) * StepDelay;
                    longestDelay = Mathf.Max(longestDelay, delay);
                    tiles.Add(tile.transform);
                    delays.Add(delay);
                }
            }
        }
    }
}
