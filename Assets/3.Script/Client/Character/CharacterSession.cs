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

    /// <summary>
    /// 로그인 또는 회원가입 성공 시 서버가 내려준 계정 ID를 저장합니다.
    /// 이후 캐릭터 생성과 입장 요청에는 이 계정 ID가 함께 전송됩니다.
    /// </summary>
    public void SetAccount(int accountId)
    {
        AccountId = accountId;
    }

    /// <summary>
    /// 서버에서 받은 캐릭터 목록을 저장합니다.
    /// 계정에 캐릭터가 하나도 없으면 빈 리스트가 저장되고, 선택 화면은 3개의 빈 슬롯을 보여줍니다.
    /// </summary>
    public void SetCharacters(List<CharacterData> characters)
    {
        Characters = characters ?? new List<CharacterData>();
        SortBySlot();
    }

    /// <summary>
    /// 캐릭터 생성 응답처럼 단일 캐릭터가 내려왔을 때 목록에 추가하거나 갱신합니다.
    /// </summary>
    public void UpsertCharacter(CharacterData character)
    {
        if (character == null)
        {
            return;
        }

        for (int i = 0; i < Characters.Count; i++)
        {
            if (Characters[i].CharacterId == character.CharacterId)
            {
                Characters[i] = character;
                SortBySlot();
                return;
            }
        }

        Characters.Add(character);
        SortBySlot();
    }

    /// 서버에서 삭제가 승인된 캐릭터를 세션 목록에서도 제거합니다.
    public void RemoveCharacter(int characterId)
    {
        Characters.RemoveAll(character => character.CharacterId == characterId);
        if (SelectedCharacter != null && SelectedCharacter.CharacterId == characterId)
        {
            SetSelectedCharacter(null);
        }
    }

    /// 로그아웃 시 계정과 캐릭터 선택 상태를 모두 비웁니다.
    public void Clear()
    {
        AccountId = 0;
        Characters.Clear();
        SetSelectedCharacter(null);
    }

    /// <summary>
    /// 게임에 입장한 캐릭터를 현재 플레이 캐릭터로 저장합니다.
    /// GameScene은 이 값을 읽어 로컬 플레이어 위치, 직업, 스킬을 초기화합니다.
    /// </summary>
    public void SetSelectedCharacter(CharacterData character)
    {
        SelectedCharacter = character;
        SelectedCharacterChanged?.Invoke(character);
    }

    /// 슬롯 번호 기준으로 정렬해 서버 저장 순서와 UI 표시 순서를 맞춥니다.
    private void SortBySlot()
    {
        Characters.Sort((left, right) => left.SlotIndex.CompareTo(right.SlotIndex));
    }
}
