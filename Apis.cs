using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Exad.Classes;

namespace Exad.Apis.Webuntis
{
    public class Session(string schoolUrl, string schoolName, string username, string password)
    {
        private string _schoolUrl { get; } = (
                                                schoolUrl.StartsWith("https://")
                                                    ? schoolUrl
                                                    : $"https://{schoolUrl.TrimStart("http://".ToCharArray())}"
                                            ).TrimEnd('/');
        private string _schoolName { get; } = schoolName.ToLower().Replace('-', ' ');
        private string _username { get; } = username;
        private string _password { get; } = password;
        private string _cookie => $"JSESSIONID={_jSessionId}; schoolname={_schoolId}";

        private string? _jSessionId { get; set; }
        private string? _schoolId { get; set; }

        private bool _loggedIn { get; set; }

        public Session(Config.Config config)
            : this(config.SchoolUrl, config.SchoolName, config.Username, config.Password) { }


        private void EnsureLoggedIn()
        {
            if (!_loggedIn) { throw new InvalidOperationException("You need to login first!"); }
        }

        public async Task TryLogin()
        {
            if (!_loggedIn)
            {
                using var client = new HttpClient();

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new($"{_schoolUrl}/WebUntis/j_spring_security_check"),
                    Headers = { { "Accept", "application/json" } },
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                        {
                            { "school", _schoolName },
                            { "j_username", _username },
                            { "j_password", _password },
                            { "token", "" }
                        })
                };

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var responseCookies = response.Headers.GetValues("Set-Cookie");

                    _jSessionId = responseCookies.First(cookie => cookie.StartsWith("JSESSIONID")).Split(';')[0].Split('=')[1];
                    _schoolId = responseCookies.First(cookie => cookie.StartsWith("schoolname")).Split(';')[0].Split('=')[1].Trim('"');

                    if (_jSessionId is null || _schoolId is null) { throw new Exception("Login failed"); }
                    else { _loggedIn = true; }
                }
                else { throw new Exception("Login failed"); }
            }
        }

        public async Task<IList<Exam>> TryGetExams(DateOnly start, DateOnly? end = null)
        {
            EnsureLoggedIn();

            using var client = new HttpClient();

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new($"{_schoolUrl}/WebUntis/api/exams?startDate={start:yyyyMMdd}&endDate={end ?? new(DateTime.Now.Year + (DateTime.Now.Month <= 7 ? 0 : 1), 7, 8):yyyyMMdd}"),
                Headers =
                    {
                        { "Accept", "application/json" },
                        { "Cookie", _cookie }
                    }
            };

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<IList<Exam>>(
                        content[content.IndexOf('[')..(content.LastIndexOf(']') + 1)],
                        new JsonSerializerOptions { Converters = { new DateOnlyConverter(), new TimeOnlyConverter() } }
                    ) ?? throw new Exception("Failed to get exams");
            }
            else { throw new Exception("Failed to get exams"); }
        }

        public async Task<IList<Exam>> TryGetExams() => await TryGetExams(new DateOnly(DateTime.Now.Year - (DateTime.Now.Month <= 7 ? 1 : 0), 9, 1));
    }

    public class DateOnlyConverter : JsonConverter<DateOnly>
    {
        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var date = reader.GetInt32();
            return new DateOnly(date / 10_000, date / 100 % 100, date % 100);
        }

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    public class TimeOnlyConverter : JsonConverter<TimeOnly>
    {
        public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var time = reader.GetInt32();
            return new TimeOnly(time / 100, time % 100);
        }

        public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}

namespace Exad.Apis.Google
{
    public static class Calendar
    {
        public static async Task AddExams(IEnumerable<Exam> exams, Exad.Config.Config config)
        {
            UserCredential credential;
            using var stream = new FileStream(config.ClientSecretsFile, FileMode.Open, FileAccess.Read);

            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                [CalendarService.Scope.CalendarEvents],
                "user",
                CancellationToken.None
            );

            var service = new CalendarService(new BaseClientService.Initializer() { HttpClientInitializer = credential });

            var existingEventsRequest = service.Events.List(config.CalendarId);
            existingEventsRequest.SingleEvents = true;
            existingEventsRequest.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
            var existingEvents = existingEventsRequest.Execute().Items;

            TimeZoneInfo.TryConvertWindowsIdToIanaId(TimeZoneInfo.Local.Id, out var timezone);

            foreach (var exam in exams)
            {
                var @event = new Event
                {
                    Summary = exam.Subject,
                    Description = exam.Description,
                    Start = new()
                    {
                        DateTimeDateTimeOffset = exam.Start,
                        TimeZone = timezone
                    },
                    End = new()
                    {
                        DateTimeDateTimeOffset = exam.End,
                        TimeZone = timezone
                    },
                    Reminders = new() { UseDefault = true }
                };

                var existingEventsMatch = existingEvents.FirstOrDefault(existingEvent => existingEvent.Summary == exam.Subject && DateOnly.FromDateTime(existingEvent.Start.DateTimeDateTimeOffset!.Value.Date) == exam.Date);

                if (existingEventsMatch is not null)
                {
                    if (existingEventsMatch.Start.DateTimeDateTimeOffset != exam.Start || existingEventsMatch.End.DateTimeDateTimeOffset != exam.End)
                    {
                        var updateEventRequest = service.Events.Update(@event, config.CalendarId, existingEventsMatch.Id);
                        updateEventRequest.Execute();
                        Console.WriteLine($"updated event '{exam.Subject}' on {exam.Date}");
                    }
                }
                else
                {
                    var newEventRequest = service.Events.Insert(@event, config.CalendarId);
                    newEventRequest.Execute();
                    Console.WriteLine($"added event '{exam.Subject}' on {exam.Date}");
                }
            }
        }
    }
}
