using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class GOAPWindowTool
{
    public static GlobalObjectId AgentId;
    public static string ComponentType;
    const string AgentIdKey = "GOAPWindow.AgentId";
    const string AgentTypeKey = "GOAPWindow.AgentType";
    static GOAPWindowTool()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                CaptureAgent(GOAPEditorWindow.Window?.Agent);
            }
            
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                CaptureAgent(GOAPEditorWindow.Window?.Agent);
            }

            if (state == PlayModeStateChange.EnteredEditMode)
            {
                GOAPEditorWindow.ReLoadWindow();
            }
        };
    }

    public static void CaptureAgent(GOAPAgent agent)
    {
        if (agent == null) return;
        AgentId = GlobalObjectId.GetGlobalObjectIdSlow(agent);
        SessionState.SetString(AgentIdKey, AgentId.ToString());
        ComponentType = agent.GetType().AssemblyQualifiedName;
        SessionState.SetString(AgentTypeKey, ComponentType);
        Debug.Log("记录");
    }

    public static GOAPAgent GetAgent()
    {
        var savedId = SessionState.GetString(AgentIdKey, string.Empty);
        if (GlobalObjectId.TryParse(savedId, out var parsedId)) AgentId = parsedId;
        var agent = (GOAPAgent)GlobalObjectId.GlobalObjectIdentifierToObjectSlow(AgentId);
        if (agent != null)
        {
            Debug.Log("使用");
            return agent;
        }
        return null;
    }
    /// <summary>
    /// 不能用于接口，只能用于类，且只返回直接子类
    /// </summary>
    /// <param name="rootType"></param>
    /// <returns></returns>
    public static List<Type> GetNextClasses(this Type rootType)
    {
        var typeList = new List<Type>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsClass && rootType.IsAssignableFrom(type) && type.BaseType.FullName == rootType.FullName)
                {
                    typeList.Add(type);
                }
            }
        }
        return typeList;
    } 
    public static List<Type> GetSubClasses(this Type rootType)
    {
        var typeList = new List<Type>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsClass && !type.IsAbstract && rootType.IsAssignableFrom(type))
                {
                    typeList.Add(type);
                }
            }
        }
        return typeList;
    } 
    public static List<Type> GetAbstractSubClasses(this Type rootType)
    {
        var typeList = new List<Type>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsClass && type.IsAbstract && rootType.IsAssignableFrom(type))
                {
                    typeList.Add(type);
                }
            }
        }
        return typeList;
    }
}
