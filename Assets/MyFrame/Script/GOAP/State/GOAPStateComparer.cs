/// <summary>
/// 比较器用于比较状态的值是否满足条件以及应用效果(其内部的枚举等辅助字段用于描述比较或应用的方式)
/// 或者描述一个目标的期望达到的数值
/// </summary>
public abstract class GOAPStateComparer:IDeepCopy<GOAPStateComparer>
{
        public abstract bool MatchWithComparer(GOAPStateComparer other);
        public GOAPStateComparer DeepCopy()
        {
                return Copy();
        }
        public abstract GOAPStateComparer Copy();
}

/// <typeparam name="S">状态类(专属于其比较的状态)</typeparam>
/// <typeparam name="C">比较器类(其本身)</typeparam>
public abstract class GOAPStateComparer<S, C> : GOAPStateComparer where S : GOAPStateBase where C : GOAPStateComparer
{
        public abstract bool MatchWithComparer(C other);

        public override bool MatchWithComparer(GOAPStateComparer other)
        {
                return MatchWithComparer((C)other);
        }

        public override GOAPStateComparer Copy()
        {
                // comparer 仅包含值类型与 Unity 引用字段，浅拷贝即可满足配置到运行时实例隔离
                return (GOAPStateComparer)MemberwiseClone();
        }
        
}


