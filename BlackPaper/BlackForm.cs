using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace BlackPaper
{
    /// <summary>
    /// 主窗体。
    /// 它本身不再承担遮罩职责（原来的做法就是一个最大化窗体，盖不住开始菜单），
    /// 而是一个不可见的「控制器」：持有托盘图标 + 菜单，并驱动 OverlayManager。
    /// </summary>
    public partial class BlackForm : Form
    {
        private AppConfig _config;
        private WindowController _wc;
        private OverlayManager _overlay;

        /// <summary>抑制下拉框回写，避免在加载/刷新 UI 时把精确亮度吸附成档位值。</summary>
        private bool _suppressComboEvent;

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

            // 控制器窗体自身保持隐藏，只负责接收广播消息（显示变更 / 会话解锁）
            Hide();

            _wc = new WindowController(this);

            OverlayMode mode = _config.Mode == 1 ? OverlayMode.Curtain : OverlayMode.Dim;
            _overlay = new OverlayManager(mode);
            _overlay.SetEnabled(_config.IsEnabled);

            // 以注册表的真实状态为准，纠正配置里的缓存值
            _config.IsAutoRun = _wc.checkIfAutoRun(_config.AppSubkey, _config.AppName);

            if (_config.IsAutoChange)
            {
                setLightLevel(-1);
            }
            else
            {
                setLightLevel(_config.BeginLight);
            }

            开机自动启动ToolStripMenuItem.Checked = _config.IsAutoRun;

            LoadTrayIcon();

            // 菜单里只能放一个 Checked，根据当前模式勾选
            自动调整亮度ToolStripMenuItem.Checked = _config.IsAutoChange;
            使用整屏压暗ToolStripMenuItem.Checked = _overlay.Mode == OverlayMode.Dim;

            RefreshStateText();
        }

        /// <summary>
        /// 托盘图标优先从内嵌资源加载，避免路径拼接 + 文件缺失导致启动崩溃。
        /// </summary>
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

                notifyIcon2.Text = "BlackPaper — 屏幕遮罩";
            }
            catch
            {
                // 图标加载失败不应影响主功能
            }
        }

        private void 开机自动启动ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_config.IsAutoRun)
            {
                _wc.AutoRun(_config.AppSubkey, _config.AppName, false);
                _config.IsAutoRun = false;
            }
            else
            {
                _wc.AutoRun(_config.AppSubkey, _config.AppName, true);
                _config.IsAutoRun = true;
            }

            开机自动启动ToolStripMenuItem.Checked = _config.IsAutoRun;
            PersistConfig();
        }

        /// <summary>
        /// 设置亮度。level >= 0 为手动档位（0~1），level < 0 表示按时段自动调整。
        /// </summary>
        private void setLightLevel(double level)
        {
            if (level >= 0)
            {
                _config.IsAutoChange = false;
                _config.BeginLight = level;
            }
            else
            {
                int nowHour = DateTime.Now.Hour;

                if (nowHour >= 8 && nowHour <= 18)
                {
                    level = 0.0;      // 白天透明
                }
                else if (nowHour > 18 && nowHour <= 22)
                {
                    level = 0.2;      // 傍晚
                }
                else
                {
                    level = 0.45;     // 夜间
                }

                _config.IsAutoChange = true;
                _config.BeginLight = level;
            }

            _overlay?.SetOpacity(level);

            RefreshStateText();
            自动调整亮度ToolStripMenuItem.Checked = _config.IsAutoChange;
        }

        /// <summary>
        /// 更新菜单里的"当前亮度"文字与下拉框选中项。
        /// 注意：设置 SelectedIndex 会触发 SelectedIndexChanged，进而回写亮度并把
        /// 配置文件里的精确值吸附到最近的 UI 档位。用 _suppressComboEvent 抑制这个回环。
        /// </summary>
        private void RefreshStateText()
        {
            double level = _overlay?.Opacity ?? 0.0;
            ToolStripMenuItem_state.Text = $"当前亮度：{level * 100:F0}%";

            int index = (int)Math.Round(level * 5.0);
            index = Math.Max(0, Math.Min(toolStripComboBox1.Items.Count - 1, index));

            if (toolStripComboBox1.SelectedIndex != index)
            {
                _suppressComboEvent = true;
                try
                {
                    toolStripComboBox1.SelectedIndex = index;
                }
                finally
                {
                    _suppressComboEvent = false;
                }
            }
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PersistConfig();

            // 先收拾托盘图标，否则 Win11 上会留下"幽灵图标"
            notifyIcon2.Visible = false;
            notifyIcon2.Dispose();

            _overlay?.Dispose();
            _overlay = null;

            // 双重保险：无论如何把画面亮度拉回 1.0
            MagnifierDim.Restore();

            Application.Exit();
        }

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

        private void 自动调整亮度ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setLightLevel(-1);
        }

        /// <summary>在“整屏压暗”和“黑膜叠加”两种模式间切换。</summary>
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
            RefreshStateText();
            PersistConfig();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (_config != null && _config.IsAutoChange)
            {
                setLightLevel(-1);
            }

            // 定时兜底：确保遮罩仍在最顶层
            _overlay?.ReassertTopMost();
        }

        private void 关闭菜单ToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            contextMenuStrip1.Hide();
        }

        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_overlay == null || _suppressComboEvent)
            {
                return;
            }

            double op = (double)toolStripComboBox1.SelectedIndex / 5.0;
            setLightLevel(op);
        }

        private void notifyIcon2_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // 双击暂不响应，保留给以后扩展
        }

        /// <summary>
        /// 接收显示设置变更 / 会话解锁 / 电源恢复等广播，重建或重压遮罩。
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
                        // 解锁后 DWM 会重新合成，必须重建遮罩
                        _overlay?.OnDisplayChanged();
                    }
                    break;
            }
        }
    }
}
