# BlackPaper

屏幕压暗小工具，用于夜间护眼。纯 Windows Forms / .NET Framework 4.8 实现，无外部依赖。

> **仓库迁移说明**
>
> 旧库 [`momordica2020/BlackPaper`](https://github.com/momordica2020/BlackPaper) **已归档（archived）**，仅作只读保留。
> 本仓库迁移至 **[`ttjoint/BlackPaper`](https://github.com/ttjoint/BlackPaper)**，后续开发提交均在此进行。
>
> ⚠️ 请勿向旧库推送。仓库内已配置 `pre-push` 钩子（`githooks/pre-push`）拦截指向旧库的推送，
> 且 Git 配置中已移除旧 `origin`，仅在 `blackpaper.archived.*` 下保留归档记录。

---

## 功能

- **整屏压暗**（默认）—— 通过 Windows 放大镜颜色矩阵对整个桌面做线性衰减，**开始菜单、搜索、任务视图、UAC、全屏独占游戏统统生效**
- **黑膜叠加**（备选）—— 传统的分层透明窗口，只覆盖普通应用窗口
- 按时段自动调节（白天不压暗 / 傍晚 / 夜间）
- 亮度记忆，重启后恢复上次档位
- 开机自动启动
- 多显示器支持，每屏独立遮罩窗口

## 两种模式的区别

| | 整屏压暗（Dim） | 黑膜叠加（Curtain） |
|---|---|---|
| 实现 | `MagSetFullscreenColorEffect` | 分层窗口 + `SetLayeredWindowAttributes` |
| 覆盖开始菜单 | ✅ | ❌ 开始菜单会浮在上面 |
| 覆盖 UAC / 全屏独占 | ✅ | ❌ |
| 变暗方式 | 线性缩放 `result = c × k`，无色相 / 对比度变化 | 叠加半透明黑色 |
| 多显示器 | 天然覆盖全部 | 每屏一个窗口 |
| 进程被强杀 | 系统自动恢复 | 可能残留黑屏 |
| 冲突 | 与 Windows 放大镜互斥（全局单例） | 无 |

托盘右键菜单里可随时切换，选择会写入 `appconfig.json`。

## 使用

运行 `BlackPaper.exe` 后，程序驻留系统托盘，右键托盘图标：

- **亮度下拉框** —— 0（透明）到 100%（全黑），6 个档位
- **自动亮度** —— 按当前时间自动切换档位
- **整屏压暗模式** —— 切换上述两种模式
- **开机自动启动** —— 写入 `HKCU\...\CurrentVersion\Run`
- **退出** —— 恢复屏幕亮度并退出

## 构建

需要 Visual Studio 2019+ 或 MSBuild 18，以及 .NET Framework 4.8 开发包。

```bat
nuget restore BlackPaper.sln
msbuild BlackPaper.sln /p:Configuration=Release
```

`packages/` 为 NuGet 还原产物，不纳入版本控制，克隆后需先 `nuget restore`
（用 Visual Studio 打开解决方案会自动还原）。

输出位于 `BlackPaper\bin\Release\`。

## 配置文件

`appconfig.json` 位于 exe 同目录，首次运行自动生成：

```json
{
  "appName": "BlackPaper",
  "appSubkey": "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run",
  "beginLight": 0.25,
  "isAutoChange": false,
  "isAutoRun": false,
  "isEnabled": true,
  "mode": 0
}
```

- `beginLight` —— 手动亮度，0 ~ 1
- `isAutoChange` —— 是否启用按时段自动调节（开启时忽略 `beginLight`）
- `mode` —— `0` = 整屏压暗，`1` = 黑膜叠加

文件损坏时不会阻塞启动：原文件会被备份为 `appconfig.json.broken_<时间戳>`，随后重建默认配置。

## 已知限制

1. **整屏压暗是全局独占的** —— `MagSetFullscreenColorEffect` 同一时刻只能被一个进程持有。
   同时运行 Windows 放大镜或某些色彩管理 / 录屏工具时会互相抢占。已用 Mutex 限制单实例。
2. **黑膜模式盖不住 shell 表面** —— 开始菜单、UAC 等由 UIAccess 进程托管，普通 topmost 窗口从权限模型上无法覆盖。这是选择「整屏压暗」作为默认模式的原因。
3. **HDR 显示器** 上放大镜颜色矩阵可能不生效，此时可切回黑膜模式，或使用系统夜间模式。

## 相对旧版本的修复

- 修复亮度记忆失效（`AppConfig` 原用字段而非属性，`System.Text.Json` 不序列化字段，配置文件恒为 `{}`）
- 修复 64 位下 `SetWindowLong` / `GetWindowLong` 参数宽度不匹配，改用 `...LongPtrW`，并由「覆写」改为「读-改-写」保留原有扩展样式
- 修复 `Opacity` 与 `SetLayeredWindowAttributes` 双机制互相覆盖
- 修复下拉框 `SelectedIndex` 回写导致配置精确值被吸附到 UI 档位
- 修复退出后托盘图标残留（Win11 上的「幽灵图标」）
- 修复路径含空格时开机自启动命令行被错误拆分
- 新增单实例保护、崩溃 / 强杀时的亮度恢复兜底

## 许可

未声明。
