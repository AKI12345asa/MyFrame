using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyFrame
{
    public class MonoSystem:MonoBehaviour
    {
        private MonoSystem(){}
        private static MonoSystem _instance;
        private static event Action updateAction;
        private static event Action fixedUpdateAction;
        private static event Action lateUpdateAction;

        public static void Init()
        {
            _instance = MyFrameRoot.Instance.GetComponent<MonoSystem>();
            updateAction = null;
            fixedUpdateAction = null;
            lateUpdateAction = null;
            PoolSystem.InitObjectPool<List<Coroutine>>("MonoSystem_Coroutines",defaultQuantity:10,pushAction:list=>list.Clear(),getAction:list => list.Clear());
        }
        private void Update()
        {
            updateAction?.Invoke();
        }

        private void FixedUpdate()
        {
            fixedUpdateAction?.Invoke();
        }

        private void LateUpdate()
        {
            lateUpdateAction?.Invoke();
        }
        
        
        #region 生命周期函数挂载
        public static void AddUpdateAction(Action action)
        {
            updateAction += action;
        }

        public static void RemoveUpdateAction(Action action)
        {
            updateAction -= action;
        }
        
        public static void AddFixedUpdateAction(Action action)
        {
            fixedUpdateAction += action;
        }

        public static void RemoveFixedUpdateAction(Action action)
        {
            fixedUpdateAction -= action;
        }

        public static void AddLateUpdateAction(Action action)
        {
            lateUpdateAction += action;
        }

        public static void RemoveLateUpdateAction(Action action)
        {
            lateUpdateAction -= action;
        }
        #endregion
        
        #region 携程挂载
        private static Dictionary<object,List<Coroutine>> _coroutineDic = new Dictionary<object,List<Coroutine>>();
        
        
        
        public static Coroutine Start_Coroutine(IEnumerator routine)
        {
           return _instance.StartCoroutine(routine);
        }

        public static void Start_Coroutine(object obj, IEnumerator routine)
        {
            var coroutine = _instance.StartCoroutine(routine);
            if (_coroutineDic.TryGetValue(obj, out var list))
            {
                list.Add(coroutine);
            }
            else
            {
                list = new List<Coroutine>() { coroutine };
                _coroutineDic.Add(obj, list);
            }
        }

        public static void Stop_Coroutine(object obj,Coroutine routine)
        {
            if (_coroutineDic.TryGetValue(obj, out var list))
            {
                _instance.StopCoroutine(routine);
                list.Remove(routine);
            }
        }

        public static void Stop_Coroutine(object obj)
        {
            if (_coroutineDic.TryGetValue(obj, out var list))
            {
                list.ForEach(coroutine => _instance.StopCoroutine(coroutine));
                PoolSystem.PushObject(list,"MonoSystem_Coroutines");
                _coroutineDic.Remove(obj);
            }
        }

        public static void StopAll_Coroutine()
        {
            foreach(var list in _coroutineDic.Values)
            {
                list.ForEach(coroutine => _instance.StopCoroutine(coroutine));
                PoolSystem.PushObject(list,"MonoSystem_Coroutines");
            }
            _coroutineDic.Clear();
            _instance.StopAllCoroutines();
        }
        #endregion
    }
}