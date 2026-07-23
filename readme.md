# PowerToys Run for DeepSeek / 知乎直答

一个 PowerToys Run 插件，在快速输入框输入内容后，从 DeepSeek API 或知乎直答 API 获取 AI 回应。默认使用 `deepseek-v4-flash` 模型，短回答直接显示，长回答自动跳转浏览器。

## 安装

### 编译

```bash
# 安装 .NET 9 SDK（Linux / macOS）
wget -qO- https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 9.0 --install-dir $HOME/.dotnet9
export DOTNET_ROOT=$HOME/.dotnet9
export PATH="$DOTNET_ROOT:$PATH"

# 编译 Release 版
dotnet build src/PowertoysRun.DeepSeek/PowertoysRun.DeepSeek.csproj -c Release
```

### 部署

1. 在 PowerToys Run 插件目录下创建 `DeepSeek` 文件夹（通常位于 `%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins\`）
2. 将编译输出（`bin/Release/net9.0-windows/` 下所有文件）复制到该文件夹
3. 重启 PowerToys 或重新加载插件

## 配置

在插件目录下创建 `settings.json`：

### DeepSeek 模式（默认）

```json
{
  "Provider": "deepseek",
  "ApiKey": "sk-你的DeepSeek-API-Key",
  "Model": "deepseek-v4-flash"
}
```

### 知乎直答模式

```json
{
  "Provider": "zhida",
  "ZhidaAccessSecret": "你的知乎-Access-Secret",
  "ZhidaModel": "zhida-fast-1p5"
}
```

### 配置项说明

| 字段              | 说明                                | 默认值              |
| ----------------- | ----------------------------------- | ------------------- |
| `Provider`        | 服务提供商：`deepseek` 或 `zhida`  | `deepseek`          |
| `ApiKey`          | DeepSeek API Key                   | 无                  |
| `Model`           | DeepSeek 模型名称                   | `deepseek-v4-flash` |
| `ZhidaAccessSecret` | 知乎 Access Secret               | 无                  |
| `ZhidaModel`      | 知乎直答模型档位                    | `zhida-fast-1p5`    |

### 知乎直答支持的模型

| 模型                | 说明       |
| ------------------- | ---------- |
| `zhida-fast-1p5`    | 快速回答   |
| `zhida-thinking-1p5` | 深度思考  |
| `zhida-agent`       | 智能思考   |

## 使用

1. 按 `Alt+Space` 打开 PowerToys Run
2. 输入激活关键词 `ds`，然后输入你的问题，例如：
   ```
   ds 什么是依赖注入？
   ```
3. 按 Enter：
   - **短回答**（≤200字符且不含换行）：直接显示在结果中，按 `Ctrl+Shift+C` 复制
   - **长回答**：自动生成 HTML 页面并在浏览器中打开

## 构建要求

- .NET SDK 9.0+
- 依赖 [Community.PowerToys.Run.Plugin.Dependencies](https://www.nuget.org/packages/Community.PowerToys.Run.Plugin.Dependencies) NuGet 包
- 交叉编译（Linux → Windows）需在 csproj 设置 `<EnableWindowsTargeting>true</EnableWindowsTargeting>`