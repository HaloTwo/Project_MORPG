using UnityEngine;

public sealed class SkillPacket : PacketBase
{
    public override PacketId Id => PacketId.Skill;

    public int CasterId { get; set; }
    public int SkillSlot { get; set; }
    public int SkillId { get; set; }
    public Vector3 CastPosition { get; set; }
    public Vector3 CastDirection { get; set; }

    public SkillPacket(int casterId, int skillSlot, int skillId, Vector3 castPosition, Vector3 castDirection)
    {
        CasterId = casterId;
        SkillSlot = skillSlot;
        SkillId = skillId;
        CastPosition = castPosition;
        CastDirection = castDirection;
    }
}
