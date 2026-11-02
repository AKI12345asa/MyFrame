using MyFrame;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>
    /// 所有动画节点的基类，负责管理播放图中的输入端口、连接状态、播放速度及对象池回收。
    /// </summary>
    public abstract class AnimationNodeBase
    {
        /// <summary>此节点连接到父混合器的输入端口。</summary>
        public int InputPort;
        /// <summary>节点当前是否已连接到播放图。</summary>
        public bool IsLinked;
        /// <summary>节点的播放速度。</summary>
        public virtual float Speed { get; set; }
        
        
        /// <summary>节点是否保留在对象池中以便重复使用。</summary>
        public virtual bool IsCached{ get; protected set; }

        /// <summary>回收节点并断开其逻辑连接状态。</summary>
        /// <param name="isForced">为 true 时强制回收已缓存的节点。</param>
        public virtual void PushPool(bool isForced = false)
        {
            IsLinked = false;
            this.ObjectPushPool();
        }
        
        /// <summary>设置节点在父混合器中的输入端口。</summary>
        /// <param name="port">父混合器输入端口索引。</param>
        public void SetInputPort(int port)
        {
            InputPort = port;
        }
        protected virtual void ConnectToPlayableGraph<V>(Playable connector, PlayableGraph graph, V outputMixer, int sourceOutPutPort,bool isTransitioning)where V : struct,IPlayable
        {
            if (isTransitioning) return;
            graph.Connect(connector, sourceOutPutPort, outputMixer, InputPort);
            IsLinked = true;
        }
        /// <summary>将节点的 Playable 接入父混合器；过渡期间由控制器负责接线。</summary>
        public abstract void ConnectToPlayableGraph<V>(V outputMixer, PlayableGraph graph, bool isTransitioning)where V : struct,IPlayable;

        /// <summary>设置节点内所有动画片段的播放速度。</summary>
        /// <param name="speed">播放速度倍率。</param>
        public abstract void SetClipSpeed(float speed);
        
    }
