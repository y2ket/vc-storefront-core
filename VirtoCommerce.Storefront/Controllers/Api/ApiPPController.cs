using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using VirtoCommerce.Storefront.AutoRestClients.CatalogModuleApi;
using VirtoCommerce.Storefront.AutoRestClients.OrdersModuleApi;
using VirtoCommerce.Storefront.AutoRestClients.PlatformModuleApi;
using VirtoCommerce.Storefront.Domain;
using VirtoCommerce.Storefront.Model;
using VirtoCommerce.Storefront.Model.Cart.Services;
using VirtoCommerce.Storefront.Model.Common;
using VirtoCommerce.Storefront.Model.Customer.Services;
using VirtoCommerce.Storefront.Model.PP;
using VirtoCommerce.Storefront.Model.Security;
using VirtoCommerce.Storefront.Model.StaticContent;
namespace VirtoCommerce.Storefront.Controllers.Api
{
    public class ApiPPController : StorefrontControllerBase
    {
        private readonly IContentBlobProvider _contentBlobProvider;
        private readonly ICartBuilder _cartBuilder;
        private readonly IOrderModule _orderApi;
        private readonly UserManager<User> _userManager;
        private readonly IMemberService _memberService;
        private readonly IAuthorizationService _authorizationService;
        private static IList<ApprovalRequestBase> _inMemoryRequestsStore = new List<ApprovalRequestBase>();
        private static DynamicApprovalWorkflow _currentOrderApprovalWorkflow = null;
        private static object _lockObject = new object();
        private readonly IWorkContextAccessor _workContextAccessor;
        private readonly ICatalogModuleProducts _productsApi;
        private readonly ICatalogModuleProperties _propertiesApi;
        private readonly IDynamicProperties _dynamicPropertiesApi;
        static ApiPPController()
        {
            //Default state
            _currentOrderApprovalWorkflow = new DynamicApprovalWorkflow();
            _currentOrderApprovalWorkflow.ImageUrl = "themes/assets/images/workflow.png";
            _currentOrderApprovalWorkflow.States.Add(new State { IsInitial = true, Name = "New", PermittedTransitions = new[] { new PermittedTransition { Trigger = "Approve", Roles = new[] { SecurityConstants.Roles.Administrator }, ToState = "Approved" }, new PermittedTransition { Trigger = "Reject", Roles = new[] { SecurityConstants.Roles.Administrator }, ToState = "Rejected" } } });
            _currentOrderApprovalWorkflow.States.Add(new State { Name = "Approved" });
            _currentOrderApprovalWorkflow.States.Add(new State { Name = "Rejected" });
        }

        public ApiPPController(IWorkContextAccessor workContextAccessor, IStorefrontUrlBuilder urlBuilder, UserManager<User> userManager, IAuthorizationService authorizationService, IMemberService memberService,
                                   ICartBuilder cartBuilder, IOrderModule orderApi, IContentBlobProvider contentBlobProvider, ICatalogModuleProducts productsApi, ICatalogModuleProperties propertiesApi, IDynamicProperties dynamicPropertiesApi)
            : base(workContextAccessor, urlBuilder)
        {
            _userManager = userManager;
            _memberService = memberService;
            _authorizationService = authorizationService;
            _cartBuilder = cartBuilder;
            _orderApi = orderApi;
            _contentBlobProvider = contentBlobProvider;
            _workContextAccessor = workContextAccessor;
            _productsApi = productsApi;
            _propertiesApi = propertiesApi;
            _dynamicPropertiesApi = dynamicPropertiesApi;
        }


        // POST: storefrontapi/orderrequests
        [HttpPost]
        public async Task<ActionResult> CreateNewOrderRequestFromCart()
        {
            var cart = WorkContext.CurrentCart.Value;
            //Need to try load fresh cart from cache or service to prevent parallel update conflict
            //because WorkContext.CurrentCart may contains old cart
            await _cartBuilder.LoadOrCreateNewTransientCartAsync(cart.Name, WorkContext.CurrentStore, cart.Customer, cart.Language, cart.Currency);

            var orderDto = await _orderApi.CreateOrderFromCartAsync(_cartBuilder.Cart.Id);
            await _cartBuilder.RemoveCartAsync();

            var orderRequest = new NewOrderRequest
            {
                Order = orderDto.ToCustomerOrder(WorkContext.AllCurrencies, WorkContext.CurrentLanguage),
                Number = orderDto.Number,//TimeBasedNumberGeneratorImpl.GenerateNumber("PO"),
                StoreId = _workContextAccessor.WorkContext.CurrentUser.StoreId,
                CreatedBy = WorkContext.CurrentUser.UserName,
                CreatedDate = DateTimeOffset.Now,
                StateMachine = _currentOrderApprovalWorkflow.GetStateMachine((transition) => _workContextAccessor.WorkContext.CurrentUser.IsUserHasAnyRoles(transition.Roles.ToArray())),
                OrganisationId = _workContextAccessor.WorkContext.CurrentUser.Contact?.OrganizationId
            };
            //Approve order for admin
            if (WorkContext.CurrentUser.IsAdministrator)
            {
                orderRequest.Order.IsApproved = true;
                orderRequest.StateMachine.Fire("Approve");
            }
            if (!_inMemoryRequestsStore.Contains(orderRequest))
            {
                _inMemoryRequestsStore.Add(orderRequest);
            }
            return Json(orderRequest);
        }

