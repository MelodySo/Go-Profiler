using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.MemoryProfiler;
namespace GoProfiler
{
    [Serializable]
    public class Group
    {
		public Group(int classId,string typeName) {
			this.classId = classId;
			packedNativeObjectList = new List<PackedNativeUnityEngineObject> ();
			itemNode = new PackedItemNode(typeName);
		}
        public int classId;
        public PackedItemNode itemNode;
        public List<PackedNativeUnityEngineObject> packedNativeObjectList;
    }
}
