using System;
using System.Collections.Generic;
using System.Linq;
using MyFrame;
using NodeSystemTree.Editor.SpeakTreeView;
using NodeTreeViewTool;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;


public class OpenNodeViewAttributeDrawer<T> : OdinAttributeDrawer<OpenNodeViewAttribute, T> where T : NodeTreeData
{
   protected override void Initialize()
   {
      base.Initialize();
   }

   protected override void DrawPropertyLayout(GUIContent label)
   {
      CallNextDrawer(label);
      if (ValueEntry.SmartValue.NodeTreeDataOwner != null && ValueEntry.SmartValue.IfOwnerIsRight()) 
      {
         if (GUILayout.Button(Attribute.MuneName))
         {
            if (string.IsNullOrEmpty(ValueEntry.SmartValue.Guid))
            {
               ValueEntry.SmartValue.Guid = Guid.NewGuid().ToString();
            }
            if (ValueEntry.SmartValue.GetHierarchInfoList() == null)
            {
               ValueEntry.SmartValue.InitHierarchInfoList();
            }
            NodeTreeData.CurrenEditorTreeData =  ValueEntry.SmartValue;
            ValueEntry.SmartValue.InitNodesOnEditorWindow();
            NodeTreeSystemWindow.TreeData = ValueEntry.SmartValue;
            NodeTreeSystemWindow.OpenWindow();
         }
      }
      else
      {
         EditorGUILayout.HelpBox("请给予正确的节点宿主",MessageType.Warning);
      }
   }
}
[InitializeOnLoad]
public static class PlayModeStateChangedExample
{
   static PlayModeStateChangedExample()
   {
      EditorApplication.playModeStateChanged += LogPlayModeState;
   }

   private static void LogPlayModeState(PlayModeStateChange state)//让窗口在进入和退出播放模式时刷新
   {
      if (!NodeTreeSystemWindow.Window) return;
      if (state != PlayModeStateChange.ExitingEditMode && state != PlayModeStateChange.EnteredPlayMode) return;
      NodeTreeSystemWindow.Window.Close();
      
   }
}
namespace NodeSystemTree.Editor.SpeakTreeView
{
   public class NodeTreeSystemWindow : EditorWindow
   {
      public static NodeTreeSystemWindow Window;
      public static NodeTreeData TreeData;
      public SplitView WindowRoot;
      public ListView HierarchyListView;
      
      [MenuItem("Tools/SpeakSystemTreeView _&i")]
      public static void OpenWindow()
      {
         Window = GetWindow<NodeTreeSystemWindow>();
         Window.titleContent = new GUIContent("SpeakSystemTreeView");
      }

