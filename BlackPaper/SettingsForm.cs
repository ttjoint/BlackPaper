using System;
using System.Drawing;
using System.Windows.Forms;

namespace BlackPaper
{
    /// <summary>
    /// 设置窗口：拖动条亮度 + 可拖拽折线的时段配置。
    /// </summary>
    internal sealed class SettingsForm : Form
    {
        private readonly AppConfig _config;

        private Label _lblPercent;
        private BrightnessSlider _slider;
        private ScheduleEditor _schedule;
        private CheckBox _chkSmooth;
        private Label _lblSummary;

        /// <summary>亮度变化时回调（0~1），父窗口负责落地。</summary>
        public event Action<double> BrightnessChanged;

        /// <summary>时段配置变化时回调。</summary>
        public event Action ScheduleChanged;

        public SettingsForm(AppConfig config)
        {
            _config = config;

            BuildUi();
            LoadFromConfig();
        }

        private void BuildUi()
        {
            Text = "BlackPaper 设置";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Microsoft YaHei UI", 9f);
            BackColor = Color.White;
            ClientSize = new Size(520, 430);

            // ---- 亮度 ----
            var lblBrightTitle = new Label
            {
                Text = "屏幕亮度",
                Font = new Font("Microsoft YaHei UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50),
                Location = new Point(20, 16),
                AutoSize = true,
            };

            _lblPercent = new Label
            {
                Text = "0%",
                Font = new Font("Microsoft YaHei UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight,
                Location = new Point(420, 14),
                Size = new Size(80, 22),
            };

            _slider = new BrightnessSlider
            {
                Location = new Point(18, 40),
                Size = new Size(484, 34),
            };
            _slider.ValueChanged += (s, e) =>
            {
                int v = _slider.Value;
                _lblPercent.Text = v + "%";

                // 滑块值就是亮度：100 = 最亮，0 = 最暗
                _config.IsAutoChange = false;
                _config.BeginLight = v / 100.0;

                BrightnessChanged?.Invoke(v / 100.0);
            };

            // ---- 时段 ----
            var lblScheduleTitle = new Label
            {
                Text = "亮度计划",
                Font = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50),
                Location = new Point(20, 88),
                AutoSize = true,
            };

            _schedule = new ScheduleEditor
            {
                Location = new Point(16, 112),
                Size = new Size(488, 150),
            };
            _schedule.ScheduleChanged += (s, e) =>
            {
                // 控制点列表就是主数据，直接整体同步回配置
                _config.Points = new System.Collections.Generic.List<LightPoint>();

                foreach (LightPoint p in _schedule.Points)
                {
                    _config.Points.Add(new LightPoint(p.Hour, p.Light));
                }

                _config.Normalize();

                UpdateSummary();
                ScheduleChanged?.Invoke();
            };

            _chkSmooth = new CheckBox
            {
                Text = "切换时段时渐变过渡",
                Location = new Point(20, 272),
                AutoSize = true,
                Checked = true,
            };
            _chkSmooth.CheckedChanged += (s, e) =>
            {
                _config.SmoothTransition = _chkSmooth.Checked;
                ScheduleChanged?.Invoke();
            };

            _lblSummary = new Label
            {
                Text = string.Empty,
                Location = new Point(20, 298),
                Size = new Size(480, 20),
                ForeColor = Color.FromArgb(110, 110, 110),
            };

            // ---- 按钮 ----
            // 配置是随改随生效的，所以只需要一个「确定」用来关闭窗口。
            // 注意：DialogResult 只在 ShowDialog() 时才自动关窗，
            // 本窗口用 Show() 显示，必须显式 Close()，否则按钮看起来没反应。
            var btnOk = new Button
            {
                Text = "确定",
                Location = new Point(422, 380),
                Size = new Size(86, 30),
            };

            btnOk.Click += (s2, e2) => Close();

            AcceptButton = btnOk;

            Controls.AddRange(new Control[]
            {
                lblBrightTitle, _lblPercent, _slider,
                lblScheduleTitle, _schedule,
                _chkSmooth, _lblSummary,
                btnOk,
            });
        }

        /// <summary>用配置刷新界面控件（不触发回调）。</summary>
        public void LoadFromConfig()
        {
            int percent = (int)Math.Round(_config.BeginLight * 100);
            _slider.Value = percent;
            _lblPercent.Text = percent + "%";

            _schedule.SetPoints(_config.NormalizedPoints());

            _chkSmooth.Checked = _config.SmoothTransition;

            UpdateSummary();
        }

        /// <summary>外部亮度变化（例如滚轮调节）时同步界面。</summary>
        public void SyncBrightness(double level)
        {
            int percent = (int)Math.Round(level * 100);
            _slider.Value = percent;
            _lblPercent.Text = percent + "%";
        }

        private void UpdateSummary()
        {
            _lblSummary.Text = string.Format(
                "共 {0} 个控制点　|　左键拖动调整，空白处左键添加，右键删除",
                _schedule.Points.Count);
        }
    }
}
