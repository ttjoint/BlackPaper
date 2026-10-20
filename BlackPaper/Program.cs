using System;
using System.Threading;
using System.Windows.Forms;

namespace BlackPaper
{
    static class Program
    {
        private const string SingleInstanceMutexName = @"Local\BlackPaper.SingleInstance";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 单实例：第二个实例直接退出，避免多个进程抢着重置颜色矩阵
            using (var mutex = new Mutex(true, SingleInstanceMutexName, out bool isFirstInstance))
            {
                if (!isFirstInstance)
                {
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // 所有异常退出路径都把画面亮度恢复，避免留下暗屏
                AppDomain.CurrentDomain.ProcessExit += (s, e) => MagnifierDim.Restore();
                AppDomain.CurrentDomain.UnhandledException += (s, e) => MagnifierDim.Restore();
                Application.ThreadException += (s, e) => MagnifierDim.Restore();

                try
                {
                    Application.Run(new BlackForm());
                }
                finally
                {
                    MagnifierDim.Restore();
                }
            }
        }
    }
}
