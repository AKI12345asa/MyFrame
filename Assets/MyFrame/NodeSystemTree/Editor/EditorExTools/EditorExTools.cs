using System;
using NodeSystemTree.Editor.SpeakTreeView;
using UnityEditor.Experimental.GraphView;

namespace SpeakSystemTree.Editor.EditorExTools
{
    public static class NodeTreeSystemWindowUtility
    {
        public static void ConnectNode(this Edge edge)
        {
            NodeView input = edge.input.node as NodeView;
            NodeView output = edge.output.node as NodeView;
            output?.AddChild(input);
        }

        public static void RemoveNode(this Edge edge)
        {
            NodeView input = edge.input.node as NodeView;
            NodeView output = edge.output.node as NodeView;
            output.RemoveChild(input);
        }
    }
}

