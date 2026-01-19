using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class WaterRenderingFixer : EditorWindow
{
    [MenuItem("Tools/修复水面渲染问题")]
    static void ShowWindow()
    {
        GetWindow<WaterRenderingFixer>("水面渲染修复工具");
    }

    private void OnGUI()
    {
        GUILayout.Label("水面渲染问题修复工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "如果建筑物图形\"残余\"到水面上方，可能是以下原因：\n" +
            "1. 水面材质的深度测试（ZTest）设置不正确\n" +
            "2. 水面材质的深度写入（ZWrite）被禁用\n" +
            "3. 渲染队列（Render Queue）设置错误\n" +
            "4. 材质转换后透明设置不正确",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("检查场景中的水面对象"))
        {
            CheckWaterObjects();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("修复所有水面材质"))
        {
            FixWaterMaterials();
        }

        EditorGUILayout.Space();

        EditorGUILayout.Space();

        if (GUILayout.Button("检查建筑物材质设置"))
        {
            CheckBuildingMaterials();
        }

        EditorGUILayout.Space();

        GUI.backgroundColor = Color.yellow;
        if (GUILayout.Button("修复透明材质的ZWrite问题", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("确认修复", 
                "这将为所有透明材质启用ZWrite（深度写入）。\n\n" +
                "注意：对于某些真正的透明材质（如玻璃），\n" +
                "启用ZWrite可能会改变渲染效果。\n\n" +
                "是否继续？", 
                "确定", "取消"))
            {
                FixTransparentMaterialZWrite();
            }
        }
        GUI.backgroundColor = Color.white;
    }

    private void CheckWaterObjects()
    {
        List<GameObject> waterObjects = new List<GameObject>();
        
        // 查找所有可能的水面对象
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            // 检查名称包含water、水面、plane等关键词
            string name = obj.name.ToLower();
            if (name.Contains("water") || name.Contains("水面") || 
                (name.Contains("plane") && obj.GetComponent<MeshRenderer>() != null))
            {
                MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
                if (renderer != null && renderer.sharedMaterials.Length > 0)
                {
                    waterObjects.Add(obj);
                }
            }
        }

        Debug.Log($"找到 {waterObjects.Count} 个可能的水面对象：");
        foreach (GameObject obj in waterObjects)
        {
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            Material[] materials = renderer.sharedMaterials;
            
            Debug.Log($"  - {obj.name} (位置: {obj.transform.position})");
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null)
                {
                    Material mat = materials[i];
                    string shaderName = mat.shader != null ? mat.shader.name : "Missing Shader";
                    int renderQueue = mat.renderQueue;
                    bool isTransparent = renderQueue >= 2500;
                    
                    Debug.Log($"    材质[{i}]: {mat.name}, Shader: {shaderName}, RenderQueue: {renderQueue}, 透明: {isTransparent}");
                    
                    // 检查ZWrite设置
                    if (mat.HasProperty("_ZWrite"))
                    {
                        float zWrite = mat.GetFloat("_ZWrite");
                        Debug.Log($"      ZWrite: {zWrite} (0=Off, 1=On)");
                    }
                    
                    // 检查Surface类型
                    if (mat.HasProperty("_Surface"))
                    {
                        float surface = mat.GetFloat("_Surface");
                        Debug.Log($"      Surface: {surface} (0=Opaque, 1=Transparent)");
                    }
                }
            }
        }

        if (waterObjects.Count == 0)
        {
            Debug.LogWarning("未找到明显的水面对象。请手动检查场景中是否有Plane或其他水面对象。");
        }
    }

    private void FixWaterMaterials()
    {
        // 查找所有材质
        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
        int fixedCount = 0;

        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null) continue;

            string matName = mat.name.ToLower();
            string shaderName = mat.shader != null ? mat.shader.name : "";

            // 检查是否是水面材质（通过名称或shader判断）
            bool isWaterMaterial = matName.Contains("water") || matName.Contains("水面") ||
                                   shaderName.Contains("Water") || shaderName.Contains("water");

            if (!isWaterMaterial) continue;

            bool modified = false;

            // 修复1: 确保使用URP shader
            if (!shaderName.Contains("Universal Render Pipeline"))
            {
                Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
                if (urpLitShader != null)
                {
                    mat.shader = urpLitShader;
                    modified = true;
                    Debug.Log($"修复材质 {mat.name}: 转换为URP Lit shader");
                }
            }

            // 修复2: 设置正确的渲染队列（水面应该在透明队列，但需要深度写入）
            // 对于半透明水面，使用Transparent队列但启用深度写入
            if (mat.HasProperty("_Surface"))
            {
                // 如果是透明表面，可能需要调整
                float surface = mat.GetFloat("_Surface");
                if (surface > 0.5f) // Transparent
                {
                    // 对于水面，通常需要深度写入来正确遮挡水下物体
                    if (mat.HasProperty("_ZWrite"))
                    {
                        mat.SetFloat("_ZWrite", 1f); // 启用深度写入
                        modified = true;
                    }
                }
            }

            // 修复3: 确保RenderQueue设置正确
            // 水面应该在Geometry队列（2500）或稍高一点，但不要太高
            if (mat.renderQueue >= 3000)
            {
                mat.renderQueue = 2500; // Geometry队列
                modified = true;
                Debug.Log($"修复材质 {mat.name}: 调整RenderQueue为2500");
            }

            if (modified)
            {
                EditorUtility.SetDirty(mat);
                fixedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"修复完成！共修复 {fixedCount} 个水面材质。");
        EditorUtility.DisplayDialog("修复完成", $"已修复 {fixedCount} 个水面材质。\n\n建议：\n1. 检查场景中的水面对象\n2. 确保水面材质的ZWrite已启用\n3. 如果问题仍然存在，检查建筑物的材质设置", "确定");
    }

    private void CheckBuildingMaterials()
    {
        // 查找场景中所有建筑物对象
        Renderer[] renderers = FindObjectsOfType<Renderer>();
        int issueCount = 0;

        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;
            foreach (Material mat in materials)
            {
                if (mat == null) continue;

                bool hasIssue = false;
                string issues = "";

                // 检查1: Shader是否兼容URP
                string shaderName = mat.shader != null ? mat.shader.name : "";
                if (!shaderName.Contains("Universal Render Pipeline") && 
                    !shaderName.Contains("Hidden") &&
                    !shaderName.Contains("Sprites"))
                {
                    hasIssue = true;
                    issues += "Shader可能不兼容URP; ";
                }

                // 检查2: RenderQueue是否异常
                if (mat.renderQueue < 0 || mat.renderQueue > 5000)
                {
                    hasIssue = true;
                    issues += $"RenderQueue异常: {mat.renderQueue}; ";
                }

                // 检查3: 如果是透明材质，检查ZWrite设置
                if (mat.renderQueue >= 2500)
                {
                    if (mat.HasProperty("_ZWrite"))
                    {
                        float zWrite = mat.GetFloat("_ZWrite");
                        if (zWrite < 0.5f) // ZWrite Off
                        {
                            // 对于建筑物，通常不应该使用透明材质且ZWrite Off
                            hasIssue = true;
                            issues += "透明材质但ZWrite关闭; ";
                        }
                    }
                }

                if (hasIssue)
                {
                    issueCount++;
                    Debug.LogWarning($"材质问题: {mat.name} (在 {renderer.gameObject.name} 上)\n  问题: {issues}");
                }
            }
        }

        if (issueCount == 0)
        {
            Debug.Log("所有建筑物材质检查通过！");
        }
        else
        {
            Debug.LogWarning($"发现 {issueCount} 个材质问题。请查看上面的警告信息。");
        }
    }

    private void FixTransparentMaterialZWrite()
    {
        // 查找所有材质
        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
        int fixedCount = 0;
        int skippedCount = 0;

        // 需要跳过的材质名称（这些通常应该保持ZWrite关闭）
        HashSet<string> skipMaterialNames = new HashSet<string>
        {
            "particle", "smoke", "fire", "fx", "effect"
        };

        Undo.SetCurrentGroupName("修复透明材质ZWrite");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null) continue;

            // 检查是否是透明材质
            if (mat.renderQueue < 2500) continue; // 不是透明队列

            // 检查材质名称，跳过某些特殊材质
            string matName = mat.name.ToLower();
            bool shouldSkip = false;
            foreach (string skipName in skipMaterialNames)
            {
                if (matName.Contains(skipName))
                {
                    shouldSkip = true;
                    break;
                }
            }
            if (shouldSkip)
            {
                skippedCount++;
                continue;
            }

            // 检查是否有ZWrite属性
            if (!mat.HasProperty("_ZWrite")) continue;

            // 检查当前ZWrite设置
            float currentZWrite = mat.GetFloat("_ZWrite");
            if (currentZWrite > 0.5f) // 已经启用
            {
                skippedCount++;
                continue;
            }

            // 启用ZWrite
            Undo.RecordObject(mat, "Enable ZWrite for transparent material");
            mat.SetFloat("_ZWrite", 1f);
            EditorUtility.SetDirty(mat);
            fixedCount++;

            Debug.Log($"修复材质: {mat.name} - 启用ZWrite");
        }

        Undo.CollapseUndoOperations(undoGroup);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"=== 修复透明材质ZWrite完成 ===");
        Debug.Log($"修复: {fixedCount} 个材质");
        Debug.Log($"跳过: {skippedCount} 个材质");

        EditorUtility.DisplayDialog("修复完成", 
            $"已修复 {fixedCount} 个透明材质的ZWrite设置\n" +
            $"跳过 {skippedCount} 个材质\n\n" +
            "详细日志请查看Console窗口。", 
            "确定");
    }
}
