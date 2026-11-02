using System;

namespace MyFrame
{
    public interface IStateMachineOwner { }
    public abstract class StateBase
    {
        public virtual void UnInit()
        {
            // 放回对象池
            this.ObjectPushPool();
        }
    
        public virtual void Enter() { }
    
        public virtual void Exit() { }

        public virtual void Update() { }
        public virtual void LateUpdate() { }
        public virtual void FixedUpdate() { }
        
        public virtual bool CheckCanEnterState()
        {
            return true;
        }
    }

    public abstract class StateBase<TOwner,TState> : StateBase where TOwner:IStateMachineOwner where TState:StateBase<TOwner,TState>
    {
        protected TOwner Owner;
        public virtual void BeforeEnter(TState state)
        {
            
        }
        
        public virtual void BeforeExit(TState stateEnum)
        {
            
        }
        
        
        public virtual void Init(TOwner owner)
        {
            Owner = owner;
        }    
    }
}