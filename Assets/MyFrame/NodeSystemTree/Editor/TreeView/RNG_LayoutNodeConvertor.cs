using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JLBehaviourTree.BTAutoLayout;
using NodeSystemTree.Editor.SpeakTreeView;
using UnityEngine;

public class RNG_LayoutNodeConvertor : INodeForLayoutConvertor
{
    public float SiblingDistance => 100;
    public float TreeDistance => 80;
    public object PrimRootNode => m_PrimRootNode;
    private object m_PrimRootNode;
    private NodeAutoLayouter.TreeNode m_LayoutRootNode;
    public NodeAutoLayouter.TreeNode LayoutRootNode => m_LayoutRootNode;
    
    public INodeForLayoutConvertor Init(object primRootNode)
    {
        this.m_PrimRootNode = primRootNode;
        return this;
    }
    
    public NodeAutoLayouter.TreeNode PrimNode2LayoutNode()
    { 
        NodeView graphNodeViewerBase = m_PrimRootNode as NodeView;
        m_LayoutRootNode =
            new NodeAutoLayouter.TreeNode(150 + SiblingDistance,
                120,
                graphNodeViewerBase.NodeData.Position.y);
        
         Convert2LayoutNode(graphNodeViewerBase,
            m_LayoutRootNode, graphNodeViewerBase.NodeData.Position.y + 120,
            NodeAutoLayouter.CalculateMode.Vertical |
            NodeAutoLayouter.CalculateMode.Positive);
        return m_LayoutRootNode;
    }
    
    /// <summary>
    /// 上个节点的左下角坐标点.y
    /// </summary>
    /// <param name="rootPrimNode"></param>
    /// <param name="rootLayoutNode"></param>
    /// <param name="lastHeightPoint"></param>
    /// <param name="calculateMode"></param>
    private void Convert2LayoutNode(NodeView rootPrimNode,
        NodeAutoLayouter.TreeNode rootLayoutNode, float lastHeightPoint,
        NodeAutoLayouter.CalculateMode calculateMode)
    {
        if (rootPrimNode.OutputPort == null)return;
        if (rootPrimNode.OutputPort.connected)
        {
            foreach (var edge in rootPrimNode.OutputPort.connections)
            {
                NodeView childNode = edge.input.node as  NodeView;
                NodeAutoLayouter.TreeNode childLayoutNode = new NodeAutoLayouter.TreeNode(150 + SiblingDistance, 120, lastHeightPoint + SiblingDistance, calculateMode);
                rootLayoutNode.AddChild(childLayoutNode);
                Convert2LayoutNode(childNode, childLayoutNode, lastHeightPoint + SiblingDistance + 120, calculateMode);
            }
        }
    }
    
    public void LayoutNode2PrimNode()
    {
        Vector2 calculateRootResult = m_LayoutRootNode.GetPos();
        NodeView root = m_PrimRootNode as NodeView;
        root.SetPosition(new Rect(calculateRootResult,Vector2.one));
        root.NodeData.Position = calculateRootResult;
        
        Convert2PrimNode(m_PrimRootNode as NodeView, m_LayoutRootNode, root.NodeData.Position);
    }
    
    private void Convert2PrimNode(NodeView rootPrimNode,
        NodeAutoLayouter.TreeNode rootLayoutNode, Vector2 offset)
    {
        if (rootPrimNode.OutputPort == null)return;
        if (rootPrimNode.OutputPort.connected)
        {
            List<NodeView> children = rootPrimNode.OutputPort.connections.Select(edge=> edge.input.node as NodeView).ToList();
            for (int i = 0; i < rootLayoutNode.children.Count; i++)
            {
                Vector2 calculateResult = rootLayoutNode.children[i].GetPos();
                children[i].NodeData.Position = calculateResult;
                children[i].SetPosition(new Rect(calculateResult,Vector2.one));
                Convert2PrimNode(children[i], rootLayoutNode.children[i], offset);
            }
        }
    }
}