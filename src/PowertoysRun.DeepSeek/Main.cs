using System.Diagnostics;
using PowertoysRun.DeepSeek.Services;
using Wox.Plugin;

namespace PowertoysRun.DeepSeek;

public class Main : IPlugin
{
    private DeepSeekService? _deepSeekService;
    private Services.PluginSettings? _settings;

    public static string PluginID => "CE0C0B4E-C5A7-4E1A-9F8D-2A3B4C5D6E7F";
    public string Name => "DeepSeek";
    public string Description => "通过 DeepSeek API 获取 AI 回应，短回答直接显示，长回答跳转浏览器";

    private const string IconPathDark = "Images\\DeepSeek.dark.png";
    private const string IconPathLight = "Images\\DeepSeek.light.png";

    public void Init(PluginInitContext context)
    {
        _settings = PluginSettings.Load(context.CurrentPluginMetadata.PluginDirectory);
        _deepSeekService = new DeepSeekService();
        _deepSeekService.Configure(_settings.ApiKey, _settings.Model);
    }

    public List<Result> Query(Query query)
    {
        var search = query.Search?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(search))
            return GetHelpResults();

        if (_deepSeekService == null || _settings == null)
            return [new Result { Title = "插件未初始化", SubTitle = "请重启 PowerToys", IcoPath = IconPathDark }];

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            return [new Result
            {
                Title = "请先配置 DeepSeek API Key",
                SubTitle = "编辑 settings.json 填入你的 API Key",
                IcoPath = IconPathDark
            }];

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
            return [new Result
            {
                Title = response.Answer,
                SubTitle = "[DeepSeek] — Ctrl+Shift+C 复制",
                IcoPath = IconPathDark,
                Action = _ => true
            }];
        }

        return [new Result
        {
            Title = "回答较长，按 Enter 在浏览器中查看",
            SubTitle = $"[DeepSeek] {Truncate(response.Answer, 100)}",
            IcoPath = IconPathDark,
            Action = _ =>
            {
                OpenInBrowser(response.Question, response.Answer);
                return true;
            }
        }];
    }

    private static void OpenInBrowser(string question, string answer)
    {
        var html = BuildHtmlPage(question, answer);
        var tempPath = Path.Combine(Path.GetTempPath(), $"deepseek_response_{DateTime.Now:yyyyMMddHHmmss}.html");
        File.WriteAllText(tempPath, html);
        Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
    }

    private List<Result> GetHelpResults()
    {
        return
        [
            new Result
            {
                Title = "DeepSeek 查询 (ds)",
                SubTitle = "输入问题，按 Enter 获取 DeepSeek 回答。短回答直接显示，长回答跳转浏览器。",
                IcoPath = IconPathDark,
                Score = 100
            },
            new Result
            {
                Title = "示例: ds 如何用C#写Hello World",
                SubTitle = "输入 ds 加空格后输入你的问题",
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

    private static string BuildHtmlPage(string question, string answer)
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
            "<title>DeepSeek 回答</title>\n<style>\n" +
            "body {{ {0} }}\n.question {{ {1} }}\n.answer {{ {2} }}\nh2 {{ {3} }}\n" +
            "</style>\n</head>\n<body>\n" +
            "<div class=\"question\">\n<h2>问题</h2>\n<p>{4}</p>\n</div>\n" +
            "<h2>回答</h2>\n<div class=\"answer\">{5}</div>\n</body>\n</html>",
            css, cssQuestion, cssAnswer, cssH2, escapedQuestion, escapedAnswer);
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
