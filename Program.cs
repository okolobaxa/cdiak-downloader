using System.Reflection;
using ElarDownloader;
using CdiakDownloader;
using Spectre.Console;

var version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
    .InformationalVersion;
AnsiConsole.Write(new FigletText($"CDIAK Downloader v.{version}").Centered().Color(Color.Green));
ConsoleHelper.GetAnyKey();

var fond = AnsiConsole.Ask<short>("Номер фонда?");
var opis = AnsiConsole.Ask<short>("Номер описи?");
var delo = AnsiConsole.Ask<short>("Номер дела?");

var downloadInfo = await PageParser.ParserPage(fond, opis, delo);
if (downloadInfo == null)
{
    AnsiConsole.MarkupLine($"[red]Дело не найдено. Возможно ещё не оцифровано. Проверьте реквизиты и сравните с сайтом архива[/]");
    ConsoleHelper.GetAnyKey();
    
    return;
}

if (downloadInfo.Links.Any())
{
    var folderName = $"{fond}_{opis}_{delo}";
    await DownloadManager.Download(folderName, downloadInfo);
}

ConsoleHelper.GetAnyKey();

internal static class ConsoleHelper
{
    public static void GetAnyKey()
    {
        AnsiConsole.MarkupLine("Нажмите любую клавишу чтобы продолжить...\n");
        Console.Read();
    }
}