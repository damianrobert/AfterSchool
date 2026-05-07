using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AfterSchool.Models;

namespace AfterSchool.Services;

public static class MiloService
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private const string GeminiModel = "gemini-2.0-flash";

    private const string SystemPromptTemplate = @"You are Milo, a friendly and professional AI assistant embedded in an afterschool management system. You help both students and administrative staff.

Current user: {0} (Role: {1})
Today's date: {2}

School context:
{3}

Guidelines:
- Be concise, warm, and professional.
- For questions about the school (courses, schedule, students, grades), use the school context provided.
- For general knowledge questions, answer from your training data.
- Use the web_search tool only when you need current, real-world information not covered by training data.
- Respond in the same language the user writes in (English or Romanian).";

    public static async Task<string> SendAsync(
        List<ChatMessage> history,
        string userMessage,
        string userName,
        string userRole,
        string schoolContext)
    {
        var geminiKey = EnvConfig.Get("GEMINI_API_KEY");
        if (string.IsNullOrEmpty(geminiKey))
            return "Milo is not configured. Please set the GEMINI_API_KEY in .env.local.";

        var systemPrompt = string.Format(SystemPromptTemplate,
            userName, userRole, DateTime.Today.ToString("yyyy-MM-dd"), schoolContext);

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{GeminiModel}:generateContent?key={geminiKey}";

        // Build contents array from history + new user message
        var contents = new JsonArray();
        foreach (var msg in history)
        {
            contents.Add(new JsonObject
            {
                ["role"] = msg.Role,
                ["parts"] = new JsonArray { new JsonObject { ["text"] = msg.Content } }
            });
        }
        contents.Add(new JsonObject
        {
            ["role"] = "user",
            ["parts"] = new JsonArray { new JsonObject { ["text"] = userMessage } }
        });

        var tools = new JsonArray
        {
            new JsonObject
            {
                ["functionDeclarations"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["name"] = "web_search",
                        ["description"] = "Search the web for current information when needed.",
                        ["parameters"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["properties"] = new JsonObject
                            {
                                ["query"] = new JsonObject
                                {
                                    ["type"] = "string",
                                    ["description"] = "The search query."
                                }
                            },
                            ["required"] = new JsonArray { "query" }
                        }
                    }
                }
            }
        };

        var body = new JsonObject
        {
            ["systemInstruction"] = new JsonObject
            {
                ["parts"] = new JsonArray { new JsonObject { ["text"] = systemPrompt } }
            },
            ["contents"] = contents,
            ["tools"] = tools,
            ["generationConfig"] = new JsonObject
            {
                ["temperature"] = 0.7,
                ["maxOutputTokens"] = 1024
            }
        };

        var response = await PostGeminiAsync(url, body);
        if (response == null) return "Milo could not connect. Please check your internet connection.";

        // Surface API-level errors
        var apiError = response["error"]?["message"]?.GetValue<string>();
        if (!string.IsNullOrEmpty(apiError)) return $"Gemini API error: {apiError}";

        // Check for function call
        var candidate = response["candidates"]?[0];
        var finishReason = candidate?["finishReason"]?.GetValue<string>();
        var parts = candidate?["content"]?["parts"];
        if (parts == null)
        {
            if (finishReason == "SAFETY") return "Milo couldn't respond to that message due to safety filters.";
            return "Milo returned an empty response.";
        }

        foreach (var part in parts.AsArray())
        {
            var funcCall = part?["functionCall"];
            if (funcCall != null)
            {
                var query = funcCall["args"]?["query"]?.GetValue<string>() ?? "";
                var searchResult = await BraveSearchAsync(query);

                // Add model's function call turn
                contents.Add(new JsonObject
                {
                    ["role"] = "model",
                    ["parts"] = new JsonArray { new JsonObject { ["functionCall"] = funcCall!.DeepClone() } }
                });

                // Add function result
                contents.Add(new JsonObject
                {
                    ["role"] = "user",
                    ["parts"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["functionResponse"] = new JsonObject
                            {
                                ["name"] = "web_search",
                                ["response"] = new JsonObject { ["result"] = searchResult }
                            }
                        }
                    }
                });

                // Second call with search results
                body["contents"] = contents;
                body.Remove("tools"); // no more tool calls needed
                response = await PostGeminiAsync(url, body);
                if (response == null) return "Milo could not process the search results.";
                var apiError2 = response["error"]?["message"]?.GetValue<string>();
                if (!string.IsNullOrEmpty(apiError2)) return $"Gemini API error: {apiError2}";
                candidate = response["candidates"]?[0];
                parts = candidate?["content"]?["parts"];
                if (parts == null) return "Milo returned an empty response after search.";
                break;
            }
        }

        // Extract text
        var sb = new StringBuilder();
        foreach (var part in parts.AsArray())
        {
            var text = part?["text"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(text)) sb.Append(text);
        }

        var result = sb.ToString().Trim();
        return string.IsNullOrEmpty(result) ? "Milo could not generate a response." : result;
    }

    private static async Task<JsonNode?> PostGeminiAsync(string url, JsonObject body)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
            };
            var resp = await _http.SendAsync(req);
            var json = await resp.Content.ReadAsStringAsync();
            return JsonNode.Parse(json);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string> BraveSearchAsync(string query)
    {
        var braveKey = EnvConfig.Get("BRAVE_SEARCH_API_KEY");
        if (string.IsNullOrEmpty(braveKey)) return "Web search not configured.";

        try
        {
            var encoded = Uri.EscapeDataString(query);
            var req = new HttpRequestMessage(HttpMethod.Get,
                $"https://api.search.brave.com/res/v1/web/search?q={encoded}&count=5");
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            req.Headers.Add("X-Subscription-Token", braveKey);

            var resp = await _http.SendAsync(req);
            var json = await resp.Content.ReadAsStringAsync();
            var node = JsonNode.Parse(json);

            var results = node?["web"]?["results"]?.AsArray();
            if (results == null || results.Count == 0) return "No results found.";

            var sb = new StringBuilder();
            foreach (var r in results.Take(4))
            {
                var title = r?["title"]?.GetValue<string>() ?? "";
                var snippet = r?["description"]?.GetValue<string>() ?? "";
                sb.AppendLine($"- {title}: {snippet}");
            }
            return sb.ToString();
        }
        catch
        {
            return "Web search failed.";
        }
    }

    public static string BuildSchoolContext()
    {
        var sb = new StringBuilder();
        try
        {
            var courses = Data.CourseRepository.GetAll().ToList();
            sb.AppendLine($"Courses ({courses.Count}):");
            foreach (var c in courses)
                sb.AppendLine($"  - {c.Name} (Teacher: {(string.IsNullOrWhiteSpace(c.Teacher) ? "unassigned" : c.Teacher)}, Capacity: {c.Capacity})");

            var today = DateTime.Today.DayOfWeek.ToString();
            var todaySlots = Data.ScheduleRepository.GetAll()
                .Where(s => s.DayOfWeek.Equals(today, StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.StartTime)
                .ToList();

            sb.AppendLine($"\nToday's schedule ({today}, {todaySlots.Count} slots):");
            foreach (var s in todaySlots)
                sb.AppendLine($"  - {s.CourseName}: {s.StartTime}–{s.EndTime} in {s.Room}");
        }
        catch { sb.AppendLine("(School data unavailable)"); }
        return sb.ToString();
    }
}
