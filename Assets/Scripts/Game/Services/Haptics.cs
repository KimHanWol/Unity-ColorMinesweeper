using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>짧은 진동. 맞췄을 때는 가볍게, 틀렸을 때는 묵직하게 준다.</summary>
    public static class Haptics
    {
        public static bool Enabled = true;

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject vibrator;
        static int sdk;

        static AndroidJavaObject Vibrator
        {
            get
            {
                if (vibrator == null)
                {
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    }

                    using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                    {
                        sdk = version.GetStatic<int>("SDK_INT");
                    }
                }

                return vibrator;
            }
        }

        static void Vibrate(long milliseconds, int amplitude)
        {
            if (!Enabled)
            {
                return;
            }

            try
            {
                if (Vibrator == null)
                {
                    return;
                }

                if (sdk >= 26)
                {
                    using (var effect = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var oneShot = effect.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, amplitude))
                    {
                        Vibrator.Call("vibrate", oneShot);
                    }
                }
                else
                {
                    Vibrator.Call("vibrate", milliseconds);
                }
            }
            catch (System.Exception)
            {
                // 진동은 없어도 게임이 된다. 기기별 예외로 흐름을 끊지 않는다.
            }
        }

        /// <summary>
        /// Unity 는 Handheld.Vibrate 참조가 있을 때만 VIBRATE 권한을 매니페스트에 넣는다. 실제로 부르지는 않는다.
        /// </summary>
        static bool requestPermission;

        public static void EnsurePermissionReference()
        {
            if (requestPermission)
            {
                Handheld.Vibrate();
            }
        }
#else
        static void Vibrate(long milliseconds, int amplitude)
        {
        }

        public static void EnsurePermissionReference()
        {
        }
#endif

        public static void Tick() => Vibrate(12, 60);
        public static void Light() => Vibrate(18, 90);
        public static void Heavy() => Vibrate(60, 255);
        public static void Success() => Vibrate(40, 160);
    }
}
