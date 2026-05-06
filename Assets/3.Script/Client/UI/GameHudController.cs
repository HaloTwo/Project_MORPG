using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class GameHudController : MonoBehaviour
{
    private readonly Text[] skillSlotTexts = new Text[3];
    private readonly List<Text> inventorySlotTexts = new List<Text>();
    private readonly List<Text> equipmentSlotTexts = new List<Text>();
    private Text characterNameText;
    private Text characterClassText;
    private Text combatBannerText;
    private Text chatLogText;
    private InputField chatInputField;
    private RectTransform menuPanel;
    private RectTransform inventoryPanel;
    private RectTransform equipmentPanel;
    private RectTransform chatWindowPanel;
    private SkillController observedSkillController;
    private PacketDispatcher subscribedDispatcher;
    private readonly List<string> chatLines = new List<string>();
    private float bindRetryTimer;
    private float combatBannerTimer;

    private void Start()
    {
        BuildUi();
        RefreshCharacterInfo();
        RefreshInventoryPanel();
        RefreshEquipmentPanel();
        BindLocalPlayerSkillController();
        SubscribeNetworkEvents();
    }

    private void Update()
    {
        UpdateCombatBanner();

        if (observedSkillController == null)
        {
            bindRetryTimer -= Time.unscaledDeltaTime;
            if (bindRetryTimer <= 0.0f)
            {
                bindRetryTimer = 0.5f;
                BindLocalPlayerSkillController();
            }
        }
    }

    private void OnDestroy()
    {
        UnsubscribeNetworkEvents();
        UnbindLocalPlayerSkillController();
    }

    // 게임 씬의 모바일 HUD, 메뉴, 인벤토리, 장비창, 채팅창을 구성합니다.
    private void BuildUi()
    {
        Canvas canvas = RuntimeUiFactory.CreateCanvas("GameHudCanvas");
        canvas.sortingOrder = 10;

        RectTransform joystickInputLayer = RuntimeUiFactory.CreatePanel(canvas.transform, "VirtualJoystickInputLayer", new Color(1.0f, 1.0f, 1.0f, 0.0f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        joystickInputLayer.gameObject.AddComponent<VirtualJoystick>();

        BuildPlayerStatus(canvas.transform);
        BuildTopRightHud(canvas.transform);
        BuildChatBar(canvas.transform);
        BuildItemSlots(canvas.transform);
        BuildSkillSlots(canvas.transform);
        BuildInventoryPanel(canvas.transform);
        BuildEquipmentPanel(canvas.transform);
        BuildChatWindow(canvas.transform);
        BuildCombatBanner(canvas.transform);
    }

    // 왼쪽 위 캐릭터 상태창입니다.
    private void BuildPlayerStatus(Transform root)
    {
        RectTransform panel = RuntimeUiFactory.CreatePanel(root, "PlayerStatusPanel", new Color(0.02f, 0.05f, 0.07f, 0.62f), new Vector2(0.01f, 0.78f), new Vector2(0.30f, 0.98f), Vector2.zero, Vector2.zero);
        RuntimeUiFactory.CreatePanel(panel, "Portrait", new Color(0.75f, 0.86f, 0.92f, 0.92f), new Vector2(0.02f, 0.28f), new Vector2(0.23f, 0.92f), Vector2.zero, Vector2.zero);
        RuntimeUiFactory.CreateText(panel, "Level", "85", 24, TextAnchor.MiddleCenter, Color.white, new Vector2(0.02f, 0.12f), new Vector2(0.23f, 0.28f), Vector2.zero, Vector2.zero);

        characterClassText = RuntimeUiFactory.CreateText(panel, "ClassText", "직업", 20, TextAnchor.MiddleLeft, new Color(0.88f, 0.92f, 0.96f, 1.0f), new Vector2(0.27f, 0.66f), new Vector2(0.96f, 0.88f), Vector2.zero, Vector2.zero);
        characterNameText = RuntimeUiFactory.CreateText(panel, "NameText", "Character", 24, TextAnchor.MiddleLeft, Color.white, new Vector2(0.27f, 0.46f), new Vector2(0.96f, 0.68f), Vector2.zero, Vector2.zero);

        CreateBar(panel, "HpBar", new Color(0.05f, 0.1f, 0.08f, 0.94f), new Color(0.16f, 0.9f, 0.32f, 1.0f), new Vector2(0.27f, 0.28f), new Vector2(0.96f, 0.40f));
        CreateBar(panel, "MpBar", new Color(0.04f, 0.07f, 0.12f, 0.94f), new Color(0.18f, 0.52f, 1.0f, 1.0f), new Vector2(0.27f, 0.14f), new Vector2(0.96f, 0.25f));
    }

    // 오른쪽 위에는 메뉴 버튼 하나만 두고, 세부 기능은 펼침 메뉴에서 처리합니다.
    private void BuildTopRightHud(Transform root)
    {
        RectTransform minimap = RuntimeUiFactory.CreatePanel(root, "MiniMapPlaceholder", new Color(0.72f, 0.84f, 0.48f, 0.78f), new Vector2(0.86f, 0.76f), new Vector2(0.985f, 0.985f), Vector2.zero, Vector2.zero);
        RuntimeUiFactory.CreateText(minimap, "MiniMapText", "MAP", 24, TextAnchor.MiddleCenter, new Color(0.05f, 0.12f, 0.12f, 1.0f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Button menuButton = RuntimeUiFactory.CreateButton(root, "MenuButton", "메뉴", new Vector2(0.78f, 0.89f), new Vector2(0.84f, 0.97f), Vector2.zero, Vector2.zero);
        menuButton.onClick.AddListener(ToggleMenuPanel);

        menuPanel = RuntimeUiFactory.CreatePanel(root, "MenuPanel", new Color(0.03f, 0.05f, 0.07f, 0.94f), new Vector2(0.70f, 0.50f), new Vector2(0.84f, 0.88f), Vector2.zero, Vector2.zero);
        AddMenuButton(menuPanel, "InventoryMenuButton", "가방", 0, ToggleInventoryPanel);
        AddMenuButton(menuPanel, "EquipmentMenuButton", "장비", 1, ToggleEquipmentPanel);
        AddMenuButton(menuPanel, "ChatMenuButton", "채팅", 2, ToggleChatWindow);
        AddMenuButton(menuPanel, "CharacterSelectMenuButton", "캐릭터 선택", 3, ReturnToCharacterSelect);
        menuPanel.gameObject.SetActive(false);
    }

    private void AddMenuButton(Transform parent, string name, string label, int index, UnityEngine.Events.UnityAction action)
    {
        float yMax = 0.95f - index * 0.23f;
        Button button = RuntimeUiFactory.CreateButton(parent, name, label, new Vector2(0.08f, yMax - 0.18f), new Vector2(0.92f, yMax), Vector2.zero, Vector2.zero);
        button.onClick.AddListener(action);
    }

    // 왼쪽 아래 채팅 바입니다. 터치하면 채팅창을 열고 닫습니다.
    private void BuildChatBar(Transform root)
    {
        Button chatButton = RuntimeUiFactory.CreateButton(root, "ChatOpenButton", "내 주변에 말하기", new Vector2(0.02f, 0.02f), new Vector2(0.25f, 0.11f), Vector2.zero, Vector2.zero);
        chatButton.onClick.AddListener(ToggleChatWindow);
        chatButton.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.02f, 0.58f);
    }

    // 모바일 UI라 키보드 단축키 표기는 빼고 소비 아이템 슬롯만 보여줍니다.
    private void BuildItemSlots(Transform root)
    {
        string[] labels = { "물약\n5", "주문서\n5", "회복\n2", "버프\n3" };
        for (int i = 0; i < labels.Length; i++)
        {
            float xMin = 0.38f + i * 0.055f;
            RectTransform slot = RuntimeUiFactory.CreatePanel(root, $"ItemSlot{i + 1}", new Color(0.08f, 0.10f, 0.13f, 0.72f), new Vector2(xMin, 0.02f), new Vector2(xMin + 0.048f, 0.105f), Vector2.zero, Vector2.zero);
            RuntimeUiFactory.CreateText(slot, "Label", labels[i], 17, TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }
    }

    // 오른쪽 아래에는 전투 스킬 버튼만 남깁니다. 가방/채팅 보조 버튼은 메뉴로 이동했습니다.
    private void BuildSkillSlots(Transform root)
    {
        for (int i = 0; i < skillSlotTexts.Length; i++)
        {
            int slot = i + 1;
            float xMin = 0.72f + i * 0.078f;
            Button button = RuntimeUiFactory.CreateButton(root, $"SkillButton{slot}", $"{slot}", new Vector2(xMin, 0.06f), new Vector2(xMin + 0.070f, 0.19f), Vector2.zero, Vector2.zero);
            button.onClick.AddListener(() => UseSkillFromHud(slot));
            button.GetComponent<Image>().color = new Color(0.13f, 0.10f, 0.15f, 0.86f);

            skillSlotTexts[i] = RuntimeUiFactory.CreateText(button.transform, "SkillName", "-", 15, TextAnchor.LowerCenter, new Color(0.82f, 0.90f, 1.0f, 1.0f), new Vector2(0.0f, -0.42f), new Vector2(1.0f, 0.22f), Vector2.zero, Vector2.zero);
        }
    }

    // 선택 캐릭터의 Inventory 데이터를 20칸 그리드로 보여주는 임시 인벤토리 창입니다.
    private void BuildInventoryPanel(Transform root)
    {
        inventoryPanel = RuntimeUiFactory.CreatePanel(root, "InventoryPanel", new Color(0.03f, 0.05f, 0.07f, 0.94f), new Vector2(0.60f, 0.24f), new Vector2(0.84f, 0.76f), Vector2.zero, Vector2.zero);
        RuntimeUiFactory.CreateText(inventoryPanel, "Title", "인벤토리", 28, TextAnchor.MiddleLeft, Color.white, new Vector2(0.06f, 0.88f), new Vector2(0.72f, 0.98f), Vector2.zero, Vector2.zero);
        Button closeButton = RuntimeUiFactory.CreateButton(inventoryPanel, "CloseButton", "X", new Vector2(0.84f, 0.90f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);
        closeButton.onClick.AddListener(ToggleInventoryPanel);

        inventorySlotTexts.Clear();
        for (int i = 0; i < 20; i++)
        {
            int row = i / 4;
            int column = i % 4;
            float xMin = 0.06f + column * 0.23f;
            float yMax = 0.82f - row * 0.15f;
            RectTransform slot = RuntimeUiFactory.CreatePanel(inventoryPanel, $"InventorySlot_{i}", new Color(0.08f, 0.10f, 0.13f, 0.92f), new Vector2(xMin, yMax - 0.12f), new Vector2(xMin + 0.20f, yMax), Vector2.zero, Vector2.zero);
            Text label = RuntimeUiFactory.CreateText(slot, "Label", "-", 15, TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            inventorySlotTexts.Add(label);
        }

        inventoryPanel.gameObject.SetActive(false);
    }

    // 선택 캐릭터의 Equipment 데이터를 슬롯별로 보여주는 임시 장비창입니다.
    private void BuildEquipmentPanel(Transform root)
    {
        equipmentPanel = RuntimeUiFactory.CreatePanel(root, "EquipmentPanel", new Color(0.04f, 0.04f, 0.06f, 0.94f), new Vector2(0.34f, 0.24f), new Vector2(0.58f, 0.76f), Vector2.zero, Vector2.zero);
        RuntimeUiFactory.CreateText(equipmentPanel, "Title", "장비창", 28, TextAnchor.MiddleLeft, Color.white, new Vector2(0.06f, 0.88f), new Vector2(0.72f, 0.98f), Vector2.zero, Vector2.zero);
        Button closeButton = RuntimeUiFactory.CreateButton(equipmentPanel, "CloseButton", "X", new Vector2(0.84f, 0.90f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);
        closeButton.onClick.AddListener(ToggleEquipmentPanel);

        equipmentSlotTexts.Clear();
        EquipSlot[] slots = { EquipSlot.Weapon, EquipSlot.Helmet, EquipSlot.Armor, EquipSlot.Gloves, EquipSlot.Boots, EquipSlot.Accessory };
        for (int i = 0; i < slots.Length; i++)
        {
            float yMax = 0.82f - i * 0.12f;
            RectTransform slotPanel = RuntimeUiFactory.CreatePanel(equipmentPanel, $"EquipSlot_{slots[i]}", new Color(0.08f, 0.09f, 0.12f, 0.92f), new Vector2(0.06f, yMax - 0.09f), new Vector2(0.94f, yMax), Vector2.zero, Vector2.zero);
            Text label = RuntimeUiFactory.CreateText(slotPanel, "Label", "-", 18, TextAnchor.MiddleLeft, Color.white, new Vector2(0.06f, 0.0f), new Vector2(0.96f, 1.0f), Vector2.zero, Vector2.zero);
            equipmentSlotTexts.Add(label);
        }

        equipmentPanel.gameObject.SetActive(false);
    }

    // 채팅 로그, 입력창, 전송 버튼을 가진 임시 채팅창입니다. 서버 채팅 패킷은 다음 단계에서 연결합니다.
    private void BuildChatWindow(Transform root)
    {
        chatWindowPanel = RuntimeUiFactory.CreatePanel(root, "ChatWindowPanel", new Color(0.02f, 0.02f, 0.025f, 0.92f), new Vector2(0.02f, 0.13f), new Vector2(0.36f, 0.46f), Vector2.zero, Vector2.zero);
        RuntimeUiFactory.CreateText(chatWindowPanel, "Title", "채팅", 24, TextAnchor.MiddleLeft, Color.white, new Vector2(0.05f, 0.82f), new Vector2(0.80f, 0.98f), Vector2.zero, Vector2.zero);
        Button closeButton = RuntimeUiFactory.CreateButton(chatWindowPanel, "CloseButton", "X", new Vector2(0.84f, 0.86f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);
        closeButton.onClick.AddListener(ToggleChatWindow);

        chatLogText = RuntimeUiFactory.CreateText(chatWindowPanel, "ChatLog", "[시스템] GameScene 입장\n[시스템] 채팅 서버 패킷 연결됨", 18, TextAnchor.UpperLeft, new Color(0.86f, 0.90f, 0.92f, 1.0f), new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.80f), Vector2.zero, Vector2.zero);
        chatInputField = RuntimeUiFactory.CreateInputField(chatWindowPanel, "ChatInput", "메시지 입력", false, new Vector2(0.05f, 0.06f), new Vector2(0.72f, 0.22f), Vector2.zero, Vector2.zero);
        Button sendButton = RuntimeUiFactory.CreateButton(chatWindowPanel, "SendButton", "전송", new Vector2(0.74f, 0.06f), new Vector2(0.95f, 0.22f), Vector2.zero, Vector2.zero);
        sendButton.onClick.AddListener(SendChatMessage);

        chatWindowPanel.gameObject.SetActive(false);
    }

    // 화면 중앙의 큰 전투 알림 텍스트입니다. 스킬 사용 시 잠깐 표시합니다.
    private void BuildCombatBanner(Transform root)
    {
        combatBannerText = RuntimeUiFactory.CreateText(root, "CombatBannerText", string.Empty, 58, TextAnchor.MiddleCenter, Color.white, new Vector2(0.26f, 0.58f), new Vector2(0.74f, 0.76f), Vector2.zero, Vector2.zero);
        combatBannerText.gameObject.SetActive(false);
    }

    // 현재 선택 캐릭터 정보를 HUD와 스킬 슬롯 이름에 반영합니다.
    private void RefreshCharacterInfo()
    {
        CharacterData character = CharacterSession.Instance.SelectedCharacter;
        if (character == null)
        {
            characterNameText.text = "No Character";
            characterClassText.text = "직업 없음";
            RefreshSkillSlotTexts(null);
            return;
        }

        characterNameText.text = character.Name;
        characterClassText.text = character.GetClassNameKr();
        RefreshSkillSlotTexts(character);
    }

    private void RefreshSkillSlotTexts(CharacterData character)
    {
        for (int i = 0; i < skillSlotTexts.Length; i++)
        {
            string label = "-";
            if (character != null && character.QuickSlotSkillIds != null && i < character.QuickSlotSkillIds.Length)
            {
                int skillId = character.QuickSlotSkillIds[i];
                if (SkillDatabase.TryGetSkill(skillId, out SkillData skillData))
                {
                    label = skillData.Name;
                }
            }

            skillSlotTexts[i].text = label;
        }
    }

    // 서버에서 받은 캐릭터 인벤토리 데이터를 현재 UI 슬롯에 다시 그립니다.
    private void RefreshInventoryPanel()
    {
        CharacterData character = CharacterSession.Instance.SelectedCharacter;
        for (int i = 0; i < inventorySlotTexts.Count; i++)
        {
            inventorySlotTexts[i].text = "빈 칸";
        }

        if (character == null || character.Inventory == null)
        {
            return;
        }

        foreach (InventoryItemData item in character.Inventory)
        {
            if (item == null || item.SlotIndex < 0 || item.SlotIndex >= inventorySlotTexts.Count)
            {
                continue;
            }

            inventorySlotTexts[item.SlotIndex].text = $"{GetItemTypeName(item.ItemType)}\nID {item.ItemId}\nx{item.Count}";
        }
    }

    // 서버에서 받은 장착 UID를 슬롯별로 보여줍니다. 아이템 상세 테이블이 생기면 이름 조회로 교체합니다.
    private void RefreshEquipmentPanel()
    {
        CharacterData character = CharacterSession.Instance.SelectedCharacter;
        EquipSlot[] slots = { EquipSlot.Weapon, EquipSlot.Helmet, EquipSlot.Armor, EquipSlot.Gloves, EquipSlot.Boots, EquipSlot.Accessory };
        for (int i = 0; i < equipmentSlotTexts.Count && i < slots.Length; i++)
        {
            long itemUid = character != null && character.Equipment != null ? character.Equipment.GetEquippedItemUid(slots[i]) : 0;
            string itemText = itemUid == 0 ? "비어 있음" : $"UID {itemUid}";
            equipmentSlotTexts[i].text = $"{GetEquipSlotName(slots[i])} : {itemText}";
        }
    }

    // HUD 버튼은 입력만 담당하고 실제 스킬 패킷 송신은 SkillController에 위임합니다.
    private void UseSkillFromHud(int slot)
    {
        if (observedSkillController == null)
        {
            BindLocalPlayerSkillController();
        }

        if (observedSkillController != null)
        {
            observedSkillController.UseSkill(slot);
        }
    }

    private void ToggleMenuPanel()
    {
        if (menuPanel != null)
        {
            menuPanel.gameObject.SetActive(!menuPanel.gameObject.activeSelf);
        }
    }

    private void ToggleInventoryPanel()
    {
        if (inventoryPanel == null)
        {
            return;
        }

        bool nextActive = !inventoryPanel.gameObject.activeSelf;
        inventoryPanel.gameObject.SetActive(nextActive);
        if (nextActive)
        {
            RefreshInventoryPanel();
        }
    }

    private void ToggleEquipmentPanel()
    {
        if (equipmentPanel == null)
        {
            return;
        }

        bool nextActive = !equipmentPanel.gameObject.activeSelf;
        equipmentPanel.gameObject.SetActive(nextActive);
        if (nextActive)
        {
            RefreshEquipmentPanel();
        }
    }

    private void ToggleChatWindow()
    {
        if (chatWindowPanel != null)
        {
            chatWindowPanel.gameObject.SetActive(!chatWindowPanel.gameObject.activeSelf);
        }
    }

    /// 채팅 전송 버튼에서 호출됩니다. 로컬 HUD에 먼저 표시하고 서버에는 다른 클라이언트 브로드캐스트용 패킷을 보냅니다.
    private void SendChatMessage()
    {
        if (chatInputField == null || chatLogText == null)
        {
            return;
        }

        string message = chatInputField.text.Trim();
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        CharacterData character = CharacterSession.Instance.SelectedCharacter;
        string sender = character != null ? character.Name : "Player";
        int actorId = character != null ? character.CharacterId : 0;
        AppendChatLine(sender, message);
        chatInputField.text = string.Empty;
        NetworkManager.Instance.SendPacket(new ChatPacket(actorId, sender, message));
    }

    /// 서버에서 받은 다른 플레이어 채팅을 현재 HUD에 반영합니다.
    private void HandleChat(ChatPacket packet)
    {
        if (packet == null)
        {
            return;
        }

        CharacterData character = CharacterSession.Instance.SelectedCharacter;
        if (character != null && packet.ActorId == character.CharacterId)
        {
            return;
        }

        AppendChatLine(packet.Sender, packet.Message);
    }

    /// 채팅 로그가 무한히 길어지지 않도록 최근 메시지만 유지합니다.
    private void AppendChatLine(string sender, string message)
    {
        if (chatLogText == null || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        string safeSender = string.IsNullOrWhiteSpace(sender) ? "Player" : sender;
        chatLines.Add($"[{safeSender}] {message}");
        while (chatLines.Count > 12)
        {
            chatLines.RemoveAt(0);
        }

        chatLogText.text = string.Join("\n", chatLines);
    }

    /// GameScene HUD가 살아있는 동안만 네트워크 이벤트를 받아 MissingReference를 방지합니다.
    private void SubscribeNetworkEvents()
    {
        if (NetworkManager.Instance == null || NetworkManager.Instance.Dispatcher == subscribedDispatcher)
        {
            return;
        }

        UnsubscribeNetworkEvents();
        subscribedDispatcher = NetworkManager.Instance.Dispatcher;
        subscribedDispatcher.ChatReceived += HandleChat;
    }

    /// 씬 전환이나 HUD 파괴 후 패킷 콜백이 UI에 접근하지 않도록 구독을 해제합니다.
    private void UnsubscribeNetworkEvents()
    {
        if (subscribedDispatcher == null)
        {
            return;
        }

        subscribedDispatcher.ChatReceived -= HandleChat;
        subscribedDispatcher = null;
    }

    // 게임 씬에서도 현재 계정을 유지한 채 캐릭터 선택 화면으로 돌아갑니다.
    private void ReturnToCharacterSelect()
    {
        CharacterSession.Instance.SetSelectedCharacter(null);
        SceneFlow.SetNextScene(SceneNames.CharacterSelect);
        SceneManager.LoadScene(SceneNames.Loading);
    }

    // 로컬 플레이어가 런타임에 생성될 수 있으므로 찾는 즉시 스킬 이벤트를 연결합니다.
    private void BindLocalPlayerSkillController()
    {
        QuarterViewPlayerController player = FindFirstObjectByType<QuarterViewPlayerController>();
        if (player == null)
        {
            return;
        }

        SkillController skillController = player.GetComponent<SkillController>();
        if (skillController == null || skillController == observedSkillController)
        {
            return;
        }

        UnbindLocalPlayerSkillController();
        observedSkillController = skillController;
        observedSkillController.SkillUsed += HandleSkillUsed;
    }

    private void UnbindLocalPlayerSkillController()
    {
        if (observedSkillController == null)
        {
            return;
        }

        observedSkillController.SkillUsed -= HandleSkillUsed;
        observedSkillController = null;
    }

    // 스킬 사용 이벤트를 받아 중앙 전투 배너를 잠깐 표시합니다.
    private void HandleSkillUsed(int slot, SkillData skillData)
    {
        if (combatBannerText == null || skillData == null)
        {
            return;
        }

        combatBannerText.text = slot == 3 ? "BREAK!" : skillData.Name;
        combatBannerText.color = slot == 3 ? new Color(1.0f, 0.36f, 0.12f, 1.0f) : Color.white;
        combatBannerText.gameObject.SetActive(true);
        combatBannerTimer = 0.75f;
    }

    private void UpdateCombatBanner()
    {
        if (combatBannerText == null || !combatBannerText.gameObject.activeSelf)
        {
            return;
        }

        combatBannerTimer -= Time.unscaledDeltaTime;
        if (combatBannerTimer <= 0.0f)
        {
            combatBannerText.gameObject.SetActive(false);
        }
    }

    private void CreateBar(Transform parent, string name, Color backgroundColor, Color fillColor, Vector2 anchorMin, Vector2 anchorMax)
    {
        RectTransform background = RuntimeUiFactory.CreatePanel(parent, name, backgroundColor, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        RuntimeUiFactory.CreatePanel(background, "Fill", fillColor, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    private string GetEquipSlotName(EquipSlot slot)
    {
        switch (slot)
        {
            case EquipSlot.Weapon:
                return "무기";
            case EquipSlot.Helmet:
                return "투구";
            case EquipSlot.Armor:
                return "갑옷";
            case EquipSlot.Gloves:
                return "장갑";
            case EquipSlot.Boots:
                return "신발";
            case EquipSlot.Accessory:
                return "장신구";
            default:
                return "없음";
        }
    }

    private string GetItemTypeName(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Weapon:
                return "무기";
            case ItemType.Armor:
                return "방어구";
            case ItemType.Consumable:
                return "소모품";
            case ItemType.Material:
                return "재료";
            default:
                return "아이템";
        }
    }
}
