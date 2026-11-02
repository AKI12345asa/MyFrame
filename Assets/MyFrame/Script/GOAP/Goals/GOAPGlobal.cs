using System;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

public class GOAPGlobal: SerializedMonoBehaviour
{
        public static GOAPGlobal Instance;
        private void Awake()
        { 
          Instance = this;
        }

        [NonSerialized,OdinSerialize]private GOAPStates _globalStates;
        
        public GOAPStates GlobalStates => _globalStates;

        public bool TryGetGlobalState(string targetState, out GOAPStateBase state)
        {
                state = default;
                if (_globalStates == null || _globalStates.StateDic == null)
                {
                        return false;
                }

                return _globalStates.TryGetState(targetState, out state);
        }
}
