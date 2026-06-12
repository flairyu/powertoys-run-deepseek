using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PowertoysRun.DeepSeek.Services;

public class DeepSeekService
{
    private static readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri("https://api.deepseek.com"),
        Timeout = TimeSpan.FromSeconds(30)
    };

    private const string DefaultModel = "deepseek-chat";
    private const int ShortResponseMaxLength = 200;

    private string _apiKey = string.Empty;
    private string _model = DefaultModel;

    public void Configure(string apiKey, string? model = null)
    {
        _apiKey = apiKey;
        if (!string.IsNullOrWhiteSpace(model))
            _model = model;
    }

    public async Task<DeepSeekResponse> AskAsync(string question, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            return DeepSeekResponse.Error("请先配置 API Key");

        var requestBody = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = "你是一个简洁的助手。请尽量简短回答用户的问题。如果答案较短（200字以内），给出直接答案。如果问题复杂需要长回答，用清晰的结构组织内容。" },
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
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = ParseErrorMessage(responseJson) ?? $"API 请求失败 ({(int)response.StatusCode})";
                return DeepSeekResponse.Error(errorMsg);
            }

            var apiResponse = JsonSerializer.Deserialize<DeepSeekApiResponse>(responseJson);
            var answer = apiResponse?.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? string.Empty;

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

public class DeepSeekResponse
{
    public bool Success { get; init; }
    public string Answer { get; init; } = string.Empty;
    public bool IsShort { get; init; }
    public string Question { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;

    public static DeepSeekResponse Error(string message) => new()
    {
        Success = false,
        ErrorMessage = message,
        Answer = message
    };
}

internal class DeepSeekApiResponse
{
    [JsonPropertyName("choices")]
    public List<DeepSeekChoice>? Choices { get; set; }
}

internal class DeepSeekChoice
{
    [JsonPropertyName("message")]
    public DeepSeekMessage? Message { get; set; }
}

internal class DeepSeekMessage
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}
