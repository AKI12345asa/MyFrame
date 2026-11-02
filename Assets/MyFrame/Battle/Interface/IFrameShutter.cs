using System.Collections;

public interface IFrameStutter :ITimeOwner
{
    /// <summary>
    /// 作为顿帧发起者的回调(推荐通过TimeSystem管理)
    /// </summary>
    /// <param name="timeScale"></param>
    /// <param name="duration"></param>
    /// <returns></returns>
    public IEnumerator SetShutter(float timeScale,float duration);
    
    /// <summary>
    /// 接收到顿帧信息(推荐通过TimeSystem管理)
    /// </summary>
    /// <param name="timeScale"></param>
    /// <param name="duration"></param>
    /// <returns></returns>
    public IEnumerator GetShutter(float timeScale,float duration);
}
