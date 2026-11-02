using System.Collections.Generic;
using GOAP;

public interface IGOAPOwner
{
    public void UpdateGoapStates(IValueBinder valueBinder, GOAPStateBase stateBase, GOAPStateBaseType stateBaseType);
    
    public List<StateAndPropertyBinder> Binders { get;  }
}
