using UnityEngine;

public class BoolState: GOAPStateBase<BoolState,bool,BoolStateComparer>
{
    public override GOAPStateBase Copy()
    {
        return new BoolState { Value = Value };
    }

    public override bool EqualsValue(BoolState other)
    {
        return Value == other.Value;
    }   
    
    public override bool CompareWithPreCondition(BoolStateComparer comparer)
    {
        switch (comparer.Value)
        {
            case BoolValue.是:
                return Value;
            case BoolValue.否:
                return !Value;
        }

        return false;
    }

    public override bool CompareWithEffect(BoolStateComparer comparer)
    {
        return CompareWithPreCondition(comparer);
    }

    public override void ApplyEffect(BoolStateComparer comparer)
    {
        switch (comparer.Value)
        {
            case BoolValue.是:
                Value = true;
                break;
            case BoolValue.否:
                Value = false;
                break;
        }
    }
    
}

public class BoolStateComparer : GOAPStateComparer<BoolState, BoolStateComparer>
{
    public BoolValue Value;

    public override GOAPStateComparer Copy()
    {
        return new BoolStateComparer
        {
            Value = Value,
        };
    }

    public override bool MatchWithComparer(BoolStateComparer other)
    {
        switch (other.Value)
        {
            case BoolValue.是:
                return Value == BoolValue.是;
            case BoolValue.否:
                return Value == BoolValue.否;
            default:
                return false;
        }
    }
}

public enum BoolValue
{
    是,
    否
}