        // POST: storefrontapi/approvalrequests/search
        [HttpPost]
        public ActionResult SearchApprovalRequests([FromBody] ApprovalRequestSearchCriteria searchCriteria)
        {

            //AactualizeOrdersList();

            //var query = _inMemoryRequestsStore.AsQueryable().Where(x => x.StoreId == _workContextAccessor.WorkContext.CurrentUser.StoreId);
            ////query = query.Where(x => searchCriteria.Type.HasFlag(x.Type));
            ////if (searchCriteria.State != null)
            ////{
            ////    query = query.Where(x => x.State == searchCriteria.State);
            ////}
            ////Filter only own user requests
            //if (WorkContext.CurrentUser.IsUserHasAnyRoles(SecurityConstants.Roles.CSR.Id))
            //{
            //    query = query.Where(x => x.CreatedBy == WorkContext.CurrentUser.UserName);
            //}
            //else if (!_workContextAccessor.WorkContext.CurrentUser.IsUserHasAnyRoles(SecurityConstants.Roles.AllRoles.Select(x => x.Id).ToArray()))
            //{
            //    var currrentUser = _workContextAccessor.WorkContext.CurrentUser;
            //    var userOrgId = currrentUser.Contact?.OrganizationId;
            //    if (userOrgId == null)
            //    {
            //        query = query.Where(x => x.CreatedBy == WorkContext.CurrentUser.UserName);
            //    }
            //    else
            //    {
            //        var childOrgsBranchIds = _memberService.GetOrganizationChildBranch(userOrgId).Select(x => x.Id).ToArray();
            //        query = query.Where(x => childOrgsBranchIds.Contains(x.OrganisationId) || x.CreatedBy == WorkContext.CurrentUser.UserName);
            //    }

            //}

            //var totalCount = query.Count();
            //var result = query.Skip(searchCriteria.Start).Take(searchCriteria.PageSize).ToArray();

            //return Json(new { TotalCount = totalCount, Result = result });

            return NotFound();
        }

        private void AactualizeOrdersList()
        {
            var orderNumbers = _inMemoryRequestsStore.Select(x => x.Number).ToArray();

            var actualOrders = _orderApi.Search(new AutoRestClients.OrdersModuleApi.Models.CustomerOrderSearchCriteria()
            {
                Numbers = orderNumbers,
                Skip = 0,
                Take = int.MaxValue,
            }).CustomerOrders;

            for (int i = _inMemoryRequestsStore.Count - 1; i >= 0; i--)
            {
                if (!actualOrders.Any(x => x.Number == _inMemoryRequestsStore[i].Number))
                {
                    _inMemoryRequestsStore.RemoveAt(i);
                }
            }

        }


        //GET: storefrontapi/approvalworkflows/current
        [HttpGet]
        public IActionResult GetCurrentWorkflow()
        {
            return Ok(_currentOrderApprovalWorkflow);
        }

        // POST: storefrontapi/approvalworkflows/upload
        [HttpPost]
        public async Task<ActionResult<DynamicApprovalWorkflow>> UploadWorkflow()
        {
            DynamicApprovalWorkflow result = null;
            var form = await Request.ReadFormAsync();
            var formFile = form.Files.FirstOrDefault();
            lock (_lockObject)
            {
                if (formFile != null)
                {
                    using (var stream = formFile.OpenReadStream())
                    {
                        result = JsonConvert.DeserializeObject<DynamicApprovalWorkflow>(stream.ReadToString());
                    }
                }
            }
            return Ok(result);
        }

        //POST: storefrontapi/approvalworkflows/apply
        [HttpPost]
        public IActionResult ApplyWorkflow([FromBody] DynamicApprovalWorkflow workflow)
        {
            _currentOrderApprovalWorkflow = workflow;
            return Ok();
        }

        //POST: storefrontapi/orderrequests/triggers/fire
        [HttpPost]
        public IActionResult FireTriggerForRequest([FromBody]FireTriggerAction fireTriggerAction)
        {
            var request = _inMemoryRequestsStore.FirstOrDefault(x => x.Number.EqualsInvariant(fireTriggerAction.RequestNumber));
            if (request != null)
            {
                request.StateMachine.Fire(fireTriggerAction.Trigger);
            }
            return Ok(request);
        }





    }
}
