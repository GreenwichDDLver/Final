using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AutoColliderAdder : EditorWindow
{
    private bool addToStaticObjects = true;
    private bool addToNonStaticObjects = false;
    private bool useMeshCollider = true;
    private bool useConvexMeshCollider = false;
    private bool skipSmallObjects = true;
    private new float minSize = 0.1f;
    private bool skipUI = true;
    private bool skipParticles = true;
    private bool skipLights = true;
    private bool skipCameras = true;
    private bool skipTriggers = false;

    [MenuItem("Tools/自动添加碰撞体")]
    static void ShowWindow()
    {
        GetWindow<AutoColliderAdder>("自动添加碰撞体工具");
    }

    private void OnGUI()
    {
        GUILayout.Label("自动添加碰撞体工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "此工具会自动为场景中需要碰撞的物体添加碰撞体。\n" +
            "建议先备份场景，然后使用此工具。",
            MessageType.Info);

        EditorGUILayout.Space();

        // 选项设置
        EditorGUILayout.LabelField("添加选项:", EditorStyles.boldLabel);
        addToStaticObjects = EditorGUILayout.Toggle("为静态物体添加", addToStaticObjects);
        addToNonStaticObjects = EditorGUILayout.Toggle("为非静态物体添加", addToNonStaticObjects);
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("碰撞体类型:", EditorStyles.boldLabel);
        useMeshCollider = EditorGUILayout.Toggle("使用MeshCollider", useMeshCollider);
        if (useMeshCollider)
        {
            EditorGUI.indentLevel++;
            useConvexMeshCollider = EditorGUILayout.Toggle("使用Convex（凸体，性能更好）", useConvexMeshCollider);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("过滤选项:", EditorStyles.boldLabel);
        skipSmallObjects = EditorGUILayout.Toggle("跳过过小的物体", skipSmallObjects);
        if (skipSmallObjects)
        {
            EditorGUI.indentLevel++;
            minSize = EditorGUILayout.FloatField("最小尺寸", minSize);
            EditorGUI.indentLevel--;
        }

        skipUI = EditorGUILayout.Toggle("跳过UI对象", skipUI);
        skipParticles = EditorGUILayout.Toggle("跳过粒子系统", skipParticles);
        skipLights = EditorGUILayout.Toggle("跳过灯光", skipLights);
        skipCameras = EditorGUILayout.Toggle("跳过摄像机", skipCameras);
        skipTriggers = EditorGUILayout.Toggle("跳过已有触发器", skipTriggers);

        EditorGUILayout.Space();

        if (GUILayout.Button("分析场景（不添加）", GUILayout.Height(30)))
        {
            AnalyzeScene();
        }

        EditorGUILayout.Space();

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("开始添加碰撞体", GUILayout.Height(40)))
        {
            AddColliders();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space();

        if (GUILayout.Button("移除所有自动添加的碰撞体", GUILayout.Height(30)))
        {
            RemoveAutoAddedColliders();
        }
    }

    private void AnalyzeScene()
    {
        List<GameObject> candidates = FindCandidates();
        
        Debug.Log($"=== 场景分析结果 ===");
        Debug.Log($"找到 {candidates.Count} 个可能需要添加碰撞体的物体：");
        
        int staticCount = 0;
        int nonStaticCount = 0;
        int hasColliderCount = 0;
        int meshRendererCount = 0;
        
        foreach (GameObject obj in candidates)
        {
            if (obj.isStatic) staticCount++;
            else nonStaticCount++;
            
            if (obj.GetComponent<Collider>() != null) hasColliderCount++;
            if (obj.GetComponent<MeshRenderer>() != null) meshRendererCount++;
        }
        
        Debug.Log($"  - 静态物体: {staticCount}");
        Debug.Log($"  - 非静态物体: {nonStaticCount}");
        Debug.Log($"  - 已有碰撞体: {hasColliderCount}");
        Debug.Log($"  - 需要添加: {candidates.Count - hasColliderCount}");
        Debug.Log($"  - 有MeshRenderer: {meshRendererCount}");
        
        EditorUtility.DisplayDialog("分析完成", 
            $"找到 {candidates.Count} 个候选物体\n" +
            $"其中 {candidates.Count - hasColliderCount} 个需要添加碰撞体\n\n" +
            "详细日志请查看Console窗口。", 
            "确定");
    }

    private List<GameObject> FindCandidates()
    {
        List<GameObject> candidates = new List<GameObject>();
        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            // 检查是否应该跳过
            if (ShouldSkip(obj)) continue;

            // 检查静态/非静态选项
            if (obj.isStatic && !addToStaticObjects) continue;
            if (!obj.isStatic && !addToNonStaticObjects) continue;

            // 检查是否已有碰撞体
            Collider existingCollider = obj.GetComponent<Collider>();
            if (existingCollider != null)
            {
                if (skipTriggers && existingCollider.isTrigger) continue;
                // 如果已有碰撞体且不是触发器，跳过
                if (!existingCollider.isTrigger) continue;
            }

            // 检查是否有MeshRenderer或MeshFilter（表示是3D物体）
            MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();

            if (meshRenderer == null && meshFilter == null) continue;

            // 检查尺寸
            if (skipSmallObjects)
            {
                Bounds bounds = GetBounds(obj);
                float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                if (size < minSize) continue;
            }

            candidates.Add(obj);
        }

        return candidates;
    }

    private bool ShouldSkip(GameObject obj)
    {
        // 跳过UI
        if (skipUI && (obj.GetComponent<Canvas>() != null || 
                       obj.GetComponent<RectTransform>() != null ||
                       obj.name.Contains("UI") || obj.name.Contains("Canvas")))
            return true;

        // 跳过粒子系统
        if (skipParticles && obj.GetComponent<ParticleSystem>() != null)
            return true;

        // 跳过灯光
        if (skipLights && obj.GetComponent<Light>() != null)
            return true;

        // 跳过摄像机
        if (skipCameras && obj.GetComponent<Camera>() != null)
            return true;

        // 跳过玩家、敌人等已有脚本的对象（通常它们已经有碰撞体）
        if (obj.GetComponent<PlayerMovement>() != null ||
            obj.GetComponent<EnemyController>() != null ||
            obj.GetComponent<CharacterController>() != null)
            return true;

        // 跳过特定名称的对象
        string name = obj.name.ToLower();
        if (name.Contains("effect") || name.Contains("fx") || 
            name.Contains("particle") || name.Contains("light") ||
            name.Contains("camera") || name.Contains("ui"))
            return true;

        return false;
    }

    private Bounds GetBounds(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds;
        }

        // 如果没有Renderer，尝试从MeshFilter获取
        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            return meshFilter.sharedMesh.bounds;
        }

        // 默认返回一个小的bounds
        return new Bounds(obj.transform.position, Vector3.one * 0.1f);
    }

    private void AddColliders()
    {
        List<GameObject> candidates = FindCandidates();
        int addedCount = 0;
        int skippedCount = 0;

        Undo.SetCurrentGroupName("自动添加碰撞体");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (GameObject obj in candidates)
        {
            // 再次检查是否已有碰撞体（可能在分析后手动添加了）
            Collider existingCollider = obj.GetComponent<Collider>();
            if (existingCollider != null && !existingCollider.isTrigger)
            {
                skippedCount++;
                continue;
            }

            // 如果是触发器，移除它
            if (existingCollider != null && existingCollider.isTrigger)
            {
                Undo.DestroyObjectImmediate(existingCollider);
            }

            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();

            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                skippedCount++;
                continue;
            }

            // 添加碰撞体
            if (useMeshCollider)
            {
                MeshCollider meshCollider = obj.GetComponent<MeshCollider>();
                if (meshCollider == null)
                {
                    Undo.AddComponent<MeshCollider>(obj);
                    meshCollider = obj.GetComponent<MeshCollider>();
                }

                meshCollider.sharedMesh = meshFilter.sharedMesh;
                meshCollider.convex = useConvexMeshCollider;
                meshCollider.isTrigger = false;

                addedCount++;
            }
            else
            {
                // 使用BoxCollider作为替代
                BoxCollider boxCollider = obj.GetComponent<BoxCollider>();
                if (boxCollider == null)
                {
                    Undo.AddComponent<BoxCollider>(obj);
                    boxCollider = obj.GetComponent<BoxCollider>();
                }

                // 自动计算bounds
                Bounds bounds = GetBounds(obj);
                boxCollider.center = obj.transform.InverseTransformPoint(bounds.center);
                boxCollider.size = bounds.size;

                addedCount++;
            }

            // 标记为已处理（可选：添加一个标记组件）
            // 这里我们通过名称来标记，或者可以添加一个自定义组件
        }

        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"=== 添加碰撞体完成 ===");
        Debug.Log($"成功添加: {addedCount} 个");
        Debug.Log($"跳过: {skippedCount} 个");

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("完成", 
            $"已为 {addedCount} 个物体添加碰撞体\n" +
            $"跳过 {skippedCount} 个物体\n\n" +
            "详细日志请查看Console窗口。", 
            "确定");
    }

    private void RemoveAutoAddedColliders()
    {
        if (!EditorUtility.DisplayDialog("确认", 
            "确定要移除所有自动添加的碰撞体吗？\n\n" +
            "注意：这只会移除MeshCollider和BoxCollider，\n" +
            "不会移除其他类型的碰撞体。", 
            "确定", "取消"))
        {
            return;
        }

        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int removedCount = 0;

        Undo.SetCurrentGroupName("移除自动添加的碰撞体");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (GameObject obj in allObjects)
        {
            MeshCollider meshCollider = obj.GetComponent<MeshCollider>();
            BoxCollider boxCollider = obj.GetComponent<BoxCollider>();

            // 只移除MeshCollider和BoxCollider（假设这些是自动添加的）
            // 保留其他类型的碰撞体（如CapsuleCollider，通常是手动添加的）
            if (meshCollider != null)
            {
                Undo.DestroyObjectImmediate(meshCollider);
                removedCount++;
            }
            else if (boxCollider != null)
            {
                // 检查是否是玩家或敌人（这些通常需要保留BoxCollider）
                if (obj.GetComponent<PlayerMovement>() == null && 
                    obj.GetComponent<EnemyController>() == null)
                {
                    Undo.DestroyObjectImmediate(boxCollider);
                    removedCount++;
                }
            }
        }

        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"移除了 {removedCount} 个碰撞体");
        EditorUtility.DisplayDialog("完成", $"已移除 {removedCount} 个碰撞体", "确定");
    }
}
