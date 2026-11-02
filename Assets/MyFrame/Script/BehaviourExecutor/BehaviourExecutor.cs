using System;
using MyFrame;
using UnityEngine;

namespace MyFrame.BehaviourExecutor
{
    public class BehaviourExecutor
    {
        public BehaviourExecutor()
        {
            PoolSystem.InitObjectPool("PlayerBehaviourHandlerPool",() => new BehaviourHandler(), 10);
        }
        
        private BehaviourHandler _curBehaviourHandler;
        public BehaviourHandler CurBehaviourHandler => _curBehaviourHandler;
        internal void ClearCurrentHandlerIfMatch(BehaviourHandler handler)
        {
            if (ReferenceEquals(_curBehaviourHandler, handler))
            {
                _curBehaviourHandler = null;
            }
        }
        private BehaviourHandler ExecuteBehaviour(ref PlayerBehaviour behaviour)
        {
            if (_curBehaviourHandler != null  && (behaviour.ID != null && _curBehaviourHandler.BehaviourID == behaviour.ID))
            {
                return _curBehaviourHandler;
            }
            var handler = PoolSystem.GetObject<BehaviourHandler>("PlayerBehaviourHandlerPool");
            if(handler == null) handler = new BehaviourHandler();
            handler.BindExecutor(this);
            handler.SetBehaviour(ref behaviour);
            bool checkResult = true;
            if (_curBehaviourHandler != null)
            {
                if (_curBehaviourHandler?.State == PlayerBehaviourState.Working)
                {
                    checkResult = _curBehaviourHandler.Checker?.Invoke(_curBehaviourHandler.Behaviour,behaviour) ?? behaviour.Priority > _curBehaviourHandler.Priority;
                }
                if (!checkResult)
                {
                    handler.State = PlayerBehaviourState.Failed;
                    return handler;
                }
            }
            handler.State = PlayerBehaviourState.Working;
            _curBehaviourHandler = handler;
            behaviour.Action?.Invoke();
            return handler;
        }
        public BehaviourHandler ExecuteBehaviour(PlayerBehaviour behaviour)
        {
            var handler = ExecuteBehaviour(ref behaviour);
            return handler;
        }
        public BehaviourHandler ExecuteBehaviour(Action behaviour = null, float priority = 0f, Func<PlayerBehaviour,PlayerBehaviour,bool> checker = null,string behaviourID = null)
        {
            var behaviourInstance = PlayerBehaviour.Create(behaviour, priority, checker, behaviourID);
            var handler = ExecuteBehaviour(ref behaviourInstance);
            return handler;
        }
    }
    
    
    
    public enum PlayerBehaviourState
    {
        None,
        Failed,
        Working,
        End
    }
    public class BehaviourHandler
    { 
        private BehaviourExecutor _executor;
        private PlayerBehaviour _behaviour;
        internal PlayerBehaviour Behaviour => _behaviour;
        public string BehaviourID => _behaviour.ID;
        public PlayerBehaviourState State;
        public float Priority => _behaviour.Priority;
        public Func<PlayerBehaviour,PlayerBehaviour,bool> Checker => _behaviour.Checker;
        
        public void SetBehaviourEnd()
        {
            State = PlayerBehaviourState.End;
            _executor?.ClearCurrentHandlerIfMatch(this);
        }

        internal void SetBehaviour(ref PlayerBehaviour behaviour)
        {
            _behaviour = behaviour;
        }

        internal BehaviourHandler()
        {
             
        }
        internal void BindExecutor(BehaviourExecutor executor)
        {
            _executor = executor;
        }
        
        public void Destroy()
        {
            PoolSystem.PushObject(this, "PlayerBehaviourHandlerPool");
        }
    }
    public struct PlayerBehaviour
    {
        public readonly Action Action;
        public readonly Func<PlayerBehaviour,PlayerBehaviour,bool> Checker;
        public readonly float Priority;
        public readonly string ID;
        private PlayerBehaviour(Action action,float priority,Func<PlayerBehaviour,PlayerBehaviour,bool> checker,string id = null)
        {
            Action = action;
            Checker = checker;
            Priority = priority;
            ID = id;
        }

        public static PlayerBehaviour Create(Action behaviour = null, float priority = 0f, Func<PlayerBehaviour,PlayerBehaviour,bool> checker = null, string id = null)
        {
            var behaviourInstance = new PlayerBehaviour(behaviour,priority, checker, id);
            return behaviourInstance;
        }
    }
}
