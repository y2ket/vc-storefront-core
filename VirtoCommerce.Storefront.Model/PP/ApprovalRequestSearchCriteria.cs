using System;
using System.Collections.Specialized;
using VirtoCommerce.Storefront.Model.Common;

namespace VirtoCommerce.Storefront.Model.PP
{
    public class ApprovalRequestSearchCriteria : PagedSearchCriteria
    {
        public ApprovalRequestSearchCriteria()
            : base(new NameValueCollection(), 20)
        {
        }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string State { get; set; }

    }
}
