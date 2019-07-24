using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Stateless;
using VirtoCommerce.Storefront.Model.Common;

namespace VirtoCommerce.Storefront.Model.PP
{
    public abstract class ApprovalRequestBase : ValueObject
    {
        [JsonIgnore]
        public StateMachine<string, string> StateMachine { get; set; }

        public DateTimeOffset CreatedDate { get; set; }
        public string CreatedBy { get; set; }

        public string OrganisationId { get; set; }

        public string StoreId { get; set; }

        public string State
        {
            get
            {
                return StateMachine?.State;
            }
        }
        public string Number { get; set; }
        public IEnumerable<string> PermittedTriggers
        {
            get
            {
                return StateMachine?.PermittedTriggers;
            }
        }
        public void Fire(string trigger)
        {
            StateMachine?.Fire(trigger);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Number;
        }
    }
}

