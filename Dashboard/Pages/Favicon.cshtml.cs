using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;

namespace Dashboard.Pages;

/// <summary>
/// Proxy endpoint that fetches and returns the favicon for a given URL.
/// </summary>
public partial class FaviconModel(IHttpClientFactory httpClientFactory, ILogger<FaviconModel> logger) : PageModel
{
    private readonly HttpClient HttpClient = httpClientFactory.CreateClient();
    private readonly ILogger<FaviconModel> Logger = logger;

    [GeneratedRegex(@"<link[^>]*rel=[""'][^""']*icon[^""']*[""'][^>]*href=[""']([^""']+)[""']", RegexOptions.IgnoreCase)]
    private static partial Regex IconRelFirst();

    [GeneratedRegex(@"<link[^>]*href=[""']([^""']+)[""'][^>]*rel=[""'][^""']*icon[^""']*[""']", RegexOptions.IgnoreCase)]
    private static partial Regex IconHrefFirst();

    /// <summary>
    /// Fetches the favicon for the given URL, checking HTML meta tags then falling back to /favicon.ico.
    /// </summary>
    /// <param name="url">The service URL to fetch the favicon for.</param>
    public async Task<IActionResult> OnGetAsync(string url)
    {
        if (string.IsNullOrEmpty(url)) return NotFound();

        try
        {
            Uri baseUri = new(url);
            string origin = baseUri.GetLeftPart(UriPartial.Authority);

            HttpResponseMessage htmlResponse = await HttpClient.GetAsync(url);
            if (htmlResponse.IsSuccessStatusCode)
            {
                string html = await htmlResponse.Content.ReadAsStringAsync();
                Match match = IconRelFirst().Match(html);
                if (!match.Success) match = IconHrefFirst().Match(html);

                if (match.Success)
                {
                    string faviconPath = match.Groups[1].Value;
                    string faviconUrl = faviconPath.StartsWith("http") ? faviconPath : origin + "/" + faviconPath.TrimStart('/');
                    HttpResponseMessage faviconResponse = await HttpClient.GetAsync(faviconUrl);
                    if (faviconResponse.IsSuccessStatusCode)
                    {
                        byte[] bytes = await faviconResponse.Content.ReadAsByteArrayAsync();
                        string contentType = faviconResponse.Content.Headers.ContentType?.MediaType ?? "image/x-icon";
                        return File(bytes, contentType);
                    }
                }
            }

            HttpResponseMessage icoResponse = await HttpClient.GetAsync($"{origin}/favicon.ico");
            if (icoResponse.IsSuccessStatusCode)
            {
                byte[] bytes = await icoResponse.Content.ReadAsByteArrayAsync();
                return File(bytes, "image/x-icon");
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning("Failed to fetch favicon for {url}: {message}", url, ex.Message);
        }

        return NotFound();
    }
}