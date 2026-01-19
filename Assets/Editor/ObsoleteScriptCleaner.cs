using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class ObsoleteScriptCleaner : EditorWindow
{
    private List<string> obsoleteScripts = new List<string>
    {
        "Assets/Ruins/Standard Assets/Image Based/DesaturateEffect.cs",
        "Assets/Ruins/Standard Assets/Image Effects (Pro Only)/BlurEffect.cs",
        "Assets/Ruins/Standard Assets/Image Effects (Pro Only)/MotionBlur.cs",
        "Assets/Ruins/Standard Assets/Image Effects (Pro Only)/ContrastStretchEffect.cs",
        "Assets/Ruins/Standard Assets/Image Effects (Pro Only)/GlowEffect.cs",
        "Assets/Ruins/Standard Assets/Image Effects (Pro Only)/NoiseEffect.cs",
        "Assets/Ruins/Standard Assets/Image Effects (Pro Only)/SSAOEffectDepthCutoff.cs",
        "Assets/Ruins/Standard Assets/Image Effects (Pro Only)/ImageEffectBase.cs"
    };

    [MenuItem("Tools/清理过时的脚本警告")]
    static void ShowWindow()
    {
        GetWindow<ObsoleteScriptCleaner>("清理过时脚本");
    }

    private void OnGUI()
    {
        GUILayout.Label("清理过时的脚本警告", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "这些警告来自Unity旧版本的Standard Assets图像效果脚本。\n" +
            "这些脚本在URP中已经不兼容，如果未使用可以删除。\n\n" +
            "注意：删除前请确保这些脚本没有在场景或Prefab中使用。",
            MessageType.Info);

        EditorGUILayout.Space();

        // 显示要检查的脚本列表
        EditorGUILayout.LabelField("要检查的脚本:", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        foreach (string script in obsoleteScripts)
        {
            bool exists = File.Exists(script);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Path.GetFileName(script), exists ? EditorStyles.label : EditorStyles.miniLabel);
            if (exists)
            {
                EditorGUILayout.LabelField("存在", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("不存在", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUI.indentLevel--;

        EditorGUILayout.Space();

        if (GUILayout.Button("检查脚本是否被使用", GUILayout.Height(30)))
        {
            CheckScriptUsage();
        }

        EditorGUILayout.Space();

        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("删除未使用的过时脚本", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("确认删除", 
                "确定要删除未使用的过时脚本吗？\n\n" +
                "这将删除所有在场景和Prefab中未使用的过时脚本。\n" +
                "建议先备份项目。\n\n" +
                "是否继续？", 
                "确定", "取消"))
            {
                DeleteUnusedScripts();
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "如果不想删除这些脚本，可以：\n" +
            "1. 在Project Settings > Player > Other Settings > Scripting Define Symbols 中添加 'DISABLE_IMAGE_EFFECTS'\n" +
            "2. 或者使用 #pragma warning disable 来禁用警告",
            MessageType.Info);
    }

    private void CheckScriptUsage()
    {
        Debug.Log("=== 检查过时脚本使用情况 ===");
        
        int usedCount = 0;
        int unusedCount = 0;

        foreach (string scriptPath in obsoleteScripts)
        {
            if (!File.Exists(scriptPath))
            {
                Debug.Log($"跳过（不存在）: {scriptPath}");
                continue;
            }

            string scriptName = Path.GetFileNameWithoutExtension(scriptPath);
            System.Type scriptType = System.Type.GetType(scriptName + ", Assembly-CSharp");

            if (scriptType == null)
            {
                Debug.LogWarning($"无法找到类型: {scriptName}");
                continue;
            }

            // 检查场景中的使用
            bool usedInScene = false;
            GameObject[] sceneObjects = FindObjectsOfType<GameObject>();
            foreach (GameObject obj in sceneObjects)
            {
                if (obj.GetComponent(scriptType) != null)
                {
                    usedInScene = true;
                    Debug.LogWarning($"脚本被使用: {scriptName} (在场景对象 {obj.name} 上)");
                    break;
                }
            }

            // 检查Prefab中的使用
            bool usedInPrefab = false;
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            foreach (string guid in prefabGuids)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null && prefab.GetComponent(scriptType) != null)
                {
                    usedInPrefab = true;
                    Debug.LogWarning($"脚本被使用: {scriptName} (在Prefab {prefabPath} 上)");
                    break;
                }
            }

            if (usedInScene || usedInPrefab)
            {
                usedCount++;
                Debug.Log($"✓ {scriptName}: 正在使用中");
            }
            else
            {
                unusedCount++;
                Debug.Log($"✗ {scriptName}: 未使用，可以删除");
            }
        }

        Debug.Log($"=== 检查完成 ===");
        Debug.Log($"使用中: {usedCount} 个");
        Debug.Log($"未使用: {unusedCount} 个");

        EditorUtility.DisplayDialog("检查完成", 
            $"检查完成！\n\n" +
            $"使用中: {usedCount} 个\n" +
            $"未使用: {unusedCount} 个\n\n" +
            "详细日志请查看Console窗口。", 
            "确定");
    }

    private void DeleteUnusedScripts()
    {
        int deletedCount = 0;
        int skippedCount = 0;

        foreach (string scriptPath in obsoleteScripts)
        {
            if (!File.Exists(scriptPath))
            {
                skippedCount++;
                continue;
            }

            string scriptName = Path.GetFileNameWithoutExtension(scriptPath);
            System.Type scriptType = System.Type.GetType(scriptName + ", Assembly-CSharp");

            if (scriptType == null)
            {
                Debug.LogWarning($"无法找到类型: {scriptName}，跳过删除");
                skippedCount++;
                continue;
            }

            // 检查是否被使用
            bool isUsed = false;
            
            // 检查场景
            GameObject[] sceneObjects = FindObjectsOfType<GameObject>();
            foreach (GameObject obj in sceneObjects)
            {
                if (obj.GetComponent(scriptType) != null)
                {
                    isUsed = true;
                    break;
                }
            }

            // 检查Prefab
            if (!isUsed)
            {
                string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
                foreach (string guid in prefabGuids)
                {
                    string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab != null && prefab.GetComponent(scriptType) != null)
                    {
                        isUsed = true;
                        break;
                    }
                }
            }

            if (!isUsed)
            {
                // 删除脚本和.meta文件
                try
                {
                    AssetDatabase.DeleteAsset(scriptPath);
                    string metaPath = scriptPath + ".meta";
                    if (File.Exists(metaPath))
                    {
                        AssetDatabase.DeleteAsset(metaPath);
                    }
                    deletedCount++;
                    Debug.Log($"已删除: {scriptPath}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"删除失败: {scriptPath}\n错误: {e.Message}");
                }
            }
            else
            {
                skippedCount++;
                Debug.Log($"跳过（正在使用）: {scriptPath}");
            }
        }

        AssetDatabase.Refresh();

        Debug.Log($"=== 删除完成 ===");
        Debug.Log($"已删除: {deletedCount} 个脚本");
        Debug.Log($"跳过: {skippedCount} 个脚本");

        EditorUtility.DisplayDialog("删除完成", 
            $"已删除 {deletedCount} 个未使用的过时脚本\n" +
            $"跳过 {skippedCount} 个脚本（正在使用或不存在）\n\n" +
            "详细日志请查看Console窗口。", 
            "确定");
    }
}
