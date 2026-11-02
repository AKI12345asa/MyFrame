using System;
using System.Collections.Generic;
using MyFrame;
using UnityEngine;

[Serializable]
public class InputManager:SingletonMono<InputManager>
{
    private static Dictionary<Action<KeyData>,List<KeyData>> _inputActionDic = new(); 
    private static Dictionary<string,KeyData> _keyDataWithConfigId = new();

    private void Start()
    {
        _startListening = false;
        DontDestroyOnLoad(this);
    }
    private static Dictionary<Action<KeyData>,KeyData> _inputKeyInvokeDic = new();
    private static List<Action<KeyData>> _inputNullInvokeList = new();
    private void Update()
    {
        if (!_startListening)
        {
            foreach (var actionWithKey in _inputActionDic)
            {
                var isTrigger = false;
                foreach (var key in actionWithKey.Value)
                {
                    if (isTrigger) continue;
                    key.Update();
                    if (key.GetKeyState())
                    {
                        _inputKeyInvokeDic.TryAdd(actionWithKey.Key, key);
                        isTrigger = true;
                    }
                }
                if (!isTrigger)
                {
                    _inputNullInvokeList.Add(actionWithKey.Key);
                }
            }
            foreach (var actionWithKey in _inputKeyInvokeDic)
            {
                actionWithKey.Key.Invoke(actionWithKey.Value);
            }
            _inputKeyInvokeDic.Clear();
            foreach (var action in _inputNullInvokeList)
            {
                action.Invoke(null);
            }
            _inputNullInvokeList.Clear();
        }
        UpdateKeyListening();
    }

    #region 输入绑定

    public static KeyData BindKeyboardKey(KeyCode keyCode,Action<KeyData> action,KeyConfig keyConfig,Func<bool> inputChecker = null)
    {   
        var keyData = new KeyData
        {
            KeyBoardKey = new KeyBoardKey
            {
                keyCode = keyCode,
                isCache = keyConfig.isCache,
                cacheTime = keyConfig.cacheTime,
                InputChecker = inputChecker,
            },
            KeyName = keyConfig.keyName,
            keyResponseType =  keyConfig.keyResponseType,
            bindConfigGuid = keyConfig.keyGuid,
        };
        if (action != null &&_inputActionDic.ContainsKey(action))
        {
            _inputActionDic[action].Add(keyData);
        }
        else
        {
            _inputActionDic.TryAdd(action, new List<KeyData>() { keyData });
        }
        _keyDataWithConfigId.TryAdd(keyConfig.keyGuid, keyData);
        return keyData;
    }

    public static KeyData BindMouseKey(int mouseID, Action<KeyData> action,KeyConfig keyConfig,Func<bool> inputChecker = null)
    {
        var keyData = new KeyData
        {
            MouseKey = new MouseKey
            {
                mouseButtonID = mouseID,
                isCache = keyConfig.isCache,
                cacheTime = keyConfig.cacheTime,
                InputChecker = inputChecker,
            },
            KeyName = keyConfig.keyName,
            keyResponseType =  keyConfig.keyResponseType,
            bindConfigGuid = keyConfig.keyGuid,
        };
        if (_inputActionDic.ContainsKey(action))
        {
            _inputActionDic[action].Add(keyData);
        }
        else
        {
            _inputActionDic.Add(action, new List<KeyData>() { keyData });
        }
        _keyDataWithConfigId.TryAdd(keyConfig.keyGuid, keyData);
        return keyData;
    }

    public static void BindKeyData(KeyData keyData, Action<KeyData> action, KeyConfig keyConfig, Func<bool> inputChecker = null)
    {
        if (_inputActionDic.ContainsKey(action))
        {
            _inputActionDic[action].Add(keyData);
        }
        else
        {
            _inputActionDic.Add(action, new List<KeyData>() { keyData });
        }
        _keyDataWithConfigId.TryAdd(keyConfig.keyGuid, keyData);
    }
    
    public KeyData GetKeyByConfig(KeyConfig keyConfig)
    {
        return _keyDataWithConfigId.GetValueOrDefault(keyConfig.keyGuid);
    }

    public KeyData GetKeyByConfigId(string configId)
    {
        return _keyDataWithConfigId.GetValueOrDefault(configId);
    }
    
    public static KeyData BindKeyboardKey(KeyCode keyCode, Action<KeyData> action, float cacheTime = 0, string keyName = null,KeyResponseType keyResponseType = KeyResponseType.Down,Func<bool> inputChecker = null)
    {
        var keyData = new KeyData
        {
            KeyBoardKey = new KeyBoardKey
            {
                keyCode = keyCode,
                isCache = cacheTime > 0,
                cacheTime = cacheTime,
                InputChecker = inputChecker,
            },
            KeyName = keyName,
            keyResponseType =  keyResponseType,
        };
        if (_inputActionDic.ContainsKey(action))
        {
            _inputActionDic[action].Add(keyData);
        }
        else
        {
            _inputActionDic.TryAdd(action, new List<KeyData>() { keyData });
        }
        return keyData;
    }

