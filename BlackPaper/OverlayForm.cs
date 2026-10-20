using System;
using System.Drawing;
using System.Windows.Forms;

namespace BlackPaper
{
    /// <summary>
    /// 单块纯黑遮罩窗口。
    /// 设计要点：
    ///  - WS_EX_LAYERED + SetLayeredWindowAttributes 控制不透明度（纯黑叠加，不做任何颜色变换）
    ///  - WS_EX_TRANSPARENT + WS_EX_NOACTIVATE 保证鼠标完全穿透、且永不抢焦点
    ///  - WS_EX_TOOLWINDOW 让它不进 Alt+Tab
    /// </summary>
    internal sealed class OverlayForm : Form
    {
        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_NOACTIVATE = 3;

        private readonly Screen _screen;
        private double _opacity;

        public OverlayForm(Screen screen)
        {
            _screen = screen;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            ShowIcon = false;
            MinimizeBox = false;
            MaximizeBox = false;
            ControlBox = false;
            Text = "BlackPaper";
            BackColor = Color.Black;

            // WinForms 自己的 Opacity 走的是 UpdateLayeredWindow，和我们要用的
            // SetLayeredWindowAttributes 是两套机制，会互相覆盖，所以这里保持 1.0，
            // 透明度完全交给 ApplyOpacity() 里的 Win32 调用处理。
            base.Opacity = 1.0;
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;

                cp.ExStyle |= NativeMethods.WS_EX_LAYERED
                            | NativeMethods.WS_EX_TRANSPARENT
                            | NativeMethods.WS_EX_NOACTIVATE
                            | NativeMethods.WS_EX_TOOLWINDOW;

                // 去掉 APPWINDOW，避免出现在任务栏 / Alt+Tab
                cp.ExStyle &= ~NativeMethods.WS_EX_APPWINDOW;

                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // 让这个窗口不参与 Aero Peek 透视，也不在任务视图预览里露出来
            int disallowPeek = 1;
            NativeMethods.DwmSetWindowAttribute(
                Handle, NativeMethods.DWMWA_DISALLOW_PEEK, ref disallowPeek, sizeof(int));

            int excludedFromPeek = 1;
            NativeMethods.DwmSetWindowAttribute(
                Handle, NativeMethods.DWMWA_EXCLUDED_FROM_PEEK, ref excludedFromPeek, sizeof(int));

            ApplyBounds();
            ApplyOpacity(_opacity);
        }

        /// <summary>把窗口铺满所属显示器。</summary>
        public void ApplyBounds()
        {
            Rectangle b = _screen.Bounds;
            Bounds = b;
        }

        /// <summary>应用不透明度。0 = 完全透明，1 = 纯黑不透明。</summary>
        public void ApplyOpacity(double level)
        {
            _opacity = Math.Max(0.0, Math.Min(1.0, level));

            if (!IsHandleCreated)
            {
                return;
            }

            // WinForms 的 Opacity 与 SetLayeredWindowAttributes 冲突，这里统一走 Win32。
            base.Opacity = 1.0;

            byte alpha = (byte)Math.Round(_opacity * 255.0);

            if (_opacity <= 0.0)
            {
                // 完全透明时直接隐藏，避免 ws_ex_transparent 下仍拦截极少量的合成开销
                NativeMethods.SetLayeredWindowAttributes(Handle, 0, 0, 0x2);
                if (Visible)
                {
                    NativeMethods.ShowWindow(Handle, 0); // SW_HIDE
                }
                return;
            }

            if (!Visible)
            {
                NativeMethods.ShowWindow(Handle, 5);  // SW_SHOW
            }

            // LWA_ALPHA = 0x2 —— 用 alpha 值控制整体透明度
            // 注意：crKey 只在 LWA_COLORKEY 下有意义，这里传 0 即可
            NativeMethods.SetLayeredWindowAttributes(Handle, 0, alpha, 0x2);
        }

        /// <summary>重新压到 topmost。SWP_NOACTIVATE 保证不抢走开始菜单的焦点。</summary>
        public void ReassertTopMost()
        {
            if (!IsHandleCreated)
            {
                return;
            }

            NativeMethods.SetWindowPos(
                Handle,
                NativeMethods.HWND_TOPMOST,
                0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE
                | NativeMethods.SWP_NOSIZE
                | NativeMethods.SWP_NOACTIVATE
                | NativeMethods.SWP_NOOWNERZORDER);
        }

        /// <summary>永不接受鼠标激活，保证点击直接落在下面的应用上。</summary>
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_MOUSEACTIVATE)
            {
                m.Result = (IntPtr)MA_NOACTIVATE;
                return;
            }

            base.WndProc(ref m);
        }
    }
}
