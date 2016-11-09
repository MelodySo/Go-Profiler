using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.MemoryProfiler;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using System.IO;
namespace GoProfiler
{
    public enum ClassIDMap : int
	{
		AnimationClip = 74,
		Animator = 95,
		AnimatorController = 91,
		AssetBundle = 142,
		AudioClip = 83,
		AudioManager = 11,
		AudioSource = 82,
		BoxCollider = 65,
		Camera = 20,
		CubeMap = 89,
		Font = 128,
		GameObject = 1,
		LineRenderer = 120,
		Material = 21,
        Mesh = 43,
		MeshRenderer = 23,
		MonoBehavior = 114,
		MonoScript = 115,
		ParticleSystem = 198,
		ParticleSystemRenderer = 199,
		RenderTexture = 84,
		ResourceManager = 12,
		Rigidbody = 54,
		ScriptMapper=94,
		Shader = 48,
		SkinnedMeshRenderer = 137,
		TextAsset = 49,
		TextMesh = 102,
		Texture2D = 28,
		Transform = 4,
        MeshFilter = 33,
    }
    [Serializable]
    public class PackedMemoryData
    {
        [SerializeField]
		public PackedMemorySnapshot mSnapshot;
    }
    public class GoProfilerWindow : EditorWindow
	{
        [SerializeField]
        TreeViewNode memoryRootNode;
        [SerializeField]
        PackedMemoryData data;
        [SerializeField]
        MemoryFilterSettings memoryFilters;
        protected Vector2 scrollPosition = Vector2.zero;
        int _prevInstance;
        Texture2D _textureObject;
        GUIStyle toolBarStyle;
		bool showObjectInspector = false;
        public static object selectedObject;
        [MenuItem("Window/GoProfiler")]
        static void ShowWindow()
        {
            GoProfilerWindow window = (GetWindow(typeof(GoProfilerWindow)) as GoProfilerWindow);
            window.titleContent = new GUIContent("Go Profiler", AssetPreview.GetMiniTypeThumbnail(typeof(UnityEngine.EventSystems.EventSystem)), "Amazing!");
            window.Init();
        }
        void Init()
        {
            if (memoryRootNode==null)
            {
                Debug.Log("Go-Profiler Init");
                memoryRootNode = TreeViewNode.BuildNode<TreeViewNode>("Root");
            }
			if (data == null)
				data = new PackedMemoryData();

			if(!memoryFilters)
				memoryFilters = AssetDatabase.LoadAssetAtPath<MemoryFilterSettings>("Assets/GoProfiler/Editor/MemoryFilters.asset");

			if (toolBarStyle == null) {
				toolBarStyle = new GUIStyle();
				toolBarStyle.alignment = TextAnchor.MiddleCenter;
				toolBarStyle.normal.textColor = Color.white;
				toolBarStyle.fontStyle = FontStyle.Bold;
			}
        }
        void OnEnable()
        {
            MemorySnapshot.OnSnapshotReceived += IncomingSnapshot;
			Init ();
        }
        public void OnDisable()
        {
            MemorySnapshot.OnSnapshotReceived -= IncomingSnapshot;
            selectedObject = null;
        }
        void OnGUI()
        {
			//if put these code to OnEnable function,the EditorStyles.boldLabel is null , and everything is wrong.

            //Debug.Log("ONGUI"); 
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Take Sample: " + ProfilerDriver.GetConnectionIdentifier(ProfilerDriver.connectedProfiler), EditorStyles.toolbarButton))
            {
                ProfilerDriver.ClearAllFrames();
                ProfilerDriver.deepProfiling = true;
                MemorySnapshot.RequestNewSnapshot();
            }
            if (GUILayout.Button(new GUIContent("Clear Editor References", "Design for profile in editor.\nEditorUtility.UnloadUnusedAssetsImmediate() can be called."), EditorStyles.toolbarButton))
            {
                EditorUtility.UnloadUnusedAssetsImmediate();
                System.GC.Collect();
                Resources.UnloadUnusedAssets();
            }
            if (GUILayout.Button("Save Snapshot", EditorStyles.toolbarButton))
            {
                if (data.mSnapshot != null)
                {
                    string fileName = EditorUtility.SaveFilePanel("Save Snapshot", null, "MemorySnapshot", "memsnap");
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                        using (Stream stream = File.Open(fileName, FileMode.Create))
                        {
							bf.Serialize(stream, data.mSnapshot);
                        }
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning("No snapshot to save.  Try taking a snapshot first.");
                }
            }
            if (GUILayout.Button("Load Snapshot", EditorStyles.toolbarButton))
            {
                string fileName = EditorUtility.OpenFilePanel("Load Snapshot", null, "memsnap");
                if (!string.IsNullOrEmpty(fileName))
                {
                    System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    using (Stream stream = File.Open(fileName, FileMode.Open))
                    {
                        IncomingSnapshot(bf.Deserialize(stream) as PackedMemorySnapshot);
                    }
                }
			}
			GUILayout.FlexibleSpace();
			showObjectInspector = EditorGUILayout.Toggle ("Show In Inspector", showObjectInspector);
            EditorGUILayout.EndHorizontal();
            //Top tool bar end...


            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            memoryFilters = EditorGUILayout.ObjectField(memoryFilters, typeof(MemoryFilterSettings), false) as MemoryFilterSettings;
            if (GUILayout.Button("Save as plist/xml", EditorStyles.toolbarButton))
            {
            }
            if (GUILayout.Button("Load plist/xml", EditorStyles.toolbarButton))
            {
			}
			GUILayout.FlexibleSpace();
			if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Refresh"), EditorStyles.toolbarButton, GUILayout.Width(30)))
			{
				IncomingSnapshot (data.mSnapshot);
				Repaint();
			}
            EditorGUILayout.EndHorizontal();
            if (!memoryFilters) {
                EditorGUILayout.HelpBox("Please Select a MemoryFilters object or load it from the .plist/.xml file", MessageType.Warning);
            }
            //MemoryFilters end...


			Rect titleRect = EditorGUILayout.GetControlRect();
			EditorGUI.DrawRect(titleRect, new Color(0.15f, 0.15f, 0.15f, 1));
			EditorGUI.DrawRect(new Rect(titleRect.x + titleRect.width - 200, titleRect.y, 1, Screen.height), new Color(0.15f, 0.15f, 0.15f, 1));
			EditorGUI.DrawRect(new Rect(titleRect.x + titleRect.width - 100, titleRect.y, 1, Screen.height), new Color(0.15f, 0.15f, 0.15f, 1));
			GUI.Label(new Rect(titleRect.x, titleRect.y, titleRect.width - 200, titleRect.height), "Name", toolBarStyle);
			GUI.Label(new Rect(titleRect.x + titleRect.width - 175, titleRect.y, 50, titleRect.height), "Size", toolBarStyle);
			GUI.Label(new Rect(titleRect.x + titleRect.width - 75, titleRect.y, 50, titleRect.height), "Count", toolBarStyle);
			//Title bar end...

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            memoryRootNode.OnGUI();
            GUILayout.EndScrollView();
            //Contents end...

			//TODO:handle the selected object.
            //UnityEngine.Object selectedObject = Selection.activeObject;
            //if (selectedObject is PackedItemNode)
            //{
            //    if ((selectedObject as PackedItemNode).classID == ClassIDMap.Texture2D)
            //    {
            //        //EditorGUILayout.HelpBox("Watching Texture Detail Data is only for Editor.", MessageType.Warning, true);
            //        PackedItemNode nativeObject = Selection.activeObject as PackedItemNode;
            //        if (_prevInstance != nativeObject.instanceID)
            //        {
            //            _textureObject = EditorUtility.InstanceIDToObject(nativeObject.instanceID) as Texture2D;
            //            _prevInstance = nativeObject.instanceID;
            //            Selection.instanceIDs = new int[] { nativeObject.instanceID };
            //        }
            //        if (_textureObject != null)
            //        {
            //            EditorGUILayout.LabelField("textureInfo: " + _textureObject.width + "x" + _textureObject.height + " " + _textureObject.format);
            //            EditorGUILayout.ObjectField(_textureObject, typeof(Texture2D),false);
            //            _textureSize = EditorGUILayout.Slider(_textureSize, 100.0f, 1024.0f);
            //            GUILayout.Label(_textureObject, GUILayout.Width(_textureSize), GUILayout.Height(_textureSize * _textureObject.height / _textureObject.width));
            //        }
            //        else
            //        {
            //            EditorGUILayout.LabelField("Can't instance texture,maybe it was already released.");
            //        }
            //    }
            //}

            //handle the select event to Repaint
            if (Event.current.type == EventType.mouseDown)
            {
                Repaint();
            }
        }
        void IncomingSnapshot(PackedMemorySnapshot snapshot)
        {
            if (null==snapshot)
                return;
            Debug.Log("GoProfilerWindow.IncomingSnapshot");
			data.mSnapshot = snapshot;
            memoryRootNode.Clear();
            TreeViewNode assetsRoot = TreeViewNode.BuildNode<TreeViewNode>("Assets");
            TreeViewNode sceneMemoryRoot = TreeViewNode.BuildNode<TreeViewNode>("SceneMemory");
            PackedItemNode managerNode = TreeViewNode.BuildNode<PackedItemNode>("Managers");
            memoryRootNode.AddNode(assetsRoot);
            memoryRootNode.AddNode(sceneMemoryRoot);
            assetsRoot.AddNode(managerNode);

            Dictionary<int, Group> assetGroup = new Dictionary<int, Group>();
            Dictionary<int, Group> sceneMemoryGroup = new Dictionary<int, Group>();
            List<PackedNativeUnityEngineObject> assetsObjectList = new List<PackedNativeUnityEngineObject>();
            List<PackedNativeUnityEngineObject> sceneMemoryObjectList = new List<PackedNativeUnityEngineObject>();
            //List<PackedNativeUnityEngineObject> managerList = new List<PackedNativeUnityEngineObject>();
            for ( int i=0;i< snapshot.nativeObjects.Length;i++)
            {
                PackedNativeUnityEngineObject obj = snapshot.nativeObjects[i];
					if (obj.isPersistent && ((obj.hideFlags & HideFlags.DontUnloadUnusedAsset) == 0))
                {
                        assetsObjectList.Add(obj);
                }
                else if (!obj.isPersistent && obj.hideFlags == HideFlags.None)
                {
                        sceneMemoryObjectList.Add(obj);
					
                }
            }
            for (int i = 0; i < assetsObjectList.Count; i++)
            {
                PackedNativeUnityEngineObject assetsObject = assetsObjectList[i];
                if (!assetGroup.ContainsKey(assetsObject.classId)) 
					assetGroup.Add(assetsObject.classId, new Group(assetsObject.classId,data.mSnapshot.nativeTypes[assetsObject.classId].name));                
                assetGroup[assetsObject.classId].packedNativeObjectList.Add(assetsObject);
            }
            for (int i = 0; i < sceneMemoryObjectList.Count; i++)
            {
                PackedNativeUnityEngineObject sceneObject = sceneMemoryObjectList[i];
                if (!sceneMemoryGroup.ContainsKey(sceneObject.classId))
					sceneMemoryGroup.Add(sceneObject.classId, new Group(sceneObject.classId,data.mSnapshot.nativeTypes[sceneObject.classId].name));
                sceneMemoryGroup[sceneObject.classId].packedNativeObjectList.Add(sceneObject);
            }

            using (var i = assetGroup.GetEnumerator())//replace foreach
            {
                while (i.MoveNext())
                {
                    Group group = i.Current.Value;
                    SetNodeByClassID(group.classId, group.itemNode, group.packedNativeObjectList);
                    if (group.itemNode != null)
                    {
                        assetsRoot.AddNode(group.itemNode);
                    }
                }
            }
            using (var i = sceneMemoryGroup.GetEnumerator())//replace foreach
            {
                while (i.MoveNext())
                {
                    Group group = i.Current.Value;
                    SetNodeByClassID(group.classId, group.itemNode, group.packedNativeObjectList);
                    if (group.itemNode != null)
                    {
                        sceneMemoryRoot.AddNode(group.itemNode);
                    }
                }
            }
            memoryRootNode.Convert();
            memoryRootNode.Sort();
        }
		void SetNodeByClassID(int classID, PackedItemNode nodeRoot, List<PackedNativeUnityEngineObject> nativeUnityObjectList)
        {
            nodeRoot.Clear();
			nodeRoot.nativeType = data.mSnapshot.nativeTypes [classID];

            int index = -1;
            if (memoryFilters) {
                for (int i = 0; i < memoryFilters.memoryFilterList.Count; i++) {
					if ((int)memoryFilters.memoryFilterList[i].iD == classID)
                    {
                        index = i;
                    }
                }
            }

			if (index > -1)//这样写好蛋疼啊0.0
            {
                Dictionary<PackedItemNode, RegexElement> tempDict = new Dictionary<PackedItemNode, RegexElement>();
                PackedItemNode otherNode = TreeViewNode.BuildNode<PackedItemNode>("Others");
				otherNode.nativeType = data.mSnapshot.nativeTypes [classID];
                nodeRoot.AddNode(otherNode);
                MemoryFilter memoryFilter = memoryFilters.memoryFilterList[index];
                for (int i = 0; i < memoryFilter.regexElementList.Count; i++)
                {
                    PackedItemNode filterNode = TreeViewNode.BuildNode<PackedItemNode>(memoryFilter.regexElementList[i].key);
					filterNode.nativeType = data.mSnapshot.nativeTypes [classID];
                    nodeRoot.AddNode(filterNode);
                    tempDict.Add(filterNode, memoryFilter.regexElementList[i]);
                }
                while(nativeUnityObjectList.Count>0)
                {
                    PackedNativeUnityEngineObject item = nativeUnityObjectList[0];
                    string name = item.name;
                    PackedItemNode childNode = TreeViewNode.BuildNode<PackedItemNode>(name);
                    childNode.itemName = name;
                    childNode.size = item.size;
                    childNode.instanceID = item.instanceId;
					childNode.nativeType = data.mSnapshot.nativeTypes [classID];

                    bool isMatch = false;
                    using (var i = tempDict.GetEnumerator())//replace foreach
                    {
                        while (i.MoveNext())
                        {
                            RegexElement regexElement = i.Current.Value;

                            for (int j = 0; j < regexElement.regexList.Count; j++)
                            {
                                if (StringMatchWith(name, regexElement.regexList[j]))
                                {
                                    i.Current.Key.AddNode(childNode);
                                    isMatch = true;
                                    break;
                                }
                            }
                        }
					}
					if (!isMatch) {
						otherNode.AddNode(childNode);
					}
                    nativeUnityObjectList.RemoveAt(0);
                }
            }
            else
            {
                for (int i = 0; i < nativeUnityObjectList.Count; i++)
                {
                    PackedNativeUnityEngineObject item = nativeUnityObjectList[i];
                    string name = item.name;
                    PackedItemNode node = TreeViewNode.BuildNode<PackedItemNode>(name);
                    node.itemName = name;
                    node.size = item.size;
                    node.instanceID = item.instanceId;
					node.nativeType = data.mSnapshot.nativeTypes [classID];
                    nodeRoot.AddNode(node);
                }
            }
        }
        static bool StringStartWith(string name,List<string> filter)
        {
            for (int i = 0; i < filter.Count; i++)
            {
                if (string.IsNullOrEmpty(filter[i]))
                    continue;
                if (name.StartsWith(filter[i]))
                    return true;
            }
            return false;
        }
        static bool StringStartWith(string name, string[] filter)
        {
            for (int i = 0; i < filter.Length; i++)
            {
                if (string.IsNullOrEmpty(filter[i]))
                    continue;
                if (name.StartsWith(filter[i]))
                    return true;
            }
            return false;
        }
        static bool StringMatchWith(string name, string filter)
        {
            Regex regex = new Regex(filter, RegexOptions.None);

            for (int i = 0; i < filter.Length; i++)
            {
                if (regex.IsMatch(name))
                    return true;
            }
            return false;
        }
    }
}