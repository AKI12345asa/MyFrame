using System;
using Sirenix.OdinInspector;
using UnityEngine;

public class CharacterProperties : MonoBehaviour
{
    [ShowInInspector]public float CurrentHp { get; private set; }
    [ShowInInspector]public float CurrentMp { get; private set; }
    public FloatProperties maxHp = new FloatProperties();
    public FloatProperties maxMp = new FloatProperties();
    public FloatProperties attack = new FloatProperties();
    public FloatProperties defence = new FloatProperties();

    //TODO:基于存档回复当前血量等信息
    public void Init(float hpBaseValue,float mpBaseValue,float attackBaseValue,float defenceBaseValue,float currentHp,float currentMp)
    {
        maxHp.Init(hpBaseValue,null,null,null,OnMaxHpChanged);
        maxMp.Init(mpBaseValue,null,null,null,OnMaxMpChanged);
        attack.Init(attackBaseValue,null,null,null,null);
        defence.Init(defenceBaseValue,null,null,null,null);
        CurrentHp = currentHp;
        CurrentMp = currentMp;
    }

    public void AddHp(float addValue)
    {
        SetHp(CurrentHp + addValue);
        //同步到UI
    }

    public void SetHp(float value)
    {
        CurrentHp = Mathf.Clamp(value, 0, maxHp.Total);
    }

    public void AddMp(float addValue)
    {
        SetMp(CurrentMp + addValue);
        //同步到UI
    }

    public void SetMp(float value)
    {
        CurrentMp = Mathf.Clamp(value, 0, maxMp.Total);
    }
    
    private void OnMaxHpChanged(float oldMaxHp, float newMaxHp)
    {
        //最大生命值变化时，让玩家的声明根据之前的比例同步变化
        var hpProportion = CurrentHp / oldMaxHp;
        CurrentHp = newMaxHp * hpProportion;
        //同步给UI
    }
    private void OnMaxMpChanged(float oldMaxMp, float newMaxMp)
    {
        //最大魔法值变化时，让玩家的声明根据之前的比例同步变化
        var mpProportion = CurrentMp / oldMaxMp;
        CurrentMp = newMaxMp * mpProportion;
        //同步给UI
    }
    [Button]
    public void TestAddMaxHp(float value)
    {
        maxHp.FixedBonus += value;
    }
}
[Serializable]
public class FloatProperties
{
    [SerializeField]private float baseValue;
    [SerializeField]private float fixedBonus;
    [SerializeField]private float percentBonus;
    private Action<float, float> _onBaseValueChangedAction;
    private Action<float, float> _onFixedValueChangedAction;
    private Action<float, float> _onPercentValueChangedAction;
    private Action<float,float> _onTotalValueChangedAction;
     public void Init(float baseValue,Action<float,float> onBaseEvent,Action<float,float> onFixedEvent,Action<float,float> onPercentEvent,Action<float,float> onTotalEvent)
    {
        this.baseValue = baseValue;
        _onBaseValueChangedAction = onBaseEvent;
        _onFixedValueChangedAction = onFixedEvent;
        _onPercentValueChangedAction = onPercentEvent;
        _onTotalValueChangedAction = onTotalEvent;
    }
    
    public float Total => baseValue + fixedBonus + baseValue * (percentBonus / 100);

    public float GetTotalVirtually(float newFixedBonus, float newPercentBonus)
    {
        return baseValue + newFixedBonus + baseValue * (newPercentBonus / 100);
    }
    
    public float GetAdditionVirtually(float newFixedBonus, float newPercentBonus)
    {
        return newFixedBonus + baseValue * (newPercentBonus / 100);
    }
    public float DifferenceAddition(float fixedBonus1, float percentBonus1, float fixedBonus2, float percentBonus2)
    {
        return GetAdditionVirtually(fixedBonus1, percentBonus1) - GetAdditionVirtually(fixedBonus2, percentBonus2);
    }
    
    public float BaseValue
    {
        get => baseValue;
        set
        {
            _onBaseValueChangedAction?.Invoke(baseValue, value);
            if (_onTotalValueChangedAction != null)
            {
                var oldTotal = Total;
                baseValue = value;
                _onTotalValueChangedAction.Invoke(oldTotal,Total);
            }
            else baseValue = value;
        }
    }
    
    public float FixedBonus
    {
        get => fixedBonus;
        set
        {
            _onFixedValueChangedAction?.Invoke(fixedBonus, value);
            if (_onTotalValueChangedAction != null)
            {
                var oldTotal = Total;
                fixedBonus = value;
                _onTotalValueChangedAction.Invoke(oldTotal,Total);
            }
            else fixedBonus = value;
        }
    }

    public float PercentBonus
    {
        get => percentBonus;
        set 
        {
            _onPercentValueChangedAction?.Invoke(fixedBonus, value);
            if (_onTotalValueChangedAction != null)
            {
                var oldTotal = Total;
                percentBonus = value;
                _onTotalValueChangedAction.Invoke(oldTotal,Total);
            }
            else percentBonus = value;
        }
    }
}
