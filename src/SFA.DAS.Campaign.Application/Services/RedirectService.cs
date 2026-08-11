using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SFA.DAS.Campaign.Domain.Api.Interfaces;
using SFA.DAS.Campaign.Domain.Content;
using SFA.DAS.Campaign.Infrastructure.Api.Requests;
using SFA.DAS.Campaign.Infrastructure.Api.Responses;

namespace SFA.DAS.Campaign.Application.Services
{
    public class RedirectService : IRedirectService
    {
        internal const string CacheKey = "cms-redirects";
        internal static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
        internal static readonly TimeSpan FailureCacheDuration = TimeSpan.FromSeconds(30);

        private readonly IApiClient _apiClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RedirectService> _logger;

        public RedirectService(IApiClient apiClient, IMemoryCache cache, ILogger<RedirectService> logger)
        {
            _apiClient = apiClient;
            _cache = cache;
            _logger = logger;
        }

        public async Task<CmsRedirect> GetRedirect(string path)
        {
            var normalisedPath = NormalisePath(path);

            if (normalisedPath == null)
            {
                return null;
            }

            var map = await GetRedirectMap().ConfigureAwait(false);

            if (map.Exact.TryGetValue(normalisedPath, out var exactMatch))
            {
                return exactMatch;
            }

            return map.Prefixes.FirstOrDefault(redirect => IsPrefixMatch(normalisedPath, redirect.FromPath));
        }

        /// <summary>
        /// Lower cased, no query string, no trailing slash, always leading slash. Returns null for the site root
        /// or anything that can't be matched, so the home page can never be redirected away by a stray CMS entry.
        /// </summary>
        internal static string NormalisePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            var normalisedPath = path.Trim();

            var queryIndex = normalisedPath.IndexOfAny(new[] { '?', '#' });
            if (queryIndex >= 0)
            {
                normalisedPath = normalisedPath.Substring(0, queryIndex);
            }

            if (!normalisedPath.StartsWith("/", StringComparison.Ordinal))
            {
                normalisedPath = "/" + normalisedPath;
            }

            normalisedPath = normalisedPath.TrimEnd('/').ToLowerInvariant();

            return normalisedPath.Length == 0 ? null : normalisedPath;
        }

        private static bool IsPrefixMatch(string normalisedPath, string prefix)
        {
            return normalisedPath.Equals(prefix, StringComparison.Ordinal)
                   || normalisedPath.StartsWith(prefix + "/", StringComparison.Ordinal);
        }

        private async Task<RedirectMap> GetRedirectMap()
        {
            return await _cache.GetOrCreateAsync(CacheKey, async entry =>
            {
                try
                {
                    var response = await _apiClient.Get<GetRedirectsResponse>(new GetRedirectsRequest()).ConfigureAwait(false);

                    entry.AbsoluteExpirationRelativeToNow = CacheDuration;

                    return BuildMap(response?.Redirects);
                }
                catch (Exception ex)
                {
                    // A redirect lookup happens while we are already serving a 404, so failing here must not turn
                    // that into a 500. Cache the empty map briefly so an outage doesn't hammer the API on every miss.
                    _logger.LogWarning(ex, "Unable to retrieve redirects from the content API, serving none for {Seconds} seconds", FailureCacheDuration.TotalSeconds);

                    entry.AbsoluteExpirationRelativeToNow = FailureCacheDuration;

                    return RedirectMap.Empty;
                }
            }).ConfigureAwait(false);
        }

        private RedirectMap BuildMap(IEnumerable<Redirect> redirects)
        {
            if (redirects == null)
            {
                return RedirectMap.Empty;
            }

            var exact = new Dictionary<string, CmsRedirect>(StringComparer.Ordinal);
            var prefixes = new List<CmsRedirect>();

            foreach (var redirect in redirects)
            {
                var fromPath = NormalisePath(redirect?.FromPath);
                var toPath = redirect?.ToPath?.Trim();

                if (fromPath == null || string.IsNullOrWhiteSpace(toPath))
                {
                    _logger.LogWarning("Ignoring CMS redirect with a missing from or to path");
                    continue;
                }

                if (fromPath.Equals(NormalisePath(toPath), StringComparison.Ordinal))
                {
                    _logger.LogWarning("Ignoring CMS redirect from {FromPath} because it points at itself", fromPath);
                    continue;
                }

                var cmsRedirect = new CmsRedirect
                {
                    FromPath = fromPath,
                    ToPath = toPath,
                    MatchType = redirect.MatchType,
                    Permanent = redirect.Permanent
                };

                if (cmsRedirect.MatchType == RedirectMatchType.Prefix)
                {
                    prefixes.Add(cmsRedirect);
                }
                else if (!exact.TryAdd(fromPath, cmsRedirect))
                {
                    _logger.LogWarning("Ignoring duplicate CMS redirect from {FromPath}", fromPath);
                }
            }

            return new RedirectMap
            {
                Exact = exact,
                // Longest first, so /employers/funding beats /employers when both are configured.
                Prefixes = prefixes.OrderByDescending(redirect => redirect.FromPath.Length).ToList()
            };
        }

        private class RedirectMap
        {
            public static readonly RedirectMap Empty = new RedirectMap
            {
                Exact = new Dictionary<string, CmsRedirect>(StringComparer.Ordinal),
                Prefixes = new List<CmsRedirect>()
            };

            public IDictionary<string, CmsRedirect> Exact { get; set; }
            public IList<CmsRedirect> Prefixes { get; set; }
        }
    }
}
