public interface ITimeOwner
{
    public TimeSystem.TimeScaleContainer timeScaleContainer { get; set; }
    
    public void WhenUpdateTimeScale(float timeScale);
}
