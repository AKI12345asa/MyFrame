using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class PreConditionItem : GOAPItemBase
{
    public const string PreConditionItemAssetPath = "Assets/MyFrame/GOAPEditor/Style/ItemAsset/PreconditionItem.uxml";
    public GOAPTypeAndComparer PreCondition;
    public static readonly Color OnSelectedColor = new (0.01176471f, 0.2745098f, 0.6f,1f);
    public static readonly Color NormalColor = new (0.01176471f, 0.2745098f, 0.6f,0.6509804f);
    
    public void Init(GOAPTypeAndComparer preCondition,int index, Action<GOAPItemBase> onDelete,
        Action<GOAPItemBase> onSelect,VisualElement parent)
    {
        base.Init(onDelete,onSelect,PreConditionItemAssetPath,parent);
        PreCondition = preCondition;
        SetIndex(index);
        Root.RegisterCallback<MouseDownEvent>(OnMouseDown);
        SetItemName(PreCondition.StateType);
    }

    public override void RefreshItem()
    {
        SetItemName(PreCondition.StateType);
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
