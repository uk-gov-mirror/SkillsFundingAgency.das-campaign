using System.Collections.Generic;
using Newtonsoft.Json;
using SFA.DAS.Campaign.Domain.Content;

namespace SFA.DAS.Campaign.Infrastructure.Api.Responses
{
    public class GetRedirectsResponse
    {
        [JsonProperty("redirects")]
        public List<Redirect> Redirects { get; set; }
    }

    public class Redirect
    {
        [JsonProperty("fromPath")]
        public string FromPath { get; set; }

        [JsonProperty("toPath")]
        public string ToPath { get; set; }

        [JsonProperty("matchType")]
        public RedirectMatchType MatchType { get; set; }

        [JsonProperty("permanent")]
        public bool Permanent { get; set; }
    }
}
