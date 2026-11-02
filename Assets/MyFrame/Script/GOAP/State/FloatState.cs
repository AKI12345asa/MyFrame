using System;
using UnityEngine;

public class FloatState : GOAPStateBase<FloatState,float,FloatStateComparer>
{
    public override GOAPStateBase Copy()
    {
        return new FloatState { Value = Value };
    }

    public override bool EqualsValue(FloatState other)
    {
        return Mathf.Approximately(Value, other.Value);
    }

    public override bool CompareWithEffect(FloatStateComparer comparer)
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
    
    public override void ApplyEffect(FloatStateComparer comparer)
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
    public override bool CompareWithPreCondition(FloatStateComparer comparer)
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
                return true;
            case NumberCompareSymbol.下降即可:
                return true;
            case NumberCompareSymbol.等于:
                return Mathf.Approximately(Value, comparer.Value);
        }

        return false;
    }
}

public class FloatStateComparer : GOAPStateComparer<FloatState, FloatStateComparer>
{
    public NumberCompareSymbol Symbol;
    public float Value;

    public override GOAPStateComparer Copy()
    {
        return new FloatStateComparer
        {
            Symbol = Symbol,
            Value = Value,
        };
    }

    public override bool MatchWithComparer(FloatStateComparer other)
    {
        switch (other.Symbol)
        {
            case NumberCompareSymbol.提升即可:
                return Value > 0 || Symbol == NumberCompareSymbol.提升即可;
            case NumberCompareSymbol.下降即可:
                return Value < 0 || Symbol == NumberCompareSymbol.下降即可;
            case NumberCompareSymbol.等于:
                return Mathf.Approximately(Value, other.Value);
            case NumberCompareSymbol.大于:
                return Value > other.Value || Symbol == NumberCompareSymbol.提升即可;
            case NumberCompareSymbol.小于:
                 return Value < other.Value || Symbol == NumberCompareSymbol.下降即可;
            case NumberCompareSymbol.大于等于:
                return Value >= other.Value || Symbol == NumberCompareSymbol.提升即可;
            case NumberCompareSymbol.小于等于:
                return Value <= other.Value || Symbol == NumberCompareSymbol.下降即可;
            default:
                return false;
        }
    }
}

public enum NumberCompareSymbol
{
    大于,
    小于,
    大于等于,
    小于等于,
    提升即可,
    下降即可,
    等于
}
