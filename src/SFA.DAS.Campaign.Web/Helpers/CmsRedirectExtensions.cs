using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SFA.DAS.Campaign.Application.Services;
using SFA.DAS.Campaign.Domain.Content;

namespace SFA.DAS.Campaign.Web.Helpers
{
    public static class CmsRedirectExtensions
    {
        /// <summary>
        /// Returns a redirect to wherever the CMS says the supplied path has moved to, or null when there is no
        /// redirect configured for it and the caller should carry on serving its not found page.
        /// </summary>
        public static async Task<IActionResult> GetCmsRedirect(this ControllerBase controller, IRedirectService redirectService, string path, QueryString queryString)
        {
            var redirect = await redirectService.GetRedirect(path).ConfigureAwait(false);

            if (redirect == null)
            {
                return null;
            }

            var location = redirect.BuildLocation(queryString);

            return redirect.Permanent ? controller.RedirectPermanent(location) : controller.Redirect(location);
        }

        /// <summary>
        /// Builds the Location for a CMS redirect, carrying the original query string over to the new page.
        /// A destination that already carries its own query string wins, so campaign tracking set by an editor
        /// isn't mangled by whatever was on the inbound link.
        /// </summary>
        public static string BuildLocation(this CmsRedirect redirect, QueryString queryString)
        {
            if (!queryString.HasValue || redirect.ToPath.Contains("?", StringComparison.Ordinal))
            {
                return redirect.ToPath;
            }

            return redirect.ToPath + queryString.Value;
        }
    }
}
