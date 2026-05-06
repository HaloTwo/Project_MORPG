using UnityEngine;

public sealed class CombatDummy : MonoBehaviour
{
    [SerializeField] private int maxHp = 300;
    [SerializeField] private float respawnDelay = 2.5f;

    private int currentHp;
    private float deadTimer;
    private TextMesh label;

    public bool IsAlive => currentHp > 0;

    private void Awake()
    {
        currentHp = maxHp;
        BuildLabel();
        UpdateLabel();
    }

    private void Update()
    {
        if (currentHp > 0)
        {
            return;
        }

        deadTimer += Time.deltaTime;
        if (deadTimer >= respawnDelay)
        {
            currentHp = maxHp;
            deadTimer = 0.0f;
            UpdateLabel();
        }
    }

    // 서버 전투 판정이 붙기 전까지 클라이언트 전투감을 확인하기 위한 임시 데미지 반영입니다.
    public void TakePreviewDamage(int damage, bool critical)
    {
        if (currentHp <= 0)
        {
            return;
        }

        currentHp = Mathf.Max(0, currentHp - damage);
        Color color = critical ? new Color(1.0f, 0.25f, 0.08f, 1.0f) : new Color(1.0f, 0.78f, 0.18f, 1.0f);
        string prefix = critical ? "Critical " : string.Empty;
        FloatingCombatText.Spawn(transform.position + Vector3.up * 2.3f, $"{prefix}{damage}", color, critical ? 64 : 46);
        UpdateLabel();
    }

    // 임시 캡슐 위에 이름/HP 라벨을 붙여 전투 타겟임을 확인할 수 있게 합니다.
    private void BuildLabel()
    {
        GameObject labelObject = new GameObject("DummyLabel");
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = Vector3.up * 2.2f;

        label = labelObject.AddComponent<TextMesh>();
        label.fontSize = 32;
        label.characterSize = 0.08f;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = Color.white;
    }

    private void LateUpdate()
    {
        if (label != null && Camera.main != null)
        {
            label.transform.rotation = Quaternion.LookRotation(label.transform.position - Camera.main.transform.position, Vector3.up);
        }
    }

    private void UpdateLabel()
    {
        if (label == null)
        {
            return;
        }

        label.text = currentHp > 0 ? $"수련용 허수아비\nHP {currentHp}/{maxHp}" : "Break!";
    }
}
