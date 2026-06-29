using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Dashboard.Services;

/// <summary>
/// Queries the GitHub Actions API for the latest workflow run status on a given repo, authenticated so private repos are accessible. Results are cached briefly to avoid hammering GitHub's API on every badge render.
/// </summary>
public class GitHubBadgeService(HttpClient httpClient, IConfiguration configuration, IMemoryCache memoryCache)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Fetches the latest run status for the given owner/repo and workflow file, returning a shields.io-compatible badge message and color. Cached for 60 seconds per repo.
    /// </summary>
    /// <param name="ownerRepo">The repository in "owner/repo" form.</param>
    /// <param name="workflowFile">The workflow filename, e.g. "deploy.yml".</param>
    /// <param name="branch">The branch to check.</param>
    public async Task<(string Message, string Color)> GetStatusAsync(string ownerRepo, string workflowFile, string branch)
    {
        string cacheKey = $"badge:{ownerRepo}:{workflowFile}:{branch}";

        if (memoryCache.TryGetValue(cacheKey, out (string Message, string Color) cached)) return cached;

        string token = configuration["GitHub:Token"] ?? string.Empty;
        string url = $"https://api.github.com/repos/{ownerRepo}/actions/workflows/{workflowFile}/runs?branch={branch}&per_page=1";

        using HttpRequestMessage request = new(HttpMethod.Get, url);
        request.Headers.Add("Accept", "application/vnd.github+json");
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        request.Headers.Add("User-Agent", "Dashboard");

        HttpResponseMessage response = await httpClient.SendAsync(request);

        (string Message, string Color) result;

        if (!response.IsSuccessStatusCode)
        {
            result = ("unknown", "9e9e9e");
        }
        else
        {
            string body = await response.Content.ReadAsStringAsync();
            using JsonDocument document = JsonDocument.Parse(body);

            JsonElement.ArrayEnumerator runs = document.RootElement.GetProperty("workflow_runs").EnumerateArray();

            if (!runs.MoveNext())
            {
                result = ("unknown", "9e9e9e");
            }
            else
            {
                JsonElement latestRun = runs.Current;
                string status = latestRun.GetProperty("status").GetString() ?? string.Empty;

                if (status != "completed")
                {
                    result = ("running", "dfb317");
                }
                else
                {
                    string conclusion = latestRun.GetProperty("conclusion").GetString() ?? string.Empty;
                    result = conclusion == "success" ? ("passing", "198754") : ("failing", "dc3545");
                }
            }
        }

        memoryCache.Set(cacheKey, result, CacheDuration);
        return result;
    }
}