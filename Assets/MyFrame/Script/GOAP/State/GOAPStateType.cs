using System.Collections.Generic;
using Sirenix.OdinInspector;

[HideLabel]
public struct GOAPStateType
{
   [HideLabel,ValueDropdown("GetAllState")]public string Name;

   public static implicit operator GOAPStateType(string name)
   {
      return new  GOAPStateType() { Name = name };
   }

   public static implicit operator string(GOAPStateType state)
   {
      return state.Name;
   }
#if UNITY_EDITOR
   private List<string> GetAllState()
   {
      var res = new List<string>();
      var global = GOAPEditorUtility.Global;
      if (global != null && global.GlobalStates != null && global.GlobalStates.StateDic != null)
      {
         foreach (var item in global.GlobalStates.StateDic)
         {
            res.Add(item.Key);
         }
      }

      if (GOAPEditorUtility.Agent != null && GOAPEditorUtility.Agent.States != null && GOAPEditorUtility.Agent.States.StateDic != null)
      {
         foreach (var item in GOAPEditorUtility.Agent.States.StateDic)
         {
            res.Add(item.Key);
         }
      }
      return res;
   }
#endif
}
