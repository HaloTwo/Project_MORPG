public sealed class EnterGameResponsePacket : PacketBase
{
    public override PacketId Id => PacketId.EnterGameResponse;

    public bool Success { get; set; }
    public CharacterData Character { get; set; }
    public string Message { get; set; }

    public EnterGameResponsePacket(bool success, CharacterData character, string message)
    {
        Success = success;
        Character = character;
        Message = message;
    }
}
