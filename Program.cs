using System.Text;
using System.Globalization;
using Cocona;
using Exad.Config;

CoconaApp.Run(async (
        [Argument] string? option,
        [Option('o', Description = "path to the output file (.csv)")] string outputFile = "events.csv"
    ) =>
{
    option = option?.ToLower();
    var config = Config.TryLoad();

    if (config is null) { Console.WriteLine($"config file created at {Config.Path}"); }
    else if (option is null) { Console.WriteLine("invalid option"); }
    else
    {
        var session = new Exad.Apis.Webuntis.Session(config);
        await session.TryLogin();
        var exams = await session.TryGetExams(DateOnly.FromDateTime(DateTime.Now));

        foreach (var exam in exams)
        {
            exam.TranslateSubject();
        }

        switch (option)
        {
            case "push":
                {
                    await Exad.Apis.Google.Calendar.AddExams(exams, config);
                    break;
                }

            case "save":
                {
                    var sb = new StringBuilder("Subject,Description,Start Date,Start Time,End Time\n");

                    foreach (var exam in exams)
                    {
                        sb.AppendLine($"{exam.Subject},{exam.Description},{exam.Date.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture)},{exam.StartTime:h:mm tt},{exam.EndTime:h:mm tt}");
                    }

                    File.WriteAllText(outputFile, sb.ToString());
                    Console.WriteLine($"saved {exams.Count} exams to {outputFile}");
                    break;
                }
        }
    }
});
