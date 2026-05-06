public sealed class ChatPacket : PacketBase
{
    public override PacketId Id => PacketId.Chat;

    public int ActorId { get; set; }
    public string Sender { get; set; }
    public string Message { get; set; }

    public ChatPacket(int actorId, string sender, string message)
    {
        ActorId = actorId;
        Sender = sender;
        Message = message;
    }
}
