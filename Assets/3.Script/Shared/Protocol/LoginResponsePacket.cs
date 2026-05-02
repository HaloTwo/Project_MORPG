public sealed class LoginResponsePacket : PacketBase
{
    public override PacketId Id => PacketId.LoginResponse;

    public bool Success { get; set; }
    public int AccountId { get; set; }
    public string Message { get; set; }

    public LoginResponsePacket(bool success, int accountId, string message)
    {
        Success = success;
        AccountId = accountId;
        Message = message;
    }
}
