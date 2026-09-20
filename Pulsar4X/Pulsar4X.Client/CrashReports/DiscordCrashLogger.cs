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
    private struct DiscordField
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public bool? Inline { get; set; }
    }

    // Define the structure for Discord webhook embeds
    private struct DiscordEmbed
    {
        public string Title { get; set; }
        public int Color { get; set; }
        public DiscordField[] Fields { get; set; }
        public DiscordFooter Footer { get; set; }
    }

    private struct DiscordFooter
    {
        public string Text { get; set; }
    }

    private struct DiscordWebhookPayload
    {
        public DiscordEmbed[] Embeds { get; set; }
    }

    public DiscordCrashLogger(string webhookUrl)
    {
        _webhookUrl = webhookUrl;
        _httpClient = new HttpClient();
    }

    public async Task LogCrashAsync(Exception exception, string? userInfo = null)
    {
        try
        {
            var fields = new DiscordField[]
            {
                new DiscordField { Name = "Exception Type", Value = exception.GetType().FullName ?? "Unknown", Inline = true },
                new DiscordField { Name = "Timestamp", Value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"), Inline = true },
                new DiscordField { Name = "Message", Value = exception.Message },
                new DiscordField { Name = "Stack Trace", Value = $"```\n{exception.StackTrace?.Substring(0, Math.Min(1000, exception.StackTrace?.Length ?? 0))}\n```" },
                new DiscordField { Name = "User Info", Value = string.IsNullOrEmpty(userInfo) ? "No user info provided" : userInfo }
            };

            var payload = new DiscordWebhookPayload
            {
                Embeds = new DiscordEmbed[]
                {
                    new DiscordEmbed
                    {
                        Title = "❌ Application Crash Report",
                        Color = 15158332, // Red color
                        Fields = fields,
                        Footer = new DiscordFooter
                        {
                            Text = "Application Crash Logger"
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