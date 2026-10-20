using System;
using System.Drawing;
using System.Windows.Forms;

namespace BlackPaper
{
    /// <summary>
    /// 横向亮度拖动条。
    ///
    /// 用 UserControl 包一个 TrackBar，而不是直接暴露 TrackBar：
    /// 这样调用方拿到的就是一个普通 Control，可以直接放进任何容器，
    /// 也方便在设置窗口和未来的菜单里复用。
    /// </summary>
    internal sealed class BrightnessSlider : UserControl
    {
        public const int MinValue = 0;
        public const int MaxValue = 100;

        private readonly TrackBar _trackBar;
        private bool _suppressEvent;

        public BrightnessSlider()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

            BackColor = Color.White;

            _trackBar = new TrackBar
            {
                Minimum = MinValue,
                Maximum = MaxValue,
                TickFrequency = 10,
                SmallChange = 1,
                LargeChange = 10,
                TickStyle = TickStyle.None,
                AutoSize = false,
                Dock = DockStyle.Fill,
                BackColor = Color.White,
            };

            _trackBar.ValueChanged += (s, e) =>
            {
                if (_suppressEvent)
                {
                    return;
                }

                ValueChanged?.Invoke(this, EventArgs.Empty);
            };

            // 鼠标停在滑块上滚动即可微调，无需拖拽
            _trackBar.MouseWheel += (s, e) =>
            {
                var handled = e as HandledMouseEventArgs;
                if (handled != null)
                {
                    handled.Handled = true;
                }

                int delta = e.Delta > 0 ? _trackBar.SmallChange : -_trackBar.SmallChange;
                int next = Math.Max(MinValue, Math.Min(MaxValue, _trackBar.Value + delta));

                if (next != _trackBar.Value)
                {
                    _trackBar.Value = next;
                }
            };

            Controls.Add(_trackBar);
        }

        /// <summary>用户拖动时触发。</summary>
        public event EventHandler ValueChanged;

        /// <summary>当前亮度百分比，0~100。</summary>
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

                // 程序化赋值不回调，避免和外部刷新形成回环
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

        /// <summary>键盘微调时每次变化的量。</summary>
        public int SmallChange
        {
            get => _trackBar.SmallChange;
            set => _trackBar.SmallChange = Math.Max(1, value);
        }
    }
}
