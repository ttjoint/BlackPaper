using System;
using System.Collections.Generic;
using System.Threading;
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
        private readonly System.Windows.Forms.Timer _reassertTimer;

        // 渐变泵线程（子线程，不阻塞 UI）
        private Thread _pumpThread;
        private CancellationTokenSource _pumpCts;

        // 遮罩刷新必须回到 UI 线程（Magnifier API 的线程约束）
        private readonly SynchronizationContext _syncContext;
        private int _applyPending;
        private readonly NativeMethods.WinEventDelegate _winEventProc;
        private IntPtr _foregroundHook;
        private IntPtr _showHook;
        private bool _disposed;

        public double Opacity { get; private set; }

        /// <summary>
        /// 当前过渡的「意图目标值」。
        /// 与 Opacity 的区别：Opacity 是画面此刻的真实亮度，Target 是用户希望到达的值。
        /// 渐变线程每帧读 Target 并朝它逼近，所以快速反向调节能立即响应。
        /// </summary>
        public double Target
        {
            get => Volatile.Read(ref _target);
            private set => Volatile.Write(ref _target, value);
        }

        private double _target;

        /// <summary>是否正在过渡中。</summary>
        public bool IsTransitioning => _pumpThread != null;
        public bool Enabled { get; private set; } = true;

        /// <summary>当前模式。切换时自动迁移当前亮度。</summary>
        public OverlayMode Mode { get; private set; }

        /// <summary>一旦 Dim 模式不可用（例如旧系统），自动降级到 Curtain。</summary>
        public bool DimAvailable => MagnifierDim.IsAvailable;

        public OverlayManager(OverlayMode mode)
        {
            Mode = mode;

            // 捕获创建时的 UI 上下文，供 Curtain 模式跨线程回到 UI 线程
            _syncContext = SynchronizationContext.Current;

            _winEventProc = OnWinEvent;

            if (!MagnifierDim.Initialize() && Mode == OverlayMode.Dim)
            {
                // 环境不支持放大镜，安静降级
                Mode = OverlayMode.Curtain;
            }

            if (Mode == OverlayMode.Curtain)
            {
                InstallHooks();
                _reassertTimer = new System.Windows.Forms.Timer { Interval = 700 };
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
            StopTransition();
            Opacity = Math.Max(0.0, Math.Min(1.0, level));
            Target = Opacity;
            ApplyOpacity();
        }

        /// <summary>
        /// 平滑过渡到目标亮度。
        ///
        /// 用后台线程做「拖尾泵」：每帧读一次 Target，朝它逼近一小步。
        /// 与原先的「按时间公式」相比，好处是：
        ///  - 反向时不需要跑完旧方向，下一帧就转向，所以交错滚动也跟手
        ///  - 目标可在途中随时改写，下一帧自动适用
        ///  - 永远从当前真实值出发，不会回跳
        /// durationMs 只决定大致节奏，不决定精确完成时刻。
        /// </summary>
        public void SetOpacitySmooth(double level, int durationMs)
        {
            double target = Math.Max(0.0, Math.Min(1.0, level));

            // 先记录当前画面真实亮度，新过渡从这里起步
            double from = Opacity;

            StopTransition();
            Target = target;

            if (durationMs <= 0 || Math.Abs(target - from) < 0.0005)
            {
                Opacity = target;
                ApplyOpacity();
                return;
            }

            const int frameMs = 8;

            // 恒定速度。这里的关键是步长必须和「本次要走多远」挂钩，
            // 不能按「走完全程 0~1」算 —— 否则小幅度调节（例如滚轮一次 4%）
            // 会被单帧步长一口吞掉，视觉上等于没有渐变。
            //
            // 同时设一个下限，避免极短时长时步长过小导致拖尾过长。
            int frames = Math.Max(1, durationMs / frameMs);
            double span = Math.Abs(target - from);
            double stepPerFrame = Math.Max(0.0008, span / frames);

            var cts = new CancellationTokenSource();
            _pumpCts = cts;

            var thread = new Thread(() =>
            {
                double current = from;

                while (!cts.IsCancellationRequested)
                {
                    double goal = Target;
                    double delta = goal - current;

                    if (Math.Abs(delta) <= stepPerFrame)
                    {
                        // 一步之内能到，直接到位，避免长尾
                        current = goal;
                    }
                    else
                    {
                        current += Math.Sign(delta) * stepPerFrame;
                    }

                    current = Math.Max(0.0, Math.Min(1.0, current));
                    Opacity = current;
                    ApplyOpacityFromAnyThread();

                    if (Math.Abs(Target - current) < 0.0015)
                    {
                        return;
                    }

                    try
                    {
                        Thread.Sleep(frameMs);
                    }
                    catch (ThreadInterruptedException)
                    {
                        return;
                    }
                }
            })
            {
                IsBackground = true,
                Name = "BlackPaper-Fade",
            };

            _pumpThread = thread;
            thread.Start();
        }

        private void StopTransition()
        {
            CancellationTokenSource cts = _pumpCts;
            _pumpCts = null;

            if (cts != null)
            {
                try
                {
                    cts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // 已释放，忽略
                }
            }

            Thread th = _pumpThread;
            _pumpThread = null;

            // 等泵线程退出，避免它和新过渡同时写 Opacity 造成锯齿。
            // 泵每帧只睡 10ms，这里的等待很短；
            // 且调用来自 UI 线程以外的时机不安全，所以用带超时的 Join。
            if (th != null && th.IsAlive)
            {
                try
                {
                    th.Join(80);
                }
                catch (ThreadStateException)
                {
                    // 忽略
                }
            }

            if (cts != null)
            {
                try
                {
                    cts.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // 忽略
                }
            }
        }

        /// <summary>
        /// 从任意线程应用当前遮罩值。
        /// Magnifier 的 P/Invoke 本身线程安全（无状态系统调用）；
        /// Curtain 模式涉及 WinForms 控件，需要回到 UI 线程。
        /// </summary>
        /// <summary>
        /// 从任意线程请求刷新遮罩。
        ///
        /// 重要：MagSetFullscreenColorEffect 只能在**初始化 Magnifier 的那个线程**
        /// （即 UI 线程）上成功调用，从后台渐变线程直接调会静默失败（返回 false），
        /// 表现为「数值在变但画面不动」。
        /// 所以这里一律切回 UI 线程执行，并用「合并投递」避免消息队列积压。
        /// </summary>
        private void ApplyOpacityFromAnyThread()
        {
            SynchronizationContext ctx = _syncContext;

            // 没有 UI 上下文（例如单元测试）时退化为直接调用
            if (ctx == null)
            {
                ApplyOpacity();
                return;
            }

            // 已有一次待处理的刷新就跳过：每帧最多投递一次，
            // 且 UI 执行时读取的是最新 Opacity，天然合并。
            if (Interlocked.Exchange(ref _applyPending, 1) == 1)
            {
                return;
            }

            try
            {
                ctx.Post(_ =>
                {
                    Interlocked.Exchange(ref _applyPending, 0);

                    try
                    {
                        ApplyOpacity();
                    }
                    catch
                    {
                        // UI 已销毁等情况忽略
                    }
                }, null);
            }
            catch
            {
                Interlocked.Exchange(ref _applyPending, 0);
            }
        }

        public void SetEnabled(bool enabled)
        {
            StopTransition();
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

            StopTransition();
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