      public void InitSpeakWindow()
      {
         Window = this;
         var root = rootVisualElement;
         var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/MyFrame/NodeSystemTree/Editor/Resources/NodeSystemTreeView.uxml");
         visualTree.CloneTree(root);
         WindowRoot = root.Q<SplitView>("SpeakSplitView"); 
         WindowRoot.TreeView = WindowRoot.Q<TreeView>("SpeakTreeView");
         WindowRoot.Inspector = WindowRoot.Q<Inspector>("SpeakInspector");
         WindowRoot.HierarchyView = WindowRoot.Q<ListView>("HierarchyPanel");
         WindowRoot.ChangeToNodeInspector = WindowRoot.Q<ToolbarButton>("ChangeToNodeInspector");
         WindowRoot.ChangeToParameterInspector = WindowRoot.Q<ToolbarButton>("ChangeToParameterList"); 
         WindowRoot.CurrentHierarchyRootNodeName = WindowRoot.Q<Label>("CurrentHierarchyRootNodeName");
         HierarchyListView = WindowRoot.HierarchyView;
         InitHierarchyView();
         if (TreeData == null || TreeData.SelectedHierarchyInfo == null) return;
         if (TreeData != null)
         {
            WindowRoot.TreeView.viewTransform.position = TreeData.SelectedHierarchyInfo.WindowData.Position;
            WindowRoot.TreeView.viewTransform.scale = TreeData.SelectedHierarchyInfo.WindowData.Scale;
         }

         WindowRoot.ChangeToNodeInspector.clicked += () =>
         {
            WindowRoot.Inspector.SwitchSignal = false;
            WindowRoot.Inspector.UpdateInspector();
         };
         WindowRoot.ChangeToParameterInspector.clicked += () =>
         {
            WindowRoot.Inspector.SwitchSignal = true;
            WindowRoot.Inspector.UpdateInspector();
         };
      }
      private void InitNodeViews()
      {
         RefreshCurrentHierarchyRootNodeName();
         if (TreeData.SelectedHierarchyInfo == null)return;
         HierarchyInfo selectedHierarchyInfo = TreeData.SelectedHierarchyInfo;
         WindowRoot.TreeView.viewTransform.position = TreeData.SelectedHierarchyInfo.WindowData.Position;
         WindowRoot.TreeView.viewTransform.scale = TreeData.SelectedHierarchyInfo.WindowData.Scale;
         WindowRoot.TreeView.RootNodeView = null;
         WindowRoot.TreeView.ClearAllNodesAndEdges();
         selectedHierarchyInfo.GetHierarchyNodes().ForEach(n => CreateNode(n,selectedHierarchyInfo.GetRootNode()));
         WindowRoot.TreeView.nodes.OfType<NodeView>().ForEach(n => n.AddEdge());
      }

      public void RefreshCurrentHierarchyRootNodeName()
      {
         if (TreeData.SelectedHierarchyInfo == null)
         {
            WindowRoot.CurrentHierarchyRootNodeName.style.visibility = Visibility.Hidden;
            return;
         }
         var rootNode = TreeData.SelectedHierarchyInfo.GetRootNode();
         if (rootNode != null)
         {
            WindowRoot.CurrentHierarchyRootNodeName.text = "RootNodeName:" + rootNode.NodeName;
         }
         else
         {
            WindowRoot.CurrentHierarchyRootNodeName.text = "RootNodeName:";
         }
      }
      
      private void CreateNode(INodeViewer data,INodeViewer root = null)
      {
         TreeView treeView = Window.WindowRoot.TreeView;
         NodeView nodeView = new NodeView(data);
         data.GUID ??= Guid.NewGuid().ToString();
         if (data.TreeID != TreeData.Guid || string.IsNullOrEmpty(data.TreeID))
         {
            data.TreeID = Guid.NewGuid().ToString();
         }
         if(data == root)
            treeView.RootNodeView = nodeView;
         nodeView.SetPosition(new Rect(data.Position,Vector2.one));
         treeView.AddElement(nodeView);
      }
      
      private void CreateGUI()
      {
         if (TreeData == null) return;
         InitSpeakWindow();
      }

      private void OnDestroy()
      {
         Save();
         TreeData = null;
      }

      public void OnEnable()
      {
         Undo.undoRedoPerformed += Refresh;
      }
      
      public void OnInspectorUpdate()
      {
         if (TreeData == null) return;
         WindowRoot.TreeView.OnGUI();
      }

      public void Update()
      {
         Window.WindowRoot.Inspector.RefreshInspector();
      }
      
      public void Refresh()
      {
         if(TreeData != null)
         {
            rootVisualElement.Clear();
            InitSpeakWindow();
         }
      }

      private void UpdateWindowData()
      {
         if(TreeData.SelectedHierarchyInfo == null)return;
         TreeData.SelectedHierarchyInfo.WindowData = new WindowData();
         var scale = WindowRoot.TreeView.viewTransform.scale;
         TreeData.SelectedHierarchyInfo.WindowData.Scale = scale;
         TreeData.SelectedHierarchyInfo.WindowData.Position = WindowRoot.TreeView.RootNodeView == null? WindowRoot.TreeView.viewTransform.position : CalcViewTransformPosition_CenterOnNode(WindowRoot.TreeView,WindowRoot.TreeView.RootNodeView);
      }

