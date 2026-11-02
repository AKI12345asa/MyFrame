using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class GOAPActionItem: GOAPItemBase
{
    public const string ActionItemAssetPath = "Assets/MyFrame/GOAPEditor/Style/ItemAsset/ActionItem.uxml";
    public GOAPActionBase Action;
    public static readonly Color OnSelectedColor = new (0.1843137f, 0.6039216f,0.6666667f, 1f);
    public static readonly Color NormalColor = new (0.1843137f, 0.6039216f,0.6666667f, 0.7137255f);
    
    public void Init(GOAPActionBase action, int index, Action<GOAPItemBase> onDelete,
        Action<GOAPItemBase> onSelect,VisualElement parent)
    {
        base.Init(onDelete,onSelect,ActionItemAssetPath,parent);
        Action = action;
        OnSelect = onSelect;
        SetIndex(index);
        Root.RegisterCallback<MouseDownEvent>(OnMouseDown);
        SetItemName(action.ActionName);
    }

    public override void RefreshItem()
    {
        SetItemName(Action.ActionName);
    }

    private void OnMouseDown(MouseDownEvent evt)
    {
        OnSelect?.Invoke(this);
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
