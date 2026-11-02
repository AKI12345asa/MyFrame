using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JLBehaviourTree.BTAutoLayout;
using NodeTreeViewTool;
using SpeakSystemTree.Data;
using SpeakSystemTree.Editor.EditorExTools;
using SpeakSystemTree.Editor.SpeakTreeView;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace NodeSystemTree.Editor.SpeakTreeView
{
    public class TreeView : GraphView
    {
        public new class UxmlFactory : UxmlFactory<TreeView, UxmlTraits>{}

        public Dictionary<string, NodeView> NodeViews;

        public NodeView RootNodeView;
        
        public TreeView()
        {
            Insert(0,new GridBackground());
            
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            
            NodeViews = new Dictionary<string, NodeView>();
            AddRightMenuItem();
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/MyFrame/NodeSystemTree/Editor/Resources/NodeSystemTreeView.uss"));
            
            RegisterCallback<KeyDownEvent>(OnKeyDown);
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            
            graphViewChanged = OnGraphViewChanged;
            
            
        }

        public void ClearAllNodesAndEdges()
        {
            List<GraphElement> elementsToRemove = new List<GraphElement>();
            elementsToRemove.AddRange(edges.ToList());
            elementsToRemove.AddRange(nodes.ToList());
            foreach (var elementToRemove in elementsToRemove)
            {
                RemoveElement(elementToRemove);
            }
            NodeViews.Clear();
        }
        
        public new void AddElement(GraphElement element)
        {
            base.AddElement(element);
            if (element is NodeView nodeView)
            {
                NodeViews.Add(nodeView.NodeData.GUID, nodeView);
            }
         
        }

        public override void RemoveFromSelection(ISelectable selectable)
        {
            base.RemoveFromSelection(selectable);
            switch (selectable)
            {
                case NodeView nodeView:
                    NodeViews.Remove(nodeView.NodeData.GUID);
                    NodeTreeSystemWindow.TreeData.SelectedHierarchyInfo.RemoveNode(nodeView.NodeData);
                    break;
                case Edge edge:
                    edge.RemoveNode();
                    break;
            }
        }
        
        private GraphViewChange OnGraphViewChanged(GraphViewChange viewChange)
        {
            //对于创建和删除的检测一定要判空,也就是加上?号来检验
           for(int i = 0; i < viewChange.edgesToCreate?.Count; i++)
            {
                viewChange.edgesToCreate[i].ConnectNode();
                viewChange.edgesToCreate[i] = new EdgeView(viewChange.edgesToCreate[i]);
            }

            viewChange.elementsToRemove?.ForEach(element =>
            {
                switch (element)
                {
                    case Edge edge:
                        edge.RemoveNode();
                        break;
                    case NodeView view: 
                        NodeViews.Remove(view.NodeData.GUID);
                      NodeTreeSystemWindow.TreeData.SelectedHierarchyInfo.RemoveNode(view.NodeData);
                        break;
                }
            });
            return viewChange;
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            NodeTreeSystemWindow.Window.WindowRoot.Inspector.UpdateInspector();
        }

        public void OnGUI()
        {
            nodes.ForEach(n => n as NodeView).ForEach(n => n.UpdateNodeView());
        }
        
        
        
        private void OnKeyDown(KeyDownEvent evt)
        {
          NodeTreeSystemWindow.Window.WindowRoot.Inspector.UpdateInspector();
            if (evt.keyCode == KeyCode.Tab)
            {
                evt.StopPropagation();//阻止当前事件继续向下传递
            }
            if (!evt.ctrlKey) return;

            switch (evt.keyCode)
            {
                case KeyCode.C:
                    Copy(null);
                    evt.StopPropagation();
                    break;
                case KeyCode.V:
                    Paste(null);
                    evt.StopPropagation();
                    break;
                case KeyCode.E:
                    OrderNodes(null);
                    evt.StopPropagation();
                    break;
                case KeyCode.X:
                    Cut(null);
                    evt.StopPropagation();
                    break;
                case KeyCode.F:
                    FocusOnRootNode(null);
                    evt.StopPropagation();
                    break;
            }
        }

        private void Copy(DropdownMenuAction da)
        {
            SpeakSystemWindowSetting.GetSetting().CopyNodes = selection.OfType<NodeView>()
                .Select(view => view.NodeData).ToList();
            Debug.Log("Copy Success");
        }
        public void OrderNodes(DropdownMenuAction da)
        {
            NodeAutoLayouter.Layout(new RNG_LayoutNodeConvertor().Init(RootNodeView));
        }

        private void FocusOnRootNode(DropdownMenuAction da)
        {
            if (RootNodeView == null) return;
            var rootPosition = RootNodeView.GetPosition().position;
            var windowPosition = contentViewContainer.layout.size / 2;
            viewTransform.position = windowPosition - rootPosition;
            viewTransform.scale = Vector3.one;
        }
        
        public void Paste(DropdownMenuAction da)
        {
          SpeakSystemWindowSetting setting = SpeakSystemWindowSetting.GetSetting();
          if(setting == null) return;
           if (setting.CopyNodes == null) return;
           List<INodeViewer> pasteNodes = setting.CopyNodes.Clone();
           ClearSelection();
           foreach (var node in pasteNodes)
           {
               var nodeView = new NodeView(node);
               nodeView.SetPosition(new Rect(node.Position,Vector2.one));
               NodeTreeSystemWindow.TreeData.SelectedHierarchyInfo.AddNode(nodeView.NodeData);
               AddElement(nodeView);//对于节点和边都应该使用AddElement方法
           }
           nodes.OfType<NodeView>().ToList().ForEach(n => n.AddEdge());
        }

        private void Cut(DropdownMenuAction da)
        {
            Copy(da);
            CutSelectionCallback();
        }

       


        #region 右键菜单


        private RightMenuItem _rightMenuItem;

        private void AddRightMenuItem()
        {
            _rightMenuItem = ScriptableObject.CreateInstance<RightMenuItem>();
            _rightMenuItem.OnMenuClicked += OnMenuClicked;

            nodeCreationRequest += context =>
            {
                SearchWindow.Open(new SearchWindowContext(context.screenMousePosition),_rightMenuItem);
            };
        }

        private bool OnMenuClicked(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            var windowRoot = NodeTreeSystemWindow.Window.rootVisualElement;
            var windowMousePosition = windowRoot.WorldToLocal(context.screenMousePosition);
            var graphViewMousePosition = contentViewContainer.WorldToLocal(windowMousePosition);
            var nodeBase = Activator.CreateInstance((Type)searchTreeEntry.userData) as INodeViewer;
            var nodeLabel = ((Type)searchTreeEntry.userData).GetCustomAttribute<NodeExtension.NodeLabelAttribute>();
            if (nodeBase == null)
            {
                return false;
            }
            if (nodeLabel != null && nodeLabel.Label != "")
            {
                nodeBase.NodeName = nodeLabel.Label;
            }
            nodeBase.Position = graphViewMousePosition;
            nodeBase.GUID = Guid.NewGuid().ToString();
            nodeBase.TreeID = NodeTreeSystemWindow.TreeData.Guid;
            nodeBase.InitOnEditorWindow((INodeTreeDataOwner)NodeTreeSystemWindow.TreeData.NodeTreeDataOwner);
            var nodeView = new NodeView(nodeBase);
            nodeView.SetPosition(new Rect(graphViewMousePosition, Vector2.one));
            AddElement(nodeView);
            NodeTreeSystemWindow.TreeData.SelectedHierarchyInfo.AddNode(nodeBase);
            return true;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
           evt.menu.AppendAction("Create Node", _ =>
           {
               var windowRoot = NodeTreeSystemWindow.Window.rootVisualElement;
               
               var mousePosition = windowRoot.ChangeCoordinatesTo(windowRoot.parent,
                   _.eventInfo.mousePosition + NodeTreeSystemWindow.Window.position.position);
                SearchWindow.Open(new SearchWindowContext(mousePosition),_rightMenuItem);
           });
           evt.menu.AppendAction("Paste",Paste);
           evt.menu.AppendAction("Cut",Cut);
           evt.menu.AppendAction("Copy",Copy);
           evt.menu.AppendAction("Order Nodes",OrderNodes);
           base.BuildContextualMenu(evt);
        }

        #endregion

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.Where(endport 
                => endport != startPort &&
                startPort.portType.IsAssignableFrom(((NodeView)endport.node).NodeData.GetType()) &&
                    endport.node != startPort.node && 
                    endport.direction != startPort.direction)
                .ToList();
        }
    }
}
