using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using MyFrame;
using UnityEngine;


namespace GOAP
{
    public static class GOAPUtility
    {
        private static ConcurrentDictionary<string, Dictionary<string, IFieldBinder>> _goapStateBindDic = new();
        public static Action<IGOAPOwner, IValueBinder, GOAPStateBase, GOAPStateBaseType> GoapValueUpdateAction;
        
        [RuntimeInitializeOnLoadMethod]
        public static void Initialize()
        {
            var methodInfo = typeof(IGOAPOwner).GetMethod("UpdateGoapStates");
            var controllerParam = Expression.Parameter(typeof(IGOAPOwner), "GOAPOwner");
            var valueBinderParam = Expression.Parameter(typeof(IValueBinder), "valueBinder");
            var stateBaseParam = Expression.Parameter(typeof(GOAPStateBase), "stateBase");
            var stateTypeParam = Expression.Parameter(typeof(GOAPStateBaseType), "stateBaseType");
            var callExpr = Expression.Call(controllerParam, methodInfo, valueBinderParam,stateBaseParam,stateTypeParam);
            var lamba = Expression.Lambda<Action<IGOAPOwner,IValueBinder,GOAPStateBase,GOAPStateBaseType>>(callExpr, controllerParam, valueBinderParam,stateBaseParam,stateTypeParam);
            GoapValueUpdateAction = lamba.Compile();
        }
        
    public static Dictionary<string,IFieldBinder> InitCurrentGoapStateBindList(IGOAPOwner controllerForGoap,string ownerID = null) //ID用于缓存来节约反射占时,可以不写
    {
        var type = controllerForGoap.GetType();
        if (!string.IsNullOrEmpty(ownerID) && _goapStateBindDic.TryGetValue(ownerID, out var dic))
        {
            return  dic;
        }
        var fieldDic = new Dictionary<string, IFieldBinder>();
        foreach (var binder in controllerForGoap.Binders)
        {
            switch (binder.StateType)
            {
                case GOAPStateBaseType.Int:
                    var funcInt = GetBinderGetter<int>(binder,type,typeof(int));
                    var intBinder = new ActionSetForGOAPBinder<int>(funcInt);
                    fieldDic.Add(binder.FieldName,intBinder);
                    break;
                case GOAPStateBaseType.Float:
                    var funcFloat = GetBinderGetter<float>(binder,type,typeof(float));
                    var floatBinder = new ActionSetForGOAPBinder<float>(funcFloat);
                    fieldDic.Add(binder.FieldName,floatBinder);
                    break;
                case GOAPStateBaseType.Bool:
                    var funcBool = GetBinderGetter<bool>(binder,type,typeof(bool));
                    var boolBinder = new ActionSetForGOAPBinder<bool>(funcBool);
                    fieldDic.Add(binder.FieldName,boolBinder);
                    break;
                case GOAPStateBaseType.Object:
                    var funcObject = GetBinderGetter<UnityEngine.Object>(binder,type,typeof(UnityEngine.Object));
                    var objectBinder = new ActionSetForGOAPBinder<UnityEngine.Object>(funcObject);
                    fieldDic.Add(binder.FieldName,objectBinder);
                    break;
            }
            if (!string.IsNullOrEmpty(ownerID))
            {
                _goapStateBindDic.GetOrAdd(ownerID,fieldDic);
            }
        }
        return fieldDic;
    }

    public static Func<IGOAPOwner,T> GetBinderGetter<T>(StateAndPropertyBinder binder,Type ownerType,Type fieldType)
    {
        var field = GetFieldInfo(binder.FieldName,ownerType);
        if (field == null)
        {
            Debug.Log("未找到字段:" + binder.FieldName);
            return null;
        }
        if (field.FieldType != fieldType)
        {
            Debug.Log("字段类型不匹配:" + binder.FieldName);
            return null;
        }
        var instanceParam = Expression.Parameter(typeof(IGOAPOwner), "instance");
        var memberAccess = Expression.Field(Expression.Convert(instanceParam, ownerType), field);
        var lambda = Expression.Lambda<Func<IGOAPOwner, T>>(memberAccess, instanceParam);
        return lambda.Compile();
    }
    
    public static FieldInfo GetFieldInfo(string fieldName,Type type)
    {
        while (type != null && type != typeof(MonoBehaviour))
        {
            var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null) return field;
            type = type.BaseType;
        }
        return null;
    }

    }
}
