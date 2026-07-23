using System.Diagnostics;
using PowertoysRun.DeepSeek.Services;
using Wox.Plugin;

namespace PowertoysRun.DeepSeek;

public class Main : IPlugin
{
    private DeepSeekService? _deepSeekService;
    private ZhidaService? _zhidaService;
    private Services.PluginSettings? _settings;
    private string _provider = "deepseek";
    private CancellationTokenSource? _debounceCts;
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(2000);

    public static string PluginID => "CE0C0B4E-C5A7-4E1A-9F8D-2A3B4C5D6E7F";
    public string Name => "DeepSeek";
    public string Description => "通过 DeepSeek / 知乎直答 API 获取 AI 回应，短回答直接显示，长回答跳转浏览器";

    private const string IconPathDark = "Images\\DeepSeek.dark.png";
    private const string IconPathLight = "Images\\DeepSeek.light.png";

    public void Init(PluginInitContext context)
    {
        _settings = PluginSettings.Load(context.CurrentPluginMetadata.PluginDirectory);
        _provider = _settings.Provider ?? "deepseek";

        _deepSeekService = new DeepSeekService();
        _deepSeekService.Configure(_settings.ApiKey, _settings.Model);

        _zhidaService = new ZhidaService();
        _zhidaService.Configure(_settings.ZhidaAccessSecret, _settings.ZhidaModel);
    }

    public List<Result> Query(Query query)
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var debounceToken = _debounceCts.Token;

