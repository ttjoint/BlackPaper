using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace BlackPaper
{
    /// <summary>
    /// 时段亮度曲线上的一个控制点。
    /// Hour 为 0~24 的时刻，Light 为该时刻的亮度（0~1，1 = 最亮）。
    /// </summary>
    public sealed class LightPoint
    {
        /// <summary>时刻，0~24（可为小数，如 8.5 表示 8:30）。</summary>
        public double Hour { get; set; }

        /// <summary>亮度 0~1，1 = 最亮。</summary>
        public double Light { get; set; }

        public LightPoint()
        {
        }

        public LightPoint(double hour, double light)
        {
            Hour = hour;
            Light = light;
        }
    }

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

        /// <summary>
        /// 亮度，0~1。**语义：1 = 最亮（不遮罩），0 = 最暗（全黑）**。
        /// 注意：底层 OverlayManager.Opacity 存的是遮罩强度，两者互为镜像：
        ///   mask = 1 - brightness
        /// 旧版本（ConfigVersion 缺失或 &lt; 2）存的是遮罩强度，加载时自动反转。
        /// </summary>
        public double BeginLight { get; set; } = 1.0;

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

        /// <summary>白天时段的起始小时（0~23），同时也是夜间时段的结束小时。</summary>
        public int DayStartHour { get; set; } = 8;

        /// <summary>白天时段的结束小时（0~23），同时也是夜间时段的起始小时。</summary>
        public int DayEndHour { get; set; } = 18;

        /// <summary>白天时段的亮度，0~1（1 = 最亮）。</summary>
        public double DayLight { get; set; } = 1.0;

        /// <summary>夜间时段的亮度，0~1（1 = 最亮）。</summary>
        public double NightLight { get; set; } = 0.55;

        /// <summary>
        /// 时段亮度的控制点（本版本的主数据）。
        /// 每个点的 Hour 为 0~24 的时刻，Light 为该时刻的亮度（0~1）。
        /// 相邻点之间线性插值；为空时回退到旧的 DayStart/DayEnd 四字段。
        /// </summary>
        public List<LightPoint> Points { get; set; }

        /// <summary>
        /// 配置结构版本。
        /// 1（或缺失）= 亮度字段实际存的是遮罩强度；
        /// 2 = 亮度字段存的是亮度（本版本）。
        ///
        /// 默认值必须是 1：只有这样才能区分「旧配置文件里根本没有这个键」
        /// 和「已经迁移过」。新建的配置文件在 SaveConfig 前会显式设为 2。
        /// </summary>
        public int ConfigVersion { get; set; } = 1;

        /// <summary>切换时段时是否渐变过渡，而不是瞬间跳变。</summary>
        public bool SmoothTransition { get; set; } = true;

        /// <summary>渐变时长，毫秒。</summary>
        public int TransitionMs { get; set; } = 1200;

        /// <summary>
        /// 计算指定小时（可为小数）对应的亮度。
        /// 有控制点时按分段线性插值；否则回退到白天/夜间二值切换。
        /// </summary>
        public double GetLightForHour(double hour)
        {
            hour = Clamp(hour, 0, 24);

            List<LightPoint> pts = NormalizedPoints();

            if (pts.Count == 0)
            {
                double dayStart = DayStartHour;
                double dayEnd = DayEndHour;

                bool isDay = dayStart <= dayEnd
                    ? hour >= dayStart && hour < dayEnd
                    : hour >= dayStart || hour < dayEnd;

                return isDay ? DayLight : NightLight;
            }

            if (pts.Count == 1)
            {
                return pts[0].Light;
            }

            if (hour <= pts[0].Hour)
            {
                return pts[0].Light;
            }

            for (int i = 0; i < pts.Count - 1; i++)
            {
                LightPoint a = pts[i];
                LightPoint b = pts[i + 1];

                if (hour >= a.Hour && hour <= b.Hour)
                {
                    double span = b.Hour - a.Hour;

                    if (span <= 0.0001)
                    {
                        return b.Light;
                    }

                    double t = (hour - a.Hour) / span;
                    return a.Light + (b.Light - a.Light) * t;
                }
            }

            return pts[pts.Count - 1].Light;
        }

        /// <summary>返回排序去重后的控制点副本（不改动原列表）。</summary>
        public List<LightPoint> NormalizedPoints()
        {
            var src = Points;

            if (src == null || src.Count == 0)
            {
                return new List<LightPoint>();
            }

            var list = new List<LightPoint>();

            foreach (LightPoint p in src)
            {
                if (p == null)
                {
                    continue;
                }

                list.Add(new LightPoint
                {
                    Hour = Clamp(p.Hour, 0, 24),
                    Light = Clamp01(p.Light),
                });
            }

            list.Sort((x, y) => x.Hour.CompareTo(y.Hour));

            // 同一时刻只保留后一个，避免零宽度区间
            var result = new List<LightPoint>();

            foreach (LightPoint p in list)
            {
                if (result.Count > 0 && Math.Abs(result[result.Count - 1].Hour - p.Hour) < 0.01)
                {
                    result[result.Count - 1] = p;
                }
                else
                {
                    result.Add(p);
                }
            }

            return result;
        }

        /// <summary>
        /// 把配置修正到合法范围，避免手改配置导致异常。
        /// 同时强制一条不变量：跨 0 点的首末两点亮度必须相等。
        /// </summary>
        public void Normalize()
        {
            BeginLight = Clamp(BeginLight, 0.0, 1.0);
            DayLight = Clamp(DayLight, 0.0, 1.0);
            NightLight = Clamp(NightLight, 0.0, 1.0);
            DayStartHour = ClampHour(DayStartHour);
            DayEndHour = ClampHour(DayEndHour);
            TransitionMs = Math.Max(0, Math.Min(10000, TransitionMs));

            if (Mode != 0 && Mode != 1)
            {
                Mode = 0;
            }

            if (Points != null && Points.Count > 0)
            {
                Points = NormalizedPoints();
                SyncWrapEnds(Points);
            }
        }

        /// <summary>
        /// 强制首末两点亮度相同，消除跨 0 点处的亮度阶跃。
        /// 以**末点**（24 时）为基准，把首点（0 时）拉齐；
        /// 调用方在拖动某个端点的场景下可自行决定先改哪一端。
        /// </summary>
        public static void SyncWrapEnds(List<LightPoint> pts)
            => SyncWrapEnds(pts, false);

        /// <summary>
        /// 首末亮度同步，可指定以哪一端为准。
        /// 默认以末点为准；拖动**首点**时必须传 useFirst = true，
        /// 否则刚拖的值会被末点的旧值覆盖回去（表现为左端点拖不动）。
        /// </summary>
        public static void SyncWrapEnds(List<LightPoint> pts, bool useFirst)
        {
            if (pts == null || pts.Count < 2)
            {
                return;
            }

            LightPoint first = pts[0];
            LightPoint last = pts[pts.Count - 1];

            // 首点应钉在 0 点、末点钉在 24 点，否则插值会覆盖不到全天
            first.Hour = 0;
            last.Hour = 24;

            double v = useFirst ? first.Light : last.Light;

            first.Light = v;
            last.Light = v;
        }

        private static double Clamp(double v, double min, double max)
            => v < min ? min : (v > max ? max : v);

        internal static double Clamp01(double v)
            => v < 0 ? 0 : (v > 1 ? 1 : v);

        /// <summary>
        /// 由「白天起/止 + 白天亮度 + 夜间亮度」构造四个控制点。
        /// 首末点钉在 0 点和 24 点，使曲线覆盖完整一天。
        /// </summary>
        public static List<LightPoint> BuildDefaultPoints(double dayStart, double dayEnd, double dayLight, double nightLight)
        {
            double s = Clamp(dayStart, 0, 24);
            double e = Clamp(dayEnd, 0, 24);

            if (e <= s)
            {
                e = Math.Min(24.0, s + 1.0);
            }

            double night = Clamp01(nightLight);

            return new List<LightPoint>
            {
                new LightPoint(0, night),
                new LightPoint(s, Clamp01(dayLight)),
                new LightPoint(e, Clamp01(dayLight)),
                new LightPoint(24, night),
            };
        }

        private static int ClampHour(int h)
            => h < 0 ? 0 : (h > 23 ? 23 : h);
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
                    AppConfig fresh = new AppConfig { ConfigVersion = 2 };
                    SaveConfig(fresh);
                    return fresh;
                }

                string json = File.ReadAllText(_configFilePath);
                AppConfig loaded = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);

                // 空文件 / {} 会得到 null，退回默认值
                if (loaded == null)
                {
                    // 文件存在但解析为空：按新版本处理，避免把默认值反转一次
                    loaded = new AppConfig { ConfigVersion = 2 };
                    SaveConfig(loaded);
                }

                // 旧配置里没有新字段时，属性初始化器会给默认值；这里统一修正范围
                bool migrated = MigrateLightSemantics(loaded);
                loaded.Normalize();

                // 迁移/修正后立即回写，避免每次启动都重算
                if (migrated)
                {
                    SaveConfig(loaded);
                }

                return loaded;
            }
            catch (Exception)
            {
                // 配置损坏时不要阻塞启动：备份原文件，再用默认值重建。
                TryBackupBrokenConfig();

                AppConfig fallback = new AppConfig { ConfigVersion = 2 };
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

        /// <summary>
        /// 把旧版本（存储掩强度）的亮度字段反转成新语义（亮度）。
        /// ConfigVersion 为 1 或缺失时视为旧版本。
        /// </summary>
        /// <returns>是否实际做了迁移。</returns>
        private static bool MigrateLightSemantics(AppConfig loaded)
        {
            bool migrated = false;

            if (loaded.ConfigVersion < 2)
            {
                // 旧值：1 = 全黑。新值：1 = 最亮。直接取反。
                loaded.BeginLight = 1.0 - AppConfig.Clamp01(loaded.BeginLight);
                loaded.DayLight = 1.0 - AppConfig.Clamp01(loaded.DayLight);
                loaded.NightLight = 1.0 - AppConfig.Clamp01(loaded.NightLight);

                loaded.ConfigVersion = 2;
                migrated = true;
            }

            // 旧配置没有控制点列表：用白天/夜间四字段生成等价曲线
            if (loaded.Points == null || loaded.Points.Count == 0)
            {
                loaded.Points = AppConfig.BuildDefaultPoints(
                    loaded.DayStartHour,
                    loaded.DayEndHour,
                    loaded.DayLight,
                    loaded.NightLight);

                migrated = true;
            }

            return migrated;
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
