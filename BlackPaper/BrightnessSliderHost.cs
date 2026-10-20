using System;
using System.Drawing;
using System.Windows.Forms;

namespace BlackPaper
{
    /// <summary>
    /// 菜单内嵌的横向亮度滑块。
    ///
    /// 用 ToolStripControlHost 托管 TrackBar，使拖拽、点击、键盘、
    /// 滚轮调节都能在右键菜单里直接完成，无需打开设置窗口。
    ///
    /// 菜单里放滑块有两个常见的坑，这里都处理了：
    ///  1. ToolStrip 持有鼠标捕获，TrackBar 拖不动 → MouseDown 时交出捕获
    ///  2. 菜单会吃掉滚轮导致直接关闭 → 拦截并转成数值调节
    ///
    /// 语义约定：Value 表示「亮度」，100 = 最亮（不遮罩），0 = 最暗（全黑）。
    /// 底层遮罩强度是它的镜像：mask = 1 - brightness。
    /// </summary>
    internal sealed class BrightnessSliderHost : ToolStripControlHost
    {
        public const int MinValue = 0;
        public const int MaxValue = 100;

        private readonly TrackBar _trackBar;
        private bool _suppressEvent;

        public BrightnessSliderHost() : base(new TrackBar())
        {
            _trackBar = (TrackBar)Control;

            // ToolStripControlHost 默认 AutoSize 会把宿主塌陷为 0 高度，
            // 必须关掉并手动给尺寸，否则滑块在菜单里不可见
            AutoSize = false;
            Size = new Size(220, 36);
            Margin = new Padding(4, 2, 4, 2);

            _trackBar.Minimum = MinValue;
            _trackBar.Maximum = MaxValue;
            _trackBar.TickFrequency = 10;
            _trackBar.SmallChange = 1;
            _trackBar.LargeChange = 10;
            _trackBar.TickStyle = TickStyle.None;
            _trackBar.AutoSize = false;
            _trackBar.Height = 34;
            _trackBar.Width = 210;
            _trackBar.BackColor = SystemColors.ControlLight;

            _trackBar.ValueChanged += (s, e) =>
            {
                if (_suppressEvent)
                {
                    return;
                }

                ValueChanged?.Invoke(this, EventArgs.Empty);
            };

            _trackBar.MouseDown += (s, e) => ReleaseMenuCapture();
        }

        /// <summary>用户拖动 / 滚轮调节时触发。</summary>
        public event EventHandler ValueChanged;

        /// <summary>
        /// 滑块上收到的滚轮事件。
        /// 上抛给宿主，由宿主用同一套意图值逻辑处理，避免重复计步。
        /// </summary>
        public event MouseEventHandler WheelReceived;

        /// <summary>当前亮度百分比 0~100。100 = 最亮，0 = 最暗。</summary>
        public int Value
        {
            get => _trackBar.Value;
            set
            {
                int clamped = Math.Max(MinValue, Math.Min(MaxValue, value));
                if (_trackBar.Value == clamped)
                {
                    return;
                }

                _suppressEvent = true;
                try
                {
                    _trackBar.Value = clamped;
                }
                finally
                {
                    _suppressEvent = false;
                }
            }
        }

        /// <summary>让 ToolStrip 不要抢焦点。</summary>
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            _trackBar.Focus();
        }

        protected override void OnSubscribeControlEvents(Control control)
        {
            base.OnSubscribeControlEvents(control);
            control.MouseWheel += OnChildMouseWheel;
        }

        protected override void OnUnsubscribeControlEvents(Control control)
        {
            control.MouseWheel -= OnChildMouseWheel;
            base.OnUnsubscribeControlEvents(control);
        }

        /// <summary>
        /// 在滑块上滚轮：转成亮度增减，并且不让事件冒到菜单（否则菜单会关闭）。
        /// </summary>
        /// <summary>
        /// 在滑块上滚轮：不让 ToolStrip 处理（否则菜单会关），并且不改自身值，
        /// 而是把滚轮意图向上报给宿主统一处理。
        /// 否则「菜单滚轮」+「滑块自己改值」会让一次滚动走两步。
        /// </summary>
        private void OnChildMouseWheel(object sender, MouseEventArgs e)
        {
            if (e is HandledMouseEventArgs handled)
            {
                handled.Handled = true;
            }

            WheelReceived?.Invoke(this, e);
        }

        private void ReleaseMenuCapture()
        {
            if (Owner is ToolStrip owner && owner.Capture)
            {
                owner.Capture = false;
            }

            _trackBar.Capture = true;
        }
    }
}
