using System.Linq;
using SpeakSystemTree.Editor.SpeakTreeView;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;


namespace NodeSystemTree.Editor.SpeakTreeView
{
    public class NodeView : Node
    {
        public INodeViewer NodeData;
        public Port InputPort;
        public Port OutputPort;
        private VisualElement _nodeBorderBar;
        private VisualElement _nodeTitleBar;
        private VisualElement _nodeTextBar;
        private Label _titleLabel;
        public NodeView(INodeViewer nodeData) : base("Assets/MyFrame/NodeSystemTree/Editor/Resources/NodeView.uxml")
        {
            NodeData = nodeData;
            InitNode();
            InitNodeStyleData();
        }

        private void InitNodeStyleData()
        {
            _nodeTitleBar = this.Q<VisualElement>("title");
            _nodeBorderBar = this.Q<VisualElement>("node-border");
            _nodeTextBar = this.Q<VisualElement>("title-label");
            ChangeBarColor(NodeTreeSystemWindow.TreeData.ColorTable.GetNodeColor(NodeData.GetType().Name));
            ChangeTextColor(NodeTreeSystemWindow.TreeData.ColorTable.GetTitleColor(NodeData.GetType().Name));
        }
        
        public void InitNode()
        {
            var inputPortInfo = NodeData.GetInputPortInfo();
            InputPort = PortView.CreatePort<Edge>(inputPortInfo);
            if (NodeData.GetOutputPortInfo(out var outputPortInfo))
            {
                OutputPort = PortView.CreatePort<Edge>(outputPortInfo);
                outputContainer.Add(OutputPort);
            }
            inputContainer.Add(InputPort);
        }

    
        public void UpdateNodeView()
        {
            title = NodeData.NodeName;
            ChangeTextColor(NodeTreeSystemWindow.TreeData.ColorTable.GetTitleColor(NodeData.GetType().Name));
            if (Application.isPlaying && NodeData.State == NodeState.Executing)
            {
                ChangeBarColor(NodeTreeSystemWindow.TreeData.ColorTable.ExcutingNodeColor);
            }
            else
            {
                ChangeBarColor(NodeTreeSystemWindow.TreeData.ColorTable.GetNodeColor(NodeData.GetType().Name));
            }

            if (InputPort.connected && Application.isPlaying)
            {
                UpdateEdgeState();
            }
            if (NodeData.Sort())
            {
                OutputPort.DisconnectAll();
                AddEdge();
            }
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            NodeData.Position = new Vector2(newPos.x, newPos.y);
        }
    

        public void AddEdge()
        {
            TreeView tree = NodeTreeSystemWindow.Window.WindowRoot.TreeView;
            if(NodeData.GetOutPutList(out var outputNodes))
            {
                if (outputNodes == null)
                {
                    return;
                }
                foreach (var executor in outputNodes)
                {
                    LinkNodes(tree.NodeViews[executor.GUID].InputPort, OutputPort);
                }
            }
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            evt.menu.AppendAction("设为层级根节点",SetHierarchyRootNode);
        }
        
        void SetHierarchyRootNode(DropdownMenuAction obj)
        { 
            NodeTreeSystemWindow.TreeData.SelectedHierarchyInfo.SetRootNode(NodeData);
            NodeTreeSystemWindow.Window.WindowRoot.TreeView.RootNodeView = this;
            NodeTreeSystemWindow.Window.RefreshCurrentHierarchyRootNodeName();
            NodeTreeSystemWindow.Window.WindowRoot.TreeView.OrderNodes(null);
        }
        
        void LinkNodes(Port inputPort, Port outputPort)
        {
            EdgeView edge = new EdgeView(inputPort, outputPort);
            edge?.input?.Connect(edge);
            edge?.output?.Connect(edge);
            NodeTreeSystemWindow.Window.WindowRoot.TreeView.AddElement(edge);
        }

        public void AddChild(NodeView nodeView)
        {
            NodeData.Add(nodeView.NodeData);
        }

        public void RemoveChild(NodeView nodeView)
        {
            NodeData.Remove(nodeView.NodeData);
        }

        void UpdateEdgeState()
        {
            if (NodeData.State == NodeState.Executing)
            {
                (InputPort.connections.First() as EdgeView)?.OnStartMovePoint();
            }
            else
            {
                (InputPort.connections.First() as EdgeView)?.OnStopMovePoint();
            }
        }
    
        public override void OnSelected()
        {
            base.OnSelected();
            NodeTreeSystemWindow.Window.WindowRoot.Inspector.UpdateInspector();
            _nodeTitleBar.style.backgroundImage = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/NPCSystem/DialogueSystem/Sprite/T_ButtonA_ForSystem.png");
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            NodeTreeSystemWindow.Window.WindowRoot.Inspector.UpdateInspector();
            _nodeTitleBar.style.backgroundImage = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/NPCSystem/DialogueSystem/Sprite/T_ButtonA_Back.png");
            
        }

        public void ChangeBarColor(Color color)
        {
            if (_nodeBorderBar == null || _nodeTitleBar == null) return;
            _nodeBorderBar.style.unityBackgroundImageTintColor = color;
            _nodeTitleBar.style.unityBackgroundImageTintColor = color;
        }

        public void ChangeTextColor(Color color)
        {
            if (_nodeTextBar == null) return;
            _nodeTextBar.style.color = color;
        }

        public class PortView
        {
            public static Port CreatePort<T>(PortInfo portInfo) where T : Edge, new()
            {
                Port port = Port.Create<T>(portInfo.orientation, portInfo.direction, portInfo.capacity, portInfo.type);
                port.portName = "";
                return port;
            }
        }
    }
}
