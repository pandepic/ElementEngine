using System.Collections.Generic;

namespace ElementEngine
{
    public class SimpleStateBase
    {
        public string Name;
        public SimpleStateMachine Parent;

        public SimpleStateBase() { }

        public SimpleStateBase(string name)
        {
            Name = name;
        }

        public virtual void Begin() { }
        public virtual void Update(GameTimer gameTimer) { }
        public virtual void End() { }
    } // SimpleStateBase

    public class SimpleStateTimedBase : SimpleStateBase
    {
        public float Duration;

        public SimpleStateTimedBase() { }
        public SimpleStateTimedBase(string name) : base(name) { }

        public override void Begin()
        {
            base.Begin();
        }

        public override void Update(GameTimer gameTimer)
        {
            Duration -= gameTimer.DeltaMS;

            if (Duration <= 0)
                EndDuration();
        }

        public override void End()
        {
            base.End();
        }

        public virtual void EndDuration() { }
    } // SimpleStateTimedBase

    public class SimpleStateMachine
    {
        public Dictionary<string, SimpleStateBase> States = new Dictionary<string, SimpleStateBase>();

        public SimpleStateBase CurrentState, NextState, PrevState;

        public void RegisterState(SimpleStateBase state)
        {
            if (States.ContainsKey(state.Name))
                return;

            state.Parent = this;
            States.Add(state.Name, state);
        }

        public void RemoveState(SimpleStateBase state)
        {
            RemoveState(state.Name);
        }

        public void RemoveState(string name)
        {
            if (!States.ContainsKey(name))
                return;

            States.Remove(name);
        }

        public void SetState(SimpleStateBase state)
        {
            SetState(state.Name);
        }

        public void SetState(string name)
        {
            if (!States.ContainsKey(name))
                return;

            SetCurrentStateInternal(States[name]);
        }

        public void SetState<T>() where T : SimpleStateBase
        {
            foreach (var kvp in States)
            {
                if (kvp.Value is T)
                {
                    SetCurrentStateInternal(kvp.Value);
                    return;
                }
            }
        }

        public T GetState<T>() where T : SimpleStateBase
        {
            foreach (var kvp in States)
            {
                if (kvp.Value is T val)
                    return val;
            }

            return null;
        }

        public void Start(SimpleStateBase state)
        {
            Start(state.Name);
        }

        public void Start(string name)
        {
            if (!States.ContainsKey(name))
                return;

            NextState = null;
            PrevState = null;
            SetCurrentStateInternal(States[name]);
        }

        public void Start<T>() where T : SimpleStateBase
        {
            foreach (var kvp in States)
            {
                if (kvp.Value is T)
                {
                    Start(kvp.Value.Name);
                    return;
                }
            }
        }

        public void Update(GameTimer gameTimer)
        {
            CurrentState?.Update(gameTimer);

            if (NextState != null)
            {
                SetCurrentStateInternal(NextState);
                NextState = null;
            }
        }

        protected void SetCurrentStateInternal(SimpleStateBase state)
        {
            CurrentState?.End();
            PrevState = CurrentState;

            CurrentState = state;
            CurrentState.Begin();
        }
    } // SimpleStateMachine
}
