using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 화면 전환. 화면이 배경색 도트로 잘게 덮이며 사라졌다가(도트 그림이 뭉개지듯), 같은 순서로 도트가 걷히며 다음 화면이
    /// 나타난다. 번지는 모양(방향)과 덮였을 때 보이는 도트 무늬는 전환할 때마다 무작위로 고른다.
    /// 도트는 배경색에 가깝고 군데군데만 옅은 파스텔이 섞여서, 벽이 서는 느낌 없이 화면이 배경으로 녹아드는 것처럼 보인다.
    /// </summary>
    public sealed class TileWipe : MonoBehaviour
    {
        const float Cell = 1.25f;
        const int Order = 2000;

        /// <summary>한쪽 끝에서 반대쪽 끝까지 번지는 데 걸리는 시간과, 도트마다 조금씩 어긋나게 하는 흔들림.</summary>
        const float SweepSeconds = 0.16f;
        const float Jitter = 0.05f;
        const float CoverSeconds = 0.1f;
        const float OpenSeconds = 0.13f;

        /// <summary>다 덮인 채로 머무는 시간. 거의 바로 걷힌다.</summary>
        const float HoldSeconds = 0.04f;

        readonly List<Transform> tiles = new List<Transform>();
        readonly List<float> delays = new List<float>();

        /// <summary>도트마다 화면 안에서의 자리(왼쪽 아래 0,0 → 오른쪽 위 1,1). 번지는 모양을 고를 때 쓴다.</summary>
        readonly List<Vector2> spots = new List<Vector2>();
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
            PickPattern();
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
            PickPattern();
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
                }, Ease.InCubic, HoldSeconds + delays[i]);
            }

            Tween.Delay(this, HoldSeconds + longestDelay + OpenSeconds, () =>
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
            spots.Clear();
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
                    tiles.Add(tile.transform);
                    delays.Add(0f);
                    spots.Add(new Vector2(x / (float)(columns - 1), (rows - 1 - y) / (float)(rows - 1)));
                }
            }
        }

        /// <summary>
        /// 이번에 번질 모양을 무작위로 고른다: 대각선, 가로, 세로, 가운데에서 바깥으로(또는 반대로), 아무 데서나 흩어져서.
        /// 방향도 매번 뒤집어서 같은 전환이 연달아 나오는 일이 드물게 한다. 걸리는 시간은 어느 모양이든 같다.
        /// </summary>
        void PickPattern()
        {
            int pattern = UnityEngine.Random.Range(0, 5);
            bool flipX = UnityEngine.Random.value < 0.5f;
            bool flipY = UnityEngine.Random.value < 0.5f;
            longestDelay = 0f;
            for (int i = 0; i < tiles.Count; i++)
            {
                float x = flipX ? 1f - spots[i].x : spots[i].x;
                float y = flipY ? 1f - spots[i].y : spots[i].y;
                float along;
                switch (pattern)
                {
                    case 0:
                        along = (x + y) / 2f;
                        break;
                    case 1:
                        along = x;
                        break;
                    case 2:
                        along = y;
                        break;
                    case 3:
                        along = Mathf.Clamp01(Vector2.Distance(spots[i], new Vector2(0.5f, 0.5f)) / 0.7071f);
                        along = flipX ? 1f - along : along;
                        break;
                    default:
                        along = UnityEngine.Random.value;
                        break;
                }

                delays[i] = along * SweepSeconds + UnityEngine.Random.Range(0f, Jitter);
                longestDelay = Mathf.Max(longestDelay, delays[i]);
            }

            Repaint();
        }

        /// <summary>
        /// 덮였을 때 보이는 도트를 다시 칠한다. 색감은 늘 같다(배경색에 가깝고 군데군데 옅은 파스텔이 섞인 부드러운 무늬).
        /// 어느 도트가 밝고 어느 도트에 파스텔이 묻는지만 전환할 때마다 새로 뽑아서 무늬가 매번 달라진다.
        /// </summary>
        void Repaint()
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                Color color = Color.Lerp(Theme.BackgroundBottom, Theme.BackgroundTop, spots[i].y);
                color = Color.Lerp(color, Color.white, UnityEngine.Random.Range(0f, 0.35f));
                if (UnityEngine.Random.value < 0.14f)
                {
                    color = Color.Lerp(color, Theme.Pastels[UnityEngine.Random.Range(0, Theme.Pastels.Length)], 0.4f);
                }

                tiles[i].GetComponent<SpriteRenderer>().color = color;
            }
        }
    }
}
