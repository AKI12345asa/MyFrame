using System.Linq;
using MyFrame;
using Sirenix.OdinInspector;

public class GOAPPlan
{
    private GOAPPlanNode StartNode;//起始节点(其实就是最后要完成的节点)
    private GOAPPlanNode RunningNode;//当前执行节点
    public string GoalName;//该计划对应的目标名称
    [ShowInInspector,ReadOnly]public bool Running { get; private set; }
    private GOAPPlanNode RunningNodeParent => RunningNode.Parent;
    public void StartRun(string goalName,GOAPPlanNode goapNode)
    {
        //找到整个数最下层的节点
        Running = false;
        GoalName = goalName;
        StartNode = goapNode;
        StartRunNode(GetDeepestNode(StartNode));
    }

    private GOAPPlanNode GetDeepestNode(GOAPPlanNode startNode)
    {
        if (startNode == null) return null;
        if (startNode.Preconditions.Count == 0) return startNode;
        var tempNode = startNode.Preconditions.Max;
        return GetDeepestNode(tempNode);
    }

    public void OnUpdate()
    {
        var nodeState = RunningNode.Update();
        if (nodeState == GOAPRunState.Success) //执行下一个
        {
            RunningNode.Stop();
            RunningNode.Action.AfterActionEndUpdateEvent();
            //如果完成的是startNode,代表计划完成
            if (RunningNode == StartNode)
            {
                Stop();
                return;
            }
            //先运行兄弟节点
            if (RunningNodeParent != null && RunningNodeParent.Preconditions.Count > 0)
            {
                StartRunNode(RunningNodeParent.Preconditions.Max);
            }
            //无兄弟节点再运行父节点
            else
            {
                StartRunNode(RunningNodeParent);
            }
        }
        else if(nodeState == GOAPRunState.Failed)
        {
            Stop();
        }
        //不用考虑运行中
    }

    private void RecycleNodes(GOAPPlanNode node)
    {
        foreach (var child in node.Preconditions)
        {
            RecycleNodes(child);
        }
        node.Action = null;
        node.Parent = null;
        node.Preconditions.Clear();
        node.ObjectPushPool();
    }

    private void StartRunNode(GOAPPlanNode node)
    {
        RunningNode = node;
        Running = RunningNode.Start() != GOAPRunState.Failed;
        if (RunningNodeParent != null)RunningNodeParent.Preconditions.Remove(node);
        if (!Running)
        {
            RecycleNodes(StartNode);
        }
    }

    public void OnDestroy()
    {
        if (RunningNode != null)
        {
            RunningNode.Action?.OnDestroy();
        }
        if (StartNode != null)
        {
            RecycleNodes(StartNode);   
        }
    }

    public void Stop()
    {
        RunningNode?.Stop();
        Running = false;
        if(StartNode != null)RecycleNodes(StartNode);
        StartNode = null;
        GoalName = "";
        RunningNode = null;
    }
}



