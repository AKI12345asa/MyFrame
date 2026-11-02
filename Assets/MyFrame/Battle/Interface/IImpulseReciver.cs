public interface IImpulseReciver
{
    public void ReceiveImpulse(ImpulseData impulseData);
    public ImpulseData GetImpulseData(ImpulseDetectionEvent impulseEvent);
}
