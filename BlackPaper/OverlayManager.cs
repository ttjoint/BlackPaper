using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace BlackPaper
{
    /// <summary>遮罩工作模式。</summary>
    internal enum OverlayMode
    {
        /// <summary>放大镜颜色矩阵，整屏线性变暗，可覆盖开始菜单/UAC/全屏独占。</summary>
        Dim = 0,

        /// <summary>分层黑膜窗口，只覆盖普通应用窗口。</summary>
        Curtain = 1,
    }

    /// <summary>
    /// 遮罩管理器。支持两种实现，并在运行时切换。
    /// </summary>
    internal sealed class OverlayManager : IDisposable
    {
        private readonly List<OverlayForm> _forms = new List<OverlayForm>();
        private readonly Timer _reassertTimer;
        private readonly NativeMethods.WinEventDelegate _winEventProc;
        private IntPtr _foregroundHook;
        private IntPtr _showHook;
        private bool _disposed;

        public double Opacity { get; private set; }
        public bool Enabled { get; private set; } = true;

        /// <summary>当前模式。切换时自动迁移当前亮度。</summary>
        public OverlayMode Mode { get; private set; }

        /// <summary>一旦 Dim 模式不可用（例如旧系统），自动降级到 Curtain。</summary>
        public bool DimAvailable => MagnifierDim.IsAvailable;

        public OverlayManager(OverlayMode mode)
        {
            Mode = mode;

            _winEventProc = OnWinEvent;

            if (!MagnifierDim.Initialize() && Mode == OverlayMode.Dim)
            {
                // 环境不支持放大镜，安静降级
                Mode = OverlayMode.Curtain;
            }

            if (Mode == OverlayMode.Curtain)
            {
                InstallHooks();
                _reassertTimer = new Timer { Interval = 700 };
                _reassertTimer.Tick += (s, e) => ReassertTopMost();
                _reassertTimer.Start();
                BuildForms();
            }
        }

        private void InstallHooks()
        {
            _foregroundHook = NativeMethods.SetWinEventHook(
                NativeMethods.EVENT_SYSTEM_FOREGROUND,
                NativeMethods.EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero,
                _winEventProc,
                0, 0,
                NativeMethods.WINEVENT_OUTOFCONTEXT | NativeMethods.WINEVENT_SKIPOWNPROCESS);

            _showHook = NativeMethods.SetWinEventHook(
                NativeMethods.EVENT_OBJECT_SHOW,
                NativeMethods.EVENT_OBJECT_SHOW,
                IntPtr.Zero,
                _winEventProc,
                0, 0,
                NativeMethods.WINEVENT_OUTOFCONTEXT | NativeMethods.WINEVENT_SKIPOWNPROCESS);
        }

        private void RemoveHooks()
        {
            if (_foregroundHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWinEvent(_foregroundHook);
                _foregroundHook = IntPtr.Zero;
            }

            if (_showHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWinEvent(_showHook);
                _showHook = IntPtr.Zero;
            }
        }

        /// <summary>切换模式，保持当前亮度。</summary>
        public void SetMode(OverlayMode mode)
        {
            if (Mode == mode)
            {
                return;
            }

            if (mode == OverlayMode.Dim && !MagnifierDim.IsAvailable)
            {
                return;
            }

            double keep = Opacity;

            // 先彻底卸掉旧模式，避免两种效果叠加
            if (Mode == OverlayMode.Curtain)
            {
                _reassertTimer?.Stop();
                RemoveHooks();
                DestroyForms();
            }
            else
            {
                MagnifierDim.Restore();
            }

            Mode = mode;

            if (Mode == OverlayMode.Curtain)
            {
                InstallHooks();
                _reassertTimer?.Start();
                BuildForms();
            }

            SetOpacity(keep);
        }

        public void BuildForms()
        {
            DestroyForms();

            foreach (Screen screen in Screen.AllScreens)
            {
                var form = new OverlayForm(screen);
                form.Show();
                _forms.Add(form);
            }

            ApplyOpacity();
            ReassertTopMost();
        }

        private void DestroyForms()
        {
            foreach (OverlayForm f in _forms)
            {
                try
                {
                    f.Hide();
                    f.Dispose();
                }
                catch
                {
                    // 忽略
                }
            }

            _forms.Clear();
        }

        public void SetOpacity(double level)
        {
            Opacity = Math.Max(0.0, Math.Min(1.0, level));
            ApplyOpacity();
        }

        public void SetEnabled(bool enabled)
        {
            Enabled = enabled;
            ApplyOpacity();
        }

        private void ApplyOpacity()
        {
            double effective = Enabled ? Opacity : 0.0;

            if (Mode == OverlayMode.Dim)
            {
                MagnifierDim.SetDim(effective);
                return;
            }

            foreach (OverlayForm f in _forms)
            {
                if (!f.IsDisposed)
                {
                    f.ApplyOpacity(effective);
                }
            }
        }

        public void ReassertTopMost()
        {
            if (_disposed || Mode != OverlayMode.Curtain)
            {
                return;
            }

            foreach (OverlayForm f in _forms)
            {
                if (!f.IsDisposed && f.IsHandleCreated)
                {
                    f.ReassertTopMost();
                }
            }
        }

        private void OnWinEvent(IntPtr hook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint thread, uint time)
        {
            if (!_disposed && Mode == OverlayMode.Curtain)
            {
                ReassertTopMost();
            }
        }

        /// <summary>显示设置变化时调用。Curtain 需要重建窗口；Dim 无需处理。</summary>
        public void OnDisplayChanged()
        {
            if (Mode == OverlayMode.Curtain)
            {
                BuildForms();
            }
            else
            {
                // 重施一次，防止休眠/分辨率切换后 DWM 丢弃效果
                ApplyOpacity();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _reassertTimer?.Stop();
            _reassertTimer?.Dispose();
            RemoveHooks();

            if (Mode == OverlayMode.Dim)
            {
                MagnifierDim.Shutdown();
            }

            DestroyForms();
        }
    }
}
