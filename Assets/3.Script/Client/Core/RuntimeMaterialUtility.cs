using UnityEngine;

public static class RuntimeMaterialUtility
{
    private static Shader cachedRuntimeShader;

    // 런타임에 생성한 캡슐/구체가 빌드에서 보라색으로 깨지지 않도록 URP 호환 머티리얼을 직접 적용합니다.
    public static void ApplyColor(Renderer renderer, Color color)
    {
        if (renderer == null)
        {
            return;
        }

        Shader shader = GetRuntimeShader();
        if (shader == null)
        {
            SetColor(renderer, color);
            return;
        }

        Material material = new Material(shader)
        {
            name = "Runtime_URP_Color"
        };

        WriteColor(material, color);
        renderer.material = material;
    }

    // 이미 URP 호환 머티리얼을 가진 렌더러의 색만 갱신합니다. 매 프레임 새 머티리얼을 만들지 않기 위해 분리했습니다.
    public static void SetColor(Renderer renderer, Color color)
    {
        if (renderer == null || renderer.material == null)
        {
            return;
        }

        WriteColor(renderer.material, color);
    }

    // URP/Lit을 우선 사용하고, 프로젝트 설정이 달라졌을 때도 최소한 보라색 에러 셰이더는 피하도록 fallback을 둡니다.
    private static Shader GetRuntimeShader()
    {
        if (cachedRuntimeShader != null)
        {
            return cachedRuntimeShader;
        }

        cachedRuntimeShader = Shader.Find("Universal Render Pipeline/Lit");
        if (cachedRuntimeShader == null)
        {
            cachedRuntimeShader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (cachedRuntimeShader == null)
        {
            cachedRuntimeShader = Shader.Find("Sprites/Default");
        }

        if (cachedRuntimeShader == null)
        {
            cachedRuntimeShader = Shader.Find("Standard");
        }

        return cachedRuntimeShader;
    }

    // URP와 Built-in 계열이 색상 프로퍼티 이름을 다르게 쓰므로 둘 다 기록합니다.
    private static void WriteColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }
}
