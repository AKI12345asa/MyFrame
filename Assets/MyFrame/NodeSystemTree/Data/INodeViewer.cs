using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
public enum NodeState
{
    Waiting,
    Executing,
    Finished,
    Failed,
    Stop,
    Successful,
}

public class PortInfo
{
    public PortInfo(Orientation orientation, Direction direction, Port.Capacity capacity, System.Type type)
    {
        this.orientation = orientation;
        this.direction = direction;
        this.capacity = capacity;
        this.type = type;
    } 
    
    public Orientation orientation;
    public Direction direction;
    public Port.Capacity capacity;
    public System.Type type;
}


public interface INodeViewer
{
    public void Init();

    public void InitOnEditorWindow(INodeTreeDataOwner owner);
    
    public void Add(INodeViewer node);

    public void Remove(INodeViewer node);

    public PortInfo GetInputPortInfo();
    
    public  bool GetOutputPortInfo(out PortInfo outputPortInfo);
    
    public bool GetOutPutList(out List<INodeViewer> outputNodes);

    public bool Sort();
    
    public void ChangeCurrentList(List<INodeViewer> changeList);

    public bool TryToGetConditions(out List<Condition> conditions);
    
   Vector2 Position { get; set; }
   
   string NodeName { get; set; }
   
   NodeState State { get; set; }

   string GUID { get; set; }
   
   string TreeID { get; set; }
}

public interface INodeViewer<T,O> : INodeViewer,IDeepCopy<T> where T : INodeViewer<T,O> where O : INodeTreeDataOwner
{
    public abstract O NodeOwner { get; set; }
}

public interface INodeTreeDataOwner
{
    public List<ParameterForTree> GetParameterList();
}