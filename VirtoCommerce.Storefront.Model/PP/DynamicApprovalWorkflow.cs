using System;
using System.Collections.Generic;
using System.Linq;
using Stateless;

namespace VirtoCommerce.Storefront.Model.PP
{
    public class DynamicApprovalWorkflow
    {
        public string ImageUrl { get; set; }
        public IList<State> States { get; set; } = new List<State>();
        public StateMachine<string, string> GetStateMachine(Func<PermittedTransition, bool> transitionPredicate)
        {
            StateMachine<string, string> result = null;
            var initialState = States.FirstOrDefault(x => x.IsInitial);
            if (initialState != null)
            {
                result = new StateMachine<string, string>(initialState.Name);
                foreach (var state in States)
                {
                    if (!state.IsOptional)
                    {
                        var configuration = result.Configure(state.Name);

                        var stateTransitions = new List<PermittedTransition>();
                        GetAvailableTransitions(state, stateTransitions);

                        foreach (var permittedTransition in stateTransitions)
                        {
                            configuration.PermitIf(permittedTransition.Trigger, permittedTransition.ToState, () => transitionPredicate(permittedTransition));
                        }
                    }
                }
            }
            return result;
        }

        //recursive
        private void GetAvailableTransitions(State state, IList<PermittedTransition> resultTransitions)
        {
            foreach (var stateTransition in state.PermittedTransitions)
            {
                var transitionTartgeState = GetStateByName(stateTransition.ToState);
                if (transitionTartgeState != null)
                {
                    if (transitionTartgeState.IsOptional)
                    {
                        GetAvailableTransitions(transitionTartgeState, resultTransitions);
                    }
                    else
                    {
                        if (!resultTransitions.Any(t => t.ToState == stateTransition.ToState))
                        {
                            resultTransitions.Add(stateTransition);
                        }
                    }
                }

            }
        }


        private State GetStateByName(string stateName)
        {
            return States.FirstOrDefault(x => x.Name == stateName);
        }
    }



    public class State
    {
        public string Name { get; set; }
        public bool IsInitial { get; set; } = false;
        public bool IsOptional { get; set; } = false;
        public IList<PermittedTransition> PermittedTransitions { get; set; } = new List<PermittedTransition>();

    }
    public class PermittedTransition
    {
        public IList<string> Roles { get; set; } = new List<string>();
        public string Trigger { get; set; }
        public string ToState { get; set; }
    }
}
