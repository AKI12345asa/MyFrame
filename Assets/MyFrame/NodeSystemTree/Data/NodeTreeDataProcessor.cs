using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

public enum ParameterType
{
    Int,
    Float,
    Bool,
    Trigger,
}

public enum ContrastType
{
    Greater,
    Less,
    Equal,
    True,
    False,
    Trigger,
}

public class Condition:IDeepCopy<Condition>
{
    public string ParameterID;
    public float CompareValueForFloat;
    public int CompareValueForInt;
    public ContrastType Contrast;
    public Condition DeepCopy()
    {
        return new Condition()
        {
            ParameterID = ParameterID,
            CompareValueForFloat = CompareValueForFloat,
            CompareValueForInt = CompareValueForInt,
            Contrast = Contrast,
        };
    }
}
[Serializable]
public class ParameterForTree:IDeepCopy<ParameterForTree>
{
    public ParameterType Type;
    public string Guid;
    public string Name;
    [SerializeField,HideInInspector]private bool boolValue;
    [SerializeField,HideInInspector]private bool triggerValue;
    [SerializeField,HideInInspector]private int intValue;
    [SerializeField,HideInInspector]private float floatValue;
    public ParameterForTree(ParameterType type, string name)
    {
        Type = type;
        Name = name;
        Guid = GUID.Generate().ToString();
    }

    private ParameterForTree(){}
    
    public ParameterForTree(ParameterForTree other)
    {
        Type = other.Type;
        Name = other.Name;
        Guid = other.Guid;
        switch (other.Type)
        {
            case ParameterType.Bool:
                boolValue = other.boolValue;
                break;
            case ParameterType.Trigger:
                triggerValue = other.triggerValue;
                break;
            case ParameterType.Int:
                intValue = other.intValue;
                break;
            case ParameterType.Float:
                floatValue = other.floatValue;
                break;
        }
    }
    
    public object GetValue()
    {
        return Type switch
        {
            ParameterType.Int => intValue,
            ParameterType.Float => floatValue,
            ParameterType.Bool => boolValue,
            ParameterType.Trigger => triggerValue,
            _ => null
        };
    }
    public void SetValue(object value)
    {
        switch (Type)
        {
            case ParameterType.Int:
                intValue = (int)value;
                break;
            case ParameterType.Float:
                floatValue = (float)value;
                break;
            case ParameterType.Bool:
                boolValue = (bool)value;
                break;
            case ParameterType.Trigger:
                triggerValue = (bool)value;
                break;
        }
    }

    public ParameterForTree DeepCopy()
    {
        return new ParameterForTree()
        {
            Type = Type,
            Name = Name,
            Guid = Guid,
            floatValue = floatValue,
            boolValue = boolValue,
            intValue = intValue,
            triggerValue = triggerValue
        };
    }
}



    public class NodeTreeDataProcessor : SerializedMonoBehaviour
    {
        [NonSerialized]public List<ParameterForTree> ParameterList = new();

        public virtual void Init(List<ParameterForTree> parameterList)
        {
            ParameterList = parameterList.Select(parameter => parameter.DeepCopy()).ToList();
        }
        
        #region 条件节点检测
        public bool RegisterCondition(Condition condition,bool isCheck = false)
        {
            var parameter = FindParameter(condition.ParameterID);
            if (parameter == null)
            {
                throw new Exception("Parameter not found");
            }

            return parameter.Type switch
            {
                ParameterType.Bool => BoolConditionCheck(condition,parameter),
                ParameterType.Float => FloatConditionCheck(condition,parameter),
                ParameterType.Int => IntConditionCheck(condition,parameter),
                ParameterType.Trigger => TriggerConditionCheck(condition,parameter,isCheck),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public ParameterForTree FindParameter(string guid)
        {
           return ParameterList.Find(x => x.Guid == guid);
        }
        
        public bool FloatConditionCheck(Condition condition,ParameterForTree parameter)
        {
            return condition.Contrast switch
            {
                ContrastType.Greater => parameter.GetValue() is float x && x > condition.CompareValueForFloat,
                ContrastType.Less => parameter.GetValue() is float x && x < condition.CompareValueForFloat
            };
        }

        public bool IntConditionCheck(Condition condition,ParameterForTree parameter)
        {
            return condition.Contrast switch
            {
                ContrastType.Greater => parameter.GetValue() is int x && x > condition.CompareValueForInt,
                ContrastType.Less => parameter.GetValue() is int x && x < condition.CompareValueForInt,
                ContrastType.Equal => parameter.GetValue() is int x && x == condition.CompareValueForInt,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public bool BoolConditionCheck(Condition condition,ParameterForTree parameter)
        {
            return condition.Contrast switch
            {
                ContrastType.True => parameter.GetValue() is true,
                ContrastType.False => parameter.GetValue() is false,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public bool TriggerConditionCheck(Condition condition,ParameterForTree parameter,bool isCheck)
        {
            if (parameter.GetValue() is bool trigger and true)
            {
                if(!isCheck)parameter.SetValue(false);
                return true;
            }
            return false;
        }

        #endregion

        #region 参数操作

        public string GetParameterGuid(string name)
        {
            if (ParameterList.Find(x => x.Name == name) is { } parameter)
            {
                return parameter.Guid;
            }
            throw new Exception("Parameter not found");
        }
        
        public void SetFloat(string Guid, float value)
        {
            var parameter = ParameterList.Find(x => x.Guid == Guid);
            if (parameter == null || parameter.Type != ParameterType.Float)
            {
                throw new Exception("Parameter not found or type mismatch");
            }
            parameter.SetValue(value);
        }

        public void SetInt(string Guid, int value)
        {
            var parameter = ParameterList.Find(x => x.Guid == Guid);
            if (parameter == null || parameter.Type != ParameterType.Int)
            {
                throw new Exception("Parameter not found or type mismatch");
            }
            parameter.SetValue(value);
        }

        public void SetBool(string Guid, bool value)
        {
            var parameter = ParameterList.Find(x => x.Guid == Guid);
            if (parameter == null || parameter.Type != ParameterType.Bool)
            {
                throw new Exception("Parameter not found or type mismatch");
            }
            parameter.SetValue(value);
        }

        public void SetTrigger(string Guid)
        {
            var parameter = ParameterList.Find(x => x.Guid == Guid);
            if (parameter == null || parameter.Type != ParameterType.Trigger)
            {
                throw new Exception("Parameter not found or type mismatch");
            }
            parameter.SetValue(true);
        }
        #endregion
    }
