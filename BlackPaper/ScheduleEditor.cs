using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BlackPaper
{
    /// <summary>
    /// 可编辑的时段亮度折线图。
    ///
    /// 横轴 0~24 小时，纵轴亮度（0 = 最暗，100% = 最亮）。
    /// 曲线由若干个控制点连成的折线表示，相邻点之间线性插值。
    ///
    /// 交互：
    ///   - 左键拖动圆点：改变该点的时刻与亮度
    ///   - 右键点圆点：删除该点（首末两点不可删）
    ///   - 左键点空白处：在该位置新增一个控制点
    ///   - 方向键：微调选中点（←→ 改时刻，↑↓ 改亮度；Shift 加速）
    ///
    /// 约束：
    ///   - 首点（0 时）与末点（24 时）只能调亮度，不能调时刻
    ///   - 首末两点亮度始终相等，避免跨 0 点处出现亮度阶跃
    /// </summary>
    internal sealed class ScheduleEditor : Control
    {
        private const int PaddingLeft = 46;
        private const int PaddingRight = 18;
        private const int PaddingTop = 20;
        private const int PaddingBottom = 28;

        private const int HandleRadius = 6;
        private const int HitRadius = 13;

        /// <summary>最少保留的点数（首末两点）。</summary>
        private const int MinPoints = 2;

        private readonly List<LightPoint> _points = new List<LightPoint>();

        private int _dragIndex = -1;
        private int _hoverIndex = -1;

        /// <summary>用户修改任何数值时触发。</summary>
        public event EventHandler ScheduleChanged;

        public ScheduleEditor()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.Selectable, true);

            BackColor = Color.White;
            Height = 170;
            TabStop = true;

            ResetToDefault();
        }

        /// <summary>当前控制点的只读视图（已排序）。</summary>
        public IList<LightPoint> Points
        {
            get
            {
                Normalize();
                return _points.AsReadOnly();
            }
        }

        /// <summary>静默设置控制点（不触发事件），用于初始化。</summary>
        public void SetPoints(IEnumerable<LightPoint> points)
        {
            _points.Clear();

            if (points != null)
            {
                foreach (LightPoint p in points)
                {
                    if (p == null)
                    {
                        continue;
                    }

                    _points.Add(new LightPoint
                    {
                        Hour = Clamp(p.Hour, 0, 24),
                        Light = Clamp(p.Light, 0, 1),
                    });
                }
            }

            if (_points.Count < MinPoints)
            {
                ResetToDefault();
            }
            else
            {
                Normalize();
            }

            Invalidate();
        }

        /// <summary>兼容旧接口：用白天/夜间四字段生成四点曲线。</summary>
        public void SetSchedule(double dayStart, double dayEnd, double dayLight, double nightLight)
        {
            SetPoints(AppConfig.BuildDefaultPoints(dayStart, dayEnd, dayLight, nightLight));
        }

        private void ResetToDefault()
        {
            _points.Clear();
            _points.AddRange(AppConfig.BuildDefaultPoints(8, 18, 1.0, 0.55));
        }

        #region 坐标换算

        private Rectangle PlotArea => new Rectangle(
            PaddingLeft,
            PaddingTop,
            Math.Max(1, Width - PaddingLeft - PaddingRight),
            Math.Max(1, Height - PaddingTop - PaddingBottom));

        private float HourToX(double hour)
        {
            Rectangle p = PlotArea;
            return p.Left + (float)(Clamp(hour, 0, 24) / 24.0 * p.Width);
        }

        private double XToHour(float x)
        {
            Rectangle p = PlotArea;
            return Clamp((x - p.Left) / (double)p.Width * 24.0, 0, 24);
        }

        private float LightToY(double light)
        {
            Rectangle p = PlotArea;
            return p.Bottom - (float)(Clamp(light, 0, 1) * p.Height);
        }

        private double YToLight(float y)
        {
            Rectangle p = PlotArea;
            return Clamp((p.Bottom - y) / (double)p.Height, 0, 1);
        }

        private static double Clamp(double v, double min, double max)
            => v < min ? min : (v > max ? max : v);

        private static float Clamp(float v, float min, float max)
            => v < min ? min : (v > max ? max : v);

        #endregion

        /// <summary>
        /// 排序、夹紧范围，并维持首末点亮度一致。
        /// 任何修改点集的操作结束后都要调用。
        /// </summary>
        private void Normalize(bool wrapUseFirst = false)
        {
            if (_points.Count < MinPoints)
            {
                ResetToDefault();
                return;
            }

            _points.Sort((a, b) => a.Hour.CompareTo(b.Hour));

            // 去掉时刻过于接近的点，避免零宽区间导致插值除零
            for (int i = _points.Count - 1; i > 0; i--)
            {
                if (_points[i].Hour - _points[i - 1].Hour < 0.08)
                {
                    _points.RemoveAt(i);
                }
            }

            if (_points.Count < MinPoints)
            {
                ResetToDefault();
                return;
            }

            // 首末点钉在 0 和 24，亮度强制一致（跨 0 点不跳变）
            AppConfig.SyncWrapEnds(_points, wrapUseFirst);

            foreach (LightPoint p in _points)
            {
                p.Light = Clamp(p.Light, 0, 1);
            }
        }

        #region 绘制

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle plot = PlotArea;

            using (var bg = new SolidBrush(Color.White))
            {
                g.FillRectangle(bg, ClientRectangle);
            }

            DrawGrid(g, plot);
            DrawClockLabels(g, plot);
            DrawPercentLabels(g, plot);
            DrawCurve(g, plot);
            DrawPoints(g);
            DrawBubble(g, plot);
        }

        private void DrawGrid(Graphics g, Rectangle plot)
        {
            using (var gridPen = new Pen(Color.FromArgb(236, 239, 242)))
            using (var axisPen = new Pen(Color.FromArgb(205, 210, 216)))
            {
                // 每 3 小时一条竖线
                for (int h = 0; h <= 24; h += 3)
                {
                    float x = HourToX(h);
                    g.DrawLine(h == 0 || h == 24 ? axisPen : gridPen, x, plot.Top, x, plot.Bottom);
                }

                // 每 25% 一条横线
                for (int i = 0; i <= 4; i++)
                {
                    float y = plot.Bottom - plot.Height * i / 4f;
                    g.DrawLine(i == 0 ? axisPen : gridPen, plot.Left, y, plot.Right, y);
                }
            }
        }

        private void DrawClockLabels(Graphics g, Rectangle plot)
        {
            using (var brush = new SolidBrush(Color.FromArgb(120, 128, 138)))
            using (var font = new Font("Microsoft YaHei UI", 7.5f))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center })
            {
                for (int h = 0; h <= 24; h += 3)
                {
                    string text = h == 24 ? "24" : h.ToString("00");
                    g.DrawString(text, font, brush, HourToX(h), plot.Bottom + 6, sf);
                }
            }
        }

        private void DrawPercentLabels(Graphics g, Rectangle plot)
        {
            using (var brush = new SolidBrush(Color.FromArgb(140, 148, 158)))
            using (var font = new Font("Microsoft YaHei UI", 7.5f))
            using (var sf = new StringFormat { Alignment = StringAlignment.Far })
            {
                for (int i = 0; i <= 4; i++)
                {
                    float y = plot.Bottom - plot.Height * i / 4f;
                    string text = (i * 25) + "%";
                    g.DrawString(text, font, brush, plot.Left - 6, y - 8, sf);
                }
            }
        }

        private void DrawCurve(Graphics g, Rectangle plot)
        {
            if (_points.Count < 2)
            {
                return;
            }

            var pts = new PointF[_points.Count];

            for (int i = 0; i < _points.Count; i++)
            {
                pts[i] = new PointF(HourToX(_points[i].Hour), LightToY(_points[i].Light));
            }

            // 曲线下方淡色填充，便于看出高低
            var fill = new List<PointF>(pts)
            {
                new PointF(plot.Right, plot.Bottom),
                new PointF(plot.Left, plot.Bottom),
            };

            using (var fillBrush = new SolidBrush(Color.FromArgb(38, 66, 133, 244)))
            {
                g.FillPolygon(fillBrush, fill.ToArray());
            }

            using (var pen = new Pen(Color.FromArgb(66, 133, 244), 2.2f))
            {
                g.DrawLines(pen, pts);
            }
        }

        private void DrawPoints(Graphics g)
        {
            for (int i = 0; i < _points.Count; i++)
            {
                LightPoint p = _points[i];

                float x = HourToX(p.Hour);
                float y = LightToY(p.Light);

                bool isEnd = i == 0 || i == _points.Count - 1;
                bool active = i == _hoverIndex || i == _dragIndex;

                int r = active ? HandleRadius + 2 : HandleRadius;

                Color fill = isEnd ? Color.FromArgb(255, 152, 0) : Color.FromArgb(66, 133, 244);
                Color border = active ? Color.FromArgb(25, 35, 50) : Color.White;

                using (var brush = new SolidBrush(fill))
                using (var pen = new Pen(border, active ? 2.4f : 1.8f))
                {
                    g.FillEllipse(brush, x - r, y - r, r * 2, r * 2);
                    g.DrawEllipse(pen, x - r, y - r, r * 2, r * 2);
                }
            }
        }

        private void DrawBubble(Graphics g, Rectangle plot)
        {
            int idx = _dragIndex >= 0 ? _dragIndex : _hoverIndex;

            if (idx < 0 || idx >= _points.Count)
            {
                return;
            }

            LightPoint p = _points[idx];

            bool isEnd = idx == 0 || idx == _points.Count - 1;
            string timeText = isEnd
                ? (idx == 0 ? "00:00" : "24:00")
                : FormatHour(p.Hour);

            string label = timeText + "   亮度 " + (p.Light * 100).ToString("F0") + "%";

            using (var font = new Font("Microsoft YaHei UI", 8f))
            {
                SizeF size = g.MeasureString(label, font);

                float bx = HourToX(p.Hour) - size.Width / 2 - 6;
                float by = LightToY(p.Light) - size.Height - 14;

                bx = Clamp(bx, plot.Left - 30, plot.Right - size.Width - 12);
                by = Math.Max(plot.Top - 16, by);

                var rect = new RectangleF(bx, by, size.Width + 12, size.Height + 6);

                using (var bg = new SolidBrush(Color.FromArgb(238, 244, 252)))
                using (var border = new Pen(Color.FromArgb(190, 208, 232)))
                {
                    g.FillRectangle(bg, rect);
                    g.DrawRectangle(border, rect.X, rect.Y, rect.Width, rect.Height);
                }

                using (var brush = new SolidBrush(Color.FromArgb(40, 60, 90)))
                {
                    g.DrawString(label, font, brush, rect.X + 6, rect.Y + 3);
                }
            }
        }

        private static string FormatHour(double hour)
        {
            int h = (int)Math.Floor(hour);
            int m = (int)Math.Round((hour - h) * 60);

            if (m == 60)
            {
                h = (h + 1) % 24;
                m = 0;
            }

            return h.ToString("00") + ":" + m.ToString("00");
        }

        #endregion

        #region 命中测试

        private int HitTest(Point location)
        {
            for (int i = 0; i < _points.Count; i++)
            {
                float x = HourToX(_points[i].Hour);
                float y = LightToY(_points[i].Light);

                double dx = location.X - x;
                double dy = location.Y - y;

                if (dx * dx + dy * dy <= HitRadius * HitRadius)
                {
                    return i;
                }
            }

            return -1;
        }

        #endregion

        #region 鼠标交互

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            Focus();

            int hit = HitTest(e.Location);

            if (e.Button == MouseButtons.Right)
            {
                // 右键删除
                if (hit > 0 && hit < _points.Count - 1 && _points.Count > MinPoints)
                {
                    _points.RemoveAt(hit);
                    Normalize();
                    _hoverIndex = -1;
                    _dragIndex = -1;
                    Invalidate();
                    RaiseChanged();
                }

                return;
            }

            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            if (hit >= 0)
            {
                _dragIndex = hit;
            }
            else if (PlotArea.Contains(e.Location))
            {
                // 空白处左键：新增控制点
                AddPoint(e.Location);
            }
        }

        private void AddPoint(Point location)
        {
            double hour = XToHour(location.X);
            double light = YToLight(location.Y);

            // 与现有点太近就不加，避免重复点
            foreach (LightPoint p in _points)
            {
                if (Math.Abs(p.Hour - hour) < 0.3)
                {
                    return;
                }
            }

            _points.Add(new LightPoint(hour, light));
            Normalize();

            // 选中刚加的点（排序后重新定位）
            _dragIndex = -1;
            for (int i = 0; i < _points.Count; i++)
            {
                if (Math.Abs(_points[i].Hour - hour) < 0.001)
                {
                    _dragIndex = i;
                    break;
                }
            }

            Invalidate();
            RaiseChanged();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_dragIndex < 0)
            {
                int h = HitTest(e.Location);
                Cursor = h >= 0 ? Cursors.Hand : Cursors.Default;

                if (h != _hoverIndex)
                {
                    _hoverIndex = h;
                    Invalidate();
                }

                return;
            }

            if (_dragIndex >= _points.Count)
            {
                _dragIndex = -1;
                return;
            }

            LightPoint p = _points[_dragIndex];

            bool isEnd = _dragIndex == 0 || _dragIndex == _points.Count - 1;

            if (!isEnd)
            {
                double hour = XToHour(e.Location.X);

                // 不能越过相邻点
                double minH = _dragIndex > 0 ? _points[_dragIndex - 1].Hour + 0.15 : 0.15;
                double maxH = _dragIndex < _points.Count - 1
                    ? _points[_dragIndex + 1].Hour - 0.15
                    : 23.85;

                p.Hour = Clamp(hour, minH, maxH);
            }

            p.Light = YToLight(e.Location.Y);

            // 首末点亮度强制同步，避免跨 0 点跳变。
            // 必须以「正在拖的那一端」为准，否则首点刚改的值会被末点覆盖。
            if (isEnd)
            {
                AppConfig.SyncWrapEnds(_points, _dragIndex == 0);
            }

            Invalidate();
            RaiseChanged();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (_dragIndex >= 0)
            {
                bool wasFirst = _dragIndex == 0;
                _dragIndex = -1;
                Normalize(wasFirst);
                Invalidate();
                RaiseChanged();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            if (_hoverIndex >= 0 && _dragIndex < 0)
            {
                _hoverIndex = -1;
                Invalidate();
            }
        }

        #endregion

        #region 键盘

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData & Keys.KeyCode)
            {
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                    return true;
            }

            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            int idx = _dragIndex >= 0 ? _dragIndex : _hoverIndex;

            if (idx < 0 || idx >= _points.Count)
            {
                return;
            }

            bool isEnd = idx == 0 || idx == _points.Count - 1;
            bool changed = false;

            double hourStep = e.Shift ? 1.0 : 0.25;
            double lightStep = e.Shift ? 0.1 : 0.02;

            LightPoint p = _points[idx];

            switch (e.KeyCode)
            {
                case Keys.Left:
                    if (!isEnd)
                    {
                        double minH = idx > 0 ? _points[idx - 1].Hour + 0.15 : 0.15;
                        p.Hour = Clamp(p.Hour - hourStep, minH, 24);
                        changed = true;
                    }
                    break;

                case Keys.Right:
                    if (!isEnd)
                    {
                        double maxH = idx < _points.Count - 1 ? _points[idx + 1].Hour - 0.15 : 23.85;
                        p.Hour = Clamp(p.Hour + hourStep, 0, maxH);
                        changed = true;
                    }
                    break;

                case Keys.Up:
                    p.Light = Clamp(p.Light + lightStep, 0, 1);
                    changed = true;
                    break;

                case Keys.Down:
                    p.Light = Clamp(p.Light - lightStep, 0, 1);
                    changed = true;
                    break;
            }

            if (changed)
            {
                if (isEnd)
                {
                    AppConfig.SyncWrapEnds(_points, idx == 0);
                }

                Normalize(idx == 0);
                Invalidate();
                RaiseChanged();
                e.Handled = true;
            }
        }

        #endregion

        private void RaiseChanged()
        {
            ScheduleChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
