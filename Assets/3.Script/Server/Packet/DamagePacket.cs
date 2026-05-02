public sealed class DamagePacket : PacketBase
{
    public override PacketId Id => PacketId.Damage;

    public int AttackerId { get; set; }
    public int TargetId { get; set; }
    public int Damage { get; set; }
    public int CurrentHp { get; set; }
    public int MaxHp { get; set; }

    public DamagePacket(int attackerId, int targetId, int damage, int currentHp, int maxHp)
    {
        AttackerId = attackerId;
        TargetId = targetId;
        Damage = damage;
        CurrentHp = currentHp;
        MaxHp = maxHp;
    }
}

