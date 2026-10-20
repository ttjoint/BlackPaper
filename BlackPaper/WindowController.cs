using Microsoft.Win32;
using System;
using System.Windows.Forms;

namespace BlackPaper
{
    /// <summary>
    /// 开机自启动等系统集成功能。
    /// （原先的窗口穿透逻辑已迁移到 OverlayForm，那里用 64 位安全的 SetWindowLongPtr。）
    /// </summary>
    internal class WindowController
    {
        private readonly Control _ctl;

        public WindowController(Control ctl) => _ctl = ctl;

        #region 开机自动运行相关

        /// <summary>
        /// 检查注册表里是否已经设置了开机自启动。
        /// </summary>
        public bool checkIfAutoRun(string subkey, string name)
        {
            if (string.IsNullOrWhiteSpace(subkey) || string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            try
            {
                using (RegistryKey baseKey = Registry.CurrentUser)
                using (RegistryKey runKey = baseKey.OpenSubKey(subkey))
                {
                    object value = runKey?.GetValue(name);
                    if (value == null)
                    {
                        return false;
                    }

                    string text = value.ToString();
                    if (string.IsNullOrWhiteSpace(text) || text.Equals("false", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 设置或取消开机自启动。
        /// </summary>
        public void AutoRun(string subkey, string name, bool isAutoRun)
        {
            if (string.IsNullOrWhiteSpace(subkey) || string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            try
            {
                string startupPath = Application.ExecutablePath;

                using (RegistryKey baseKey = Registry.CurrentUser)
                using (RegistryKey runKey = baseKey.CreateSubKey(subkey))
                {
                    if (runKey == null)
                    {
                        return;
                    }

                    if (isAutoRun)
                    {
                        // 路径含空格时加引号，否则 Windows 会错误拆分命令行
                        string quoted = startupPath.Contains(" ")
                            ? "\"" + startupPath + "\""
                            : startupPath;
                        runKey.SetValue(name, quoted, RegistryValueKind.String);
                    }
                    else
                    {
                        runKey.DeleteValue(name, throwOnMissingValue: false);
                    }
                }
            }
            catch
            {
                // 注册表写入失败（组策略限制等）时静默忽略
            }
        }

        #endregion
    }
}
