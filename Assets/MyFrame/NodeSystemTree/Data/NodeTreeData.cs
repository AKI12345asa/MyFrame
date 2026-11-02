using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Object = UnityEngine.Object;

public abstract class HierarchyInfo
{
    public string HierarchyName;
    public abstract INodeViewer GetRootNode();
    public abstract void SetRootNode(INodeViewer node);
    [HideInInspector]public WindowData WindowData;
    public abstract List<INodeViewer> GetHierarchyNodes();
    
    public abstract void AddNode(INodeViewer node);
    
    public abstract void RemoveNode(INodeViewer node);
}

public abstract class HierarchyInfo<T,O,H> : HierarchyInfo,IDeepCopy<H> where T : INodeViewer<T,O> where O : INodeTreeDataOwner where H : HierarchyInfo<T,O,H>
{
    public T RootNode;
    public List<T> HierarchyNodes = new ();
    public override INodeViewer GetRootNode()
    {
        return RootNode;
    }

    public override void SetRootNode(INodeViewer node)
    {
        RootNode = (T)node;
    }

    public override List<INodeViewer> GetHierarchyNodes()
    {
        return HierarchyNodes.Cast<INodeViewer>().ToList();
    }

    public override void AddNode(INodeViewer node)
    {
        HierarchyNodes.Add((T)node);   
    }

    public override void RemoveNode(INodeViewer node)
    {
        HierarchyNodes.Remove((T)node);
    }

    public abstract H DeepCopy();
}


    [BoxGroup("节点树数据")]
    [HideReferenceObjectPicker]
    public abstract class NodeTreeData
    {
        public static NodeTreeData CurrenEditorTreeData;
        [LabelText("节点宿主"), Required("先给予宿主才能使用"), ShowIf("@!IfOwnerIsRight()")]
        public Object NodeTreeDataOwner;
        [HideInInspector] public HierarchyInfo SelectedHierarchyInfo;
        [LabelText("节点颜色配置表"),OdinSerialize]
        public NodeColorTable ColorTable;
        [HideInInspector]public string Guid;
        public abstract void InitHierarchInfoList();
        
        public abstract List<ParameterForTree> GetParameterForTree();
        
        public abstract List<HierarchyInfo> GetHierarchInfoList();
        
        public abstract HierarchyInfo CreateHierarchyInfo();

        public abstract void AddHierarchyInfo(HierarchyInfo info);
        
        public abstract void RemoveHierarchyInfo(HierarchyInfo info);

        public abstract void InitNodesOnEditorWindow();
        
        public abstract void RemoveHierarchyInfoAtIndex(int index);

        public abstract void IndexChange(int index1, int index2);
            
        public abstract int IndexofHierarch(HierarchyInfo info);

        public abstract bool IfOwnerIsRight();
        public abstract Type GetNodeType();
    }
    
    [Serializable]
    public struct WindowData
    {
        public Vector2 Scale;
    
        public Vector2 Position;
    }

public abstract class NodeTreeData<T,H,O,D> : NodeTreeData,IDeepCopy<D> where T : INodeViewer<T,O> where H : HierarchyInfo<T,O,H> , new() where O : Object,INodeTreeDataOwner where D : NodeTreeData<T,H,O,D>
{
    [OdinSerialize]public List<H> HierarchyInfoList;
    public O Owner => (O)NodeTreeDataOwner;
    public override Type GetNodeType()
    {
        return typeof(T);
    }

    public override void InitHierarchInfoList()
    {
        if (HierarchyInfoList == null)
        {
            HierarchyInfoList = new List<H>();
        }
    }

    public override List<HierarchyInfo> GetHierarchInfoList()
    {
       return HierarchyInfoList.Cast<HierarchyInfo>().ToList();
    }

    public virtual List<T> GetSelectedHierarchyInfoNodes()
    {
        return SelectedHierarchyInfo.GetHierarchyNodes().Cast<T>().ToList();
    }

    public override void InitNodesOnEditorWindow()
    {
        HierarchyInfoList.ForEach(info => info.HierarchyNodes.ForEach(node => node.InitOnEditorWindow(Owner)));
    }
    
    public override HierarchyInfo CreateHierarchyInfo()
    {
        return new H();
    }

    public override void AddHierarchyInfo(HierarchyInfo info)
    {
        HierarchyInfoList.Add((H)info);
    }

    public override void RemoveHierarchyInfo(HierarchyInfo info)
    {
        HierarchyInfoList.Remove((H)info);
    }

    public override void RemoveHierarchyInfoAtIndex(int index)
    {
        HierarchyInfoList.RemoveAt(index);
    }
    
    public override void IndexChange(int index1, int index2)
    {
        (HierarchyInfoList[index1], HierarchyInfoList[index2]) = (HierarchyInfoList[index2], HierarchyInfoList[index1]);
    }

    public override int IndexofHierarch(HierarchyInfo info)
    {
        return HierarchyInfoList.IndexOf((H)info);
    }
    public override bool IfOwnerIsRight()
    {
        if (NodeTreeDataOwner is not O)
        {
            Debug.Log("宿主类型错误无法开启节点");
            return false;
        }
        return true;
    }
    public override List<ParameterForTree> GetParameterForTree()
    {
        return Owner.GetParameterList();
    }

    public abstract D DeepCopy();
}


