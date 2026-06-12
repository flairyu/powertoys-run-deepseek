# AGENTS.md

## 项目概述
PowerToys Run 插件，通过快速输入框调用 DeepSeek API 获取回应。默认使用 `deepseek-v4-flash` 模型，短回答直接显示，长回答跳转浏览器。

## 技术栈
- C# / .NET 9（目标 `net9.0-windows`）
- 依赖 `Community.PowerToys.Run.Plugin.Dependencies` NuGet 包提供 PowerToys Run 接口
- 仅在 Windows 上的 PowerToys Run 中运行

## 构建命令
```bash
# 使用 .NET 9 SDK（非 snap 安装）
export DOTNET_ROOT=/home/yxb/.dotnet9
export PATH="$DOTNET_ROOT:$PATH"

# 编译 Release 版
dotnet build src/PowertoysRun.DeepSeek/PowertoysRun.DeepSeek.csproj -c Release

# 输出: src/PowertoysRun.DeepSeek/bin/Release/net9.0-windows/
```

## 项目结构
```
src/PowertoysRun.DeepSeek/
  Main.cs              — 插件入口，实现 Wox.Plugin.IPlugin
  plugin.json           — PowerToys Run 插件清单
  Services/
    DeepSeekService.cs  — DeepSeek API 客户端 (chat/completions)
    PluginSettings.cs   — 设置读写 (settings.json)
  Images/               — 图标文件 (128x128 PNG)
```

## 关键约定
- 插件激活关键词：`ds`（定义在 plugin.json ActionKeyword）
- API Key 配置：在插件目录下创建 `settings.json`，格式：`{"ApiKey": "sk-xxx", "Model": "deepseek-v4-flash"}`
- `Main` 类必须有 `static PluginID` 属性（值匹配 plugin.json 中的 ID），否则 PowerToys 初始化失败
- 短回答判定：回答长度 ≤ 200 字符且不含换行符
- 长回答：生成临时 HTML 文件并通过 `Process.Start` 在浏览器中打开
- 短回答：文字直接显示在结果标题中，用户通过 PowerToys Run 内置快捷键 Ctrl+Shift+C 复制
- `Query()` 方法中使用 `.GetAwaiter().GetResult()` 同步等待异步 API 调用（PowerToys Run 在后台线程回调 Query）

## 注意事项
- **类型身份问题**：PowerToys Run 通过 `typeof(IPlugin)` 严格校验插件类型，自建 Wox.Plugin 接口无法通过。必须通过 `Community.PowerToys.Run.Plugin.Dependencies` 引用真正的 PowerToys 接口。
- **PowerToys 与 .NET 版本对应**：PowerToys 0.100.x 对应 .NET 9。Community 包版本需与 PowerToys 版本匹配（当前 v0.97.0 → PowerToys 0.97+）。
- .NET 9 SDK 不在 snap 渠道中，需通过 `https://dot.net/v1/dotnet-install.sh` 脚本安装
- 交叉编译需在 csproj 设置 `<EnableWindowsTargeting>true</EnableWindowsTargeting>`
- `PluginSettings` 类名避免与 PowerToys 程序集中的 `Settings` 命名空间冲突
- 部署时将 `bin/Release/net9.0-windows/` 整个目录复制到 PowerToys Run 插件目录
