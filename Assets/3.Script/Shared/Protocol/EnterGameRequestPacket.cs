public sealed class EnterGameRequestPacket : PacketBase
{
    public override PacketId Id => PacketId.EnterGameRequest;

    public int AccountId { get; set; }
    public int CharacterId { get; set; }

    public EnterGameRequestPacket(int accountId, int characterId)
    {
        AccountId = accountId;
        CharacterId = characterId;
    }
}
