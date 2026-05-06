using UnityEngine;

public sealed class FloatingCombatText : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.85f;
    [SerializeField] private float riseSpeed = 1.8f;
    [SerializeField] private Vector3 drift = new Vector3(0.35f, 0.0f, 0.0f);

    private TextMesh textMesh;
    private Color startColor;
    private float elapsed;

    // 전투 결과를 월드 공간 텍스트로 보여줍니다. 나중에 전용 UI 풀링으로 바꾸기 쉬운 단일 진입점입니다.
    public static FloatingCombatText Spawn(Vector3 position, string text, Color color, int fontSize)
    {
        GameObject textObject = new GameObject("FloatingCombatText");
        textObject.transform.position = position;

        TextMesh mesh = textObject.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.color = color;
        mesh.fontSize = fontSize;
        mesh.characterSize = 0.12f;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;

        FloatingCombatText floatingText = textObject.AddComponent<FloatingCombatText>();
        floatingText.textMesh = mesh;
        floatingText.startColor = color;
        return floatingText;
    }

    private void Awake()
    {
        if (textMesh == null)
        {
            textMesh = GetComponent<TextMesh>();
            startColor = textMesh != null ? textMesh.color : Color.white;
        }
    }

    private void LateUpdate()
    {
        elapsed += Time.deltaTime;
        transform.position += (Vector3.up * riseSpeed + drift) * Time.deltaTime;

        if (Camera.main != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position, Vector3.up);
        }

        if (textMesh != null)
        {
            float alpha = Mathf.Clamp01(1.0f - elapsed / lifetime);
            textMesh.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
        }

        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
