using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MyFrame
{
    public class ObjectPoolData<O> : ObjectPoolData where O : class
    {
        public int MaxCapacity = -1;
        public readonly Queue<O> PoolQueue = new();
        public Func<O> CreateFunc;
        public Action<O> GetAction;
        public Action<O> PushAction;
        private ConstructorInfo _constructorInfo;
        public void Init(int maxCapacity = -1, Func<O> createFunc = null, Action<O> getAction = null, Action<O> pushAction = null)
        {
            MaxCapacity = maxCapacity;
            CreateFunc = createFunc;
            GetAction = getAction;
            PushAction = pushAction;
            _constructorInfo = typeof(O).GetConstructor(Type.EmptyTypes);
        }
        public void PushObject(O obj)
        {
            if (MaxCapacity != -1 && PoolQueue.Count >= MaxCapacity) return;
            PushAction?.Invoke(obj);
            PoolQueue.Enqueue(obj);
        }

        private O GetObject()
        {
            if(PoolQueue.Count == 0)
            {
                if(CreateFunc != null) return CreateFunc();
                return _constructorInfo?.Invoke(null) as O;
            }
            var obj = PoolQueue.Dequeue();
            GetAction?.Invoke(obj);
            return obj;
        }

        public override T GetObject<T>()
        {
            return GetObject() as T;
        }

        public override void PushObject<T>(T obj)
        {
            if (obj is not O pushObj) return;
            PushObject(pushObj);
        }
    }

    public abstract class ObjectPoolData
    {
        public abstract T GetObject<T>() where T : class;
        public abstract void PushObject<T>(T obj) where T : class;
    }
}