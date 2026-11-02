/// <summary>表示支持读取动画片段相位同步状态的节点。</summary>
public interface ICanSyncNode 
{
    /// <summary>此节点是否启用相位同步。</summary>
    public bool IsSync { get; }
}
