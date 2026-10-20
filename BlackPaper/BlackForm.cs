using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BlackPaper
{
    /// <summary>
    /// 主窗体。本身不承担遮罩职责，只是一个不可见的「控制器」：
    /// 持有托盘图标 + 菜单，驱动 OverlayManager，并管理设置窗口。
    /// </summary>
    public partial class BlackForm : Form
    {
        private AppConfig _config;
        private WindowController _wc;
        private OverlayManager _overlay;
        private SettingsForm _settings;
        private BrightnessSliderHost _menuSlider;

        /// <summary>一次滚轮的亮度步进（百分点）。</summary>
        private const int WheelStep = 4;

        /// <summary>滚轮渐变时长。较短，保证连续滚动跟手。</summary>
        private const int WheelFadeMs = 180;

        /// <summary>上一次自动模式算出的目标亮度，用于判断是否需要触发过渡。</summary>
        private double _lastAutoTarget = -1;

        public BlackForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // 确保任何退出路径（包括崩溃/任务管理器结束）都能恢复画面
            AppDomain.CurrentDomain.ProcessExit += (s, ev) => MagnifierDim.Restore();
            AppDomain.CurrentDomain.UnhandledException += (s, ev) => MagnifierDim.Restore();

            ConfigManager cm = new ConfigManager();
            _config = cm.LoadConfig();

            // 控制器窗体自身保持隐藏，只负责接收广播消息
            Hide();

            _wc = new WindowController(this);

            OverlayMode mode = _config.Mode == 1 ? OverlayMode.Curtain : OverlayMode.Dim;
            _overlay = new OverlayManager(mode);
            _overlay.SetEnabled(_config.IsEnabled);

            // 以注册表的真实状态为准
            _config.IsAutoRun = _wc.checkIfAutoRun(_config.AppSubkey, _config.AppName);

            if (_config.IsAutoChange)
            {
                // 启动也走渐变：从「无遮罩」（全亮）渐入到目标亮度。
                // 初始时 Overlay 处于 0 遮罩，所以直接 smooth 即可。
                applyAutoBrightness(smooth: _config.SmoothTransition);
            }
            else
            {
                double mask = ToMask(_config.BeginLight);

                if (_config.SmoothTransition && mask > 0.001)
                {
                    _overlay.SetOpacitySmooth(mask, Math.Max(400, _config.TransitionMs));
                }
                else
                {
                    _overlay.SetOpacity(mask);
                }
            }

            开机自动启动ToolStripMenuItem.Checked = _config.IsAutoRun;
            启用屏幕压暗ToolStripMenuItem.Checked = _config.IsEnabled;
            自动调整亮度ToolStripMenuItem.Checked = _config.IsAutoChange;
            使用整屏压暗ToolStripMenuItem.Checked = _overlay.Mode == OverlayMode.Dim;
            平滑过渡ToolStripMenuItem.Checked = _config.SmoothTransition;

            LoadTrayIcon();
            BuildMenuSlider();

            RefreshTrayUi();
        }

        #region 亮度 / 遮罩换算

        /// <summary>
        /// 亮度 → 遮罩强度。
        /// 亮度 1.0（最亮）= 遮罩 0.0；亮度 0.0（最暗）= 遮罩 1.0。
        /// </summary>
        private static double ToMask(double brightness)
            => 1.0 - Math.Max(0, Math.Min(1, brightness));

        /// <summary>遮罩强度 → 亮度（ToMask 的逆运算）。</summary>
        private static double ToBrightness(double mask)
            => 1.0 - Math.Max(0, Math.Min(1, mask));

        /// <summary>当前画面的亮度（0~1，1 = 最亮）。</summary>
        private double CurrentBrightness
            => ToBrightness(_overlay?.Opacity ?? 0);

        #endregion

        /// <summary>
        /// 把亮度滑块插入右键菜单，位置在状态行下方。
        /// </summary>
        private void BuildMenuSlider()
        {
            _menuSlider = new BrightnessSliderHost
            {
                Name = "menuBrightnessSlider",
            };

            _menuSlider.ValueChanged += (s, e) =>
            {
                if (_overlay == null)
                {
                    return;
                }

                double brightness = _menuSlider.Value / 100.0;
                setBrightnessManual(brightness);
            };

            // 滑块上的滚轮交给宿主统一处理（与菜单空白处同一套逻辑）
            _menuSlider.WheelReceived += (s, e) => OnMenuMouseWheel(s, e);

            // 插到状态行后面
            int index = contextMenuStrip1.Items.IndexOf(ToolStripMenuItem_state);
            contextMenuStrip1.Items.Insert(index + 1, _menuSlider);

            // 菜单展开时把当前亮度同步到滑块
            contextMenuStrip1.Opening += (s, e) => SyncMenuSlider();

            // 在菜单空白处滚动滚轮也能调亮度（不必精准对准滑块）
            contextMenuStrip1.MouseWheel += OnMenuMouseWheel;

            SyncMenuSlider();
        }

        /// <summary>
        /// 菜单内滚轮调亮度。
        ///
        /// 要点：基于 Target（意图值）累加，而不是当前实际亮度。
        /// 这样快速交错滚动时，每一次滚轮都在上一次的意图上叠加，
        /// 不会因为渐变还没跑完而丢失指令。
        /// </summary>
        private void OnMenuMouseWheel(object sender, MouseEventArgs e)
        {
            if (_overlay == null || !_config.IsEnabled)
            {
                return;
            }

            int delta = e.Delta > 0 ? WheelStep : -WheelStep;
            int basePercent = (int)Math.Round(ToBrightness(_overlay.Target) * 100);
            int next = Math.Max(0, Math.Min(100, basePercent + delta));

            if (next == basePercent)
            {
                return;
            }

            setBrightnessManual(next / 100.0, smooth: true, durationMs: WheelFadeMs);
        }

        private void SyncMenuSlider()
        {
            if (_menuSlider == null || _overlay == null)
            {
                return;
            }

            // 与 RefreshTrayUi 保持一致：显示意图值，不显示渐变途中的值
            double shown = _config.IsAutoChange ? CurrentBrightness : ToBrightness(_overlay.Target);
            _menuSlider.Value = (int)Math.Round(Math.Max(0, Math.Min(1, shown)) * 100);
        }

        /// <summary>托盘图标优先从磁盘加载，失败则回退到窗体图标。</summary>
        private void LoadTrayIcon()
        {
            try
            {
                string iconPath = Path.Combine(Application.StartupPath, "icon.ico");
                if (File.Exists(iconPath))
                {
                    notifyIcon2.Icon = new Icon(iconPath);
                }
                else if (Icon != null)
                {
                    notifyIcon2.Icon = Icon;
                }

                notifyIcon2.Text = "BlackPaper — 屏幕压暗";
            }
            catch
            {
                // 图标加载失败不应影响主功能
            }
        }

        #region 亮度与自动模式

        /// <summary>
        /// 应用自动亮度。smooth 为 true 时带渐变过渡。
        /// </summary>
        private void applyAutoBrightness(bool smooth)
        {
            double hour = DateTime.Now.Hour + DateTime.Now.Minute / 60.0;
            double target = _config.GetLightForHour(hour);

            bool changed = Math.Abs(target - _lastAutoTarget) > 0.001;
            _lastAutoTarget = target;

            double mask = ToMask(target);

            if (smooth && changed && _config.SmoothTransition)
            {
                _overlay?.SetOpacitySmooth(mask, _config.TransitionMs);
            }
            else
            {
                _overlay?.SetOpacity(mask);
            }

            RefreshTrayUi();
            SyncSettingsBrightness();
        }

        /// <summary>手动设置亮度（用户拖滑块 / 滚轮）。brightness 1.0 = 最亮。</summary>
        private void setBrightnessManual(double brightness, bool smooth = false, int durationMs = 0)
        {
            brightness = Math.Max(0, Math.Min(1, brightness));

            _config.IsAutoChange = false;
            _config.BeginLight = brightness;
            _lastAutoTarget = -1;

            double mask = ToMask(brightness);
            int ms = durationMs > 0 ? durationMs : _config.TransitionMs;

            if (smooth && ms > 0 && _config.SmoothTransition)
            {
                _overlay?.SetOpacitySmooth(mask, ms);
            }
            else
            {
                _overlay?.SetOpacity(mask);
            }

            RefreshTrayUi();
            SyncSettingsBrightness();
        }

        private void SyncSettingsBrightness()
        {
            if (_settings != null && !_settings.IsDisposed && _settings.Visible)
            {
                // 与菜单滑块一致：显示意图值，不显示渐变途中的值
                double shown = _config.IsAutoChange
                    ? CurrentBrightness
                    : ToBrightness(_overlay?.Target ?? 0);

                _settings.SyncBrightness(Math.Max(0, Math.Min(1, shown)));
            }
        }

        /// <summary>刷新托盘菜单里的状态文字与勾选。</summary>
        private void RefreshTrayUi()
        {
            // 滑块显示的是「意图值」Target，不是当前实际亮度 Opacity。
            // 这样拖动/滚轮时滑块立刻到位，而画面亮度由渐变线程慢慢追赶。
            // 若在这里用 Opacity，滑块就会跟着渐变爬行、反向时还会回折。
            double shown = _config.IsAutoChange ? CurrentBrightness : ToBrightness(_overlay?.Target ?? 0);
            int percent = (int)Math.Round(Math.Max(0, Math.Min(1, shown)) * 100);

            string state = _config.IsAutoChange
                ? $"当前亮度：{percent}%（自动）"
                : $"当前亮度：{percent}%（手动）";

            ToolStripMenuItem_state.Text = state;

            if (_menuSlider != null)
            {
                _menuSlider.Value = percent;
            }

            notifyIcon2.Text = _config.IsEnabled
                ? $"BlackPaper — 亮度 {percent}%"
                : "BlackPaper — 已关闭";

            启用屏幕压暗ToolStripMenuItem.Checked = _config.IsEnabled;
            自动调整亮度ToolStripMenuItem.Checked = _config.IsAutoChange;
            平滑过渡ToolStripMenuItem.Checked = _config.SmoothTransition;
        }

        #endregion

        #region 托盘菜单

        private void 启用屏幕压暗ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _config.IsEnabled = !_config.IsEnabled;
            _overlay?.SetEnabled(_config.IsEnabled);

            RefreshTrayUi();
            PersistConfig();
        }

        private void 自动调整亮度ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _config.IsAutoChange = !_config.IsAutoChange;

            if (_config.IsAutoChange)
            {
                applyAutoBrightness(smooth: true);
            }

            RefreshTrayUi();
            PersistConfig();
        }
        private void 平滑过渡ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _config.SmoothTransition = !_config.SmoothTransition;
            RefreshTrayUi();
            PersistConfig();
        }

        private void 设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSettings();
        }

        private void 开机自动启动ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _config.IsAutoRun = !_config.IsAutoRun;
            _wc.AutoRun(_config.AppSubkey, _config.AppName, _config.IsAutoRun);
            开机自动启动ToolStripMenuItem.Checked = _config.IsAutoRun;
            PersistConfig();
        }

        private void 使用整屏压暗ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_overlay == null)
            {
                return;
            }

            bool wantDim = _overlay.Mode != OverlayMode.Dim;

            if (wantDim && !_overlay.DimAvailable)
            {
                MessageBox.Show(
                    "当前系统不支持放大镜颜色效果，无法使用整屏压暗。",
                    "BlackPaper",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                使用整屏压暗ToolStripMenuItem.Checked = false;
                return;
            }

            _overlay.SetMode(wantDim ? OverlayMode.Dim : OverlayMode.Curtain);
            _config.Mode = wantDim ? 0 : 1;
            使用整屏压暗ToolStripMenuItem.Checked = wantDim;
            RefreshTrayUi();
            PersistConfig();
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PersistConfig();

            if (_settings != null && !_settings.IsDisposed)
            {
                _settings.Close();
            }

            // 先收拾托盘图标，否则 Win11 上会留下"幽灵图标"
            notifyIcon2.Visible = false;
            notifyIcon2.Dispose();

            // 退出也渐变：从当前亮度淡回全亮，避免画面突然“一亮”。
            // 必须同步等它跑完，因为紧接着就要 Exit。
            if (_overlay != null && _config.SmoothTransition && _overlay.Opacity > 0.005)
            {
                FadeOutAndWait(Math.Max(300, Math.Min(_config.TransitionMs, 1200)));
            }

            _overlay?.Dispose();
            _overlay = null;

            // 双重保险：无论如何把画面亮度拉回 1.0
            MagnifierDim.Restore();

            Application.Exit();
        }

        /// <summary>
        /// 退出前把遮罩淡到 0（画面回全亮），并同步等待完成。
        /// 用轮询而不是阻塞在渐变线程上，保证 UI 不会假死。
        /// </summary>
        private void FadeOutAndWait(int durationMs)
        {
            try
            {
                _overlay.SetOpacitySmooth(0.0, durationMs);

                var sw = System.Diagnostics.Stopwatch.StartNew();
                int limit = durationMs + 400;

                while (sw.ElapsedMilliseconds < limit)
                {
                    if (_overlay.Opacity <= 0.005)
                    {
                        break;
                    }

                    System.Threading.Thread.Sleep(15);

                    // 保持消息泵存活，避免托盘菜单卡住
                    Application.DoEvents();
                }

                _overlay.SetOpacity(0.0);
            }
            catch
            {
                // 退出路径上的任何异常都不应阻止程序结束
                MagnifierDim.Restore();
            }
        }

        #endregion

        #region 设置窗口

        private void ShowSettings()
        {
            if (_settings != null && !_settings.IsDisposed)
            {
                if (_settings.WindowState == FormWindowState.Minimized)
                {
                    _settings.WindowState = FormWindowState.Normal;
                }

                _settings.Activate();
                return;
            }

            _settings = new SettingsForm(_config);

            _settings.BrightnessChanged += level =>
            {
                setBrightnessManual(level);
            };

            _settings.ScheduleChanged += () =>
            {
                // 时段改动后若正处于自动模式，立即按新配置重算
                if (_config.IsAutoChange)
                {
                    _lastAutoTarget = -1;
                    applyAutoBrightness(smooth: true);
                }

                PersistConfig();
            };

            _settings.FormClosed += (s, e) =>
            {
                PersistConfig();
                _settings = null;
            };

            _settings.Show();
            _settings.Activate();
        }

        #endregion

        #region 托盘图标交互

        private void notifyIcon2_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowSettings();
            }
        }

        #endregion

        private void PersistConfig()
        {
            try
            {
                new ConfigManager().SaveConfig(_config);
            }
            catch
            {
                // 忽略写入失败
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (_config != null && _config.IsAutoChange)
            {
                applyAutoBrightness(smooth: true);
            }

            // 定时兜底：确保黑膜模式下遮罩仍在最顶层
            _overlay?.ReassertTopMost();
        }

        /// <summary>
        /// 接收显示设置变更 / 会话解锁 / 电源恢复等广播，重建或重施遮罩。
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            switch (m.Msg)
            {
                case NativeMethods.WM_DISPLAYCHANGE:
                case NativeMethods.WM_DPICHANGED:
                case NativeMethods.WM_SETTINGCHANGE:
                    _overlay?.OnDisplayChanged();
                    break;

                case NativeMethods.WM_POWERBROADCAST:
                    if ((int)m.WParam == NativeMethods.PBT_APMRESUMEAUTOMATIC ||
                        (int)m.WParam == NativeMethods.PBT_APMRESUMESUSPEND)
                    {
                        _overlay?.OnDisplayChanged();
                    }
                    break;

                case NativeMethods.WM_WTSSESSION_CHANGE:
                    if ((int)m.WParam == NativeMethods.WTS_SESSION_UNLOCK)
                    {
                        _overlay?.OnDisplayChanged();
                    }
                    break;
            }
        }
    }
}
