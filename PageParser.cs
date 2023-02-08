namespace CdiakDownloader;

public class PageParser
{
    private const string BaseUrl = "https://cdiak.archives.gov.ua/spysok_fondiv/";
    //https://cdiak.archives.gov.ua/spysok_fondiv/1782/0001/0065/img/1782_0001_0065_0007.jpg
    
    public static async Task<DownloadInfo?> ParserPage(string fond, string opis, string delo)
    {
        var client = new HttpClient();
        var uri = BuildUri(fond, opis, delo);

        try
        {
            var body = await client.GetStringAsync(uri);

            var data = Parse(uri, body);

            return data;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static DownloadInfo Parse(Uri uri, string body)
    {
        var downloadInfo = new DownloadInfo();

        const string token = "data-src=\"";

        var position = 0;
        while (position >= 0)
        {
            // <li class="col-xs-6 col-sm-4 col-md-3" data-sub-html="Фонд 1782 Опис 1 Справа 65" data-src="img/1782_0001_0065_0069.jpg">
            position = body.IndexOf(token, position + token.Length, StringComparison.OrdinalIgnoreCase);
            if (position == -1)
            {
                break;
            }

            var start = position + token.Length;
            var end = body.IndexOf("\"", start, StringComparison.OrdinalIgnoreCase);

            var str = body[start..end];

            var parts = str.Split('/');

            var link = uri + str;

            downloadInfo.Links.Add(new LinkInfo(link, parts.Last()));
        }

        return downloadInfo;
    }
    
    private static Uri BuildUri(string fond, string opis, string delo)
    {
        var f = fond.PadLeft(4, '0');
        var o = opis.PadLeft(4, '0');
        var d = delo.PadLeft(4, '0');

        return new Uri(BaseUrl + $"{f}/{o}/{d}/");
    }
}

public class DownloadInfo
{
    public List<LinkInfo> Links { get; } = new();
}

public record LinkInfo(string Url, string FileName);