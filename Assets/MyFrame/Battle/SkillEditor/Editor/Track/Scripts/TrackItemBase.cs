using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using Label = UnityEngine.UIElements.Label;

public abstract class TrackItemBase
{
    protected float FrameUnitWidth;
    
    protected int _frameIndex;
    public int FrameIndex => _frameIndex;
    public virtual void Select() {}
    
    public virtual void OnSelect(){}
    
    public virtual void OnUnSelect(){}
    
    public virtual void OnConfigChanged(){}
    
    public virtual void ResetView()
    {
        ResetView(FrameUnitWidth);
    }

    public virtual void ResetView(float frameWidth)
    {
        this.FrameUnitWidth = frameWidth;
    }
    
   
}

public abstract class TrackItemBase<T> : TrackItemBase where T : SkillTrackBase
{
    protected T Track;
    protected Color NormalColor;
    protected Color SelectColor;

    

    public SkillTrackItemStyleBase ItemStyle { get; protected set; }
    public override void Select()
    {
        SkillEditorWindow.Instance.ShowTrackItemOnInspector(this,Track);
    }

    public override void OnSelect()
    {
        ItemStyle.SetBGColor(SelectColor);
    }

    public override void OnUnSelect()
    {
        ItemStyle.SetBGColor(NormalColor);
    }

}