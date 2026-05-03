public sealed class DeleteCharacterResponsePacket : PacketBase
{
    public override PacketId Id => PacketId.DeleteCharacterResponse;

    public bool Success { get; set; }
    public int CharacterId { get; set; }
    public string Message { get; set; }

    /// 삭제 성공 여부와 삭제 대상 캐릭터 ID를 선택 화면에 돌려줍니다.
    public DeleteCharacterResponsePacket(bool success, int characterId, string message)
    {
        Success = success;
        CharacterId = characterId;
        Message = message;
    }
}
