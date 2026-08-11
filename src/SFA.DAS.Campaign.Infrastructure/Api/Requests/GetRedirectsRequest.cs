using SFA.DAS.Campaign.Domain.Api.Interfaces;

namespace SFA.DAS.Campaign.Infrastructure.Api.Requests
{
    public class GetRedirectsRequest : IGetApiRequest
    {
        public string GetUrl => "redirects";
    }
}
