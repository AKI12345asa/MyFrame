using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

[CreateAssetMenu(fileName = "节点树存储", menuName = "NodeSystemTree")]
public class NodeTreeDataManager : SerializedScriptableObject
{
    [NonSerialized,OdinSerialize]public Dictionary<string,GameObject> GameObjectDictionary;
} 
