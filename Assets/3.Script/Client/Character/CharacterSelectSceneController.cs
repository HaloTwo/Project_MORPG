using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class CharacterSelectSceneController : MonoBehaviour
{
    private Canvas canvas;
    private Text statusText;
    private readonly List<GameObject> slotObjects = new List<GameObject>();

    private void OnEnable()
    {
        PacketDispatcher dispatcher = NetworkManager.Instance.Dispatcher;
        dispatcher.EnterGameResponseReceived += HandleEnterGameResponse;
        dispatcher.CreateCharacterResponseReceived += HandleCreateCharacterResponse;
        dispatcher.CharacterListReceived += HandleCharacterList;
    }

    private void OnDisable()
    {
        NetworkManager networkManager = FindFirstObjectByType<NetworkManager>();
        if (networkManager != null)
        {
            PacketDispatcher dispatcher = networkManager.Dispatcher;
            dispatcher.EnterGameResponseReceived -= HandleEnterGameResponse;
            dispatcher.CreateCharacterResponseReceived -= HandleCreateCharacterResponse;
            dispatcher.CharacterListReceived -= HandleCharacterList;
        }
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
        RebuildCharacterSlots();
    }

    /// <summary>
    /// 현재 세션 캐릭터 목록을 기준으로 3개의 캐릭터 슬롯 UI를 다시 그립니다.
    /// 빈 슬롯은 전사/궁수/도적 생성 버튼을 보여줍니다.
    /// </summary>
    private void RebuildCharacterSlots()
    {
        foreach (GameObject slotObject in slotObjects)
        {
            Destroy(slotObject);
        }

        slotObjects.Clear();

        List<CharacterData> characters = CharacterSession.Instance.Characters;
        for (int i = 0; i < 3; i++)
        {
            CharacterData character = i < characters.Count ? characters[i] : null;
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

    /// <summary>
    /// 이미 생성된 캐릭터 슬롯을 표시하고 입장 버튼을 연결합니다.
    /// </summary>
    private void BuildCharacterSlot(RectTransform card, CharacterData character)
    {
        RuntimeUiFactory.CreateText(card, "Name", character.Name, 32, TextAnchor.MiddleCenter, Color.white, new Vector2(0.0f, 0.68f), new Vector2(1.0f, 0.9f), Vector2.zero, Vector2.zero);
        RuntimeUiFactory.CreateText(card, "Class", character.GetClassNameKr(), 27, TextAnchor.MiddleCenter, Color.white, new Vector2(0.0f, 0.50f), new Vector2(1.0f, 0.66f), Vector2.zero, Vector2.zero);
        RuntimeUiFactory.CreateText(card, "Info", $"Lv.{character.Level}\nGold {character.Gold}", 21, TextAnchor.MiddleCenter, new Color(0.9f, 0.94f, 0.96f, 1.0f), new Vector2(0.0f, 0.30f), new Vector2(1.0f, 0.48f), Vector2.zero, Vector2.zero);

        Button button = RuntimeUiFactory.CreateButton(card, "SelectButton", "입장", new Vector2(0.18f, 0.08f), new Vector2(0.82f, 0.22f), Vector2.zero, Vector2.zero);
        button.onClick.AddListener(() => RequestEnterGame(character));
    }

    /// <summary>
    /// 빈 슬롯에는 캐릭터 생성 버튼 3개를 배치합니다.
    /// 직업만 고르면 서버가 캐릭터 이름, 기본 위치, 장비, 스킬을 생성해 저장합니다.
    /// </summary>
    private void BuildEmptySlot(RectTransform card, int slotIndex)
    {
        RuntimeUiFactory.CreateText(card, "EmptyTitle", $"빈 슬롯 {slotIndex + 1}", 29, TextAnchor.MiddleCenter, Color.white, new Vector2(0.0f, 0.76f), new Vector2(1.0f, 0.92f), Vector2.zero, Vector2.zero);
        RuntimeUiFactory.CreateText(card, "EmptyInfo", "직업을 선택해\n캐릭터를 생성하세요.", 20, TextAnchor.MiddleCenter, new Color(0.78f, 0.86f, 0.92f, 1.0f), new Vector2(0.06f, 0.56f), new Vector2(0.94f, 0.74f), Vector2.zero, Vector2.zero);

        Button warriorButton = RuntimeUiFactory.CreateButton(card, "CreateWarriorButton", "전사", new Vector2(0.16f, 0.36f), new Vector2(0.84f, 0.48f), Vector2.zero, Vector2.zero);
        warriorButton.onClick.AddListener(() => RequestCreateCharacter(slotIndex, ClassType.Warrior));

        Button archerButton = RuntimeUiFactory.CreateButton(card, "CreateArcherButton", "궁수", new Vector2(0.16f, 0.22f), new Vector2(0.84f, 0.34f), Vector2.zero, Vector2.zero);
        archerButton.onClick.AddListener(() => RequestCreateCharacter(slotIndex, ClassType.Archer));

        Button rogueButton = RuntimeUiFactory.CreateButton(card, "CreateRogueButton", "도적", new Vector2(0.16f, 0.08f), new Vector2(0.84f, 0.20f), Vector2.zero, Vector2.zero);
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
        statusText.text = $"{GetClassNameKr(classType)} 캐릭터 생성 요청 중...";
        NetworkManager.Instance.SendPacket(new CreateCharacterRequestPacket(CharacterSession.Instance.AccountId, slotIndex, classType));
    }

    /// <summary>
    /// 선택한 캐릭터로 게임 입장 요청을 보냅니다.
    /// 서버는 요청한 캐릭터가 현재 계정 소유인지 확인한 뒤 입장을 허락합니다.
    /// </summary>
    private void RequestEnterGame(CharacterData character)
    {
        statusText.text = $"{character.Name} 입장 요청 중...";
        NetworkManager.Instance.SendPacket(new EnterGameRequestPacket(CharacterSession.Instance.AccountId, character.CharacterId));
    }

    /// <summary>
    /// 캐릭터 생성 성공 응답을 받으면 세션 목록에 반영하고 슬롯 UI를 다시 그립니다.
    /// </summary>
    private void HandleCreateCharacterResponse(CreateCharacterResponsePacket packet)
    {
        if (!packet.Success || packet.Character == null)
        {
            statusText.text = $"캐릭터 생성 실패: {packet.Message}";
            return;
        }

        CharacterSession.Instance.UpsertCharacter(packet.Character);
        statusText.text = $"{packet.Character.Name} 생성 완료.";
        RebuildCharacterSlots();
    }

    /// <summary>
    /// 서버에서 최신 캐릭터 목록을 내려주면 세션을 갱신하고 3슬롯 UI를 다시 그립니다.
    /// </summary>
    private void HandleCharacterList(CharacterListPacket packet)
    {
        CharacterSession.Instance.SetCharacters(packet.Characters);
        RebuildCharacterSlots();
    }

    /// <summary>
    /// 입장 성공 후 로딩 씬을 거쳐 게임 씬으로 이동합니다.
    /// </summary>
    private void HandleEnterGameResponse(EnterGameResponsePacket packet)
    {
        if (!packet.Success || packet.Character == null)
        {
            statusText.text = $"입장 실패: {packet.Message}";
            return;
        }

        CharacterSession.Instance.SetSelectedCharacter(packet.Character);
        SceneFlow.SetNextScene(SceneNames.Game);
        SceneManager.LoadScene(SceneNames.Loading);
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
