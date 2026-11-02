using System;
using System.Collections.Generic;

public static class GOAPObjectPool
{
    
    private static Dictionary<Type, Stack<object>> _pools = new();


    public static T Get<T>() where T : class
    {
        if (_pools.TryGetValue(typeof(T), out var pool) && pool.Count > 0)
        {
            return pool.Pop() as T;
        }
        return default(T);
    }

    public static T GetOrNew<T>() where T : class, new()
    {
        var obj = Get<T>();
        if (obj == null)
        {
            obj = new T();
        }
        return obj;
    }
    
    public static void RecycleObject<T>(T obj) where T : class
    {
        if (_pools.TryGetValue(typeof(T), out var pool))
        {
            pool.Push(obj);
        }
        else
        {
            var stack = new Stack<object>();
            _pools.Add(typeof(T), stack);
            stack.Push(obj);
        }
    }
}