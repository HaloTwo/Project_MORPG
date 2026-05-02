using UnityEngine;

public sealed class SpawnPacket : PacketBase
{
    public override PacketId Id => PacketId.Spawn;

    public int ActorId { get; set; }
    public string EntityType { get; set; }
    public Vector3 Position { get; set; }
    public float Yaw { get; set; }

    public SpawnPacket(int actorId, string entityType, Vector3 position, float yaw)
    {
        ActorId = actorId;
        EntityType = entityType;
        Position = position;
        Yaw = yaw;
    }
}