    public static KeyData BindMouseKey(int mouseID, Action<KeyData> action, float cacheTime, string keyName,KeyResponseType keyResponseType,Func<bool> inputChecker = null)
    {
        var keyData = new KeyData
        {
            MouseKey = new MouseKey
            {
                mouseButtonID = mouseID,
                isCache = cacheTime > 0,
                cacheTime = cacheTime,
                InputChecker = inputChecker,
            },
            KeyName = keyName,
            keyResponseType = keyResponseType,
        };
        if (_inputActionDic.ContainsKey(action))
        {
            _inputActionDic[action].Add(keyData);
        }
        else
        {
            _inputActionDic.TryAdd(action, new List<KeyData>() { keyData });
        }
        return keyData;
    }
    
    public static void RemoveAction(Action<KeyData> action)
    {
        if (_inputActionDic.ContainsKey(action))
        {
            _inputActionDic[action].ForEach(keyData =>
            {
                if(keyData.bindConfigGuid != null) _keyDataWithConfigId.Remove(keyData.bindConfigGuid);
            });
        }
        _inputActionDic.Remove(action);
    }

    public static void RemoveKey(Action<KeyData> action,KeyData keyData)
    {
        if (_inputActionDic.ContainsKey(action))
        {
            _inputActionDic[action].Remove(keyData);
            _keyDataWithConfigId.Remove(keyData.bindConfigGuid);
            if (_inputActionDic[action].Count == 0)
            {
                _inputActionDic.Remove(action);
            }
        }
    }

    public static void RemoveKey(Action<KeyData> action, string keyName)
    {
        if (_inputActionDic.ContainsKey(action))
        {
            var keyData = _inputActionDic[action].Find(x => x.KeyName == keyName);
            _inputActionDic[action].Remove(keyData);
            _keyDataWithConfigId.Remove(keyData.bindConfigGuid);
            if (_inputActionDic[action].Count == 0)
            {
                _inputActionDic.Remove(action);
            }
        }
    }
    
    #endregion

    #region 输入遍历监听

    private bool _startListening;
    private const int MouseKeyNumber = 2;
    private ListeningState _listeningState;
    private event Action<KeyCode> KeyListeningEvent;
    private event Action<int> MouseListeningEvent;
    public void StartListeningInputKey(ListeningState listeningState = ListeningState.OnlyKeyBoard)
    {
        _startListening = true;
        _listeningState = listeningState;
    }
    public void RegisterKeyListeningEvent(Action<KeyCode> action)
    {
        KeyListeningEvent += action;
    }

    public void RegisterMouseListeningEvent(Action<int> action)
    {
        MouseListeningEvent += action;
    }

    public void ClearEvent()
    {
        KeyListeningEvent = null;
        MouseListeningEvent = null;
    }

    public void UpdateKeyListening()
    {
        if (!_startListening) return;
        switch (_listeningState)
        {
            case ListeningState.OnlyMouse:
                ListeningInputMouse();
                break;
            case ListeningState.OnlyKeyBoard:
                ListeningInputKeyBoard();
                break;
            case ListeningState.BothMouseAndKeyboard:
                ListeningInputKeyBoard();
                ListeningInputMouse();
                break;
        }
    }
    
    private void ListeningInputKeyBoard()
    {
        if (Input.anyKeyDown)
        {
            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                if(key == KeyCode.Mouse0 || key == KeyCode.Mouse1 || key == KeyCode.Mouse2 || key == KeyCode.Mouse3 || key == KeyCode.Mouse4 || key == KeyCode.Mouse5 || key == KeyCode.Mouse6) continue;
                if (Input.GetKeyDown(key))
                {
                    KeyListeningEvent?.Invoke(key);
                    ClearEvent();
                    _startListening = false;
                    return;
                }
            }
        }
    }

    private void ListeningInputMouse()
    {
        if (Input.anyKeyDown)
        {
            for(int i = 0; i < MouseKeyNumber; i++)
            {
                if (Input.GetMouseButtonDown(i))
                {
                    MouseListeningEvent?.Invoke(i);
                    ClearEvent();
                    _startListening = false;
                    return;
                }
            }
        }
    }

    public enum ListeningState
    {
        OnlyMouse,
        OnlyKeyBoard,
        BothMouseAndKeyboard
    }
    #endregion
    
    public Vector2 GetMoveInput()
    {
            var h = Input.GetAxis("Horizontal");
            var v = Input.GetAxis("Vertical");
            var dir = new Vector2(h,v);
            if (dir.magnitude > 1)
            {
                dir.Normalize();
            }
            return dir;
    }
}
