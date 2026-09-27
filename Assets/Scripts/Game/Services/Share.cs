using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 이미지와 문구를 다른 앱(메신저, SNS)으로 보낸다. 게임 코드는 이 인터페이스만 알고,
    /// 실제 기기의 공유 창은 구현체를 바꿔 끼운다(광고와 같은 방식).
    /// </summary>
    public interface IShareSheet
    {
        /// <param name="imagePath">앱 캐시에 저장한 PNG 경로.</param>
        void Share(string imagePath, string text);
    }

    /// <summary>
    /// 결과 공유. 완성 화면을 그대로 찍어서 보낸다. 찍는 동안 버튼은 잠깐 숨겨 그림과 결과만 남긴다.
    /// </summary>
    public static class Share
    {
        public static IShareSheet Sheet { get; set; } = new FileShareSheet();

        static bool busy;

        /// <param name="host">코루틴을 돌릴 화면.</param>
        /// <param name="hide">찍을 때 숨길 것(버튼 등).</param>
        public static void Screen(MonoBehaviour host, Transform[] hide, string text)
        {
            if (busy || host == null)
            {
                return;
            }

            host.StartCoroutine(Capture(hide, text));
        }

        static IEnumerator Capture(Transform[] hide, string text)
        {
            busy = true;
            foreach (Transform t in hide)
            {
                if (t != null)
                {
                    t.gameObject.SetActive(false);
                }
            }

            yield return new WaitForEndOfFrame();
            Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();

            foreach (Transform t in hide)
            {
                if (t != null)
                {
                    t.gameObject.SetActive(true);
                }
            }

            string path = Path.Combine(Application.temporaryCachePath, "colorsweeper-share.png");
            try
            {
                File.WriteAllBytes(path, shot.EncodeToPNG());
                Sheet.Share(path, text);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Share] 공유 이미지를 만들지 못했습니다: " + e.Message);
            }
            finally
            {
                UnityEngine.Object.Destroy(shot);
                busy = false;
            }
        }
    }

    /// <summary>
    /// 기기 공유 창을 붙이기 전까지 쓰는 자리. 찍은 이미지 경로와 문구를 로그로 남기고, 에디터에서는 파일을 연다.
    /// </summary>
    public sealed class FileShareSheet : IShareSheet
    {
        public void Share(string imagePath, string text)
        {
            Debug.Log("[Share] 공유 자리(아직 공유 창 없음) — " + imagePath + "\n" + text);
            if (Application.isEditor)
            {
                Application.OpenURL("file://" + imagePath);
            }
        }
    }
}
