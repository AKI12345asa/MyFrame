using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MyFrame;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
[Serializable]
public class NodeColor
{
   public NodeColor(string typeName, Color nodeColor, Color titleColor)
    {
        TypeName = typeName;
        Nodecolor = nodeColor;
        Titlecolor = titleColor;
    }

   public NodeColor(string typeName)
    {
        TypeName = typeName;
    }
    
    [ColorUsage(true, false)]
    public Color Nodecolor;  
    
    [ColorUsage(true, false)]
    public Color Titlecolor;
    
    public string TypeName;
}

[CreateAssetMenu(fileName = "ColorTable", menuName = "SpeakSystemTree/NodeColorTable")]
public class NodeColorTable : SerializedScriptableObject
{
    [LabelText("节点颜色"),OdinSerialize,HideReferenceObjectPicker,BoxGroup("颜色配置")]
    public List<NodeColor> NodeColors = new List<NodeColor>();

    [BoxGroup("颜色配置"), OdinSerialize,  HideReferenceObjectPicker]
    public Color DefaultNodeColor;
    [BoxGroup("颜色配置"), OdinSerialize,  HideReferenceObjectPicker]
    public Color DefaultTitleColor;
    
    [BoxGroup("颜色配置"),OdinSerialize,  HideReferenceObjectPicker]
    public Color ExcutingNodeColor; 
    
    [OdinSerialize] public Type BaseType;
    
    [Button("创建默认节点颜色")]
    public void GetDefaultColor()
    {
        if (BaseType == null) return;

        List<Type> types = BaseType.GetDerivedClasses(false);
    
        foreach (var type in types)
        {
            if (NodeColors.Any(x => x.TypeName == type.Name)) continue;
            NodeColors.Add(new NodeColor(type.Name));
        }
    }
    
    public Color GetNodeColor(string typeName)
    {
        NodeColor nc = NodeColors.FirstOrDefault(x => x.TypeName == typeName);
        if (nc != null)
            return nc.Nodecolor;
        else
        {
            return Color.white;
        }
    }
        
    public Color GetTitleColor(string typeName)
    {
        NodeColor nc = NodeColors.FirstOrDefault(x => x.TypeName == typeName);
        if (nc != null)
        {
            return nc.Titlecolor;
        }
        else
        {
            return Color.white;
        }
    }
}
