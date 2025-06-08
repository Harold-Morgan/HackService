using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace HackService.Services;

public class DeepSeekClient : IDisposable
{
    public static string ApiKey { get; set; }
    
    public HttpClient Client { get; set; }
    
    public DeepSeekClient()
    {
        Client = new HttpClient();
        
        Client.BaseAddress = new Uri("https://api.deepseek.com");

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer",ApiKey);
    }
    
    public async Task<string> ChatAsync(string message)
    {
        var request = new
        {
            model = "deepseek-chat",
            messages = new[]{
                new
                {
                    role = "system",
                    content = message
                }
            },
            stream = false
        };
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        var response = await Client.PostAsync("/chat/completions", content);
        if (!response.IsSuccessStatusCode)
        {
            var res = await response.Content.ReadAsStringAsync();
            var errorMsg = response.StatusCode.ToString() + res;
            return String.Empty;
        }

        var resContent = await response.Content.ReadAsStringAsync();
        
        var json = JsonSerializer.Deserialize<ChatGptMessage>(resContent);

        var resString = json.choices[0].message.content;
        
        return resString;
    }

    public void Dispose()
    {
        Client.Dispose();
    }
}

// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
public class Choice
{
    public int index { get; set; }
    public Message message { get; set; }
    public object logprobs { get; set; }
    public string finish_reason { get; set; }
}

public class Message
{
    public string role { get; set; }
    public string content { get; set; }
}

public class PromptTokensDetails
{
    public int cached_tokens { get; set; }
}

public class ChatGptMessage
{
    public string id { get; set; }
    public string @object { get; set; }
    public int created { get; set; }
    public string model { get; set; }
    public List<Choice> choices { get; set; }
    public Usage usage { get; set; }
    public string system_fingerprint { get; set; }
}

public class Usage
{
    public int prompt_tokens { get; set; }
    public int completion_tokens { get; set; }
    public int total_tokens { get; set; }
    public PromptTokensDetails prompt_tokens_details { get; set; }
    public int prompt_cache_hit_tokens { get; set; }
    public int prompt_cache_miss_tokens { get; set; }
}

