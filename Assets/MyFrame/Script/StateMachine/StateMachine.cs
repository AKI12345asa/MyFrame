using System;
using System.Collections.Generic;
using MyFrame;
using UnityEngine;

namespace MyFrame
{
    public interface IStateSwitcher<TStateEnum> where TStateEnum : struct, Enum
    {
        public bool ChangeState(TStateEnum stateEnum, bool reCurrState);
        public TStateEnum CurrentStateEnum { get; }
    }

    public interface IStateRegister<TStateEnum, TStateBase> where TStateEnum : struct, Enum
        where TStateBase : StateBase
    {
        public void RegisterState(TStateEnum stateEnum, TStateBase stateBase);

        public Dictionary<TStateEnum, TStateBase> StateDic { get; }
    }


    public interface IStateOwner<out TState> where TState : StateBase
    {
        public TState CurrentState { get; }
    }

    public class StateMachine<TOwner, TStateBase, TStateEnum> : StateMachine, IStateSwitcher<TStateEnum>, IStateRegister<TStateEnum, TStateBase>, IStateOwner<TStateBase> where TStateBase : StateBase<TOwner,TStateBase> where TOwner : IStateMachineOwner where TStateEnum : struct, Enum
    {
        private Dictionary<TStateEnum, TStateBase> _stateDic;
        protected TOwner Owner;
        private TStateBase _currentState;
        private TStateEnum _currentStateEnum;
        private bool _isFirstChange = true;
        private Action _currentUpdate;
        private Action _currentLateUpdate;
        private Action _currentFixedUpdate;

        public void RegisterState(TStateEnum stateEnum, TStateBase state)
        {
            state.Init(Owner);
            _stateDic.Add(stateEnum, state);
        }

        public Dictionary<TStateEnum, TStateBase> StateDic => _stateDic;

        public TStateBase CurrentState => _currentState;

        public TStateEnum CurrentStateEnum => _currentStateEnum;

        private void UnbindCurrentStateCallbacks()
        {
            if (_currentUpdate != null)
            {
                this.RemoveUpdate(_currentUpdate);
                _currentUpdate = null;
            }

            if (_currentLateUpdate != null)
            {
                this.RemoveLateUpdate(_currentLateUpdate);
                _currentLateUpdate = null;
            }

            if (_currentFixedUpdate != null)
            {
                this.RemoveFixedUpdate(_currentFixedUpdate);
                _currentFixedUpdate = null;
            }
        }

        private void BindStateCallbacks(TStateBase state)
        {
            _currentUpdate = state.Update;
            _currentLateUpdate = state.LateUpdate;
            _currentFixedUpdate = state.FixedUpdate;
            this.AddUpdate(_currentUpdate);
            this.AddLateUpdate(_currentLateUpdate);
            this.AddFixedUpdate(_currentFixedUpdate);
        }

        public bool ChangeState(TStateEnum stateEnum, bool reCurrState)
        {
            if (stateEnum.Equals(_currentStateEnum) && !reCurrState && !_isFirstChange)
            {
                /*Debug.Log("当前状态和要切换的状态一致，并且不需要刷新状态，切换失败");*/
                return false;
            }
            var curState = _stateDic[stateEnum];
            if (!curState.CheckCanEnterState()) return false;
            if (_currentState != null)
            {
                _currentState.BeforeExit(curState);
                curState.BeforeEnter(_currentState);
                _currentState.Exit();
                UnbindCurrentStateCallbacks();
            }
            _currentState = curState;
            _currentStateEnum = stateEnum;
            _currentState.Enter();
            BindStateCallbacks(curState);
            _isFirstChange = false;
            return true;
        }
        
        public void Init(TOwner owner)
        {
            Owner = owner;
            _stateDic = ResSystem.GetOrNew<Dictionary<TStateEnum, TStateBase>>();
        }

        public override void Stop()
        {
            if (_currentState != null)
            {
                _currentState.Exit();
                UnbindCurrentStateCallbacks();
            }

            foreach (var state in _stateDic.Values)
            {
                state.UnInit();
            }

            _stateDic.Clear();
            _stateDic.ObjectPushPool();
            _stateDic = null;
            _currentState = null;
            _isFirstChange = true;
        }

        public override void Destroy()
        {
            UnbindCurrentStateCallbacks();
            Stop();
        }
    }

    public abstract class StateMachine
    {
        public static StateMachine<TOwner, TStateBase, TStateEnum> CreateMachine<TOwner, TStateBase, TStateEnum>(TOwner owner) where TStateBase : StateBase<TOwner,TStateBase> where TOwner : IStateMachineOwner where TStateEnum : struct, Enum
        {
            var stateMachine = ResSystem.GetOrNew<StateMachine<TOwner, TStateBase, TStateEnum>>();
            stateMachine.Init(owner);
            return stateMachine;
        }

        public void RegisterState<TOwner, TStateEnum, TStateBase, TState>(TStateEnum stateEnum) where TStateBase : StateBase<TOwner,TStateBase> where TStateEnum : struct, Enum where TState : TStateBase, new() where TOwner : IStateMachineOwner
        {
            var state = new TState();
            ((IStateRegister<TStateEnum, TStateBase>)this).RegisterState(stateEnum, state);
        }

        public bool ChangeState<TStateEnum>(TStateEnum stateEnum, bool reCurrState = false)
            where TStateEnum : struct, Enum
        {
            return ((IStateSwitcher<TStateEnum>)this).ChangeState(stateEnum, reCurrState);
        }

        public TStateEnum GetCurStateEnum<TStateEnum>() where TStateEnum : struct, Enum
        {
            return ((IStateSwitcher<TStateEnum>)this).CurrentStateEnum;
        }

        public TStateBase GetCurrentState<TStateBase>() where TStateBase : StateBase
        {
            return ((IStateOwner<TStateBase>)this).CurrentState;
        }

        public abstract void Stop();

        public abstract void Destroy();
    }
}