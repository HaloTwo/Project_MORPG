public sealed class RegisterRequestPacket : PacketBase
{
    public override PacketId Id => PacketId.RegisterRequest;

    public string LoginId { get; set; }
    public string Password { get; set; }

    /// <summary>
    /// 회원가입을 위해 클라이언트가 서버로 보내는 아이디와 비밀번호입니다.
    /// 서버는 같은 아이디가 이미 있는지 검사한 뒤 새 계정을 저장합니다.
    /// </summary>
    public RegisterRequestPacket(string loginId, string password)
    {
        LoginId = loginId;
        Password = password;
    }
}