      /// <summary>
      /// 该方法主要是在有根节点的时候将根节点在TreeView上的相对位置转换到之后会被保存的TreeView的世界位置
      /// </summary>
      /// <param name="graphView"></param>
      /// <param name="nodeView"></param>
      /// <returns></returns>
      private static Vector2 CalcViewTransformPosition_CenterOnNode(GraphView graphView, NodeView nodeView)
      {
         // 1) 视口中心（屏幕/像素坐标）
         var vp = graphView.contentRect; // 比 viewport.resolvedStyle 更靠谱，Rect 已是像素
         var viewportCenter = vp.center;

         // 2) 节点中心（内容坐标）
         // GraphElement(Node) 的可靠位置 API：GetPosition()
         var nodeRect = nodeView.GetPosition();
         var nodeCenterInContent = nodeRect.center;

         // 3) 当前缩放
         var s3 = graphView.viewTransform.scale;
         var scale = new Vector2(s3.x, s3.y);

         // 4) 反推 viewTransform.position（屏幕/像素单位）
         var viewPos = viewportCenter - Vector2.Scale(nodeCenterInContent, scale);

         return viewPos;
      }

      private void Save()
      {
         if (Application.isPlaying) return;
         if (TreeData == null) return;
         UpdateWindowData();
         var scene = SceneManager.GetActiveScene();
         EditorSceneManager.MarkSceneDirty(scene);
         EditorSceneManager.SaveScene(scene);
      }
      
      #region 层级面板
      
      private int _editingIndex = -1;
      private List<HierarchyListItem> _hierarchyItems = new();
    
      private void InitHierarchyView()
      { 
         HierarchyListView.itemsSource = TreeData.GetHierarchInfoList();
         HierarchyListView.reorderable = true;
         HierarchyListView.reorderMode = ListViewReorderMode.Animated;
         HierarchyListView.makeItem = MakeItem;
         HierarchyListView.bindItem = BindItem;
         HierarchyListView.RegisterCallback<KeyDownEvent>(evt =>
         {
            if (evt.keyCode == KeyCode.Delete)
            {
               TreeData.RemoveHierarchyInfoAtIndex(HierarchyListView.selectedIndex);
               HierarchyListView.RefreshItems();
            }
         });
         HierarchyListView. itemsChosen += _ =>
         {
            if (HierarchyListView.itemsSource == null) return;
            var idx = HierarchyListView.selectedIndex;
            if (idx < 0) return;
            _editingIndex = idx;
            _hierarchyItems[_editingIndex].Focus();
            HierarchyListView.RefreshItems();
         };
         HierarchyListView.selectionChanged += _ =>
         {
            if(HierarchyListView.itemsSource == null || HierarchyListView.selectedItem == null)
               return;
            if (HierarchyListView.selectedIndex != _editingIndex)
            {
               foreach (var item in _hierarchyItems)
               {
                  item.UnSelected();
                  _editingIndex = -1;
               }
            }
            if (HierarchyListView.selectedItem is not HierarchyInfo hierarchyInfo) return;
            if (TreeData.SelectedHierarchyInfo != hierarchyInfo)
            {
               TreeData.SelectedHierarchyInfo = hierarchyInfo; 
               UpdateWindowData();
               InitNodeViews();
               if(TreeData.SelectedHierarchyInfo.GetRootNode() != null && Window.WindowRoot.TreeView.NodeViews.TryGetValue(TreeData.SelectedHierarchyInfo.GetRootNode().GUID,out var currentRootNodeView))
               {
                  Window.WindowRoot.TreeView.RootNodeView = currentRootNodeView;
               }
               WindowRoot.TreeView.viewTransform.position = hierarchyInfo.WindowData.Position;
               WindowRoot.TreeView.viewTransform.scale = hierarchyInfo.WindowData.Scale;
            }
            HierarchyListView.RefreshItems();
            WindowRoot.Inspector.UpdateInspector();
         };
         HierarchyListView.itemsAdded += indices =>
         {
               var hierarchyInfo = TreeData.CreateHierarchyInfo();
               hierarchyInfo.HierarchyName = "New Hierarchy";
               hierarchyInfo.WindowData.Position = Vector2.zero;
               hierarchyInfo.WindowData.Scale = Vector2.one;
               TreeData.AddHierarchyInfo(hierarchyInfo);
               HierarchyListView.itemsSource = TreeData.GetHierarchInfoList();
         };
         HierarchyListView.itemsRemoved += _ =>
            {
               if (_editingIndex >= TreeData.GetHierarchInfoList().Count) _editingIndex = -1;
               TreeData.RemoveHierarchyInfoAtIndex(HierarchyListView.selectedIndex);
               HierarchyListView.RefreshItems();
               WindowRoot.Inspector.UpdateInspector();
            };
         HierarchyListView.itemIndexChanged += (index1, index2) =>
         {
            TreeData.IndexChange(index1, index2);
            HierarchyListView.itemsSource = TreeData.GetHierarchInfoList();
         };
         if (TreeData.SelectedHierarchyInfo != null)
         {
            InitNodeViews();
         }
         else if(TreeData.GetHierarchInfoList().Count > 0 && HierarchyListView.selectedIndex >= 0)
         {
            TreeData.SelectedHierarchyInfo =  TreeData.GetHierarchInfoList()[HierarchyListView.selectedIndex - 1];
            InitNodeViews();
         }
         HierarchyListView.SetSelection(TreeData.IndexofHierarch(TreeData.SelectedHierarchyInfo));
      }
      
