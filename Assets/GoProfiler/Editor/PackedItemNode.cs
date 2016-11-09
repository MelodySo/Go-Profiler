using UnityEngine;
using System.Collections;
using UnityEditor;
using System;
using UnityEditor.MemoryProfiler;
namespace GoProfiler
{
    [Serializable]
    public class PackedItemNode : TreeViewNode
    {
        public string itemName;
        public int instanceID;
		public PackedNativeType nativeType;
        private Texture2D icon;//这个如果换成System.Type会有问题的。。
        GUIContent guiContent;
        public override int Convert()
        {
            if (nodeList.Count > 0)
            {
                int sum = 0;
                for (int i = 0; i < nodeList.Count; i++)
                {
                    sum += (nodeList[i] as PackedItemNode).Convert();
                }
                size = sum;
            }
            sizeStr = GetFileSize(size);
			itemName = mString;
			string iconClassString = nativeType.name;
            Type iconClassType = Type.GetType("UnityEngine." + iconClassString + ",UnityEngine", false, false);
            if(iconClassType ==null)
                iconClassType = Type.GetType("UnityEditor." + iconClassString + ",UnityEditor", false, false);
            icon = AssetPreview.GetMiniTypeThumbnail(iconClassType == null ? typeof(GameObject) : iconClassType);
            guiContent = new GUIContent(itemName, icon);
            return size;
        }
        public override void Sort() {
            if (nodeList.Count > 0)
            {
                for (int i = 0; i < nodeList.Count; i++)
                {
                    nodeList[i].Sort();
                }
                nodeList.Sort();
            }
        }
        public override void OnGUI()
        {
            Rect r = EditorGUILayout.GetControlRect();
            r = new Rect(r.x - 4, r.y - 1, r.width + 8, r.height + 2);
            if (r.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.mouseDown)
                {
                    GoProfilerWindow.selectedObject = this;
                    //Selection.activeObject = this;
                }
            }

            if (GoProfilerWindow.selectedObject == this)
            {
                Rect copyRect = new Rect(r);
                copyRect.xMax -= 200;
                EditorGUI.DrawRect(copyRect, selectedColor);
            }
            else
            {
                //EditorGUI.DrawRect(r,_unselectColor);
            }

//            float _levelOffset = r.height;
            if (nodeList.Count > 0)
            {
                isFoldout = EditorGUI.Foldout(new Rect(r.x + r.height * level, r.y, r.height, r.height), isFoldout, guiContent);
            }
            else {
                EditorGUILayout.BeginHorizontal();
                //GUI.DrawTexture(new Rect(r.x + r.height * (level + 1) - 5, r.y, r.height - 2, r.height - 2), icon);
                GUI.Label(new Rect(r.x + r.height * (level + 1) -5, r.y, r.width - r.height * (level + 1) - 200, r.height), guiContent);
                EditorGUILayout.EndHorizontal();
            }
			GUI.Label(new Rect(r.x + r.width - 200 + 20, r.y, 100, r.height), sizeStr);
			if (nodeList != null && nodeList.Count > 0) {
				GUI.Label(new Rect(r.x + r.width - 50 , r.y, 100, r.height), nodeList.Count.ToString());
			}

            //if (parent.isFoldout)
            //{
            //            EditorGUILayout.BeginHorizontal();
            //GUI.DrawTexture(new Rect(r.x + r.height * (level + 1) - 5, r.y, r.height - 2, r.height - 2), icon);

            //GUI.Label(new Rect(r.x + r.height * (level + 1) + 10, r.y, r.width - r.height * (level + 1) - 200, r.height), itemName);
            //            GUI.Label(new Rect(r.x + r.width - 200 + 20, r.y, 100, r.height), sizeStr);
            //            EditorGUILayout.EndHorizontal();
            //}

            if (isFoldout)
            {
                nodeList.ForEach(p => p.OnGUI());
            }
        }
    }
}