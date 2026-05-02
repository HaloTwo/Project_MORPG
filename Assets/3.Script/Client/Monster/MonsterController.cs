using UnityEngine;

public sealed class MonsterController : MonoBehaviour
{
    [SerializeField] private int actorId;
    [SerializeField] private int maxHp = 100;
    [SerializeField] private int currentHp = 100;

    public int ActorId => actorId;
    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;

    private void Awake()
    {
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
    }

    // 서버가 확정한 데미지 정보를 적용합니다. 클라이언트 자체 판정으로 직접 호출하지 않는 구조입니다.
    public void ApplyDamagePacket(DamagePacket packet)
    {
        if (packet == null || packet.TargetId != actorId)
        {
            return;
        }

        maxHp = Mathf.Max(1, packet.MaxHp);
        TakeDamage(packet.Damage, packet.CurrentHp);
    }

    // 로컬 테스트용 간편 함수입니다. 실제 전투는 ApplyDamagePacket 흐름을 타는 것이 좋습니다.
    public void TakeDamage(int damage)
    {
        int nextHp = Mathf.Max(0, currentHp - Mathf.Max(0, damage));
        TakeDamage(damage, nextHp);
    }

    // 서버 권위 패킷에서 받은 HP 값으로 몬스터 상태를 갱신합니다.
    private void TakeDamage(int damage, int authoritativeHp)
    {
        currentHp = Mathf.Clamp(authoritativeHp, 0, maxHp);
        Debug.Log($"[MonsterController] actor={actorId} damage={damage} hp={currentHp}/{maxHp}");

        if (currentHp <= 0)
        {
            OnDead();
        }
    }

    // 사망 연출용 자리입니다. 실제 제거 타이밍은 서버가 결정하는 구조로 둡니다.
    private void OnDead()
    {
        Debug.Log($"[MonsterController] actor={actorId} dead");
    }
}

