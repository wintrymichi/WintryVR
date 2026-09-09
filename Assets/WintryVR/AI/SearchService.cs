using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WintryVR.Core;
using WintryVR.Networking;

namespace WintryVR.AI
{
    /// <summary>
    /// Web search abstraction. Providers: Tavily-style JSON API, a generic JSON endpoint (backend proxy that
    /// returns {results:[{title,snippet,url,price}]}), and a mock for demo/offline.
    /// </summary>
    public class SearchService : ISearchService
    {
        private readonly string _mode;
        private readonly string _baseUrl;
        private readonly string _key;

        public SearchService(WintryConfig cfg)
        {
            _key = SecretStore.Get("SEARCH_API_KEY");
            _baseUrl = cfg.SearchBaseUrl;
            string choice = (cfg.SearchProvider ?? "auto").ToLowerInvariant();
            if (choice == "auto") choice = !string.IsNullOrEmpty(_baseUrl) ? "generic" : !string.IsNullOrEmpty(_key) ? "tavily" : "mock";
            _mode = choice;
        }

        public string ProviderName => _mode;
        public bool IsAvailable => _mode == "mock" || ConnectivityMonitor.IsOnline;

        public async Task<SearchResult> SearchAsync(string query, string languageCode, CancellationToken ct)
        {
            var result = new SearchResult { Query = query };
            if (string.IsNullOrWhiteSpace(query)) { result.UserMessage = Localization.Get("err.generic", languageCode); return result; }
            try
            {
                switch (_mode)
                {
                    case "tavily": await TavilyAsync(query, result, ct); break;
                    case "generic": await GenericAsync(query, result, ct); break;
                    default: await MockAsync(query, result, ct); break;
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                WintryLog.E("Search", "Search failed", ex);
                result.Success = false;
            }
            if (!result.Success && string.IsNullOrEmpty(result.UserMessage))
                result.UserMessage = ConnectivityMonitor.IsOnline ? Localization.Get("err.generic", languageCode) : Localization.Get("err.offline", languageCode);
            return result;
        }

        private async Task TavilyAsync(string query, SearchResult result, CancellationToken ct)
        {
            var body = new Dictionary<string, object> { ["api_key"] = _key, ["query"] = query, ["max_results"] = 6, ["include_answer"] = true, ["search_depth"] = "basic" };
            var http = await HttpClientService.PostJsonAsync("https://api.tavily.com/search", MiniJson.Serialize(body), null, 20, ct);
            if (!http.Success) return;
            var root = MiniJson.Deserialize(http.Body);
            result.Summary = MiniJson.GetString(root, "answer", "") ?? "";
            var items = MiniJson.GetArray(root, "results");
            if (items != null) foreach (var it in items) result.Items.Add(FromGeneric(it));
            result.Success = true;
        }

        private async Task GenericAsync(string query, SearchResult result, CancellationToken ct)
        {
            var headers = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(_key)) headers["Authorization"] = "Bearer " + _key;
            var body = new Dictionary<string, object> { ["query"] = query };
            var http = await HttpClientService.PostJsonAsync(_baseUrl, MiniJson.Serialize(body), headers, 20, ct);
            if (!http.Success) return;
            var root = MiniJson.Deserialize(http.Body);
            result.Summary = MiniJson.GetString(root, "answer", MiniJson.GetString(root, "summary", "")) ?? "";
            var items = MiniJson.GetArray(root, "results") ?? MiniJson.GetArray(root, "items");
            if (items != null) foreach (var it in items) result.Items.Add(FromGeneric(it));
            result.Success = true;
        }

        private static SearchResultItem FromGeneric(object it)
        {
            var item = new SearchResultItem
            {
                Title = MiniJson.GetString(it, "title", ""),
                Snippet = MiniJson.GetString(it, "content", MiniJson.GetString(it, "snippet", "")),
                Url = MiniJson.GetString(it, "url", ""),
                Price = MiniJson.GetString(it, "price", ""),
                Availability = MiniJson.GetString(it, "availability", "")
            };
            try { if (!string.IsNullOrEmpty(item.Url)) item.Source = new Uri(item.Url).Host; } catch { item.Source = item.Url; }
            return item;
        }

        private static async Task MockAsync(string query, SearchResult result, CancellationToken ct)
        {
            await Task.Delay(400, ct);
            result.Success = true;
            result.Summary = "Demo result for: " + query;
            result.Items.Add(new SearchResultItem { Title = "Canon EOS R6 Mark II - body", Snippet = "Full-frame mirrorless, 24.2 MP, 4K60.", Url = "https://example.com/r6ii", Source = "example.com", Price = "2.499 €", Availability = "In stock" });
            result.Items.Add(new SearchResultItem { Title = "Canon EOS R6 Mark II review", Snippet = "An excellent all-rounder with fast autofocus.", Url = "https://example.com/review", Source = "example.com" });
        }
    }
}