      public static ParameterForTree GetParameter(string Guid)
      {
         return TreeData.GetParameterForTree().FirstOrDefault(x => x.Guid == Guid);
      }
      
      //在选中项目切换时也会被调用(就是双击的时候)
      private void BindItem(VisualElement element, int index)
      {
         if(index < 0 || index >= TreeData.GetHierarchInfoList().Count) return;
          var item = element as HierarchyListItem;
          item.BindItem(TreeData.GetHierarchInfoList()[index]);
          if (index == _editingIndex)
          {
             item.Selected();
          }
          else item.UnSelected();
      }
      private VisualElement MakeItem()
      {
         var item = new HierarchyListItem();
         item.Init();
         item.style.width = 400;
         item.style.flexGrow = 1;
         item.NameField.RegisterValueChangedCallback((evt) =>
         {
            HierarchyListView.RefreshItems();
         });
         item.UnSelected();
         _hierarchyItems.Add(item);
         return item;
      }

      public class HierarchyListItem: VisualElement
      {
         public TextField NameField;
        
         public Label HierarchyNameLabel;
         public HierarchyInfo HierarchyInfo;
         public void Init()
         {
            style.flexDirection = FlexDirection.Row;
            NameField = new TextField();
            NameField.style.flexGrow = 1;
            HierarchyNameLabel = new Label();
            HierarchyNameLabel.style.flexGrow = 1;
            HierarchyNameLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            NameField.bindingPath = "HierarchyName";
            HierarchyNameLabel.bindingPath = "HierarchyName";
            Add(HierarchyNameLabel);
            NameField.RegisterValueChangedCallback(evt =>
            {
               if (HierarchyInfo == null) return;
               HierarchyInfo.HierarchyName = evt.newValue;
            });
            NameField.RegisterCallback<KeyDownEvent>(evt =>
            {
               if (evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
               {
                  UnSelected();
                  evt.StopPropagation();
               }
            });
            RegisterCallback<FocusOutEvent>(evt =>
            {
               UnSelected();
            });
         }
        
        
         public void Selected()
         {
            if(Contains(HierarchyNameLabel))
               Remove(HierarchyNameLabel);
            Add(NameField);
         }

         public void UnSelected()
         {   
            if(Contains(NameField))
               Remove(NameField);
            Add(HierarchyNameLabel); 
         }
        
         public void BindItem(HierarchyInfo info)
         {
            HierarchyInfo = info;
            NameField.SetValueWithoutNotify(HierarchyInfo.HierarchyName ??= "New Hierarchy");
            HierarchyNameLabel.text = HierarchyInfo.HierarchyName ??= "New Hierarchy";
         }
      }
      #endregion
      
   }
   
   
   public class RightMenuItem:ScriptableObject,ISearchWindowProvider
   {
      public delegate bool SelectEntryDelegate(SearchTreeEntry searchTreeEntry, SearchWindowContext context);

