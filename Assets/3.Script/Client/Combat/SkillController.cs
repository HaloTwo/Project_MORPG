using System;
using UnityEngine;

public sealed class SkillController : MonoBehaviour
{
    [SerializeField] private int actorId = 1;
    [SerializeField] private ClassType classType = ClassType.None;
    [SerializeField] private int[] quickSlotSkillIds = new int[3];
    [SerializeField] private GameObject debugEffectPrefab;
    [SerializeField] private Transform effectSpawnPoint;

    public event Action<int, SkillData> SkillUsed;

    // 선택한 캐릭터의 ID, 직업, 퀵슬롯 스킬을 플레이어에게 적용합니다.
    public void InitializeFromCharacter(CharacterData character)
    {
        if (character == null)
        {
            return;
        }

        actorId = character.CharacterId;
        classType = character.ClassType;
        quickSlotSkillIds = character.QuickSlotSkillIds;
        Debug.Log($"[SkillController] {character.Name} skills loaded: {string.Join(", ", quickSlotSkillIds)}");
    }

    // 모바일 우선 구조라 스킬은 HUD 버튼 터치로만 호출합니다.
    public void UseSkill(int skillSlot)
    {
        int skillId = GetSkillIdFromSlot(skillSlot);
        if (skillId == 0)
        {
            Debug.LogWarning($"[SkillController] slot={skillSlot} 스킬이 없습니다.");
            return;
        }

        if (!SkillDatabase.TryGetSkill(skillId, out SkillData skillData))
        {
            Debug.LogWarning($"[SkillController] SkillData를 찾을 수 없습니다. skillId={skillId}");
            return;
        }

        Vector3 castPosition = transform.position;
        Vector3 castDirection = transform.forward;
        SkillPacket packet = new SkillPacket(actorId, skillSlot, skillId, castPosition, castDirection);

        NetworkManager.Instance.SendPacket(packet);
        PlayCharacterAttackAnimation();
        PlayDebugEffect(castPosition, castDirection, skillData);
        ApplyLocalCombatPreview(skillData, castPosition, castDirection);
        SkillUsed?.Invoke(skillSlot, skillData);
        Debug.Log($"[SkillController] {classType} skill slot={skillSlot}, id={skillId}, name={skillData.Name}");
    }

    // 실제 전투 판정은 서버로 보내고, 클라이언트에서는 입력 즉시 공격 모션만 재생합니다.
    private void PlayCharacterAttackAnimation()
    {
        CharacterVisualController visualController = GetComponent<CharacterVisualController>();
        if (visualController != null)
        {
            visualController.PlayAttack();
        }
    }

    // 1, 2, 3번 퀵슬롯에 연결된 실제 skillId를 꺼냅니다.
    private int GetSkillIdFromSlot(int skillSlot)
    {
        int index = skillSlot - 1;
        if (quickSlotSkillIds == null || index < 0 || index >= quickSlotSkillIds.Length)
        {
            return 0;
        }

        return quickSlotSkillIds[index];
    }

    // 서버 권위 전투 구조가 붙기 전까지 가까운 수련 더미에 임시 데미지 표시를 띄웁니다.
    private void ApplyLocalCombatPreview(SkillData skillData, Vector3 castPosition, Vector3 castDirection)
    {
        CombatDummy target = FindPreviewTarget(castPosition, castDirection, skillData.Range);
        if (target == null)
        {
            Vector3 missPosition = castPosition + castDirection.normalized * 1.6f + Vector3.up * 1.4f;
            FloatingCombatText.Spawn(missPosition, "Miss", new Color(0.65f, 0.75f, 0.85f, 1.0f), 40);
            return;
        }

        bool critical = UnityEngine.Random.value >= 0.72f;
        int damage = critical ? Mathf.RoundToInt(skillData.Damage * 1.75f) : skillData.Damage;
        target.TakePreviewDamage(damage, critical);
    }

