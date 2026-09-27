using System;
using System.Collections.Generic;
using ColorMinesweeper.Core;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>Resources/Stages 의 JSON 을 파일 이름 순으로 읽는다. 형식이 틀린 파일은 건너뛰고 로그를 남긴다.</summary>
    public static class StageCatalog
    {
        static List<Stage> stages;

        public static IReadOnlyList<Stage> All
        {
            get
            {
                if (stages == null)
                {
                    Load();
                }

                return stages;
            }
        }

        public static int IndexOf(string id)
        {
            for (int i = 0; i < All.Count; i++)
            {
                if (All[i].Id == id)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>앞 스테이지를 깼거나 첫 스테이지면 열려 있다. 개발용 치트로 전부 열 수도 있다.</summary>
        public static bool IsUnlocked(int index)
        {
            return index == 0 || (Cheats.UnlockAll && index < All.Count) || (index > 0 && index < All.Count && Progress.IsCleared(All[index - 1].Id));
        }

        /// <summary>이어하기로 들어갈 스테이지: 아직 못 깬 첫 스테이지. 다 깼으면 마지막 스테이지.</summary>
        public static int NextToPlay()
        {
            for (int i = 0; i < All.Count; i++)
            {
                if (!Progress.IsCleared(All[i].Id))
                {
                    return i;
                }
            }

            return Math.Max(0, All.Count - 1);
        }

        public static int ClearedCount()
        {
            int count = 0;
            foreach (Stage stage in All)
            {
                if (Progress.IsCleared(stage.Id))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>givens 가 비어 있는 스테이지는 id 로 만든 시드로 생성한다(에디터 도구와 같은 결과).</summary>
        public static int[] GivensFor(Stage stage)
        {
            if (stage.Givens.Length == 0)
            {
                stage.Givens = GivensGenerator.Generate(stage);
            }

            return stage.Givens;
        }

        static void Load()
        {
            stages = new List<Stage>();
            TextAsset[] files = Resources.LoadAll<TextAsset>("Stages");
            Array.Sort(files, (a, b) => string.CompareOrdinal(a.name, b.name));
            foreach (TextAsset file in files)
            {
                try
                {
                    stages.Add(StageSerializer.Parse(file.text));
                }
                catch (StageFormatException e)
                {
                    Debug.LogError("[Stage] " + file.name + ": " + e.Message);
                }
            }
        }
    }
}
