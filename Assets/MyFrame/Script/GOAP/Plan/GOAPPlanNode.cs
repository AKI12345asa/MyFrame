using System;
using System.Collections.Generic;
using MyFrame;

public class GOAPPlanNode:IEquatable<GOAPPlanNode>
{
    public float H, G;
    public float F => H + G;
    public GOAPActionBase Action;   //自身Action
    public GOAPPlanNode Parent; //父节点
    public float Priority;
    public SortedSet<GOAPPlanNode> Preconditions = new(new NodeComparer()); //前置节点
    public List<GOAPTypeAndComparer> Pending = new(); //当前节点的前置条件
    public int Id = _worldId++;
    private static int _worldId = 0;
    
    private class NodeComparer : IComparer<GOAPPlanNode>
    {
        public int Compare(GOAPPlanNode x, GOAPPlanNode y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var result = x.Priority.CompareTo(y.Priority);
            return result == 0 ? x.Id.CompareTo(y.Id) : result;
        }
    }
    
    public void OnDestroy()
    {
        if (Action == null) return;
        Action = null;
        Pending.Clear();
        Pending = null;
        Parent = null;
        foreach (var node in Preconditions)
        {
            node.OnDestroy();
            node.ObjectPushPool();
        }
        Preconditions.Clear();
    }
    
    public GOAPRunState Start()
    {
        if(Action == null) return GOAPRunState.Failed;
        return Action.StartRun();
    }
    
    public GOAPRunState Update()
    {
        return Action.OnUpdate();
    }
    
    public void Stop()
    {
        Action?.OnStop();
    }
    
    public bool Equals(GOAPPlanNode other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Action.Equals(other.Action);
    }
    
    
}