    // 조준 방향 앞쪽의 수련 더미를 우선 찾고, 없으면 범위 안 가장 가까운 대상을 선택합니다.
    private CombatDummy FindPreviewTarget(Vector3 castPosition, Vector3 castDirection, float range)
    {
        CombatDummy[] dummies = FindObjectsByType<CombatDummy>(FindObjectsSortMode.None);
        CombatDummy bestTarget = null;
        float bestScore = float.MaxValue;

        foreach (CombatDummy dummy in dummies)
        {
            if (dummy == null || !dummy.IsAlive)
            {
                continue;
            }

            Vector3 toTarget = dummy.transform.position - castPosition;
            toTarget.y = 0.0f;
            float distance = toTarget.magnitude;
            if (distance > range + 1.5f)
            {
                continue;
            }

            float angle = Vector3.Angle(castDirection, toTarget.normalized);
            float score = distance + angle * 0.05f;
            if (angle > 95.0f)
            {
                score += 10.0f;
            }

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = dummy;
            }
        }

        return bestTarget;
    }

    // 이펙트 프리팹이 있으면 사용하고, 없으면 전투 테스트용 구체 이펙트를 임시 생성합니다.
    private void PlayDebugEffect(Vector3 position, Vector3 direction, SkillData skillData)
    {
        Vector3 normalizedDirection = direction == Vector3.zero ? transform.forward : direction.normalized;
        Vector3 spawnPosition = effectSpawnPoint != null ? effectSpawnPoint.position : position + normalizedDirection * 1.6f + Vector3.up * 0.4f;

        if (debugEffectPrefab != null)
        {
            Quaternion spawnRotation = Quaternion.LookRotation(normalizedDirection, Vector3.up);
            Instantiate(debugEffectPrefab, spawnPosition, spawnRotation);
            return;
        }

        Color effectColor = GetPreviewEffectColor();
        float radius = Mathf.Clamp(skillData.Range * 0.22f, 1.0f, 2.4f);
        CombatPreviewEffect.Spawn(spawnPosition, effectColor, radius);
    }

    // 직업별 임시 스킬 이펙트 색을 분리해서 모델이 없어도 직업 차이가 보이게 합니다.
    private Color GetPreviewEffectColor()
    {
        switch (classType)
        {
            case ClassType.Warrior:
                return new Color(1.0f, 0.28f, 0.08f, 0.75f);
            case ClassType.Archer:
                return new Color(0.25f, 1.0f, 0.35f, 0.75f);
            case ClassType.Rogue:
                return new Color(0.65f, 0.35f, 1.0f, 0.75f);
            default:
                return new Color(0.4f, 0.75f, 1.0f, 0.75f);
        }
    }
}

public sealed class CombatPreviewEffect : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.42f;
    [SerializeField] private float expandMultiplier = 1.85f;

    private Renderer cachedRenderer;
    private Color startColor;
    private Vector3 startScale;
    private float elapsed;

    // 모델/애니메이션/전용 VFX가 들어오기 전까지 스킬 사용 위치를 보여주는 임시 이펙트입니다.
    public static CombatPreviewEffect Spawn(Vector3 position, Color color, float radius)
    {
        GameObject effectObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effectObject.name = "CombatPreviewEffect";
        effectObject.transform.position = position;
        effectObject.transform.localScale = Vector3.one * radius;

        Collider collider = effectObject.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = effectObject.GetComponent<Renderer>();
        RuntimeMaterialUtility.ApplyColor(renderer, color);

        CombatPreviewEffect effect = effectObject.AddComponent<CombatPreviewEffect>();
        effect.cachedRenderer = renderer;
        effect.startColor = color;
        effect.startScale = effectObject.transform.localScale;
        return effect;
    }

    private void Awake()
    {
        if (cachedRenderer == null)
        {
            cachedRenderer = GetComponent<Renderer>();
            startColor = cachedRenderer != null ? cachedRenderer.material.color : Color.white;
            startScale = transform.localScale;
        }
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float normalized = Mathf.Clamp01(elapsed / lifetime);
        transform.localScale = Vector3.Lerp(startScale, startScale * expandMultiplier, normalized);

        if (cachedRenderer != null)
        {
            float alpha = Mathf.Lerp(startColor.a, 0.0f, normalized);
            RuntimeMaterialUtility.SetColor(cachedRenderer, new Color(startColor.r, startColor.g, startColor.b, alpha));
        }

        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
