using System.Threading;
using System.Threading.Tasks;

namespace WintryVR.Core
{
    public interface ISearchService
    {
        bool IsAvailable { get; }
        string ProviderName { get; }
        Task<SearchResult> SearchAsync(string query, string languageCode, CancellationToken ct);
    }
}
