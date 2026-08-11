using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Campaign.Application.Services;
using SFA.DAS.Campaign.Domain.Api.Interfaces;
using SFA.DAS.Campaign.Domain.Content;
using SFA.DAS.Campaign.Infrastructure.Api.Requests;
using SFA.DAS.Campaign.Infrastructure.Api.Responses;

namespace SFA.DAS.Campaign.UnitTests.Application.Redirects
{
    public class WhenGettingARedirect
    {
        private Mock<IApiClient> _apiClient;
        private MemoryCache _cache;
        private RedirectService _service;

        [SetUp]
        public void Arrange()
        {
            _apiClient = new Mock<IApiClient>();
            _cache = new MemoryCache(new MemoryCacheOptions());
            _service = new RedirectService(_apiClient.Object, _cache, Mock.Of<ILogger<RedirectService>>());
        }

        [TearDown]
        public void TearDown()
        {
            _cache.Dispose();
        }

        private void SetupRedirects(params Redirect[] redirects)
        {
            _apiClient.Setup(x => x.Get<GetRedirectsResponse>(It.IsAny<GetRedirectsRequest>()))
                .ReturnsAsync(new GetRedirectsResponse { Redirects = new List<Redirect>(redirects) });
        }

        private static Redirect Redirect(string fromPath, string toPath, RedirectMatchType matchType = RedirectMatchType.Exact, bool permanent = true)
        {
            return new Redirect { FromPath = fromPath, ToPath = toPath, MatchType = matchType, Permanent = permanent };
        }

        [Test]
        public async Task Then_An_Exact_Match_Is_Returned()
        {
            SetupRedirects(Redirect("/employers/old-page", "/employers/new-page"));

            var actual = await _service.GetRedirect("/employers/old-page");

            actual.Should().NotBeNull();
            actual.ToPath.Should().Be("/employers/new-page");
            actual.Permanent.Should().BeTrue();
        }

        [TestCase("/EMPLOYERS/Old-Page")]
        [TestCase("/employers/old-page/")]
        [TestCase("/employers/old-page?utm_source=email")]
        [TestCase("employers/old-page")]
        public async Task Then_The_Requested_Path_Is_Normalised_Before_Matching(string requestedPath)
        {
            SetupRedirects(Redirect("/employers/old-page", "/employers/new-page"));

            var actual = await _service.GetRedirect(requestedPath);

            actual.Should().NotBeNull();
            actual.ToPath.Should().Be("/employers/new-page");
        }

        [TestCase("/EMPLOYERS/Old-Page/")]
        [TestCase("employers/old-page")]
        public async Task Then_The_Configured_Path_Is_Normalised_Before_Matching(string configuredPath)
        {
            SetupRedirects(Redirect(configuredPath, "/employers/new-page"));

            var actual = await _service.GetRedirect("/employers/old-page");

            actual.Should().NotBeNull();
            actual.ToPath.Should().Be("/employers/new-page");
        }

        [Test]
        public async Task Then_Null_Is_Returned_When_Nothing_Matches()
        {
            SetupRedirects(Redirect("/employers/old-page", "/employers/new-page"));

            var actual = await _service.GetRedirect("/employers/a-live-page");

            actual.Should().BeNull();
        }

        [Test]
        public async Task Then_A_Non_Permanent_Redirect_Is_Flagged_As_Such()
        {
            SetupRedirects(Redirect("/employers/old-page", "/employers/new-page", permanent: false));

            var actual = await _service.GetRedirect("/employers/old-page");

            actual.Permanent.Should().BeFalse();
        }

        [TestCase("/employers/retired-section")]
        [TestCase("/employers/retired-section/a-page")]
        [TestCase("/employers/retired-section/a-page/deeper")]
        public async Task Then_A_Prefix_Redirect_Matches_The_Section_And_Everything_Under_It(string requestedPath)
        {
            SetupRedirects(Redirect("/employers/retired-section", "/employers", RedirectMatchType.Prefix));

            var actual = await _service.GetRedirect(requestedPath);

            actual.Should().NotBeNull();
            actual.ToPath.Should().Be("/employers");
        }

        [Test]
        public async Task Then_A_Prefix_Redirect_Does_Not_Match_A_Partial_Segment()
        {
            SetupRedirects(Redirect("/employers/retired", "/employers", RedirectMatchType.Prefix));

            var actual = await _service.GetRedirect("/employers/retirement-planning");

            actual.Should().BeNull();
        }

