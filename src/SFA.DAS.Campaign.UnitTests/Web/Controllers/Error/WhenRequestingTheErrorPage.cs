using System.Threading.Tasks;
using AutoFixture.NUnit3;
using FluentAssertions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using SFA.DAS.Campaign.Application.Services;
using SFA.DAS.Campaign.Domain.Content;
using SFA.DAS.Campaign.Web.Controllers;
using SFA.DAS.Testing.AutoFixture;

namespace SFA.DAS.Campaign.UnitTests.Web.Controllers.Error
{
    public class WhenRequestingTheErrorPage
    {
        private const string OriginalPath = "/employers/old-page";

        [Test, RecursiveMoqAutoData]
        public async Task And_The_Cms_Has_A_Redirect_For_The_Original_Path_Then_It_Is_Followed(
            [Frozen] Mock<IRedirectService> mockRedirectService,
            [Greedy] ErrorController controller)
        {
            SetupController(controller, mockRedirectService, new CmsRedirect { ToPath = "/employers/new-page", Permanent = true });

            var actual = await controller.Error(404) as RedirectResult;

            actual.Should().NotBeNull();
            actual.Url.Should().Be("/employers/new-page?utm_source=email");
            actual.Permanent.Should().BeTrue();
            mockRedirectService.Verify(o => o.GetRedirect(OriginalPath), Times.Once);
        }

        [Test, RecursiveMoqAutoData]
        public async Task And_There_Is_No_Redirect_Then_The_Page_Not_Found_Page_Is_Returned(
            [Frozen] Mock<IRedirectService> mockRedirectService,
            [Greedy] ErrorController controller)
        {
            SetupController(controller, mockRedirectService, null);

            var actual = await controller.Error(404) as RedirectToActionResult;

            actual.Should().NotBeNull();
            actual.ActionName.Should().Be("PageNotFound");
        }

        [Test, RecursiveMoqAutoData]
        public async Task And_The_Status_Code_Is_Not_A_404_Then_No_Redirect_Is_Looked_Up(
            [Frozen] Mock<IRedirectService> mockRedirectService,
            [Greedy] ErrorController controller)
        {
            SetupController(controller, mockRedirectService, null);

            var actual = await controller.Error(500) as ViewResult;

            actual.Should().NotBeNull();
            actual.ViewName.Should().Be("Error");
            mockRedirectService.Verify(o => o.GetRedirect(It.IsAny<string>()), Times.Never);
        }

        private static void SetupController(ErrorController controller, Mock<IRedirectService> mockRedirectService, CmsRedirect redirect)
        {
            mockRedirectService.Setup(o => o.GetRedirect(It.IsAny<string>())).ReturnsAsync(redirect);

            var httpContext = new DefaultHttpContext();
            httpContext.Features.Set<IStatusCodeReExecuteFeature>(new StatusCodeReExecuteFeature
            {
                OriginalPath = OriginalPath,
                OriginalQueryString = "?utm_source=email"
            });

            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        }
    }
}
