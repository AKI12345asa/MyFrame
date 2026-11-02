using System;
using System.Collections.Generic;
using System.Reflection;
using MyFrame;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace GOAP
{
    public class GoapValueContainer<T> : IValueBinder
    {
        public T Value;
        public Type Type => typeof(T);
    }

    public interface IValueBinder
    {
        public Type Type { get; }
    }

    public interface IFieldBinder
    {
        public void Invoke(IGOAPOwner controller, GOAPStateBase stateBase, GOAPStateBaseType stateBaseType);
    }

    public struct GoapStateBinderCache
    {
        public GoapStateBinderCache(IFieldBinder fieldBinder, GOAPStateBase goapStateBase,
            GOAPStateBaseType goapStateType)
        {
            FieldBinderCache = fieldBinder;
            GoapState = goapStateBase;
            StateType = goapStateType;
        }

        public IFieldBinder FieldBinderCache;
        public GOAPStateBase GoapState;
        public GOAPStateBaseType StateType;
    }

    public class ActionSetForGOAPBinder<T> : IFieldBinder
    {
        private Func<IGOAPOwner, T> _getFieldValueFunc;

        public ActionSetForGOAPBinder(Func<IGOAPOwner, T> getFieldValueFunc)
        {
            _getFieldValueFunc = getFieldValueFunc;
        }

        public void Invoke(IGOAPOwner controller, GOAPStateBase stateBase, GOAPStateBaseType stateBaseType)
        {
            var container = ResSystem.GetOrNew<GoapValueContainer<T>>();
            container.Value = _getFieldValueFunc(controller);
            GOAPUtility.GoapValueUpdateAction?.Invoke(controller, container, stateBase, stateBaseType);
            container.ObjectPushPool();
        }
    }
    
    public class StateAndPropertyBinder
    {
        private static string[] _chosenFieldNames;
        [ValueDropdown("_chosenFieldNames"),LabelText("字段名称")] public string FieldName;
        [LabelText("状态名称")]public string StateName;
        public GOAPStateBaseType StateType;
        private static Type _GOAPOwnerType;

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        public static void Init()
        {
            Selection.selectionChanged += () =>
            {
                if (Selection.activeGameObject == null) return;
                var controller = Selection.activeGameObject.GetComponent<IGOAPOwner>();
                if (controller == null) return;
                RefreshFields(controller.GetType());
            };
        }
#endif
    
        public static void RefreshFields(Type type)
        {
            var names = new List<string>();
            while (type != null && type != typeof(SerializedMonoBehaviour))
            {
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if(field.GetCustomAttribute<FieldForGOAPBindAttribute>() != null)
                    {
                        names.Add(field.Name);
                    }
                }
                type = type.BaseType;
            }
            _chosenFieldNames = names.ToArray();
        }
    }
    public class FieldForGOAPBindAttribute:PropertyAttribute
    {
    
    }
}
