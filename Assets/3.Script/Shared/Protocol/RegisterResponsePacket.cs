public sealed class RegisterResponsePacket : PacketBase
{
    public override PacketId Id => PacketId.RegisterResponse;

    public bool Success { get; set; }
    public int AccountId { get; set; }
    public string Message { get; set; }

    /// <summary>
    /// 회원가입 결과를 클라이언트에 알려주는 응답 패킷입니다.
    /// 성공하면 새 accountId를 내려주고, 실패하면 실패 사유를 Message에 담습니다.
    /// </summary>
    public RegisterResponsePacket(bool success, int accountId, string message)
    {
        Success = success;
        AccountId = accountId;
        Message = message;
    }
}
