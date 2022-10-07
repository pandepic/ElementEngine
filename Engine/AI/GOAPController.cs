using System.Collections.Generic;
using System.Linq;

namespace ElementEngine
{
    public class GOAPAction<T>
    {
        public string Name { get; set; }
        public List<T> Requires { get; set; }
        public List<T> Provides { get; set; }

        public int Cost { get; set; }

        public GOAPAction(string name, int cost)
        {
            Name = name;
            Cost = cost;
            Requires = new List<T>();
            Provides = new List<T>();
        }
    } // GOAPAction

    public class GOAPGoal<T>
    {
        public string Name { get; set; }
        public GOAPAction<T> Result { get; set; }
        public List<GOAPAction<T>> Actions { get; set; }

        public GOAPGoal(string name)
        {
            Name = name;
            Actions = new List<GOAPAction<T>>();
        }
    } // GOAPGoal

    public class GOAPController<T>
    {
        public List<GOAPAction<T>> AvailableActions { get; set; }
        public GOAPGoal<T> CurrentGoal { get; set; }

        public GOAPController()
        {
            AvailableActions = new List<GOAPAction<T>>();
        }

        public GOAPController(List<GOAPAction<T>> availableActions)
        {
            AvailableActions = availableActions;
        }

        public bool TrySetGoal(List<T> Request, string name)
        {
            GOAPAction<T> Result = null;

            for (var i = 0; i < AvailableActions.Count && Result == null; i++)
            {
                var action = AvailableActions[i];
                var matches = Request.All(action.Provides.Contains);
                
                if (matches)
                    Result = action;
            }

            if (Result == null)
                return false;

            var goal = new GOAPGoal<T>(name)
            {
                Result = Result
            };

            if (TrySetGoalActions(goal))
            {
                CurrentGoal = goal;
                return true;
            }
            else
                return false;

        } // TrySetGoal

        protected bool TrySetGoalActions(GOAPGoal<T> goal)
        {
            goal.Actions.Clear();

            var currentAction = goal.Result;

            var pathFound = false;

            while (!pathFound)
            {
                if (currentAction.Requires.Count == 0)
                {
                    goal.Actions.Add(currentAction);
                    return true;
                }

                GOAPAction<T> checkAction = null;

                for (var i = 0; i < AvailableActions.Count; i++)
                {
                    var availableAction = AvailableActions[i];

                    if (availableAction.Provides.ListCompare(currentAction.Requires))
                    {
                        if (checkAction == null || checkAction.Cost > availableAction.Cost)
                            checkAction = availableAction;
                    }
                }

                if (checkAction == null)
                    return false;
                else
                {
                    goal.Actions.Add(currentAction);
                    currentAction = checkAction;
                }
            }

            return false;

        } // SetGoalActions

    } // GOAPController
}
