using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Logging;

namespace SayTheSpire2.Updates;

/// <summary>
/// Queries the GitHub releases API at startup to check whether a newer
/// version is available. The HTTP request runs on a thread-pool thread so
/// the Godot main thread is never blocked. When the request completes and
/// a newer version exists, <see cref="LatestRemoteVersion"/> is set; the
/// startup announcement reads it on the main thread and queues a follow-up
/// "update available" message.
/// </summary>
public static class UpdateChecker
{
    private const string ApiUrl =
        "https://api.github.com/repos/bradjrenshaw/say-the-spire2/releases/latest";
    private const string UserAgent = "SayTheSpire2-mod";

    /// <summary>
    /// Set to the remote tag (with leading v / V stripped) when a newer
    /// version is found. Single-reference write from the request continuation;
    /// reads from the main thread are atomic without locks.
    /// </summary>
    public static string? LatestRemoteVersion { get; private set; }

    public static void Run()
    {
        _ = RunAsync();
    }

    private static async Task RunAsync()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            http.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            http.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");

            var json = await http.GetStringAsync(ApiUrl);
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("tag_name", out var tagElement))
            {
                Log.Info("[AccessibilityMod] Update check: response missing tag_name.");
                return;
            }

            var tag = tagElement.GetString();
            if (string.IsNullOrWhiteSpace(tag))
            {
                Log.Info("[AccessibilityMod] Update check: empty tag_name.");
                return;
            }

            var remote = NormalizeVersion(tag);
            var local = ModEntry.Version;

            if (IsNewer(remote, local))
            {
                LatestRemoteVersion = remote;
                Log.Info($"[AccessibilityMod] Update available: {remote} (current: {local}).");
            }
            else
            {
                Log.Info($"[AccessibilityMod] Up to date (latest: {remote}, current: {local}).");
            }
        }
        catch (Exception ex)
        {
            // Network errors, JSON parse failures, etc. — log at Info, never fail loud.
            Log.Info($"[AccessibilityMod] Update check failed: {ex.Message}");
        }
    }

    private static string NormalizeVersion(string tag)
    {
        var t = tag.Trim();
        if (t.Length > 0 && (t[0] == 'v' || t[0] == 'V'))
            t = t.Substring(1);
        return t;
    }

    /// <summary>
    /// Strict greater-than on dot-separated integer components. Missing
    /// components on either side are treated as zero, so "1.0" &gt; "1" is
    /// false (equal) and "1.0.1" &gt; "1.0" is true. Non-numeric segments
    /// (e.g. pre-release suffixes) parse as zero, which keeps comparisons
    /// safe rather than throwing.
    /// </summary>
    private static bool IsNewer(string remote, string local)
    {
        var r = ParseVersion(remote);
        var l = ParseVersion(local);
        int len = Math.Max(r.Length, l.Length);
        for (int i = 0; i < len; i++)
        {
            int rPart = i < r.Length ? r[i] : 0;
            int lPart = i < l.Length ? l[i] : 0;
            if (rPart > lPart) return true;
            if (rPart < lPart) return false;
        }
        return false;
    }

    private static int[] ParseVersion(string v)
    {
        var parts = v.Split('.');
        var result = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], out result[i]))
                result[i] = 0;
        }
        return result;
    }
}
