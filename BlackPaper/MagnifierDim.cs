using System;
using System.Runtime.InteropServices;

namespace BlackPaper
{
    /// <summary>
    /// 基于 Windows 放大镜 API 的整屏压暗。
    ///
    /// 原理：MagSetFullscreenColorEffect 在 DWM 合成层对整个桌面做颜色矩阵变换，
    /// 因此开始菜单、搜索、任务视图、UAC、全屏独占游戏统统生效——这是普通
    /// topmost 窗口做不到的。
    ///
    /// 效果：使用对角缩放矩阵 (k, k, k)，即 result = c * k。
    /// 这正是「叠加纯黑遮罩」的数学等价形式，不改变色相、不改变对比度曲线，
    /// 只是整体线性变暗。
    /// </summary>
    internal static class MagnifierDim
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct MAGCOLOREFFECT
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            public float[] transform;
        }

        [DllImport("Magnification.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool MagInitialize();

        [DllImport("Magnification.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool MagUninitialize();

        [DllImport("Magnification.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool MagSetFullscreenColorEffect(ref MAGCOLOREFFECT effect);

        [DllImport("Magnification.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool MagGetFullscreenColorEffect(out MAGCOLOREFFECT effect);

        private static bool _initialized;

        /// <summary>API 是否可用（旧系统 / 某些虚拟显示环境下可能不可用）。</summary>
        public static bool IsAvailable { get; private set; }

        /// <summary>当前是否已经施加了非 1.0 的压暗效果。</summary>
        public static bool IsActive { get; private set; }

        /// <summary>初始化放大镜。成功返回 true。</summary>
        public static bool Initialize()
        {
            if (_initialized)
            {
                return IsAvailable;
            }

            _initialized = true;

            try
            {
                IsAvailable = MagInitialize();
            }
            catch (DllNotFoundException)
            {
                IsAvailable = false;
            }
            catch (EntryPointNotFoundException)
            {
                IsAvailable = false;
            }

            return IsAvailable;
        }

        /// <summary>
        /// 设置整屏压暗系数。level = 0 表示不变暗（原样），
        /// level = 1 表示完全变黑，中间值为线性变暗。
        /// </summary>
        public static bool SetDim(double level)
        {
            if (!_initialized && !Initialize())
            {
                return false;
            }

            if (!IsAvailable)
            {
                return false;
            }

            double clamped = Math.Max(0.0, Math.Min(1.0, level));

            // 亮度保留系数：1.0 - level
            // level=0   -> k=1.00 原画面
            // level=0.5 -> k=0.50 半暗
            // level=1   -> k=0.00 全黑
            float k = (float)(1.0 - clamped);

            MAGCOLOREFFECT effect = CreateScaleEffect(k);

            try
            {
                bool ok = MagSetFullscreenColorEffect(ref effect);
                if (ok)
                {
                    IsActive = k < 0.999f;
                }

                return ok;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>读回当前实际生效的亮度系数，用于校验。</summary>
        public static double GetCurrentScale()
        {
            if (!IsAvailable)
            {
                return 1.0;
            }

            try
            {
                if (MagGetFullscreenColorEffect(out MAGCOLOREFFECT e) &&
                    e.transform != null &&
                    e.transform.Length >= 25)
                {
                    return e.transform[0];
                }
            }
            catch
            {
                // 忽略
            }

            return 1.0;
        }

        /// <summary>恢复原始画面。</summary>
        public static void Restore()
        {
            if (!IsAvailable)
            {
                return;
            }

            try
            {
                MAGCOLOREFFECT identity = CreateScaleEffect(1.0f);
                MagSetFullscreenColorEffect(ref identity);
                IsActive = false;
            }
            catch
            {
                // 忽略
            }
        }

        public static void Shutdown()
        {
            if (!_initialized)
            {
                return;
            }

            Restore();

            try
            {
                MagUninitialize();
            }
            catch
            {
                // 忽略
            }

            IsAvailable = false;
            _initialized = false;
        }

        /// <summary>
        /// 构造 5x5 行主序颜色矩阵。
        /// 对角上 RGB 各乘 k，alpha 保持 1，偏移列全 0 —— 纯线性缩放，无色相/对比度变化。
        /// </summary>
        private static MAGCOLOREFFECT CreateScaleEffect(float k)
        {
            float[] t = new float[25];

            t[0] = k;    // R'
            t[6] = k;    // G'
            t[12] = k;   // B'
            t[18] = 1f;  // A'
            t[24] = 1f;  // 齐次坐标

            return new MAGCOLOREFFECT { transform = t };
        }
    }
}
