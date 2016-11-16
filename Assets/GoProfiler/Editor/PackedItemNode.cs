using UnityEngine;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using System;
using UnityEditor.MemoryProfiler;
namespace GoProfiler
{
    public class PackedItemNode : IComparable<PackedItemNode>
    {
        public int instanceID;
        public PackedNativeType nativeType;
        [SerializeField]
        private Texture2D icon;//这个如果换成System.Type会有问题的。。。 
        [NonSerialized]
        GUIContent guiContent;
        [SerializeField]
        public List<PackedItemNode> childList = new List<PackedItemNode>();//TODO:avoid serialize issue which caused the reference dependency loop.
        public Rect lastRect;
        public int size;
        public string sizeStr;
        public string itemName;
        public bool isFoldout = false;
        readonly static Color32 selectedColor = new Color32(62, 95, 150, 255);
        [SerializeField]
        bool isCompositeNode = false;
        [SerializeField]
        int totalNumber = 0;
        public PackedItemNode(string itemName)
        {
            this.itemName = itemName;
        }
        public PackedItemNode(string itemName, bool isCompositeNode)
        {
            this.itemName = itemName;
            this.isCompositeNode = isCompositeNode;
        }
        public void Clear()
        {
            if (childList != null)
            {
                childList.Clear();
            }
        }
        public PackedItemNode AddNode(PackedItemNode node)
        {
            childList.Add(node);
            return node;
        }
        public bool RemoveNode(PackedItemNode node)
        {
            return childList.Remove(node);
        }
        public int SetCount()
        {
            totalNumber = 0;
            for (int i = 0; i < childList.Count; i++)
            {
                if(!childList[i].isCompositeNode)
                    totalNumber += 1;
                totalNumber += childList[i].SetCount();
            }
            return totalNumber;
        }
        public int Convert()
        {
            if (childList.Count > 0)
            {
                int sum = 0;
                for (int i = 0; i < childList.Count; i++)
                {
                    sum += childList[i].Convert();
                }
                size = sum;
            }
            sizeStr = GetFileSize(size);
            SetGUIContent();
            return size;
        }
        void SetGUIContent()
        {
            string iconClassString = nativeType.name;
            if (!string.IsNullOrEmpty(iconClassString) && iconClassString.EndsWith("Manager"))
            {
                //set manager icon correctly.
                iconClassString = "AudioManager";
            }
            Type iconClassType = Type.GetType("UnityEngine." + iconClassString + ",UnityEngine", false, false);
            if (iconClassType == null)
                iconClassType = Type.GetType("UnityEditor." + iconClassString + ",UnityEditor", false, false);
            icon = AssetPreview.GetMiniTypeThumbnail(iconClassType);
            //icon = AssetPreview.GetMiniTypeThumbnail(iconClassType == null ? typeof(GameObject) : iconClassType);
            if (icon)
                icon.hideFlags = HideFlags.HideAndDontSave;
            guiContent = new GUIContent(childList.Count > 0 ? string.Format("{0}({1})", itemName, totalNumber) : itemName, icon);
        }
        public void Sort()
        {
            if (childList.Count > 0)
            {
                for (int i = 0; i < childList.Count; i++)
                {
                    childList[i].Sort();
                }
                childList.Sort();
            }
        }
        public void DrawGUI(int guiLevel = 0)
        {
            Rect r = EditorGUILayout.GetControlRect();
            r = new Rect(r.x - 4, r.y - 1, r.width + 8, r.height + 2);
            if (r.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.mouseDown)
                {
                        GoProfilerWindow.selectedObject = this;
                }
            }

            if (GoProfilerWindow.selectedObject == this)
            {
                Rect copyRect = new Rect(r);
                copyRect.xMax -= 200;
                EditorGUI.DrawRect(copyRect, selectedColor);
            }
            if (guiContent == null || guiContent.image == null)
            {
                SetGUIContent();//TODO:When reload the assembly.... 
            }
            if (childList.Count > 0)
            {
                isFoldout = EditorGUI.Foldout(new Rect(r.x + r.height * guiLevel, r.y, r.height, r.height), isFoldout, guiContent, EditorStyles.foldout);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                GUI.Label(new Rect(r.x + r.height * (guiLevel + 1) - 5, r.y, r.width - r.height * (guiLevel + 1) - 200, r.height), guiContent, EditorStyles.label);
                EditorGUILayout.EndHorizontal();
            }
            GUI.Label(new Rect(r.x + r.width - 200 + 20, r.y, 100, r.height), sizeStr);
            //GUI.Label(new Rect(r.x + r.width - 50 , r.y, 100, r.height), childList.Count.ToString());
            if (isFoldout)
            {
                childList.ForEach(p => p.DrawGUI(guiLevel + 1));
            }
        }
        public virtual int CompareTo(PackedItemNode other)
        {
            return other.size.CompareTo(size);
        }
        /// <summary>  
        /// 根据内存字节返回相应阶段的内存大小符号  
        /// </summary>  
        /// <param name="size">多少byte</param>  
        /// <returns></returns>  
        public static string GetFileSize(int size)
        {
            string sizeString = "";
            //大于等于1MB = 1*1024KB*1024B  
            if (size >= 1024 * 1024)
                sizeString = (size / 1048576f).ToString("0.00") + "MB";
            //大于1KB = 1*1024B  
            else if (size >= 1024)
                sizeString = (size / 1024f).ToString("0.00") + "KB";
            //大于1B  
            else //if (size >= 1)
                sizeString = size + "B";
            return sizeString;
        }
        /// <summary>
        /// Save tree to mindmap.
        /// </summary>
        /// <param name="mindMapLevel">The tree level.</param>
        /// <param name="showDetail">whether to show the details of each single item.</param>
        /// <param name="minSize">minimum bytes</param>
        /// <returns></returns>
        public string ToMindMap(int mindMapLevel, bool showDetail, int minSize)
        {
            StringBuilder sb = new StringBuilder();
            if (childList.Count > 0)
            {
                sb.Append(ToMindMapLine(mindMapLevel));
                for (int i = 0; i < childList.Count; i++)
                {
                    if (childList[i].size > minSize)
                        sb.Append(childList[i].ToMindMap(mindMapLevel + 1, showDetail, minSize));
                }
                return sb.ToString();
            }
            else if (showDetail)
            {
                sb.Append(ToMindMapLine(mindMapLevel));
                return sb.ToString();
            }
            else
                return string.Empty;
        }
        private StringBuilder ToMindMapLine(int mindMapLevel)
        {
            StringBuilder sb = new StringBuilder();
            int n = 0;
            while (mindMapLevel > n++)
            {
                sb.Append('\t');
            }
            sb.Append(itemName);
            sb.Append(' ');
            sb.Append(sizeStr);
            sb.Append('\n');
            return sb;//.ToString();
        }
    }
}