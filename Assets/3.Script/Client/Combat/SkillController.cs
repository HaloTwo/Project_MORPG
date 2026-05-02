using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class SkillController : MonoBehaviour
{
    [SerializeField] private int actorId = 1;
    [SerializeField] private ClassType classType = ClassType.None;
    [SerializeField] private int[] quickSlotSkillIds = new int[3];
    [SerializeField] private GameObject debugEffectPrefab;
    [SerializeField] private Transform effectSpawnPoint;

    private void Update()
    {
        if (WasSkillKeyPressed(1))
        {
            UseSkill(1);
        }
        else if (WasSkillKeyPressed(2))
        {
            UseSkill(2);
        }
        else if (WasSkillKeyPressed(3))
        {
            UseSkill(3);
        }
    }

    // 선택된 캐릭터의 직업과 퀵슬롯 스킬을 적용합니다.
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

    // Unity 6 새 Input System과 기존 Input Manager 양쪽에서 숫자키 입력을 확인합니다.
    private bool WasSkillKeyPressed(int slot)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            switch (slot)
            {
                case 1:
                    return Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame;
                case 2:
                    return Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame;
                case 3:
                    return Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame;
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        switch (slot)
        {
            case 1:
                return Input.GetKeyDown(KeyCode.Alpha1);
            case 2:
                return Input.GetKeyDown(KeyCode.Alpha2);
            case 3:
                return Input.GetKeyDown(KeyCode.Alpha3);
        }
#endif

        return false;
    }

    // 스킬 사용 의도를 네트워크 계층으로 보내고, 선택적으로 로컬 테스트 이펙트를 재생합니다.
    public void UseSkill(int skillSlot)
    {
        int skillId = GetSkillIdFromSlot(skillSlot);
        if (skillId == 0)
        {
            Debug.LogWarning($"[SkillController] slot={skillSlot}에 스킬이 없습니다.");
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
        PlayDebugEffect(castPosition, castDirection);
        Debug.Log($"[SkillController] {classType} skill slot={skillSlot}, id={skillId}, name={skillData.Name}");
    }

    // 1, 2, 3번 슬롯에서 실제 skillId를 꺼냅니다.
    private int GetSkillIdFromSlot(int skillSlot)
    {
        int index = skillSlot - 1;
        if (quickSlotSkillIds == null || index < 0 || index >= quickSlotSkillIds.Length)
        {
            return 0;
        }

        return quickSlotSkillIds[index];
    }

    // 서버 권위 이펙트 구조가 생기기 전까지 임시 로컬 이펙트를 생성합니다.
    private void PlayDebugEffect(Vector3 position, Vector3 direction)
    {
        if (debugEffectPrefab == null)
        {
            return;
        }

        Vector3 spawnPosition = effectSpawnPoint != null ? effectSpawnPoint.position : position + direction.normalized;
        Quaternion spawnRotation = Quaternion.LookRotation(direction == Vector3.zero ? transform.forward : direction, Vector3.up);
        Instantiate(debugEffectPrefab, spawnPosition, spawnRotation);
    }
}
