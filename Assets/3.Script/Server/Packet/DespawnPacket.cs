public sealed class DespawnPacket : PacketBase
{
    public override PacketId Id => PacketId.Despawn;

    public int ActorId { get; set; }

    public DespawnPacket(int actorId)
    {
        ActorId = actorId;
    }
}
