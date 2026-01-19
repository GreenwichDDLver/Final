using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MaterialConverter : EditorWindow
{
    [MenuItem("Tools/一键转换所有材质为URP")]
    static void ConvertAllMaterialsToURP()
    {
        // 查找URP Shader
        Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
        Shader urpUnlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        Shader urpSimpleLitShader = Shader.Find("Universal Render Pipeline/Simple Lit");

        if (urpLitShader == null)
        {
            Debug.LogError("未找到URP Shader！请确保项目已安装Universal Render Pipeline包。");
            EditorUtility.DisplayDialog("错误", "未找到URP Shader！请确保项目已安装Universal Render Pipeline包。", "确定");
            return;
        }

        // 查找所有材质文件
        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
        int convertedCount = 0;
        int alreadyURPCount = 0;
        int errorCount = 0;

        List<string> convertedMaterials = new List<string>();
        List<string> errorMaterials = new List<string>();

        Debug.Log($"找到 {materialGuids.Length} 个材质文件，开始转换...");

        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null) 
            {
                errorCount++;
                errorMaterials.Add(path);
                continue;
            }

            // 检查是否是URP shader
            string shaderName = mat.shader.name;
            bool isURP = shaderName.Contains("Universal Render Pipeline") || 
                         shaderName.Contains("Universal/") ||
                         shaderName.Contains("URP/") ||
                         shaderName.StartsWith("Shader Graphs/"); // Shader Graph通常也是URP

            if (isURP)
            {
                alreadyURPCount++;
                continue;
            }

            // 保存旧shader的属性
            Color? mainColor = null;
            Texture mainTex = null;
            float smoothness = 0.5f;
            float metallic = 0f;
            bool isTransparent = false;
            bool isUnlit = false;

            // 判断shader类型
            isUnlit = shaderName.Contains("Unlit");
            isTransparent = shaderName.Contains("Transparent") || shaderName.Contains("Fade");

            // 尝试获取常见的属性（兼容Built-in和URP）
            if (mat.HasProperty("_Color"))
                mainColor = mat.GetColor("_Color");
            else if (mat.HasProperty("_BaseColor"))
                mainColor = mat.GetColor("_BaseColor");
            else if (mat.HasProperty("_TintColor"))
                mainColor = mat.GetColor("_TintColor");

            if (mat.HasProperty("_MainTex"))
                mainTex = mat.GetTexture("_MainTex");
            else if (mat.HasProperty("_BaseMap"))
                mainTex = mat.GetTexture("_BaseMap");
            else if (mat.HasProperty("_MainTexture"))
                mainTex = mat.GetTexture("_MainTexture");

            if (mat.HasProperty("_Glossiness"))
                smoothness = mat.GetFloat("_Glossiness");
            else if (mat.HasProperty("_Smoothness"))
                smoothness = mat.GetFloat("_Smoothness");
            else if (mat.HasProperty("_GlossMapScale"))
                smoothness = mat.GetFloat("_GlossMapScale");

            if (mat.HasProperty("_Metallic"))
                metallic = mat.GetFloat("_Metallic");

            // 根据shader类型选择合适的URP shader
            Shader targetShader = urpLitShader;
            
            if (isUnlit || shaderName.Contains("Unlit"))
            {
                targetShader = urpUnlitShader;
            }
            else if (shaderName.Contains("Standard") || shaderName.Contains("Standard"))
            {
                targetShader = urpLitShader;
            }

            // 记录旧shader名称
            string oldShaderName = mat.shader.name;

            // 转换shader
            mat.shader = targetShader;

            // 恢复属性（使用URP的属性名）
            if (mainColor.HasValue)
            {
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", mainColor.Value);
                if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", mainColor.Value);
            }

            if (mainTex != null)
            {
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", mainTex);
                if (mat.HasProperty("_MainTex"))
                    mat.SetTexture("_MainTex", mainTex);
            }

            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", smoothness);
            
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", metallic);

            // 设置表面类型（如果需要透明）
            if (isTransparent && mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1); // 1 = Transparent
                if (mat.HasProperty("_Blend"))
                    mat.SetFloat("_Blend", 0); // Alpha blend
            }

            convertedCount++;
            convertedMaterials.Add($"{path} ({oldShaderName} -> {targetShader.name})");
            
            // 标记为已修改
            EditorUtility.SetDirty(mat);
        }

        // 保存所有更改
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 输出详细日志
        Debug.Log("=== 材质转换完成 ===");
        Debug.Log($"总共: {materialGuids.Length} 个材质");
        Debug.Log($"已转换: {convertedCount} 个");
        Debug.Log($"已经是URP: {alreadyURPCount} 个");
        Debug.Log($"错误: {errorCount} 个");

        if (convertedMaterials.Count > 0)
        {
            Debug.Log("已转换的材质列表:");
            foreach (string item in convertedMaterials)
            {
                Debug.Log($"  - {item}");
            }
        }

        if (errorMaterials.Count > 0)
        {
            Debug.LogWarning("转换失败的材质:");
            foreach (string item in errorMaterials)
            {
                Debug.LogWarning($"  - {item}");
            }
        }

        // 显示完成对话框
        EditorUtility.DisplayDialog("转换完成", 
            $"材质转换完成！\n\n" +
            $"总共: {materialGuids.Length} 个材质\n" +
            $"已转换: {convertedCount} 个\n" +
            $"已经是URP: {alreadyURPCount} 个\n" +
            $"错误: {errorCount} 个\n\n" +
            $"详细日志请查看Console窗口。", 
            "确定");
    }
}
