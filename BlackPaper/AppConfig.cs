using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace BlackPaper
{
    /// <summary>
    /// 应用配置。
    /// 注意：System.Text.Json 默认只序列化「公共属性」，不序列化字段。
    /// 原实现用的是字段，导致 SaveConfig 永远写出 {}，亮度记忆功能实际失效。
    /// 这里全部改成属性，并保留默认值。
    /// </summary>
    public class AppConfig
    {
        public string AppName { get; set; } = "BlackPaper";

        public string AppSubkey { get; set; } =
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        /// <summary>手动亮度档位，0~1。</summary>
        public double BeginLight { get; set; } = 0.0;

        /// <summary>是否启用按时段自动调节。</summary>
        public bool IsAutoChange { get; set; } = true;

        /// <summary>是否开机自启（以注册表实际状态为准，这里只做缓存）。</summary>
        public bool IsAutoRun { get; set; } = false;

        /// <summary>遮罩总开关。</summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 工作模式：0 = Dim（放大镜整屏压暗，可覆盖开始菜单），1 = Curtain（分层黑膜）。
        /// </summary>
        public int Mode { get; set; } = 0;
    }

    public class ConfigManager
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            // 容忍用户手改配置时的大小写差异
            PropertyNameCaseInsensitive = true,
            // 兼容旧版本写出的字段名（小驼峰）
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly string _configFilePath =
            Path.Combine(Application.StartupPath, "appconfig.json");

        public string ConfigFilePath => _configFilePath;

        public AppConfig LoadConfig()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    AppConfig fresh = new AppConfig();
                    SaveConfig(fresh);
                    return fresh;
                }

                string json = File.ReadAllText(_configFilePath);
                AppConfig loaded = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);

                // 空文件 / {} 会得到 null，退回默认值
                if (loaded == null)
                {
                    loaded = new AppConfig();
                    SaveConfig(loaded);
                }

                return loaded;
            }
            catch (Exception)
            {
                // 配置损坏时不要阻塞启动：备份原文件，再用默认值重建。
                TryBackupBrokenConfig();

                AppConfig fallback = new AppConfig();
                try
                {
                    SaveConfig(fallback);
                }
                catch
                {
                    // 只读目录下无法写入，忽略
                }

                return fallback;
            }
        }

        /// <summary>把损坏的配置文件改名备份，避免直接覆盖丢失用户数据。</summary>
        private void TryBackupBrokenConfig()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    return;
                }

                string backup = _configFilePath + ".broken_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.Copy(_configFilePath, backup, overwrite: true);
            }
            catch
            {
                // 备份失败不影响主流程
            }
        }

        public void SaveConfig(AppConfig config)
        {
            if (config == null)
            {
                return;
            }

            try
            {
                string json = JsonSerializer.Serialize(config, JsonOptions);
                File.WriteAllText(_configFilePath, json);
            }
            catch
            {
                // 写入失败不应导致程序崩溃（例如安装到 Program Files 时无写权限）
            }
        }
    }
}
