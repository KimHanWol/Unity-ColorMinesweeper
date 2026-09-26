using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ColorMinesweeper.Game
{
    public static class Ease
    {
        public static float Linear(float t) => t;
        public static float OutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
        public static float InCubic(float t) => t * t * t;
        public static float InOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;

        public static float OutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        public static float InBack(float t)
        {
            const float c1 = 1.70158f;
            return (c1 + 1f) * t * t * t - c1 * t * t;
        }

        /// <summary>살짝 튕기며 멈추는 스프링. 칸이 열릴 때 "톡" 하는 느낌을 준다.</summary>
        public static float OutElastic(float t)
        {
            if (t <= 0f || t >= 1f)
            {
                return t;
            }

            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
        }

        /// <summary>0 → 1 → 0 으로 한 번 튀는 곡선. 흔들기·반짝임처럼 제자리로 돌아오는 연출에 쓴다.</summary>
        public static float Pulse(float t) => Mathf.Sin(t * Mathf.PI);
    }

    /// <summary>
    /// 에셋 스토어 의존 없이 쓰는 작은 트윈 실행기. owner 가 파괴되면 그 트윈도 조용히 사라진다.
    /// 같은 owner 에 새 트윈을 걸기 전에 <see cref="Kill"/> 하면 이전 연출과 섞이지 않는다.
    /// </summary>
    public sealed class Tween : MonoBehaviour
    {
        sealed class Item
        {
            public object Owner;
            public float Delay;
            public float Duration;
            public float Elapsed;
            public Action<float> Apply;
            public Func<float, float> Ease;
            public Action Done;
            public bool Dead;
        }

        static Tween instance;
        readonly List<Item> items = new List<Item>();
        readonly List<Item> adding = new List<Item>();

        static Tween Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("Tween");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<Tween>();
                }

                return instance;
            }
        }

        /// <summary>duration 동안 apply(0→1 에 ease 를 적용한 값)를 부른다. 끝에는 apply(1) 이 보장된다.</summary>
        public static void Run(object owner, float duration, Action<float> apply, Func<float, float> ease = null,
            float delay = 0f, Action done = null)
        {
            Instance.adding.Add(new Item
            {
                Owner = owner,
                Delay = delay,
                Duration = Mathf.Max(0.0001f, duration),
                Apply = apply,
                Ease = ease ?? global::ColorMinesweeper.Game.Ease.OutCubic,
                Done = done,
            });
        }

        public static void Delay(object owner, float seconds, Action action)
        {
            Run(owner, seconds, null, global::ColorMinesweeper.Game.Ease.Linear, 0f, action);
        }

        public static void Kill(object owner)
        {
            if (instance == null)
            {
                return;
            }

            foreach (Item item in instance.items)
            {
                if (ReferenceEquals(item.Owner, owner))
                {
                    item.Dead = true;
                }
            }

            instance.adding.RemoveAll(item => ReferenceEquals(item.Owner, owner));
        }

        void Update()
        {
            if (adding.Count > 0)
            {
                items.AddRange(adding);
                adding.Clear();
            }

            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < items.Count; i++)
            {
                Item item = items[i];
                if (item.Dead)
                {
                    continue;
                }

                if (item.Owner is Object unityObject && unityObject == null)
                {
                    item.Dead = true;
                    continue;
                }

                if (item.Delay > 0f)
                {
                    item.Delay -= dt;
                    if (item.Delay > 0f)
                    {
                        continue;
                    }

                    dt = -item.Delay;
                }

                item.Elapsed += dt;
                float t = Mathf.Clamp01(item.Elapsed / item.Duration);
                item.Apply?.Invoke(item.Ease(t));
                if (t >= 1f)
                {
                    item.Dead = true;
                    item.Done?.Invoke();
                }

                dt = Time.unscaledDeltaTime;
            }

            items.RemoveAll(item => item.Dead);
        }
    }
}
