using FluentAssertions;
using NUnit.Framework;
using SFA.DAS.Campaign.Infrastructure.Api.Requests;

namespace SFA.DAS.Campaign.UnitTests.Infrastructure.Api.Requests
{
    public class WhenBuildingTheGetRedirectsRequest
    {
        [Test]
        public void Then_The_Url_Is_Correct()
        {
            var actual = new GetRedirectsRequest();

            actual.GetUrl.Should().Be("redirects");
        }
    }
}
