using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AfterSchool.Models;

namespace AfterSchool.Services;

public static class MiloService
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private const string GeminiModel = "gemini-2.5-pro";

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
- For administrative tasks (adding schedule slots, exporting reports, etc.), use the available action tools to perform the operation directly, then report what was done.
- When exporting, always open the file automatically. If the user does not specify a format, default to Excel.
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

        var functionDeclarations = new JsonArray
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
        };
        foreach (var decl in MiloActions.GetFunctionDeclarations())
            functionDeclarations.Add(decl?.DeepClone());

        var tools = new JsonArray
        {
            new JsonObject { ["functionDeclarations"] = functionDeclarations }
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
            if (funcCall == null) continue;

            var funcName = funcCall["name"]?.GetValue<string>() ?? "";
            string funcResult;
            if (funcName == "web_search")
            {
                var query = funcCall["args"]?["query"]?.GetValue<string>() ?? "";
                funcResult = await BraveSearchAsync(query);
            }
            else
            {
                funcResult = await MiloActions.ExecuteAsync(funcName, funcCall["args"]);
            }

            // Add model's function call turn
            contents.Add(new JsonObject
            {
                ["role"] = "model",
                ["parts"] = new JsonArray { new JsonObject { ["functionCall"] = funcCall.DeepClone() } }
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
                            ["name"] = funcName,
                            ["response"] = new JsonObject { ["result"] = funcResult }
                        }
                    }
                }
            });

            // Second call with function result
            body["contents"] = contents;
            body.Remove("tools"); // no more tool calls needed
            response = await PostGeminiAsync(url, body);
            if (response == null) return "Milo could not process the action result.";
            var apiError2 = response["error"]?["message"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(apiError2)) return $"Gemini API error: {apiError2}";
            candidate = response["candidates"]?[0];
            parts = candidate?["content"]?["parts"];
            if (parts == null) return "Milo returned an empty response after action.";
            break;
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
            // ── Courses ───────────────────────────────────────────────────────
            var courses = Data.CourseRepository.GetAll().ToList();
            sb.AppendLine($"## Courses ({courses.Count} total)");
            foreach (var c in courses)
            {
                var enrolled = Data.CourseRepository.GetEnrolledCount(c.Id);
                var teacher  = string.IsNullOrWhiteSpace(c.Teacher) ? "unassigned" : c.Teacher;
                sb.AppendLine($"  - [{c.Id}] {c.Name} | Teacher: {teacher} | Enrolled: {enrolled}/{c.Capacity}" +
                              (string.IsNullOrWhiteSpace(c.Description) ? "" : $" | Desc: {c.Description}"));
            }

            // ── Full weekly schedule ──────────────────────────────────────────
            var allSlots = Data.ScheduleRepository.GetAll().ToList();
            var days = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
            sb.AppendLine($"\n## Weekly Schedule ({allSlots.Count} slots)");
            foreach (var day in days)
            {
                var daySlots = allSlots
                    .Where(s => s.DayOfWeek.Equals(day, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(s => s.StartTime)
                    .ToList();
                if (daySlots.Count == 0) continue;
                sb.AppendLine($"  {day}:");
                foreach (var s in daySlots)
                    sb.AppendLine($"    - {s.CourseName} (Teacher: {s.Teacher}): {s.StartTime}–{s.EndTime}, Room: {s.Room}");
            }

            // ── Students ──────────────────────────────────────────────────────
            var students = Data.StudentRepository.GetAll().ToList();
            var active   = students.Count(s => s.Status == "Active");
            sb.AppendLine($"\n## Students ({students.Count} total, {active} active)");
            foreach (var s in students)
            {
                var courses_list = string.IsNullOrWhiteSpace(s.CourseName) ? "none" : s.CourseName;
                sb.AppendLine($"  - [{s.Id}] {s.FirstName} {s.LastName} | Status: {s.Status} | Courses: {courses_list}" +
                              (string.IsNullOrWhiteSpace(s.Email) ? "" : $" | Email: {s.Email}"));
            }

            // ── Grades ────────────────────────────────────────────────────────
            sb.AppendLine("\n## Grades (by course)");
            foreach (var c in courses)
            {
                var graded = Data.GradeRepository.GetByCourse(c.Id)
                    .Where(g => !string.IsNullOrWhiteSpace(g.Score))
                    .ToList();
                if (graded.Count == 0) continue;
                sb.AppendLine($"  {c.Name} (scale: {(string.IsNullOrWhiteSpace(c.GradingScale) ? "N/A" : c.GradingScale)}):");
                foreach (var g in graded)
                {
                    var notes = string.IsNullOrWhiteSpace(g.Notes) ? "" : $" | Notes: {g.Notes}";
                    var date  = string.IsNullOrWhiteSpace(g.GradedDate) ? "" : $" | Date: {g.GradedDate}";
                    sb.AppendLine($"    - {g.StudentFirstName} {g.StudentLastName}: {g.Score}{notes}{date}");
                }
            }

            // ── Assignments ───────────────────────────────────────────────────
            sb.AppendLine("\n## Assignments (by course)");
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            foreach (var c in courses)
            {
                var assignments = Data.AssignmentRepository.GetByCourse(c.Id).ToList();
                if (assignments.Count == 0) continue;
                sb.AppendLine($"  {c.Name}:");
                foreach (var a in assignments)
                {
                    var overdue = !string.IsNullOrWhiteSpace(a.DueDate) && string.Compare(a.DueDate, today, StringComparison.Ordinal) < 0
                        ? " [OVERDUE]" : "";
                    var desc = string.IsNullOrWhiteSpace(a.Description) ? "" : $" | {a.Description}";
                    sb.AppendLine($"    - [{a.Id}] \"{a.Title}\" | Due: {a.DueDate}{overdue} | Submissions: {a.SubmissionCount}{desc}");
                }
            }
        }
        catch { sb.AppendLine("(School data unavailable)"); }
        return sb.ToString();
    }
}
