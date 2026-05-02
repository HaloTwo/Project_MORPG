using System;

public abstract class PacketBase
{
    public abstract PacketId Id { get; }
    public long ClientTick { get; set; }
    public DateTime CreatedAtUtc { get; private set; }

    protected PacketBase()
    {
        CreatedAtUtc = DateTime.UtcNow;
    }
}

