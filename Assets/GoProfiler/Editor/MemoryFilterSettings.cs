using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEditor;
[Serializable]
public class MemoryFilterSettings : ScriptableObject{
	[SerializeField]
	public List<MemoryFilter> memoryFilterList = new List<MemoryFilter>();
}