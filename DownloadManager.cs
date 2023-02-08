using Polly;
using CdiakDownloader;
using Spectre.Console;

namespace ElarDownloader;

public class DownloadManager
{
    public static async Task Download(string folder, DownloadInfo downloadInfo)
    {
        Directory.CreateDirectory(folder);
        
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5),
        };

        await AnsiConsole.Progress()
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(), // Task description
                new ProgressBarColumn(), // Progress bar
                new PercentageColumn(), // Percentage
                new RemainingTimeColumn(), // Remaining time
                new SpinnerColumn(), // Spinner
            })
            .StartAsync(async ctx =>
            {
                var progressBar = ctx.AddTask("[green]Прогресс[/]");

                var i = 0;
                foreach (var (url, fileName) in downloadInfo.Links)
                {
                    var path = Path.Combine(folder, fileName);
                    i++;
                    var progress = (double) 100 * i / downloadInfo.Links.Count;

                    if (File.Exists(path))
                    {
                        AnsiConsole.MarkupLine($"[grey]Страница {fileName} уже скачеена. Пропускаем[/]");

                        progressBar.Value = progress;
                        continue;
                    }

                    var retryPeriods = new[]
                    {
                        TimeSpan.FromSeconds(10),
                        TimeSpan.FromSeconds(30),
                        TimeSpan.FromSeconds(60),
                        TimeSpan.FromSeconds(60),
                        TimeSpan.FromSeconds(60),
                        TimeSpan.FromSeconds(60)
                    };

                    var response = await Policy
                        .HandleResult<HttpResponseMessage>(message => !message.IsSuccessStatusCode)
                        .Or<TaskCanceledException>()
                        .Or<HttpRequestException>()
                        .WaitAndRetryAsync(retryPeriods,
                            (result, timeSpan, retryCount, _) =>
                            {
                                if (result?.Result == null)
                                {
                                    AnsiConsole.MarkupLine(
                                        $"[red]Сетевая проблема. Попытка {retryCount}/{retryPeriods.Length}; пауза {timeSpan} перед следующей попыткой[/]");
                                }
                                else
                                {
                                    AnsiConsole.MarkupLine(result.Exception is TaskCanceledException
                                        ? $"[red]Сервер не отвечает. Возможно перегружен. Попытка {retryCount}/{retryPeriods.Length}; пауза {timeSpan} перед следующей попыткой[/]"
                                        : $"[red]Запрос завершился ошибкой! code={result.Result.StatusCode}; reasonPhrase={result.Result.ReasonPhrase}; Попытка {retryCount}/{retryPeriods.Length}; пауза {timeSpan} перед следующей попыткой[/]");
                                }
                            })
                        .ExecuteAsync(async () => await httpClient.GetAsync(url));

                    if (response.IsSuccessStatusCode)
                    {
                        var stream = await response.Content.ReadAsStreamAsync();
                        await using (var fileStream = File.Create(path))
                        {
                            await stream.CopyToAsync(fileStream);

                            AnsiConsole.WriteLine($"{fileName} скачен");
                        }
                    }
                    else
                    {
                        progressBar.StopTask();
                        AnsiConsole.MarkupLine(
                            $"[red]Возникла ошибка code={response.StatusCode}; reasonPhrase={response.ReasonPhrase}[/]");
                        break;
                    }

                    progressBar.Value = progress;
                }
            });
    }
}