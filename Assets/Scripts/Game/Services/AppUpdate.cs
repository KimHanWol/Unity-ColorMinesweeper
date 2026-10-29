using System;
using System.Collections;
using ColorMinesweeper.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 새 버전 안내. 메인 화면을 열 때 웹(GitHub Pages)에 올려 둔 버전 정보를 읽어 지금 버전과 비교한다.
    ///
    /// 버전 정보는 docs/version.json 이다: {"latest": 최신 버전 코드, "minimum": 이보다 낮으면 업데이트해야만 하는 코드}.
    /// 새 버전을 스토어에 올리고 배포가 끝나면 latest 를 올린다(스토어 배포보다 먼저 올리면, 받을 수 없는 업데이트를 안내하게 된다).
    /// 인터넷이 없거나 파일을 못 읽으면 조용히 넘어간다. 한 번 켠 동안에는 한 번만 확인한다.
    /// </summary>
    public static class AppUpdate
    {
        const string VersionUrl = "https://kimhanwol.github.io/Unity-ColorMinesweeper/version.json";

        [Serializable]
        sealed class VersionInfo
        {
            public int latest;
            public int minimum;
        }

        static bool checkedThisRun;

        /// <summary>메인 화면이 코루틴으로 돌린다. 안내 창을 띄웠으면 onShown 으로 넘긴다(뒤로 가기 처리용).</summary>
        public static IEnumerator Check(Transform parent, UiRoot ui, int order, Action<UiKit.Modal, bool> onShown)
        {
            int current = StoreLink.InstalledVersionCode();
            if (checkedThisRun || current <= 0)
            {
                yield break;
            }

            checkedThisRun = true;
            using (UnityWebRequest request = UnityWebRequest.Get(VersionUrl))
            {
                request.timeout = 6;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success || parent == null)
                {
                    yield break;
                }

                VersionInfo info;
                try
                {
                    info = JsonUtility.FromJson<VersionInfo>(request.downloadHandler.text);
                }
                catch (Exception)
                {
                    yield break;
                }

                UpdateNeed need = UpdateCheck.Compare(current, info.latest, info.minimum);
                if (need == UpdateNeed.None)
                {
                    yield break;
                }

                bool required = need == UpdateNeed.Required;
                UiKit.Modal modal = required
                    ? UiKit.Dialog(parent, ui, order, Loc.T("update.title"), Loc.T("update.required"), Loc.T("update.go"),
                        StoreLink.Open, null, null, true)
                    : UiKit.Dialog(parent, ui, order, Loc.T("update.title"), Loc.T("update.optional"), Loc.T("update.go"),
                        StoreLink.Open, Loc.T("update.later"), null);
                onShown?.Invoke(modal, required);
            }
        }
    }
}
