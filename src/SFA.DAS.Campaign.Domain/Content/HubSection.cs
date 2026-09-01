using System.Collections.Generic;
using SFA.DAS.Campaign.Domain.Content.HtmlControl;

namespace SFA.DAS.Campaign.Domain.Content
{
    public class HubSection
    {
        public HubSection()
        {
            StepperLinks = new List<HubSectionLink>();
            StandardLinks = new List<HubSectionLink>();
        }
        
        public string SectionType { get; set; }

        public string Heading { get; set; }
        public string Introduction { get; set; }
        public Image Image { get; set; }
        public List<HubSectionLink> StepperLinks { get; set; }
        public List<HubSectionLink> StandardLinks { get; set; }
        public CtaPanel CtaPanel { get; set; }

        public bool HasContent =>
            !string.IsNullOrWhiteSpace(Heading)
            || !string.IsNullOrWhiteSpace(Introduction)
            || StepperLinks.Count > 0
            || StandardLinks.Count > 0
            || CtaPanel != null;
    }

    public class HubSectionLink : Card
    {
        public CtaPanel CtaPanel { get; set; }
    }

    public class CtaPanel
    {
        public string Heading { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public string ButtonText { get; set; }
        public string Url { get; set; }
    }
}
