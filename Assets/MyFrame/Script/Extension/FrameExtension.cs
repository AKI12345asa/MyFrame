using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MyFrame
{
    public static class FrameExtension
    {
        #region objct拓展

        public static void GameObjectPushPool(this GameObject go, string poolName)
        {
            PoolSystem.PushGameObject(poolName, go);
        }

        public static void GameObjectPushPool(this GameObject go)
        {
            PoolSystem.PushGameObject(go);
        }

        public static void GameObjectPushPool(this Component component)
        {
            PoolSystem.PushGameObject(component.gameObject);
        }

        public static void GameObjectPushPool(this Component component, string poolName)
        {
            PoolSystem.PushGameObject(poolName,component.gameObject);
        }
        
        public static void ObjectPushPool<T>(this T obj, string poolName) where T : class
        {
            PoolSystem.PushObject(obj, poolName);
        }

        public static void ObjectPushPool<T>(this T obj) where T : class 
        {
            PoolSystem.PushObject(obj);
        }
        #endregion

        #region Mono拓展

        public static void AddUpdate(this object obj, Action action)
        {
            MonoSystem.AddUpdateAction(action);
        }

        public static void RemoveUpdate(this object obj, Action action)
        {
            MonoSystem.RemoveUpdateAction(action);
        }
        public static void AddLateUpdate(this object obj, Action action)
        {
            MonoSystem.AddLateUpdateAction(action);
        }

        public static void RemoveLateUpdate(this object obj, Action action)
        {
            MonoSystem.RemoveLateUpdateAction(action);
        }

        public static void AddFixedUpdate(this object obj, Action action)
        {
            MonoSystem.AddFixedUpdateAction(action);
        }

        public static void RemoveFixedUpdate(this object obj, Action action)
        {
            MonoSystem.RemoveFixedUpdateAction(action);
        }

        public static void AddCoroutine(this object obj, IEnumerator routine)
        {
            MonoSystem.Start_Coroutine(obj, routine);
        }
        
        public static void StopCoroutine(this object obj, Coroutine routine)
        {
            MonoSystem.Stop_Coroutine(obj, routine);
        }
        
        public static void StopAllCoroutine(this object obj)
        {
            MonoSystem.Stop_Coroutine(obj);
        }
        #endregion

        #region 类反射

       public static List<Type> GetDerivedClasses(this Type type,bool canBeAbstract = true,bool containSelf = false)
        {
            List<Type> derivedClasses = new List<Type>();
        
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var t in assembly.GetTypes())
                {
                    if (t.IsClass && t.IsAbstract && t.IsGenericType && type.IsAssignableFrom(t))
                    {
                        derivedClasses.AddRange(GetTypeUnderTheGenericType(t));
                    } 
                    else if (t.IsClass && type.IsAssignableFrom(t))
                    {
                        if(t == type && !containSelf) continue;
                        if(!canBeAbstract && t.IsAbstract) continue;
                        derivedClasses.Add(t);
                    }
                }
            }
            return derivedClasses;
        }

        public static List<Type> GetTypeUnderTheGenericType(Type type)
        {
            var definition = type.GetGenericTypeDefinition();
            var typeList = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var t in assembly.GetTypes())
                {
                    if (t.BaseType == null || !t.BaseType.IsGenericType) continue;
                    if (t.BaseType.GetGenericTypeDefinition() == definition && t != type)
                    {
                        typeList.Add(t);
                    }
                }
            }
            return typeList;
        }
        
        public static List<Type> GetAbstractClasses(this Type type)
        {
            List<Type> derivedClasses = new List<Type>();
        
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var t in assembly.GetTypes())
                {
                    if (t.IsClass && t.IsAbstract && type.IsAssignableFrom(t) && t != type)
                    {
                        derivedClasses.Add(t);
                    }
                }
            }
            return derivedClasses;
        }

        #endregion
    }
}