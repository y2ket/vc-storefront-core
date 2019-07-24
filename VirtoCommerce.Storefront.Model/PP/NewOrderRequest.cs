using VirtoCommerce.Storefront.Model.Order;

namespace VirtoCommerce.Storefront.Model.PP
{
    public class NewOrderRequest : ApprovalRequestBase
    {
        public CustomerOrder Order { get; set; }

    }
}
