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
        PackedItemNode memoryRootNode;
        [SerializeField]
        PackedMemoryData data;
        [SerializeField]
        MemoryFilterSettings memoryFilters;
        protected Vector2 scrollPosition = Vector2.zero;
        int _prevInstance;
        GUIStyle toolBarStyle;
		bool showObjectInspector = false;
        [NonSerialized]
        public static PackedItemNode selectedObject;
        [NonSerialized]
        UnityEngine.Object objectField;
        [MenuItem("Window/Go-Profiler")]
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
                memoryRootNode = new PackedItemNode("Root");
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
        public void ClearEditorReferences() {
            //DestroyImmediate(memoryRootNode); 
            //memoryRootNode = null;
            EditorUtility.UnloadUnusedAssetsImmediate();
            System.GC.Collect();
            Resources.UnloadUnusedAssets();
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
                ClearEditorReferences();
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
            //showObjectInspector = EditorGUILayout.Toggle("Show In Inspector", showObjectInspector); //TODO
            EditorGUILayout.EndHorizontal();
            //Top tool bar end...


            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            memoryFilters = EditorGUILayout.ObjectField(memoryFilters, typeof(MemoryFilterSettings), false) as MemoryFilterSettings;
            if (GUILayout.Button(new GUIContent("Save as plist/xml", "TODO in the future..."), EditorStyles.toolbarButton))
            {
            }
            if (GUILayout.Button(new GUIContent("Load plist/xml", "TODO in the future..."), EditorStyles.toolbarButton))
            {
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Refresh"), EditorStyles.toolbarButton, GUILayout.Width(30)))
            {
                IncomingSnapshot(data.mSnapshot);
                Repaint();
            }
            EditorGUILayout.EndHorizontal();
            if (!memoryFilters)
            {
                EditorGUILayout.HelpBox("Please Select a MemoryFilters object or load it from the .plist/.xml file", MessageType.Warning);
            }

            //TODO: handle the selected object.
            //EditorGUILayout.HelpBox("Watching Texture Detail Data is only for Editor.", MessageType.Warning, true);
            if (selectedObject != null && selectedObject.childList.Count == 0)
            {
                if (selectedObject != null && _prevInstance != selectedObject.instanceID)
                {
                    objectField = EditorUtility.InstanceIDToObject(selectedObject.instanceID);
                    _prevInstance = selectedObject.instanceID;
                    Selection.instanceIDs = new int[] { selectedObject.instanceID };
                }
            }
            if (objectField != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Selected Object Info:");
                EditorGUILayout.ObjectField(objectField, objectField.GetType(), true);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField("Can't instance object,maybe it was already released.");
            }
            //MemoryFilters end...

            Rect titleRect = EditorGUILayout.GetControlRect();
            EditorGUI.DrawRect(titleRect, new Color(0.15f, 0.15f, 0.15f, 1));
            EditorGUI.DrawRect(new Rect(titleRect.x + titleRect.width - 200, titleRect.y, 1, Screen.height), new Color(0.15f, 0.15f, 0.15f, 1));
            EditorGUI.DrawRect(new Rect(titleRect.x + titleRect.width - 100, titleRect.y, 1, Screen.height), new Color(0.15f, 0.15f, 0.15f, 1));
            GUI.Label(new Rect(titleRect.x, titleRect.y, titleRect.width - 200, titleRect.height), "Name", toolBarStyle);
            GUI.Label(new Rect(titleRect.x + titleRect.width - 175, titleRect.y, 50, titleRect.height), "Size", toolBarStyle);
            GUI.Label(new Rect(titleRect.x + titleRect.width - 75, titleRect.y, 50, titleRect.height), "RefCount", toolBarStyle);
            //Title bar end...

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            if (memoryRootNode != null && memoryRootNode.childList != null)
                memoryRootNode.DrawGUI(0);
            else
                Init();
            GUILayout.EndScrollView();
            //Contents end...
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
            memoryRootNode = new PackedItemNode("Root");
            PackedItemNode assetsRoot = new PackedItemNode("Assets");
            PackedItemNode sceneMemoryRoot = new PackedItemNode("SceneMemory");
            PackedItemNode notSavedRoot = new PackedItemNode("NotSaved");
            PackedItemNode builtinResourcesRoot = new PackedItemNode("BuiltinResources");
            PackedItemNode unknownRoot = new PackedItemNode("Unknown");
            //PackedItemNode managerNode = PackedItemNode.BuildNode<PackedItemNode>("Managers");
            memoryRootNode.AddNode(assetsRoot);
            memoryRootNode.AddNode(sceneMemoryRoot);
            memoryRootNode.AddNode(notSavedRoot);
            memoryRootNode.AddNode(builtinResourcesRoot);
            //assetsRoot.AddNode(managerNode);
            Dictionary<int, Group> assetGroup = new Dictionary<int, Group>();
            Dictionary<int, Group> sceneMemoryGroup = new Dictionary<int, Group>();
            Dictionary<int, Group> notSavedGroup = new Dictionary<int, Group>();
            Dictionary<int, Group> builtinResourcesGroup = new Dictionary<int, Group>();
            Dictionary<int, Group> unknownGroup = new Dictionary<int, Group>();
            List<PackedNativeUnityEngineObject> assetsObjectList = new List<PackedNativeUnityEngineObject>();
            List<PackedNativeUnityEngineObject> sceneMemoryObjectList = new List<PackedNativeUnityEngineObject>();
            List<PackedNativeUnityEngineObject> notSavedObjectList = new List<PackedNativeUnityEngineObject>();
            List<PackedNativeUnityEngineObject> builtinResourcesList = new List<PackedNativeUnityEngineObject>();
            List<PackedNativeUnityEngineObject> unknownObjectList = new List<PackedNativeUnityEngineObject>();
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
                else if (!obj.isPersistent && (obj.hideFlags & HideFlags.HideAndDontSave) != 0)
                {
                    notSavedObjectList.Add(obj);
                }
                else if (obj.isPersistent && (obj.hideFlags & HideFlags.HideAndDontSave) != 0) {
                    builtinResourcesList.Add(obj);
                }
                else
                    unknownObjectList.Add(obj);
            }
            if (unknownObjectList.Count > 0)//I can't find any unknown object yet.
                memoryRootNode.AddNode(unknownRoot);
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
            for (int i = 0; i < notSavedObjectList.Count; i++)
            {
                PackedNativeUnityEngineObject notSavedObject = notSavedObjectList[i];
                if (!notSavedGroup.ContainsKey(notSavedObject.classId))
                    notSavedGroup.Add(notSavedObject.classId, new Group(notSavedObject.classId, data.mSnapshot.nativeTypes[notSavedObject.classId].name));
                notSavedGroup[notSavedObject.classId].packedNativeObjectList.Add(notSavedObject);
            }
            for (int i = 0; i < builtinResourcesList.Count; i++)
            {
                PackedNativeUnityEngineObject builtinResourcesObject = builtinResourcesList[i];
                if (!builtinResourcesGroup.ContainsKey(builtinResourcesObject.classId))
                    builtinResourcesGroup.Add(builtinResourcesObject.classId, new Group(builtinResourcesObject.classId, data.mSnapshot.nativeTypes[builtinResourcesObject.classId].name));
                builtinResourcesGroup[builtinResourcesObject.classId].packedNativeObjectList.Add(builtinResourcesObject);
            }
            for (int i = 0; i < unknownObjectList.Count; i++)
            {
                PackedNativeUnityEngineObject unknownObject = unknownObjectList[i];
                if (!unknownGroup.ContainsKey(unknownObject.classId))
                    unknownGroup.Add(unknownObject.classId, new Group(unknownObject.classId, data.mSnapshot.nativeTypes[unknownObject.classId].name));
                unknownGroup[unknownObject.classId].packedNativeObjectList.Add(unknownObject);
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
            using (var i = notSavedGroup.GetEnumerator())//replace foreach
            {
                while (i.MoveNext())
                {
                    Group group = i.Current.Value;
                    SetNodeByClassID(group.classId, group.itemNode, group.packedNativeObjectList);
                    if (group.itemNode != null)
                    {
                        notSavedRoot.AddNode(group.itemNode);
                    }
                }
            }
            using (var i = builtinResourcesGroup.GetEnumerator())//replace foreach
            {
                while (i.MoveNext())
                {
                    Group group = i.Current.Value;
                    SetNodeByClassID(group.classId, group.itemNode, group.packedNativeObjectList);
                    if (group.itemNode != null)
                    {
                        builtinResourcesRoot.AddNode(group.itemNode);
                    }
                }
            }
            using (var i = unknownGroup.GetEnumerator())//replace foreach
            {
                while (i.MoveNext())
                {
                    Group group = i.Current.Value;
                    SetNodeByClassID(group.classId, group.itemNode, group.packedNativeObjectList);
                    if (group.itemNode != null)
                    {
                        unknownRoot.AddNode(group.itemNode);
                    }
                }
            }
            memoryRootNode.SetCount();
            memoryRootNode.Convert();
            memoryRootNode.Sort();
            //ClearEditorReferences();//To release gc and memory.
        }
		void SetNodeByClassID(int classID, PackedItemNode nodeRoot, List<PackedNativeUnityEngineObject> nativeUnityObjectList)
        {
            nodeRoot.Clear();
			nodeRoot.nativeType = data.mSnapshot.nativeTypes [classID];

            int index = -1;
            if (memoryFilters) {
                for (int i = 0; i < memoryFilters.memoryFilterList.Count; i++) {
					if ((int)memoryFilters.memoryFilterList[i].classID == classID)
                    {
                        index = i;
                    }
                }
            }

			if (index > -1)//这样写好蛋疼啊0.0
            {
                Dictionary<PackedItemNode, RegexElement> tempDict = new Dictionary<PackedItemNode, RegexElement>();
                PackedItemNode otherNode = new PackedItemNode("Others");
				otherNode.nativeType = data.mSnapshot.nativeTypes [classID];
                nodeRoot.AddNode(otherNode);
                MemoryFilter memoryFilter = memoryFilters.memoryFilterList[index];
                for (int i = 0; i < memoryFilter.regexElementList.Count; i++)
                {
                    PackedItemNode filterNode = new PackedItemNode(memoryFilter.regexElementList[i].key ,true);
					filterNode.nativeType = data.mSnapshot.nativeTypes [classID];
                    nodeRoot.AddNode(filterNode);
                    tempDict.Add(filterNode, memoryFilter.regexElementList[i]);
                }
                while(nativeUnityObjectList.Count>0)
                {
                    PackedNativeUnityEngineObject item = nativeUnityObjectList[0];
                    string name = item.name;
                    PackedItemNode childNode = new PackedItemNode(name);
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
                    PackedItemNode node = new PackedItemNode(name);
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