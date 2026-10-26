using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ChessOpeningTrainer;

public sealed record DatasetDownloadOption(string Name, Uri Url, long Bytes, long Games);
public interface IDatasetDownloadSource
{
    Task<IReadOnlyList<DatasetDownloadOption>> List(CancellationToken token);
}
public sealed class LichessDatasetSource : IDatasetDownloadSource
{
    private static readonly HttpClient client = new() { Timeout = TimeSpan.FromMinutes(2) };
    public async Task<IReadOnlyList<DatasetDownloadOption>> List(CancellationToken token)
    {
        string html = await client.GetStringAsync("https://database.lichess.org/", token);
        var results = new List<DatasetDownloadOption>();
        foreach (Match row in Regex.Matches(html, @"<tr\b[^>]*>(.*?)</tr>", RegexOptions.Singleline))
        {
            var link = Regex.Match(row.Value, "href=\"([^\"]*standard/lichess_db_standard_rated_(\\d{4}-\\d{2})\\.pgn\\.zst)\"");
            if (!link.Success)
                continue;
            var cells = Regex.Matches(row.Value, @"<td\b[^>]*>(.*?)</td>", RegexOptions.Singleline).Select(m => WebUtility.HtmlDecode(Regex.Replace(m.Groups[1].Value, "<[^>]+>", "")).Trim()).ToArray();
            if (cells.Length < 3)
                continue;
            var size = Regex.Match(cells[1], @"([\d.]+)\s*(KB|MB|GB|TB)");
            if (!size.Success || !long.TryParse(cells[2].Replace(",", "").Replace(" ", ""), out long games))
                continue;
            long bytes = (long)(double.Parse(size.Groups[1].Value, CultureInfo.InvariantCulture) * Math.Pow(1000, Array.IndexOf(new[] { "KB", "MB", "GB", "TB" }, size.Groups[2].Value) + 1));
            results.Add(new(link.Groups[2].Value, new Uri(new Uri("https://database.lichess.org/"), link.Groups[1].Value), bytes, games));
        }
        if (results.Count == 0)
            throw new InvalidDataException("Lichess download catalog could not be read. No downloads started.");
        return results.OrderByDescending(r => r.Name).ToArray();
    }
    public async Task<string> Checksum(DatasetDownloadOption option, CancellationToken token)
    {
        string list = await client.GetStringAsync("https://database.lichess.org/standard/sha256sums.txt", token);
        string filename = Path.GetFileName(option.Url.AbsolutePath);
        var match = Regex.Match(list, @"(?m)^([a-fA-F0-9]{64})\s+\*?(?:standard/)?" + Regex.Escape(filename) + @"\s*$");
        if (!match.Success)
            throw new InvalidDataException("No official checksum available for this file.");
        return match.Groups[1].Value;
    }
}

