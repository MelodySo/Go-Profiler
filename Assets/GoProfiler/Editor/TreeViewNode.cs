using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System;
namespace GoProfiler
{

    [Serializable]
    public class TreeViewNode : ScriptableObject ,IComparable<TreeViewNode>
    {
        //[NonSerialized]
        protected List<TreeViewNode> nodeList = new List<TreeViewNode>();
        public int size;
        public string sizeStr;
        //[NonSerialized]
        protected TreeViewNode parent;
        protected string mString;
        public bool isFoldout = false;
        protected int level = 0;
		protected static Color32 selectedColor = new Color32(62, 95, 150, 255);
        protected float Height
		{
			get
			{
				float h = 18;
				if (isFoldout)
				{
					for (int i = 0; i < nodeList.Count; i++)
					{
						h += nodeList[i].Height;
					}
				}
				return h;
			}
		}
		public void Clear()
		{
			if (nodeList != null)
			{
				nodeList.Clear();
			}
		}
        public static T BuildNode<T>(string str) where T : TreeViewNode, new()
        {
            var node = ScriptableObject.CreateInstance<T>();
            //T node = new T();
            node.mString = str;
            //node.hideFlags = HideFlags.HideAndDontSave;
            return node;
        }

        public virtual int Convert()
        {
            if (nodeList.Count > 0)
            {
                int sum = 0;
                for (int i = 0; i < nodeList.Count; i++)
                {
                    sum += nodeList[i].Convert();
                }
                size = sum;
                sizeStr = GetFileSize(size);
                return sum;
            }
            return 0;
        }
        public virtual void Sort()
        {
            if (nodeList.Count > 0)
            {
                for (int i = 0; i < nodeList.Count; i++)
                {
                    nodeList[i].Sort();
                }
                nodeList.Sort();
            }
        }
        public TreeViewNode AddNode(TreeViewNode node)
        {
            node.level = level + 1;
            node.parent = this;
            node.AjustLevel();
            nodeList.Add(node);
            return node;
        }
        public bool RemoveNode(TreeViewNode node)
        {
            return nodeList.Remove(node);
        }
        public void AjustLevel()
        {
            if (parent != null)
                this.level = parent.level + 1;
            nodeList.ForEach(p => p.AjustLevel());
        }
        public Rect lastRect;
        void OnEnable() {
            //hideFlags = HideFlags.HideAndDontSave;
        }
        public virtual void OnGUI()
        {
            Rect r = EditorGUILayout.GetControlRect();
            lastRect = r;
            r = new Rect(r.x - 4, r.y - 1, r.width + 8, r.height + 2);
            if (r.Contains(Event.current.mousePosition) && Event.current.type == EventType.mouseDown)
            {
                GoProfilerWindow.selectedObject = this;
            }
            if (GoProfilerWindow.selectedObject == this)
			{
				Rect copyRect = new Rect(r);
				copyRect.xMax -= 200;
				EditorGUI.DrawRect(copyRect, selectedColor);
            }
            if (nodeList.Count > 0)
            {
				isFoldout = EditorGUI.Foldout(new Rect(r.x + r.height * level, r.y, r.height, r.height), isFoldout, "");
            }
            if (parent == null)
            {
				GUI.Label(new Rect(r.x + r.height * (level + 1), r.y, r.width - r.height * (level + 1), r.height), mString);
            }
            else if (parent.isFoldout)
            {
				GUI.Label(new Rect(r.x + r.height * (level + 1), r.y, r.width - r.height * (level + 1), r.height), mString);
            }
            GUI.Label(new Rect(r.x + r.width - 200 + 20, r.y, 100, r.height), sizeStr);

            if (isFoldout)
            {
                nodeList.ForEach(p => p.OnGUI());
            }
        }
        public virtual int CompareTo(TreeViewNode other)
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
    }
}