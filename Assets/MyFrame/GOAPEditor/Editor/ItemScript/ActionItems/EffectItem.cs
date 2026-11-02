using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class EffectItem : GOAPItemBase
{
    public const string EffectItemAssetPath = "Assets/MyFrame/GOAPEditor/Style/ItemAsset/EffectItem.uxml";
    public GOAPTypeAndComparer Effect;
    
    public static readonly Color OnSelectedColor =  new (  0.5137255f,  0.01176471f,0.6f,   1f);
    public static readonly Color NormalColor = new (  0.5137255f,  0.01176471f,0.6f,   0.6117647f);

    public void Init(GOAPTypeAndComparer effect,int index, Action<GOAPItemBase> onDelete,
        Action<GOAPItemBase> onSelect,VisualElement parent)
    {
        base.Init(onDelete,onSelect,EffectItemAssetPath,parent);
        Effect = effect;
        SetIndex(index);
        Root.RegisterCallback<MouseDownEvent>(OnMouseDown);
        SetItemName(Effect.StateType);
    }

    public override void RefreshItem()
    {
        SetItemName(Effect.StateType);
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
