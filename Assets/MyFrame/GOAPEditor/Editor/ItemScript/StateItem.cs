using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class StateItem : GOAPItemBase
{
    public const string StateItemAssetPath = "Assets/MyFrame/GOAPEditor/Style/ItemAsset/StateItem.uxml";
    public static readonly Color OnSelectedColor = new (0.6f, 0.2689167f, 0.01176471f, 1f);
    public static readonly Color NormalColor = new (0.6f, 0.2689167f, 0.01176471f, 0.7372549f);
    public GOAPStateBase State;
    public string StateType;
    public void Init(GOAPStateBase state, GOAPStateType stateType, int index, Action<GOAPItemBase> onDelete,
        Action<GOAPItemBase> onSelect,VisualElement parent)
    {
        base.Init(onDelete,onSelect,StateItemAssetPath,parent);
        State = state;
        StateType = stateType;
        SetIndex(index);
        Root.RegisterCallback<MouseDownEvent>(OnMouseDown);
        SetItemName(stateType);
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
