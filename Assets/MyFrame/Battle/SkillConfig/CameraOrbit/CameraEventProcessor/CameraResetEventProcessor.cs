public class CameraResetEventProcessor : CameraEventProcessor<CameraResetEvent>
{
    public CameraResetEventProcessor(ICharacter owner) : base(owner)
    {
    }

    protected override void StartEvent(CameraResetEvent ev, CameraEventTime time, SkillCamera camera, Skill_Player skillPlayer)
    {
        skillPlayer.ResetTriggeredCameraProcessors();
        camera.ReSetSkillCamera();
    }
}
