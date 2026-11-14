using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 기기를 옆으로 돌렸을 때 숫자를 바로 세워 보이게 할 각도. 화면 배치는 세로 그대로 두고 숫자(<see cref="PixelText"/>)만
    /// 제자리에서 돌린다. 각도가 바뀌면 살짝 튕기며 돌아간다.
    /// 휴대폰의 자동 회전이 꺼져 있거나 설정의 "숫자 회전"을 끄면 돌리지 않는다(누워서 볼 때 엉뚱하게 돌지 않게).
    /// </summary>
    public static class DeviceTurn
    {
        const float TurnSeconds = 0.4f;

        /// <summary>자동 회전 설정을 다시 읽는 간격(초). 설정을 바꾸고 돌아오면 곧 반영된다.</summary>
        const float SettingCheckSeconds = 2f;

        static float from;
        static float target;
        static float startedAt = -1f;
        static float nextSettingCheck;
        static bool autoRotate = true;

        /// <summary>지금 숫자를 돌려 둘 각도(도, 반시계 방향이 +).</summary>
        public static float Angle { get; private set; }

        /// <summary>에디터 확인용. 값을 넣으면 기기 방향 대신 이 각도로 돌린다.</summary>
        public static float? DebugTarget;

        /// <summary><see cref="GameApp"/> 이 매 프레임 부른다.</summary>
        public static void Tick()
        {
            float now = Time.unscaledTime;
            if (now >= nextSettingCheck)
            {
                nextSettingCheck = now + SettingCheckSeconds;
                autoRotate = ReadAutoRotate();
            }

            // PlayerPrefs 는 메모리에서 읽으므로 매 프레임 물어도 가볍다.
            bool follow = autoRotate && Settings.TurnDigits;
            float wanted = DebugTarget ?? (follow ? TargetFor(Input.deviceOrientation) : 0f);
            if (!Mathf.Approximately(wanted, target))
            {
                from = Angle;
                target = wanted;
                startedAt = now;
            }

            if (startedAt < 0f)
            {
                return;
            }

            float t = Mathf.Clamp01((now - startedAt) / TurnSeconds);
            Angle = Mathf.LerpUnclamped(from, target, Ease.OutBack(t));
            if (t >= 1f)
            {
                Angle = target;
                startedAt = -1f;
            }
        }

        /// <summary>
        /// 기기 방향에 맞는 각도. 왼쪽으로 눕히면(위쪽이 왼편) 숫자를 시계 방향으로 돌려야 바로 선다.
        /// 거꾸로 들거나 바닥에 눕히거나 모를 때는 지금 각도를 그대로 둔다.
        /// </summary>
        static float TargetFor(DeviceOrientation orientation)
        {
            switch (orientation)
            {
                case DeviceOrientation.Portrait:
                    return 0f;
                case DeviceOrientation.LandscapeLeft:
                    return -90f;
                case DeviceOrientation.LandscapeRight:
                    return 90f;
                default:
                    return target;
            }
        }

        static bool ReadAutoRotate()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var resolver = activity.Call<AndroidJavaObject>("getContentResolver"))
                using (var system = new AndroidJavaClass("android.provider.Settings$System"))
                {
                    return system.CallStatic<int>("getInt", resolver, "accelerometer_rotation", 0) == 1;
                }
            }
            catch (System.Exception)
            {
                // 설정을 읽지 못하는 기기에서는 기기 방향만 따른다.
                return true;
            }
#else
            return true;
#endif
        }
    }
}
