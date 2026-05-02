public sealed class LoginRequestPacket : PacketBase
{
    public override PacketId Id => PacketId.LoginRequest;

    public string LoginId { get; set; }
    public string AuthToken { get; set; }

    public LoginRequestPacket(string loginId, string authToken)
    {
        LoginId = loginId;
        AuthToken = authToken;
    }
}
