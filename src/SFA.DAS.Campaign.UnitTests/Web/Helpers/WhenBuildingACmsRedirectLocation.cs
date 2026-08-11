using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using SFA.DAS.Campaign.Domain.Content;
using SFA.DAS.Campaign.Web.Helpers;

namespace SFA.DAS.Campaign.UnitTests.Web.Helpers
{
    public class WhenBuildingACmsRedirectLocation
    {
        [Test]
        public void Then_The_Destination_Is_Used_When_There_Is_No_Query_String()
        {
            var redirect = new CmsRedirect { ToPath = "/employers/new-page" };

            var actual = redirect.BuildLocation(QueryString.Empty);

            actual.Should().Be("/employers/new-page");
        }

        [Test]
        public void Then_The_Original_Query_String_Is_Carried_Over()
        {
            var redirect = new CmsRedirect { ToPath = "/employers/new-page" };

            var actual = redirect.BuildLocation(new QueryString("?utm_source=email&utm_medium=cpc"));

            actual.Should().Be("/employers/new-page?utm_source=email&utm_medium=cpc");
        }

        [Test]
        public void Then_A_Destination_With_Its_Own_Query_String_Is_Left_Alone()
        {
            var redirect = new CmsRedirect { ToPath = "/employers/new-page?utm_source=redirect" };

            var actual = redirect.BuildLocation(new QueryString("?utm_source=email"));

            actual.Should().Be("/employers/new-page?utm_source=redirect");
        }

        [Test]
        public void Then_An_Absolute_Destination_Is_Supported()
        {
            var redirect = new CmsRedirect { ToPath = "https://www.gov.uk/apply-apprenticeship" };

            var actual = redirect.BuildLocation(QueryString.Empty);

            actual.Should().Be("https://www.gov.uk/apply-apprenticeship");
        }
    }
}
