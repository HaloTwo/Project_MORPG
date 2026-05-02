using UnityEngine;

public sealed class StopPacket : PacketBase
{
    public override PacketId Id => PacketId.Stop;

    public int ActorId { get; set; }
    public Vector3 Position { get; set; }
    public float Yaw { get; set; }

    public StopPacket(int actorId, Vector3 position, float yaw)
    {
        ActorId = actorId;
        Position = position;
        Yaw = yaw;
    }
}

