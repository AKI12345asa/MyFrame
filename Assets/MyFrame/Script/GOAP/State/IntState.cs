using UnityEngine;

public class IntState: GOAPStateBase<IntState,int,IntStateComparer>
{
    public override GOAPStateBase Copy()
    {
        return new IntState { Value = Value };
    }

    public override bool EqualsValue(IntState other)
    {
        return Value == other.Value;
    }
    
    public override void ApplyEffect(IntStateComparer comparer)
    {
        switch (comparer.Symbol)
        {
            case NumberCompareSymbol.等于:
                Value = comparer.Value;
                break;
            default:
                Value += comparer.Value;
                break;
        }
    }
    public override bool CompareWithPreCondition(IntStateComparer comparer)
    {
        switch (comparer.Symbol)
        {
            case NumberCompareSymbol.大于:
                return Value > comparer.Value;
            case NumberCompareSymbol.小于:
                return Value < comparer.Value;
            case NumberCompareSymbol.大于等于:
                return Value >= comparer.Value;
            case NumberCompareSymbol.小于等于:
                return Value <= comparer.Value;
            case NumberCompareSymbol.提升即可:
                return Value > 0;
            case NumberCompareSymbol.下降即可:
                return Value < 0;
            case NumberCompareSymbol.等于:
                return Mathf.Approximately(Value, comparer.Value);
        }

        return false;
    }
    
    public override bool CompareWithEffect(IntStateComparer comparer)
    {
        
        switch (comparer.Symbol)
        {
            case NumberCompareSymbol.大于:
                return Value > comparer.Value;
            case NumberCompareSymbol.小于:
                return Value < comparer.Value;
            case NumberCompareSymbol.大于等于:
                return Value >= comparer.Value;
            case NumberCompareSymbol.小于等于:
                return Value <= comparer.Value;
            case NumberCompareSymbol.提升即可:
                return false;
            case NumberCompareSymbol.下降即可:
                return false;
            case NumberCompareSymbol.等于:
                return Mathf.Approximately(Value, comparer.Value);
        }

        return false;
    }
}

public class IntStateComparer : GOAPStateComparer<IntState, IntStateComparer>
{
    public NumberCompareSymbol Symbol;
    public int Value;

    public override GOAPStateComparer Copy()
    {
        return new IntStateComparer
        {
            Symbol = Symbol,
            Value = Value,
        };
    }

    public override bool MatchWithComparer(IntStateComparer other)
    {
        switch (other.Symbol)
        {
            case NumberCompareSymbol.提升即可:
                return false;
            case NumberCompareSymbol.下降即可:
                return false;
            case NumberCompareSymbol.等于:
                return Mathf.Approximately(Value, other.Value);
            case NumberCompareSymbol.大于:
                return Value > 0 || Symbol == NumberCompareSymbol.提升即可;
            case NumberCompareSymbol.小于:
                return Value < 0 || Symbol == NumberCompareSymbol.下降即可;
            case NumberCompareSymbol.大于等于:
                return Value > 0 || Symbol == NumberCompareSymbol.提升即可;
            case NumberCompareSymbol.小于等于:
                return Value < 0 || Symbol == NumberCompareSymbol.下降即可;
            default:
                return false;
        }
    }
}
