//using UnityEngine;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEditor;
//using System;
//namespace GoProfiler
//{
//    [Serializable]
//    public class TreeViewNode /*ScriptableObject,*/  //TODO:not inherit from ScriptableObject
//    {
//        public virtual void Sort()
//        {
//            if (childList.Count > 0)
//            {
//                for (int i = 0; i < childList.Count; i++)
//                {
//                    childList[i].Sort();
//                }
//                childList.Sort();
//            }
//        }

//        public virtual void DrawGUI(int guiLevel = 0)
//        {
//            Rect r = EditorGUILayout.GetControlRect();
//            lastRect = r;
//            r = new Rect(r.x - 4, r.y - 1, r.width + 8, r.height + 2);
//            if (r.Contains(Event.current.mousePosition) && Event.current.type == EventType.mouseDown)
//            {
//                GoProfilerWindow.selectedObject = this;
//            }
//            if (GoProfilerWindow.selectedObject == this)
//            {
//                Rect copyRect = new Rect(r);
//                copyRect.xMax -= 200;
//                EditorGUI.DrawRect(copyRect, selectedColor);
//            }
//            if (childList.Count > 0)
//            {
//                isFoldout = EditorGUI.Foldout(new Rect(r.x + r.height * guiLevel, r.y, r.height, r.height), isFoldout, "");
//            }
//            GUI.Label(new Rect(r.x + r.height * (guiLevel + 1), r.y, r.width - r.height * (guiLevel + 1), r.height), itemName);
//            GUI.Label(new Rect(r.x + r.width - 200 + 20, r.y, 100, r.height), sizeStr);

//            if (isFoldout)
//            {
//                childList.ForEach(p => p.DrawGUI(guiLevel + 1));
//            }
//        }
//    }
//}