using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 화면 전환. 화면이 배경색 도트로 잘게 덮이며 사라졌다가(도트 그림이 뭉개지듯), 같은 순서로 도트가 걷히며 다음 화면이
    /// 나타난다. 도트는 배경과 같은 색이라 벽이 서는 느낌 없이 화면이 배경으로 녹아드는 것처럼 보이고,
    /// 군데군데 섞인 파스텔 도트가 배경에 떠다니는 타일과 이어진다.
    /// </summary>
    public sealed class TileWipe : MonoBehaviour
    {
        const float Cell = 1.25f;
        const int Order = 2000;

        /// <summary>대각선으로 번지는 데 걸리는 시간과, 줄마다 조금씩 어긋나게 하는 흔들림.</summary>
        const float SweepSeconds = 0.16f;
        const float Jitter = 0.05f;
        const float CoverSeconds = 0.1f;
        const float OpenSeconds = 0.13f;

        readonly List<Transform> tiles = new List<Transform>();
        readonly List<float> delays = new List<float>();
        UiRoot ui;
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
            wipe.blocker = UiButton.Attach(Draw.Node(root, "Blocker"), Vector2.one, Order, null);
            wipe.blocker.Pressable = false;
            wipe.blocker.enabled = false;
            return wipe;
        }

        /// <summary>
        /// 도트로 화면을 덮은 뒤 swap(화면 바꾸기)을 부르고, 이어서 도트를 걷는다.
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
            float full = Cell * 1.04f;
            for (int i = 0; i < tiles.Count; i++)
            {
                Transform tile = tiles[i];
                Tween.Kill(tile);
                Vector3 from = tile.localScale;
                Tween.Run(tile, CoverSeconds, t => tile.localScale = Vector3.Lerp(from, new Vector3(full, full, 1f), t),
                    Ease.OutCubic, delays[i]);
            }

            Tween.Delay(this, longestDelay + CoverSeconds + 0.02f, () =>
            {
                covering = false;
                Action action = pending;
                pending = null;
                action?.Invoke();
                Open();
            });
        }

        /// <summary>앱을 켰을 때: 덮인 상태에서 시작해 첫 화면이 도트가 걷히며 나타난다.</summary>
        public void OpenFromCovered()
        {
            Rebuild();
            float full = Cell * 1.04f;
            foreach (Transform tile in tiles)
            {
                tile.localScale = new Vector3(full, full, 1f);
            }

            Busy = true;
            blocker.enabled = true;
            Open();
        }

        /// <summary>덮인 순서 그대로 걷는다. 먼저 덮인 쪽부터 열려서 한 방향으로 쓸고 지나가는 것처럼 보인다.</summary>
        void Open()
        {
            float full = Cell * 1.04f;
            for (int i = 0; i < tiles.Count; i++)
            {
                Transform tile = tiles[i];
                Tween.Run(tile, OpenSeconds, t =>
                {
                    float s = full * (1f - t);
                    tile.localScale = new Vector3(s, s, 1f);
                }, Ease.InCubic, 0.04f + delays[i]);
            }

            Tween.Delay(this, 0.04f + longestDelay + OpenSeconds, () =>
            {
                Busy = false;
                blocker.enabled = false;
            });
        }

        /// <summary>처음 쓸 때와 화면 비율이 바뀌었을 때(회전, 창 크기) 도트를 화면에 맞춰 다시 깐다.</summary>
        void Rebuild()
        {
            int columns = Mathf.CeilToInt(ui.Width / Cell) + 1;
            int rows = Mathf.CeilToInt(UiRoot.Height / Cell) + 1;
            blocker.Size = new Vector2(ui.Width + 2f, UiRoot.Height + 2f);
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
                    // 배경 그라데이션과 같은 색이라 덮였을 때 빈 배경처럼 보인다. 일부만 파스텔로 물들인다.
                    Color color = Color.Lerp(Theme.BackgroundTop, Theme.BackgroundBottom, (float)y / (rows - 1));
                    color = Color.Lerp(color, Color.white, UnityEngine.Random.Range(0f, 0.35f));
                    if (UnityEngine.Random.value < 0.14f)
                    {
                        color = Color.Lerp(color, Theme.Pastels[UnityEngine.Random.Range(0, Theme.Pastels.Length)], 0.4f);
                    }

                    SpriteRenderer tile = Draw.Sprite(transform, "Dot", SpriteFactory.Square(), color, Order, position,
                        Vector2.zero);
                    // 왼쪽 아래에서 오른쪽 위로 번진다.
                    float along = (x + (rows - 1 - y)) / (float)(columns + rows - 2);
                    float delay = along * SweepSeconds + UnityEngine.Random.Range(0f, Jitter);
                    longestDelay = Mathf.Max(longestDelay, delay);
                    tiles.Add(tile.transform);
                    delays.Add(delay);
                }
            }
        }
    }
}
