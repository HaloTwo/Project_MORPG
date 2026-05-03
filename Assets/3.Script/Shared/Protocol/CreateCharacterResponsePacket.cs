public sealed class CreateCharacterResponsePacket : PacketBase
{
    public override PacketId Id => PacketId.CreateCharacterResponse;

    public bool Success { get; set; }
    public CharacterData Character { get; set; }
    public string Message { get; set; }

    /// <summary>
    /// 캐릭터 생성 결과를 알려주는 응답 패킷입니다.
    /// 성공 시 생성된 캐릭터 데이터를 내려주어 캐릭터 선택 화면을 갱신할 수 있게 합니다.
    /// </summary>
    public CreateCharacterResponsePacket(bool success, CharacterData character, string message)
    {
        Success = success;
        Character = character;
        Message = message;
    }
}