public sealed class DatasetDownloadJob
{
    public DatasetDownloadOption Option { get; set; } = null!;
    public string Sha256 { get; set; } = "";
    public string? ETag
    {
        get; set;
    }
}
/// <summary>Persist intent before opening HTTP. Reopen the mode to resume .part files.</summary>
public static class DatasetDownloader
{
    private static readonly HttpClient client = new() { Timeout = Timeout.InfiniteTimeSpan };
    public static string Journal(string root) => Path.Combine(root, "download.json");
    public static async Task<string> Download(string root, DatasetDownloadJob job, IProgress<string>? progress, CancellationToken token, HttpClient? transport = null, DatasetOperation? operation = null)
    {
        Directory.CreateDirectory(root);
        if (job.Option.Url.Scheme != "https" || job.Option.Url.Host != "database.lichess.org")
            throw new InvalidDataException("Only official Lichess HTTPS downloads are allowed.");
        string target = Path.Combine(root, Path.GetFileName(job.Option.Url.AbsolutePath)), partial = target + ".part";
        if (!Path.GetFileName(target).EndsWith(".pgn.zst", StringComparison.OrdinalIgnoreCase) ||
            Path.GetDirectoryName(Path.GetFullPath(target)) != Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar))
            throw new InvalidDataException("Invalid download filename.");
        async Task<bool> Matches(string file)
        {
            await using var input = File.OpenRead(file);
            return Convert.ToHexString(await SHA256.HashDataAsync(input, token)).Equals(job.Sha256, StringComparison.OrdinalIgnoreCase);
        }
        if (File.Exists(target))
        {
            if (!await Matches(target))
                throw new InvalidDataException("Existing download has a different checksum; move it aside before retrying.");
            // Recover a crash between publishing the verified file and clearing its journal.
            if (File.Exists(Journal(root)) && JsonSerializer.Deserialize<DatasetDownloadJob>(File.ReadAllText(Journal(root)))?.Option.Url == job.Option.Url)
                File.Delete(Journal(root));
            return target;
        }
        void Save()
        {
            File.WriteAllText(Journal(root) + ".tmp", JsonSerializer.Serialize(job));
            File.Move(Journal(root) + ".tmp", Journal(root), true);
        }
        Save();
        var timing = operation?.StartTiming();
        long offset = File.Exists(partial) ? new FileInfo(partial).Length : 0;
        using var request = new HttpRequestMessage(HttpMethod.Get, job.Option.Url);
        if (offset > 0)
        {
            request.Headers.Range = new RangeHeaderValue(offset, null);
            if (EntityTagHeaderValue.TryParse(job.ETag, out var tag))
                request.Headers.IfRange = new RangeConditionHeaderValue(tag);
        }
        using var headersTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        headersTimeout.CancelAfter(TimeSpan.FromMinutes(2));
        using var response = await (transport ?? client).SendAsync(request, HttpCompletionOption.ResponseHeadersRead, headersTimeout.Token);
        if (response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable && offset > 0 && await Matches(partial))
        {
            File.Move(partial, target);
            File.Delete(Journal(root));
            return target;
        }
        response.EnsureSuccessStatusCode();
        if (response.StatusCode == HttpStatusCode.PartialContent && response.Content.Headers.ContentRange?.From != offset)
            throw new InvalidDataException("Server returned an unexpected resume range. Partial download retained.");
        bool append = offset > 0 && response.StatusCode == HttpStatusCode.PartialContent && response.Content.Headers.ContentRange?.From == offset;
        if (!append)
            offset = 0;
        job.ETag = response.Headers.ETag?.ToString();
        Save();
        long total = offset + (response.Content.Headers.ContentLength ?? 0);
        await using (var output = new FileStream(partial, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read, 65536, true))
        await using (var input = await response.Content.ReadAsStreamAsync(token))
        {
            byte[] buffer = new byte[65536];
            long copied = offset, last = 0;
            while (true)
            {
                operation?.Wait(token);
                using var stalled = CancellationTokenSource.CreateLinkedTokenSource(token);
                stalled.CancelAfter(TimeSpan.FromMinutes(2));
                int count = await input.ReadAsync(buffer, stalled.Token);
                if (count == 0)
                    break;
                await output.WriteAsync(buffer.AsMemory(0, count), token);
                copied += count;
                if (Environment.TickCount64 - last > 400)
                {
                    progress?.Report($"{job.Option.Name}: {copied / 1e6:0.0} / {total / 1e6:0.0} MB" +
                        (timing is null ? "" : " · " + timing.Describe(copied - offset, total - offset)));
                    last = Environment.TickCount64;
                }
            }
        }
        await using var verify = File.OpenRead(partial);
        string actual = Convert.ToHexString(await SHA256.HashDataAsync(verify, token));
        verify.Close();
        if (!actual.Equals(job.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Download checksum mismatch. Partial file retained for inspection; remove it before retrying.");
        File.Move(partial, target, true);
        File.Delete(Journal(root));
        return target;
    }
}
