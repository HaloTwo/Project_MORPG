using System.Collections.Generic;

public sealed class CharacterListPacket : PacketBase
{
    public override PacketId Id => PacketId.CharacterList;

    public int AccountId { get; set; }
    public List<CharacterData> Characters { get; private set; }

    public CharacterListPacket(int accountId, List<CharacterData> characters)
    {
        AccountId = accountId;
        Characters = characters ?? new List<CharacterData>();
    }
}
