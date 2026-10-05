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
        public static IShareSheet Sheet { get; set; } =
#if UNITY_ANDROID && !UNITY_EDITOR
            new AndroidShareSheet();
#else
            new FileShareSheet();
#endif

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

            string path = Path.Combine(Application.temporaryCachePath, "pixelclue-share.png");
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

#if UNITY_ANDROID && !UNITY_EDITOR
    /// <summary>
    /// Android 기본 공유 창. 찍은 이미지를 FileProvider(Plugins/Android/PixelClueShare.androidlib)로 내보내
    /// 받는 앱이 읽을 수 있게 하고, 문구와 함께 ACTION_SEND 로 보낸다.
    /// </summary>
    public sealed class AndroidShareSheet : IShareSheet
    {
        const int FlagGrantReadUriPermission = 1;

        public void Share(string imagePath, string text)
        {
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var file = new AndroidJavaObject("java.io.File", imagePath))
                using (var provider = new AndroidJavaClass("androidx.core.content.FileProvider"))
                using (var intentClass = new AndroidJavaClass("android.content.Intent"))
                using (var clipData = new AndroidJavaClass("android.content.ClipData"))
                {
                    string authority = activity.Call<string>("getPackageName") + ".share";
                    AndroidJavaObject uri = provider.CallStatic<AndroidJavaObject>("getUriForFile", activity, authority, file);
                    var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.SEND");
                    intent.Call<AndroidJavaObject>("setType", "image/png");
                    intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.STREAM", uri);
                    intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.TEXT", text);
                    // 공유 창을 거쳐 고른 앱에도 읽기 권한이 넘어가도록 ClipData 에도 싣는다.
                    intent.Call("setClipData", clipData.CallStatic<AndroidJavaObject>("newRawUri", string.Empty, uri));
                    intent.Call<AndroidJavaObject>("addFlags", FlagGrantReadUriPermission);
                    AndroidJavaObject chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, Loc.T("clear.share"));
                    activity.Call("startActivity", chooser);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Share] 공유 창을 열지 못했습니다: " + e.Message);
            }
        }
    }
#endif
}
