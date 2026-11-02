using System;
using System.Collections.Generic;
using UnityEngine;

public enum KeyResponseType
{
    Up,
    Down,
    Hold,
}
public enum KeyType
{
    Mouse,
    Keyboard,
}
[Serializable]
public class KeyData
{
    public KeyBoardKey KeyBoardKey;
    public MouseKey MouseKey;
    public KeyResponseType keyResponseType;
    public string KeyName;
    public string bindConfigGuid;
    public bool IfCached => bindConfigGuid != null;

    /// <summary>
    /// 是否按下
    /// </summary>
    /// <returns></returns>
    public bool GetKeyDown()
    {
       return  ((KeyBoardKey != null && KeyBoardKey.GetKeyDown()) || (MouseKey != null && MouseKey.GetKeyDown()));
    }

    /// <summary>
    /// 检查按键状态
    /// </summary>
    /// <returns></returns>
    public bool GetKeyState()
    {
        switch (keyResponseType)
        {
            case KeyResponseType.Up:
                return GetKeyUp();
            case KeyResponseType.Down:
                return GetKeyDown();
            case KeyResponseType.Hold:
                return GetKeyHold();
            default:
                return false;
        }
    }
    
    /// <summary>
    /// 是否有被按住
    /// </summary>
    /// <returns></returns>
    public bool GetKeyHold()
    {
        return  ((KeyBoardKey != null && KeyBoardKey.GetKeyHold()) || (MouseKey != null && MouseKey.GetKeyHold()));
    }

    public bool GetKeyUp()
    {
        return ((KeyBoardKey != null && KeyBoardKey.GetKeyUp()) || (MouseKey != null && MouseKey.GetKeyUp()));
    }
    
    public void Update()
    {
        KeyBoardKey?.Update();
        MouseKey?.Update();
    }

    public void ResetCacheTimer()
    {
        KeyBoardKey?.ResetCacheTimer();
        MouseKey?.ResetCacheTimer();
    }
}

[Serializable]
public class KeyBoardKey
{
    public KeyCode keyCode;
    public bool isCache;
    public float cacheTime;
    [NonSerialized]private float _lastDownInputTime;
    [NonSerialized]private float _lastUpInputTime;
    public Func<bool> InputChecker;    

    public bool GetKeyDown()
    {
        if (InputChecker != null && !InputChecker()) return false;
        if (!isCache) return Input.GetKeyDown(keyCode);
        return Input.GetKeyDown(keyCode) || (Time.time - _lastDownInputTime) < cacheTime;
    }


    public bool GetKeyUp()
    {
        if (InputChecker != null && !InputChecker()) return false;
        if (!isCache) return Input.GetKeyUp(keyCode);
        return Input.GetKeyUp(keyCode) || (Time.time - _lastUpInputTime) < cacheTime;
    }
    
    public bool GetKeyHold()
    {
        if (InputChecker != null && !InputChecker()) return false;
        return Input.GetKey(keyCode);
    }
    public void Update()
    {
        if (InputChecker != null && !InputChecker()) return;
        if(!isCache) return;
        if (Input.GetKeyDown(keyCode))
        {
            _lastDownInputTime = Time.time;
        }

        if (Input.GetKeyUp(keyCode))
        {
            _lastUpInputTime = Time.time;
        }
    }

    public void ResetCacheTimer()
    {
        _lastDownInputTime = float.MinValue; //一个不可能成立的时间,肯定无法触发缓存
        _lastUpInputTime = float.MinValue;
    }
} 




  
[Serializable]
public class MouseKey
{
    public int mouseButtonID;
    public bool isCache;
    public float cacheTime;
    private float _lastDownInputTime;
    private float _lastUpInputTime;
    public Func<bool> InputChecker;    
    public bool valid;

    public bool GetKeyDown()
    {
        if (InputChecker != null && !InputChecker()) return false;
        if (!isCache) return Input.GetMouseButtonDown(mouseButtonID); ;
        return Input.GetMouseButtonDown(mouseButtonID) || (Time.time - _lastDownInputTime) < cacheTime;
    }

    public bool GetKeyHold()
    {
        if (InputChecker != null && !InputChecker()) return false;
        return Input.GetMouseButton(mouseButtonID);
    }
        
    public void Update()
    {
        if (InputChecker != null && !InputChecker()) return;
        if(!isCache) return;
        if (Input.GetMouseButtonDown(mouseButtonID))
        {
            _lastDownInputTime = Time.time;
        }
        
        if (Input.GetMouseButtonUp(mouseButtonID))
        {
            _lastUpInputTime = Time.time;
        }
    }
        
    public void ResetCacheTimer()
    {
        _lastDownInputTime = float.MinValue; //一个不可能成立的时间,肯定无法触发缓存
        _lastUpInputTime = float.MinValue;
    }

    public bool GetKeyUp()
    {
        if (InputChecker != null && !InputChecker()) return false;
        if (!isCache) return Input.GetMouseButtonUp(mouseButtonID);
        return Input.GetMouseButtonUp(mouseButtonID) || (Time.time - _lastUpInputTime) < cacheTime;
    }
}

[Serializable]
public class KeyDatas
{
    public Serialized_Dic<string, KeyData> keyDataDic = new();

    public bool TryGetKeyDataByID(string key,out KeyData keyData)
    {
        if (keyDataDic.Dictionary.TryGetValue(key, out keyData))
        {
            return true;
        }
        return false;
    }
}