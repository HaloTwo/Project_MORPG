using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ImportedAssetFixer
{
    private static readonly string[] TargetFolders =
    {
        "Assets/ExplosiveLLC",
        "Assets/RPGPP_LT"
    };

    // 새로 가져온 Built-in 머티리얼을 URP 프로젝트에서 보이도록 URP/Lit 셰이더로 변환합니다.
    [MenuItem("MORPG/Assets/Fix Imported Materials To URP")]
    public static void FixImportedMaterialsToUrp()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("[ImportedAssetFixer] Universal Render Pipeline/Lit 셰이더를 찾지 못했습니다. URP 패키지/렌더 파이프라인 설정을 먼저 확인하세요.");
            return;
        }

        int fixedCount = 0;
        foreach (Material material in LoadMaterials())
        {
            if (material == null || !NeedsUrpConversion(material))
            {
                continue;
            }

            Texture mainTexture = ReadMainTexture(material);
            Color baseColor = ReadBaseColor(material);

            material.shader = urpLit;
            WriteUrpProperties(material, mainTexture, baseColor);

            EditorUtility.SetDirty(material);
            fixedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ImportedAssetFixer] URP 머티리얼 변환 완료: {fixedCount}개");
    }

    // 지정한 에셋 폴더 아래의 모든 머티리얼을 가져옵니다.
    private static IEnumerable<Material> LoadMaterials()
    {
        string[] materialGuids = AssetDatabase.FindAssets("t:Material", TargetFolders);
        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            yield return AssetDatabase.LoadAssetAtPath<Material>(path);
        }
    }

    // URP가 아닌 Built-in/Legacy/깨진 셰이더만 변환 대상으로 판단합니다.
    private static bool NeedsUrpConversion(Material material)
    {
        if (material.shader == null)
        {
            return true;
        }

        string shaderName = material.shader.name;
        return shaderName == "Standard"
            || shaderName.StartsWith("Legacy Shaders")
            || shaderName.StartsWith("Hidden/InternalErrorShader")
            || shaderName.StartsWith("Autodesk Interactive");
    }

    // 기존 머티리얼에 들어 있던 메인 텍스처를 URP 속성으로 옮기기 위해 읽습니다.
    private static Texture ReadMainTexture(Material material)
    {
        if (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null)
        {
            return material.GetTexture("_BaseMap");
        }

        if (material.HasProperty("_MainTex"))
        {
            return material.GetTexture("_MainTex");
        }

        return null;
    }

    // 기존 색상 값을 유지해서 변환 후 모델 색이 흰색으로 날아가지 않도록 합니다.
    private static Color ReadBaseColor(Material material)
    {
        if (material.HasProperty("_BaseColor"))
        {
            return material.GetColor("_BaseColor");
        }

        if (material.HasProperty("_Color"))
        {
            return material.GetColor("_Color");
        }

        return Color.white;
    }

    // URP/Lit에서 사용하는 대표 속성에 텍스처와 색을 다시 기록합니다.
    private static void WriteUrpProperties(Material material, Texture mainTexture, Color baseColor)
    {
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", mainTexture);
        }

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", mainTexture);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", baseColor);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", baseColor);
        }
    }
}
