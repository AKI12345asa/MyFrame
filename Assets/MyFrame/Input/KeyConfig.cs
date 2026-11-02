using System;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
    public class KeyConfig
    {
        public KeyResponseType keyResponseType;
        public bool isCache;
        public KeyType keyType;
        [ShowIf("@(isCache) && (keyResponseType == KeyResponseType.Up || keyResponseType ==  KeyResponseType.Down)")]public float cacheTime;
        [ShowIf("@keyType == KeyType.Keyboard")]public KeyCode defaultKey;
        [ShowIf("@keyType == KeyType.Mouse")]public int defaultMouseID;
        public string keyName;
        [ReadOnly] public string keyGuid;
    }
