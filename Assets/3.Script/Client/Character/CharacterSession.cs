using System;
using System.Collections.Generic;

public sealed class CharacterSession
{
    private static readonly CharacterSession instance = new CharacterSession();

    public static CharacterSession Instance => instance;

    public int AccountId { get; private set; }
    public CharacterData SelectedCharacter { get; private set; }
    public List<CharacterData> Characters { get; private set; } = new List<CharacterData>();

    public event Action<CharacterData> SelectedCharacterChanged;

    private CharacterSession()
    {
    }

    // 로그인 성공 시 계정 ID를 저장합니다.
    public void SetAccount(int accountId)
    {
        AccountId = accountId;
    }

    // 서버에서 받은 캐릭터 목록을 저장합니다.
    public void SetCharacters(List<CharacterData> characters)
    {
        Characters = characters ?? new List<CharacterData>();
    }

    // 게임에 입장한 캐릭터를 현재 플레이 캐릭터로 저장합니다.
    public void SetSelectedCharacter(CharacterData character)
    {
        SelectedCharacter = character;
        SelectedCharacterChanged?.Invoke(character);
    }
}
