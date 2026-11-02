using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace NodeSystemTree.Editor.SpeakTreeView
{
    public class SplitView : TwoPaneSplitView
    {
    
        public Inspector Inspector;
        
        public TreeView TreeView;
        
        public ListView HierarchyView;

        public ToolbarButton ChangeToNodeInspector;
        
        public ToolbarButton ChangeToParameterInspector;

        public Label CurrentHierarchyRootNodeName;
        public new class UxmlFactory : UxmlFactory<SplitView, UxmlTraits>{}

        public SplitView()
        {
            Init();
        }
    
        public void Init(){}
    }
}