        var search = query.Search?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(search))
            return GetHelpResults();

        if (_settings == null)
            return [new Result { Title = "插件未初始化", SubTitle = "请重启 PowerToys", IcoPath = IconPathDark }];

        var isZhida = string.Equals(_provider, "zhida", StringComparison.OrdinalIgnoreCase);

        if (isZhida)
        {
            if (_zhidaService == null)
                return [new Result { Title = "插件未初始化", SubTitle = "请重启 PowerToys", IcoPath = IconPathDark }];

            if (string.IsNullOrWhiteSpace(_settings.ZhidaAccessSecret))
                return [new Result
                {
                    Title = "请先配置知乎 Access Secret",
                    SubTitle = "编辑 settings.json 填入你的知乎 Access Secret",
                    IcoPath = IconPathDark
                }];

            try
            {
                Task.Delay(DebounceDelay, debounceToken).GetAwaiter().GetResult();
            }
            catch (TaskCanceledException)
            {
                return [new Result { Title = $"输入中... {search}", SubTitle = "知乎直答", IcoPath = IconPathDark }];
            }

            var zhidaResponse = _zhidaService.AskAsync(search).GetAwaiter().GetResult();

            if (!zhidaResponse.Success)
                return [new Result
                {
                    Title = zhidaResponse.ErrorMessage,
                    SubTitle = "查询失败",
                    IcoPath = IconPathDark
                }];

            if (zhidaResponse.IsShort)
            {
                var answer = zhidaResponse.Answer;
                return [new Result
                {
                    Title = answer,
                    SubTitle = "[知乎直答] — Enter 复制到剪贴板",
                    IcoPath = IconPathDark,
                    Action = _ =>
                    {
                        try { System.Windows.Forms.Clipboard.SetText(answer); }
                        catch { }
                        return true;
                    }
                }];
            }

            return [new Result
            {
                Title = "回答较长，按 Enter 在浏览器中查看",
                SubTitle = $"[知乎直答] {Truncate(zhidaResponse.Answer, 100)}",
                IcoPath = IconPathDark,
                Action = _ =>
                {
                    OpenInBrowser("知乎直答", zhidaResponse.Question, zhidaResponse.Answer);
                    return true;
                }
            }];
        }

        if (_deepSeekService == null)
            return [new Result { Title = "插件未初始化", SubTitle = "请重启 PowerToys", IcoPath = IconPathDark }];

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            return [new Result
            {
                Title = "请先配置 DeepSeek API Key",
                SubTitle = "编辑 settings.json 填入你的 API Key",
                IcoPath = IconPathDark
            }];

        try
        {
            Task.Delay(DebounceDelay, debounceToken).GetAwaiter().GetResult();
        }
        catch (TaskCanceledException)
        {
            return [new Result { Title = $"输入中... {search}", SubTitle = "DeepSeek", IcoPath = IconPathDark }];
        }

        var response = _deepSeekService.AskAsync(search).GetAwaiter().GetResult();

        if (!response.Success)
            return [new Result
            {
                Title = response.ErrorMessage,
                SubTitle = "查询失败",
                IcoPath = IconPathDark
            }];

        if (response.IsShort)
        {
            var answer = response.Answer;
            return [new Result
            {
                Title = answer,
                SubTitle = "[DeepSeek] — Enter 复制到剪贴板",
                IcoPath = IconPathDark,
                Action = _ =>
                {
                    try
                    {
                        System.Windows.Forms.Clipboard.SetText(answer);
                    }
                    catch { }
                    return true;
                }
            }];
        }

        return [new Result
        {
            Title = "回答较长，按 Enter 在浏览器中查看",
            SubTitle = $"[DeepSeek] {Truncate(response.Answer, 100)}",
            IcoPath = IconPathDark,
            Action = _ =>
            {
                OpenInBrowser("DeepSeek", response.Question, response.Answer);
                return true;
            }
        }];
    }

    private static void OpenInBrowser(string provider, string question, string answer)
    {
        var html = BuildHtmlPage(provider, question, answer);
        var tempPath = Path.Combine(Path.GetTempPath(), $"deepseek_response_{DateTime.Now:yyyyMMddHHmmss}.html");
        File.WriteAllText(tempPath, html);
        Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
    }

    private List<Result> GetHelpResults()
    {
        var providerLabel = _provider == "zhida" ? "知乎直答" : "DeepSeek";
        return
        [
            new Result
            {
                Title = $"{providerLabel} 查询 (ds)",
                SubTitle = $"当前模式: {providerLabel}。输入问题，按 Enter 获取回答。短回答 Enter 复制到剪贴板，长回答跳转浏览器。",
                IcoPath = IconPathDark,
                Score = 100
            },
            new Result
            {
                Title = "示例: ds 如何用C#写Hello World",
                SubTitle = $"输入 ds 加空格后输入你的问题。在 settings.json 中设置 Provider 切换 DeepSeek / 知乎直答。",
                IcoPath = IconPathLight,
                Score = 50
            }
        ];
    }

    private static string Truncate(string text, int maxLength)
    {
        if (text.Length <= maxLength) return text;
        return text[..maxLength] + "...";
    }

    private static string BuildHtmlPage(string provider, string question, string answer)
    {
        var escapedQuestion = EscapeHtml(question);
        var escapedAnswer = EscapeHtml(answer).Replace("\n", "<br>");

        var css = "font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; max-width: 800px; margin: 40px auto; padding: 20px; background: #1e1e1e; color: #d4d4d4;";
        var cssQuestion = "background: #2d2d2d; padding: 16px; border-radius: 8px; margin-bottom: 20px; border-left: 4px solid #0078d4;";
        var cssAnswer = "line-height: 1.8; white-space: pre-wrap;";
        var cssH2 = "color: #0078d4; margin-top: 0;";

        return string.Format(
            "<!DOCTYPE html>\n<html lang=\"zh-CN\">\n<head>\n<meta charset=\"UTF-8\">\n" +
            "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\n" +
            "<title>{6} 回答</title>\n<style>\n" +
            "body {{ {0} }}\n.question {{ {1} }}\n.answer {{ {2} }}\nh2 {{ {3} }}\n" +
            "</style>\n</head>\n<body>\n" +
            "<div class=\"question\">\n<h2>问题</h2>\n<p>{4}</p>\n</div>\n" +
            "<h2>回答</h2>\n<div class=\"answer\">{5}</div>\n</body>\n</html>",
            css, cssQuestion, cssAnswer, cssH2, escapedQuestion, escapedAnswer, EscapeHtml(provider));
    }

    private static string EscapeHtml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }
}
