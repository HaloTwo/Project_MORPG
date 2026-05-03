public sealed class DeleteCharacterRequestPacket : PacketBase
{
    public override PacketId Id => PacketId.DeleteCharacterRequest;

    public int AccountId { get; set; }
    public int CharacterId { get; set; }

    /// 서버가 계정 소유 여부를 검증한 뒤 캐릭터 삭제를 처리하도록 요청합니다.
    public DeleteCharacterRequestPacket(int accountId, int characterId)
    {
        AccountId = accountId;
        CharacterId = characterId;
    }
}
