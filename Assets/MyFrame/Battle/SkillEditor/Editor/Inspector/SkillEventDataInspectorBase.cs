using UnityEngine.UIElements;

public abstract class SkillEventDataInspectorBase
{
    protected VisualElement Root;
    public int ItemFrameIndex;

    public void SetFrameIndex(int itemFrameIndex)
    {
        ItemFrameIndex = itemFrameIndex;
    }

    public virtual void Draw(VisualElement root, TrackItemBase trackItem, SkillTrackBase track)
    {
        Root = root;
        SetFrameIndex(trackItem.FrameIndex);
    }
    public abstract void OnDraw();
}

public abstract class SkillEventDataInspectorBase<TrackItem, Track> : SkillEventDataInspectorBase
    where TrackItem : TrackItemBase where Track : SkillTrackBase
{
    protected TrackItem trackItem;
    protected Track track;

    public override void Draw(VisualElement root, TrackItemBase trackItem, SkillTrackBase track)
    {
        base.Draw(root, trackItem, track);
        this.trackItem = (TrackItem)trackItem;
        this.track = (Track)track;
    }

  
}
