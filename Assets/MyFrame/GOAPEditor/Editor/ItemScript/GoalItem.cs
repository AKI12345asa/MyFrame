using System;
using UnityEngine;
using UnityEngine.UIElements;

public class GoalItem : GOAPItemBase
{
    public const string GoalItemAssetPath = "Assets/MyFrame/GOAPEditor/Style/ItemAsset/GoalItem.uxml";
    
    private GOAPGoals.Goal _goal;
    public GOAPGoals.Goal Goal => _goal;
    public string GoalName; 
    public static readonly Color OnSelectedColor = new (0.6f, 0.6f,    0.01176471f,1f);
    public static readonly Color NormalColor = new (0.6f, 0.6f,    0.01176471f,0.7137255f);
    public void Init(GOAPGoals.Goal gaol,string goalName,int index,Action<GOAPItemBase> onDelete, Action<GOAPItemBase> onMouseDown,VisualElement parent)
    {
        _goal = gaol;
        base.Init(onDelete, onMouseDown, GoalItemAssetPath,parent);
        GoalName = goalName;
        SetIndex(index);
        Root.RegisterCallback<MouseDownEvent>(OnMouseDown);
        SetItemName(goalName);
    }

    private void OnMouseDown(MouseDownEvent evt)
    {
        OnSelect.Invoke(this);
        Selected();
    }


    public override void Selected()
    {
        Root.style.backgroundColor = OnSelectedColor;
    }

    public override void UnSelected()
    {
        Root.style.backgroundColor = NormalColor;
    }
    
}
