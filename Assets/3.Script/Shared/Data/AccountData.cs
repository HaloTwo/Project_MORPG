using System.Collections.Generic;

public sealed class AccountData
{
    public int AccountId;
    public string LoginId;
    public string DisplayName;
    public List<CharacterData> Characters = new List<CharacterData>();
}
