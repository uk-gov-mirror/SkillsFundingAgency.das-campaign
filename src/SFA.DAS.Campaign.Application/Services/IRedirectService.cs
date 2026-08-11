using System.Threading.Tasks;
using SFA.DAS.Campaign.Domain.Content;

namespace SFA.DAS.Campaign.Application.Services
{
    public interface IRedirectService
    {
        /// <summary>
        /// Returns the redirect configured in the CMS for the supplied path, or null if there isn't one.
        /// </summary>
        Task<CmsRedirect> GetRedirect(string path);
    }
}
