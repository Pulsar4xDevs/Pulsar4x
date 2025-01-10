using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Pulsar4X.Client.CrashReports;

public class DiscordCrashLogger
{
    private readonly string _webhookUrl;
    private readonly HttpClient _httpClient;

    // Define the structure for Discord webhook fields
    private class DiscordField
    {
        public string name { get; set; }
        public string value { get; set; }
        public bool? inline { get; set; }
    }

    // Define the structure for Discord webhook embeds
    private class DiscordEmbed
    {
        public string title { get; set; }
        public int color { get; set; }
        public DiscordField[] fields { get; set; }
        public DiscordFooter footer { get; set; }
    }

    private class DiscordFooter
    {
        public string text { get; set; }
    }

    private class DiscordWebhookPayload
    {
        public DiscordEmbed[] embeds { get; set; }
    }

    public DiscordCrashLogger(string webhookUrl)
    {
        _webhookUrl = webhookUrl;
        _httpClient = new HttpClient();
    }

    public async Task LogCrashAsync(Exception exception, string userInfo = null)
    {
        try
        {
            var fields = new DiscordField[]
            {
                new DiscordField { name = "Exception Type", value = exception.GetType().FullName, inline = true },
                new DiscordField { name = "Timestamp", value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"), inline = true },
                new DiscordField { name = "Message", value = exception.Message },
                new DiscordField { name = "Stack Trace", value = $"```\n{exception.StackTrace?.Substring(0, Math.Min(1000, exception.StackTrace?.Length ?? 0))}\n```" },
                new DiscordField { name = "User Info", value = string.IsNullOrEmpty(userInfo) ? "No user info provided" : userInfo }
            };

            var payload = new DiscordWebhookPayload
            {
                embeds = new DiscordEmbed[]
                {
                    new DiscordEmbed
                    {
                        title = "❌ Application Crash Report",
                        color = 15158332, // Red color
                        fields = fields,
                        footer = new DiscordFooter
                        {
                            text = "Application Crash Logger"
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            await _httpClient.PostAsync(_webhookUrl, content);
        }
        catch (Exception ex)
        {
            // Handle logging failure silently or implement fallback logging
            Console.WriteLine($"Failed to log crash to Discord: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}