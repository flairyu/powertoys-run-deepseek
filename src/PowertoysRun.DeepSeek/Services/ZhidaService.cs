using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PowertoysRun.DeepSeek.Services;

public class ZhidaService
{
    private static readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri("https://developer.zhihu.com"),
        Timeout = TimeSpan.FromSeconds(60)
    };

    private const string DefaultModel = "zhida-fast-1p5";
    private const int ShortResponseMaxLength = 200;

    private string _accessSecret = string.Empty;
    private string _model = DefaultModel;

    public void Configure(string accessSecret, string? model = null)
    {
        _accessSecret = accessSecret;
        if (!string.IsNullOrWhiteSpace(model))
            _model = model;
    }

    public async Task<DeepSeekResponse> AskAsync(string question, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_accessSecret))
            return DeepSeekResponse.Error("请先配置知乎 Access Secret");

        var requestBody = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "user", content = question }
            },
            stream = false
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessSecret);
        request.Headers.Add("X-Request-Timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = ParseErrorMessage(responseJson) ?? $"API 请求失败 ({(int)response.StatusCode})";
                return DeepSeekResponse.Error(errorMsg);
            }

            var apiResponse = JsonSerializer.Deserialize<ZhidaApiResponse>(responseJson);
            var choice = apiResponse?.Choices?.FirstOrDefault();
            if (choice?.Message == null)
                return DeepSeekResponse.Error("API 返回了空回应");

            var answer = BuildAnswer(choice.Message);
            if (string.IsNullOrEmpty(answer))
                return DeepSeekResponse.Error("API 返回了空回应");

            var isShort = answer.Length <= ShortResponseMaxLength && !answer.Contains('\n');
            return new DeepSeekResponse
            {
                Answer = answer,
                IsShort = isShort,
                Success = true,
                Question = question
            };
        }
        catch (TaskCanceledException)
        {
            return DeepSeekResponse.Error("请求超时，请重试");
        }
        catch (Exception ex)
        {
            return DeepSeekResponse.Error($"请求异常: {ex.Message}");
        }
    }

    private static string BuildAnswer(ZhidaMessage message)
    {
        var reasoning = message.ReasoningContent?.Trim();
        var content = message.Content?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(reasoning))
            return content;

        if (string.IsNullOrEmpty(content))
            return reasoning;

        return $"【分析过程】\n{reasoning}\n\n【回答】\n{content}";
    }

    private static string? ParseErrorMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                if (error.TryGetProperty("message", out var msg))
                    return msg.GetString();
            }
        }
        catch { }
        return null;
    }
}

internal class ZhidaApiResponse
{
    [JsonPropertyName("choices")]
    public List<ZhidaChoice>? Choices { get; set; }
}

internal class ZhidaChoice
{
    [JsonPropertyName("message")]
    public ZhidaMessage? Message { get; set; }
}

internal class ZhidaMessage
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("reasoning_content")]
    public string? ReasoningContent { get; set; }
}