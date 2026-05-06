using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class CharacterSelectSceneController : MonoBehaviour
{
    private Canvas canvas;
    private Text statusText;
    private readonly List<GameObject> slotObjects = new List<GameObject>();
    private readonly Dictionary<int, InputField> characterNameInputs = new Dictionary<int, InputField>();
    private PacketDispatcher subscribedDispatcher;

    private void OnEnable()
    {
        SubscribeDispatcher();
    }

    private void OnDisable()
    {
        UnsubscribeDispatcher();
    }

    private void OnDestroy()
    {
        UnsubscribeDispatcher();
    }

    private void Start()
    {
        BuildUi();
    }

    /// <summary>
    /// 캐릭터 선택 씬의 실제 Unity UI를 생성합니다.
    /// 계정당 최대 3칸을 보여주고, 빈 칸에서는 직업을 골라 캐릭터를 생성합니다.
    /// </summary>
    private void BuildUi()
    {
        canvas = RuntimeUiFactory.CreateCanvas("CharacterSelectCanvas");
        RuntimeUiFactory.CreatePanel(canvas.transform, "Background", new Color(0.05f, 0.07f, 0.08f, 1.0f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        RuntimeUiFactory.CreateText(canvas.transform, "Title", "Character Select", 46, TextAnchor.MiddleCenter, Color.white, new Vector2(0.0f, 0.82f), new Vector2(1.0f, 0.94f), Vector2.zero, Vector2.zero);
        statusText = RuntimeUiFactory.CreateText(canvas.transform, "Status", "캐릭터를 선택하거나 빈 슬롯에 새 캐릭터를 생성하세요.", 24, TextAnchor.MiddleCenter, new Color(0.78f, 0.86f, 0.92f, 1.0f), new Vector2(0.0f, 0.74f), new Vector2(1.0f, 0.81f), Vector2.zero, Vector2.zero);

        Button logoutButton = RuntimeUiFactory.CreateButton(canvas.transform, "LogoutButton", "로그아웃", new Vector2(0.84f, 0.88f), new Vector2(0.96f, 0.95f), Vector2.zero, Vector2.zero);
        logoutButton.onClick.AddListener(Logout);

        RebuildCharacterSlots();
    }

    /// <summary>
    /// 현재 세션 캐릭터 목록을 기준으로 3개의 캐릭터 슬롯 UI를 다시 그립니다.
    /// 빈 슬롯은 전사/궁수/도적 생성 버튼을 보여줍니다.
    /// </summary>
    private void RebuildCharacterSlots()
    {
        if (!IsUiAlive())
        {
            return;
        }

        foreach (GameObject slotObject in slotObjects)
        {
            if (slotObject != null)
            {
                Destroy(slotObject);
            }
        }

        slotObjects.Clear();
        characterNameInputs.Clear();

        for (int i = 0; i < 3; i++)
        {
            CharacterData character = FindCharacterBySlot(i);
            float centerX = 0.24f + i * 0.26f;
            RectTransform card = RuntimeUiFactory.CreatePanel(canvas.transform, $"CharacterSlot_{i + 1}", character == null ? new Color(0.1f, 0.13f, 0.16f, 0.96f) : GetClassCardColor(character.ClassType), new Vector2(centerX - 0.11f, 0.25f), new Vector2(centerX + 0.11f, 0.69f), Vector2.zero, Vector2.zero);
            slotObjects.Add(card.gameObject);

            if (character == null)
            {
                BuildEmptySlot(card, i);
            }
            else
            {
                BuildCharacterSlot(card, character);
            }
        }
    }

    /// 이미 생성된 캐릭터 슬롯을 표시하고 입장/삭제 버튼을 연결합니다.
    private void BuildCharacterSlot(RectTransform card, CharacterData character)
    {
        RuntimeUiFactory.CreateText(card, "Name", character.Name, 32, TextAnchor.MiddleCenter, Color.white, new Vector2(0.0f, 0.68f), new Vector2(1.0f, 0.9f), Vector2.zero, Vector2.zero);
        RuntimeUiFactory.CreateText(card, "Class", character.GetClassNameKr(), 27, TextAnchor.MiddleCenter, Color.white, new Vector2(0.0f, 0.50f), new Vector2(1.0f, 0.66f), Vector2.zero, Vector2.zero);
        RuntimeUiFactory.CreateText(card, "Info", $"Lv.{character.Level}\nGold {character.Gold}", 21, TextAnchor.MiddleCenter, new Color(0.9f, 0.94f, 0.96f, 1.0f), new Vector2(0.0f, 0.30f), new Vector2(1.0f, 0.48f), Vector2.zero, Vector2.zero);

        Button enterButton = RuntimeUiFactory.CreateButton(card, "SelectButton", "입장", new Vector2(0.18f, 0.17f), new Vector2(0.82f, 0.29f), Vector2.zero, Vector2.zero);
        enterButton.onClick.AddListener(() => RequestEnterGame(character));

        Button deleteButton = RuntimeUiFactory.CreateButton(card, "DeleteButton", "삭제", new Vector2(0.18f, 0.05f), new Vector2(0.82f, 0.15f), Vector2.zero, Vector2.zero);
        deleteButton.GetComponent<Image>().color = new Color(0.36f, 0.08f, 0.08f, 0.96f);
        deleteButton.onClick.AddListener(() => RequestDeleteCharacter(character));
    }

    /// <summary>
    /// 빈 슬롯에는 캐릭터 이름 입력창과 직업 생성 버튼 3개를 배치합니다.
    /// 이름/직업을 고르면 서버가 슬롯 제한과 저장을 최종 검증합니다.
    /// </summary>
    private void BuildEmptySlot(RectTransform card, int slotIndex)
    {
        RuntimeUiFactory.CreateText(card, "EmptyTitle", $"빈 슬롯 {slotIndex + 1}", 29, TextAnchor.MiddleCenter, Color.white, new Vector2(0.0f, 0.76f), new Vector2(1.0f, 0.92f), Vector2.zero, Vector2.zero);
        RuntimeUiFactory.CreateText(card, "EmptyInfo", "이름을 입력하고\n직업을 선택하세요.", 20, TextAnchor.MiddleCenter, new Color(0.78f, 0.86f, 0.92f, 1.0f), new Vector2(0.06f, 0.58f), new Vector2(0.94f, 0.74f), Vector2.zero, Vector2.zero);
        InputField nameInput = RuntimeUiFactory.CreateInputField(card, "CharacterNameInput", "캐릭터 이름", false, new Vector2(0.12f, 0.48f), new Vector2(0.88f, 0.57f), Vector2.zero, Vector2.zero);
        characterNameInputs[slotIndex] = nameInput;

        Button warriorButton = RuntimeUiFactory.CreateButton(card, "CreateWarriorButton", "전사", new Vector2(0.16f, 0.33f), new Vector2(0.84f, 0.45f), Vector2.zero, Vector2.zero);
        warriorButton.onClick.AddListener(() => RequestCreateCharacter(slotIndex, ClassType.Warrior));

        Button archerButton = RuntimeUiFactory.CreateButton(card, "CreateArcherButton", "궁수", new Vector2(0.16f, 0.19f), new Vector2(0.84f, 0.31f), Vector2.zero, Vector2.zero);
        archerButton.onClick.AddListener(() => RequestCreateCharacter(slotIndex, ClassType.Archer));

        Button rogueButton = RuntimeUiFactory.CreateButton(card, "CreateRogueButton", "도적", new Vector2(0.16f, 0.05f), new Vector2(0.84f, 0.17f), Vector2.zero, Vector2.zero);
        rogueButton.onClick.AddListener(() => RequestCreateCharacter(slotIndex, ClassType.Rogue));
    }

    /// <summary>
    /// 직업별 카드 색상을 반환합니다.
    /// </summary>
    private Color GetClassCardColor(ClassType classType)
    {
        switch (classType)
        {
            case ClassType.Warrior:
                return new Color(0.42f, 0.12f, 0.10f, 0.96f);
            case ClassType.Archer:
                return new Color(0.12f, 0.36f, 0.17f, 0.96f);
            case ClassType.Rogue:
                return new Color(0.25f, 0.16f, 0.42f, 0.96f);
            default:
                return new Color(0.12f, 0.16f, 0.2f, 0.96f);
        }
    }

    /// <summary>
    /// 빈 슬롯에 새 캐릭터 생성을 요청합니다.
    /// 클라이언트는 원하는 직업만 보내고, 계정당 3개 제한과 저장은 서버가 판단합니다.
    /// </summary>
    private void RequestCreateCharacter(int slotIndex, ClassType classType)
    {
        string characterName = GetRequestedCharacterName(slotIndex);
        if (!ValidateCharacterName(characterName))
        {
            return;
        }

        SetStatus($"{characterName} 생성 요청 중...");
        NetworkManager.Instance.SendPacket(new CreateCharacterRequestPacket(CharacterSession.Instance.AccountId, slotIndex, classType, characterName));
    }

    /// 슬롯별 이름 입력창에서 서버로 보낼 캐릭터 이름을 가져옵니다.
    private string GetRequestedCharacterName(int slotIndex)
    {
        return characterNameInputs.TryGetValue(slotIndex, out InputField inputField) && inputField != null ? inputField.text.Trim() : string.Empty;
    }

    /// 현재 텍스트 프로토콜은 공백 기준으로 명령을 분리하므로 캐릭터 이름에는 공백을 허용하지 않습니다.
    private bool ValidateCharacterName(string characterName)
    {
        if (string.IsNullOrWhiteSpace(characterName))
        {
            SetStatus("캐릭터 이름을 입력하세요.");
            return false;
        }

        if (characterName.Length < 2 || characterName.Length > 12)
        {
            SetStatus("캐릭터 이름은 2~12자로 입력하세요.");
            return false;
        }

        for (int i = 0; i < characterName.Length; i++)
        {
            if (char.IsWhiteSpace(characterName[i]))
            {
                SetStatus("캐릭터 이름에는 공백을 사용할 수 없습니다.");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 선택한 캐릭터로 게임 입장 요청을 보냅니다.
    /// 서버는 요청한 캐릭터가 현재 계정 소유인지 확인한 뒤 입장을 허락합니다.
    /// </summary>
    private void RequestEnterGame(CharacterData character)
    {
        SetStatus($"{character.Name} 입장 요청 중...");
        NetworkManager.Instance.SendPacket(new EnterGameRequestPacket(CharacterSession.Instance.AccountId, character.CharacterId));
    }

    /// 캐릭터 삭제는 클라이언트가 바로 지우지 않고 서버 검증 결과를 받은 뒤 반영합니다.
    private void RequestDeleteCharacter(CharacterData character)
    {
        SetStatus($"{character.Name} 삭제 요청 중...");
        NetworkManager.Instance.SendPacket(new DeleteCharacterRequestPacket(CharacterSession.Instance.AccountId, character.CharacterId));
    }

    /// <summary>
    /// 캐릭터 생성 성공 응답을 받으면 세션 목록에 반영하고 슬롯 UI를 다시 그립니다.
    /// </summary>
    private void HandleCreateCharacterResponse(CreateCharacterResponsePacket packet)
    {
        if (!IsUiAlive())
        {
            return;
        }

        if (!packet.Success || packet.Character == null)
        {
            SetStatus($"캐릭터 생성 실패: {packet.Message}");
            return;
        }

        CharacterSession.Instance.UpsertCharacter(packet.Character);
        SetStatus($"{packet.Character.Name} 생성 완료.");
        RebuildCharacterSlots();
    }

    /// 삭제 성공 응답을 받으면 로컬 세션에서 제거하고 빈 슬롯으로 다시 보여줍니다.
    private void HandleDeleteCharacterResponse(DeleteCharacterResponsePacket packet)
    {
        if (!IsUiAlive())
        {
            return;
        }

        if (!packet.Success)
        {
            SetStatus($"캐릭터 삭제 실패: {packet.Message}");
            return;
        }

        CharacterSession.Instance.RemoveCharacter(packet.CharacterId);
        SetStatus("캐릭터 삭제 완료.");
        RebuildCharacterSlots();
    }

    /// <summary>
    /// 서버에서 최신 캐릭터 목록을 내려주면 세션을 갱신하고 3슬롯 UI를 다시 그립니다.
    /// </summary>
    private void HandleCharacterList(CharacterListPacket packet)
    {
        if (!IsUiAlive())
        {
            return;
        }

        CharacterSession.Instance.SetCharacters(packet.Characters);
        RebuildCharacterSlots();
    }

    /// <summary>
    /// 입장 성공 후 로딩 씬을 거쳐 게임 씬으로 이동합니다.
    /// </summary>
    private void HandleEnterGameResponse(EnterGameResponsePacket packet)
    {
        if (!IsUiAlive())
        {
            return;
        }

        if (!packet.Success || packet.Character == null)
        {
            SetStatus($"입장 실패: {packet.Message}");
            return;
        }

        CharacterSession.Instance.SetSelectedCharacter(packet.Character);
        SceneFlow.SetNextScene(SceneNames.Game);
        SceneManager.LoadScene(SceneNames.Loading);
    }

    /// 로그아웃은 계정/캐릭터 세션을 비우고 로그인 씬으로 돌아갑니다.
    private void Logout()
    {
        CharacterSession.Instance.Clear();
        NetworkManager.Instance.Disconnect();
        SceneManager.LoadScene(SceneNames.Login);
    }

    /// DB 슬롯 번호를 기준으로 캐릭터를 찾습니다. 삭제 후 중간 슬롯이 비어도 UI가 밀리지 않습니다.
    private CharacterData FindCharacterBySlot(int slotIndex)
    {
        List<CharacterData> characters = CharacterSession.Instance.Characters;
        for (int i = 0; i < characters.Count; i++)
        {
            if (characters[i].SlotIndex == slotIndex)
            {
                return characters[i];
            }
        }

        return null;
    }

    /// NetworkManager가 DontDestroyOnLoad라 씬 전환 뒤에도 남는 이벤트를 같은 참조로 해제합니다.
    private void SubscribeDispatcher()
    {
        if (subscribedDispatcher != null)
        {
            return;
        }

        subscribedDispatcher = NetworkManager.Instance.Dispatcher;
        subscribedDispatcher.EnterGameResponseReceived += HandleEnterGameResponse;
        subscribedDispatcher.CreateCharacterResponseReceived += HandleCreateCharacterResponse;
        subscribedDispatcher.DeleteCharacterResponseReceived += HandleDeleteCharacterResponse;
        subscribedDispatcher.CharacterListReceived += HandleCharacterList;
    }

    /// Destroy된 UI가 서버 응답 이벤트를 다시 받지 않도록 구독을 정리합니다.
    private void UnsubscribeDispatcher()
    {
        if (subscribedDispatcher == null)
        {
            return;
        }

        subscribedDispatcher.EnterGameResponseReceived -= HandleEnterGameResponse;
        subscribedDispatcher.CreateCharacterResponseReceived -= HandleCreateCharacterResponse;
        subscribedDispatcher.DeleteCharacterResponseReceived -= HandleDeleteCharacterResponse;
        subscribedDispatcher.CharacterListReceived -= HandleCharacterList;
        subscribedDispatcher = null;
    }

    /// 씬 전환으로 Canvas/Text가 이미 파괴된 경우 늦게 도착한 서버 응답을 무시합니다.
    private bool IsUiAlive()
    {
        return this != null && canvas != null && statusText != null;
    }

    /// 상태 문구 변경 전에 Text가 아직 살아있는지 확인합니다.
    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    /// <summary>
    /// 직업 enum을 UI에 보여줄 한글 이름으로 변환합니다.
    /// </summary>
    private string GetClassNameKr(ClassType classType)
    {
        switch (classType)
        {
            case ClassType.Warrior:
                return "전사";
            case ClassType.Archer:
                return "궁수";
            case ClassType.Rogue:
                return "도적";
            default:
                return "없음";
        }
    }
}