        [Test]
        public async Task Then_The_Longest_Matching_Prefix_Wins()
        {
            SetupRedirects(
                Redirect("/employers", "/employers-hub", RedirectMatchType.Prefix),
                Redirect("/employers/funding", "/employers/funding-an-apprenticeship", RedirectMatchType.Prefix));

            var actual = await _service.GetRedirect("/employers/funding/levy");

            actual.ToPath.Should().Be("/employers/funding-an-apprenticeship");
        }

        [Test]
        public async Task Then_An_Exact_Match_Beats_A_Prefix_Match()
        {
            SetupRedirects(
                Redirect("/employers/old", "/employers/prefix-destination", RedirectMatchType.Prefix),
                Redirect("/employers/old", "/employers/exact-destination"));

            var actual = await _service.GetRedirect("/employers/old");

            actual.ToPath.Should().Be("/employers/exact-destination");
        }

        [Test]
        public async Task Then_The_Home_Page_Can_Never_Be_Redirected()
        {
            SetupRedirects(Redirect("/", "/employers"));

            var actual = await _service.GetRedirect("/");

            actual.Should().BeNull();
        }

        [TestCase("/employers/old-page", "/employers/old-page")]
        [TestCase("/employers/old-page", "/EMPLOYERS/old-page/")]
        public async Task Then_A_Redirect_That_Points_At_Itself_Is_Ignored(string fromPath, string toPath)
        {
            SetupRedirects(Redirect(fromPath, toPath));

            var actual = await _service.GetRedirect(fromPath);

            actual.Should().BeNull();
        }

        [TestCase(null, "/employers/new-page")]
        [TestCase("", "/employers/new-page")]
        [TestCase("/employers/old-page", null)]
        [TestCase("/employers/old-page", " ")]
        public async Task Then_An_Incomplete_Redirect_Is_Ignored(string fromPath, string toPath)
        {
            SetupRedirects(Redirect(fromPath, toPath), Redirect("/a/valid-entry", "/a/destination"));

            var actual = await _service.GetRedirect(fromPath);

            actual.Should().BeNull();
        }

        [Test]
        public async Task Then_A_Duplicate_From_Path_Does_Not_Throw_And_The_First_Wins()
        {
            SetupRedirects(
                Redirect("/employers/old-page", "/employers/first"),
                Redirect("/employers/old-page", "/employers/second"));

            var actual = await _service.GetRedirect("/employers/old-page");

            actual.ToPath.Should().Be("/employers/first");
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public async Task Then_An_Empty_Requested_Path_Returns_Null_Without_Calling_The_Api(string requestedPath)
        {
            var actual = await _service.GetRedirect(requestedPath);

            actual.Should().BeNull();
            _apiClient.Verify(x => x.Get<GetRedirectsResponse>(It.IsAny<GetRedirectsRequest>()), Times.Never);
        }

        [Test]
        public async Task Then_The_Redirects_Are_Cached_Across_Lookups()
        {
            SetupRedirects(Redirect("/employers/old-page", "/employers/new-page"));

            await _service.GetRedirect("/employers/old-page");
            await _service.GetRedirect("/employers/another-page");
            await _service.GetRedirect("/employers/old-page");

            _apiClient.Verify(x => x.Get<GetRedirectsResponse>(It.IsAny<GetRedirectsRequest>()), Times.Once);
        }

        [Test]
        public async Task Then_A_Failing_Api_Serves_No_Redirects_Rather_Than_Throwing()
        {
            _apiClient.Setup(x => x.Get<GetRedirectsResponse>(It.IsAny<GetRedirectsRequest>()))
                .ThrowsAsync(new System.Net.Http.HttpRequestException("api is down"));

            var actual = await _service.GetRedirect("/employers/old-page");

            actual.Should().BeNull();
        }

        [Test]
        public async Task Then_An_Empty_Api_Response_Serves_No_Redirects()
        {
            _apiClient.Setup(x => x.Get<GetRedirectsResponse>(It.IsAny<GetRedirectsRequest>()))
                .ReturnsAsync((GetRedirectsResponse)null);

            var actual = await _service.GetRedirect("/employers/old-page");

            actual.Should().BeNull();
        }
    }
}