      public SelectEntryDelegate OnMenuClicked;
      
      
      public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
      {
         List<SearchTreeEntry> entries = new List<SearchTreeEntry>();
         var type = NodeTreeSystemWindow.TreeData.GetNodeType();
         var abstractNodeLabel = type.GetCustomAttribute<NodeExtension.NodeLabelAttribute>();
         entries.Add(new SearchTreeGroupEntry(new GUIContent("Node Create")){level = 0});
         if (abstractNodeLabel == null  || !abstractNodeLabel.IsSubClass)
         {
            List<Type> typies = NodeTreeSystemWindow.TreeData.GetNodeType().GetDerivedClasses();
            foreach (Type t in typies)
            {
               if (t.BaseType != type) continue;
               if (t.GetCustomAttribute<NodeExtension.NodeLabelAttribute>() is {} nodeLabel)
               {
                  entries = AddEntries(entries, nodeLabel.MenuName, t,1);
               }
               else
               {
                  entries = AddEntries(entries, t.Name, t,1);
               }
            }
         }
         else if (abstractNodeLabel.IsSubClass)
         {
            var parentType = type.BaseType;
            AddEntries(entries, abstractNodeLabel.MenuName, type, 1);
            if (parentType.GetCustomAttribute<NodeExtension.NodeLabelAttribute>() is {} parentNodeLabel)
            {
               entries = AddEntries(entries, parentNodeLabel.MenuName, parentType,1);
            }
            else
            {
               entries = AddEntries(entries, parentType?.Name, parentType,1);
            }
         }
         
         return entries;
      }

      public List<SearchTreeEntry> AddEntries(List<SearchTreeEntry> entries,string pathName,Type rootType,int level)//泛型改写的时候可以将其变为直接接受一个类型传递传递的方法
      {
         List<Type> rootNodeTypes = rootType.GetDerivedClasses();
         if (rootNodeTypes.Count == 0)
         {
            entries.Add(new SearchTreeEntry(new GUIContent(pathName)){level = level,userData = rootType});
            return entries;
         }
         entries.Add(new SearchTreeGroupEntry(new GUIContent(pathName)){level = level});
         foreach (Type type in rootNodeTypes)
         {
            if (type.IsAbstract)
            {
               if (type.GetCustomAttribute<NodeExtension.NodeLabelAttribute>() is { } abstractNodeLabel)
               {
                  entries = AddEntries(entries, abstractNodeLabel.MenuName, type, level + 1);
                  continue;
               }
            }
            string menuName = type.Name;
            if (type.GetCustomAttribute<NodeExtension.NodeLabelAttribute>() is {} nodeLabel)
            {
               if (nodeLabel.MenuName != "")
               {
                  menuName = nodeLabel.MenuName;
               }
            }
            entries.Add(new SearchTreeEntry(new GUIContent(menuName)){level = level + 1,userData = type});
            var subList = type.GetDerivedClasses();
            if (subList.Count > 0)
            {
               entries = AddEntries(entries, menuName, type, level + 1);
            }
         }
         return entries;
      }
      public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
      {
         if (OnMenuClicked == null)
         {
            return false;
         }

         return OnMenuClicked.Invoke(SearchTreeEntry, context);
      }
   }
}
