public sealed class CreateCharacterRequestPacket : PacketBase
{
    public override PacketId Id => PacketId.CreateCharacterRequest;

    public int AccountId { get; set; }
    public int SlotIndex { get; set; }
    public ClassType ClassType { get; set; }

    /// <summary>
    /// 빈 캐릭터 슬롯에 새 캐릭터 생성을 요청하는 패킷입니다.
    /// 지금 단계에서는 이름을 자동 생성하고, 서버는 계정당 최대 3개 제한을 검사합니다.
    /// </summary>
    public CreateCharacterRequestPacket(int accountId, int slotIndex, ClassType classType)
    {
        AccountId = accountId;
        SlotIndex = slotIndex;
        ClassType = classType;
    }
}
