using UnityEngine;

public sealed class MovePacket : PacketBase
{
    public override PacketId Id => PacketId.Move;

    public int ActorId { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Direction { get; set; }
    public float Yaw { get; set; }
    public float MoveSpeed { get; set; }

    public MovePacket(int actorId, Vector3 position, Vector3 direction, float yaw, float moveSpeed)
    {
        ActorId = actorId;
        Position = position;
        Direction = direction;
        Yaw = yaw;
        MoveSpeed = moveSpeed;
    }
}

