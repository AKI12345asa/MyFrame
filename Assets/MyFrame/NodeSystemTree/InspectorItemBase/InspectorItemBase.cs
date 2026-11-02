using UnityEditor;
using UnityEngine.UIElements;

public abstract class InspectorItemBase
{
    public abstract void Init(VisualElement parent,INodeViewer nodeData);
    public abstract void CreateInspectorGUI();
    
    public abstract void UpdateInspectorGUI();
}

public abstract class InspectorItemBase<T> : InspectorItemBase where T : INodeViewer
{
    protected T NodeData;
    protected VisualElement Root;
    protected TextField TitleField;
    public override void Init(VisualElement parent,INodeViewer nodeData)
    {
        Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/MyFrame/NodeSystemTree/InspectorItemBase/InspectorItemRoot.uxml").Instantiate().Q("Root");
        NodeData = (T)nodeData;
        TitleField = new TextField("节点名称");
        TitleField.RegisterValueChangedCallback((evt) =>
        {
            NodeData.NodeName = evt.newValue;
        });
        TitleField.SetValueWithoutNotify(NodeData.NodeName);
        parent.Add(Root);
        Root.Add(TitleField);
    }
}
