namespace SFA.DAS.Campaign.Domain.Content
{
    public class CmsRedirect
    {
        public string FromPath { get; set; }
        public string ToPath { get; set; }
        public RedirectMatchType MatchType { get; set; }
        public bool Permanent { get; set; }
    }
}