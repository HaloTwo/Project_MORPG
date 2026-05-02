using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class CharacterSelectSceneController : MonoBehaviour
{
    private Text statusText;

    private void OnEnable()
    {
        NetworkManager.Instance.Dispatcher.EnterGameResponseReceived += HandleEnterGameResponse;
    }

    private void OnDisable()
    {
        NetworkManager networkManager = FindFirstObjectByType<NetworkManager>();
        if (networkManager != null)
        {
            networkManager.Dispatcher.EnterGameResponseReceived -= HandleEnterGameResponse;
        }
    }

    private void Start()
    {
        BuildUi();
    }

    // 캐릭터 선택 씬의 실제 Unity UI를 생성합니다.
    private void BuildUi()
    {
        Canvas canvas = RuntimeUiFactory.CreateCanvas("CharacterSelectCanvas");
        RuntimeUiFactory.CreatePanel(canvas.transform, "Background", new Color(0.05f, 0.07f, 0.08f, 1.0f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        RuntimeUiFactory.CreateText(canvas.transform, "Title", "Character Select", 46, TextAnchor.MiddleCenter, Color.white, new Vector2(0.0f, 0.82f), new Vector2(1.0f, 0.94f), Vector2.zero, Vector2.zero);
        statusText = RuntimeUiFactory.CreateText(canvas.transform, "Status", "플레이할 캐릭터를 선택하세요.", 24, TextAnchor.MiddleCenter, new Color(0.78f, 0.86f, 0.92f, 1.0f), new Vector2(0.0f, 0.74f), new Vector2(1.0f, 0.81f), Vector2.zero, Vector2.zero);

        List<CharacterData> characters = CharacterSession.Instance.Characters;
        for (int i = 0; i < characters.Count; i++)
        {
            CharacterData character = characters[i];
            float centerX = 0.24f + i * 0.26f;
            RectTransform card = RuntimeUiFactory.CreatePanel(canvas.transform, $"CharacterCard_{character.CharacterId}", GetClassCardColor(character.ClassType), new Vector2(centerX - 0.11f, 0.28f), new Vector2(centerX + 0.11f, 0.68f), Vector2.zero, Vector2.zero);
            RuntimeUiFactory.CreateText(card, "Name", character.Name, 34, TextAnchor.MiddleCenter, Color.white, new Vector2(0.0f, 0.68f), new Vector2(1.0f, 0.9f), Vector2.zero, Vector2.zero);
            RuntimeUiFactory.CreateText(card, "Class", character.GetClassNameKr(), 28, TextAnchor.MiddleCenter, Color.white, new Vector2(0.0f, 0.50f), new Vector2(1.0f, 0.66f), Vector2.zero, Vector2.zero);
            RuntimeUiFactory.CreateText(card, "Info", $"Lv.{character.Level}\nGold {character.Gold}", 22, TextAnchor.MiddleCenter, new Color(0.9f, 0.94f, 0.96f, 1.0f), new Vector2(0.0f, 0.30f), new Vector2(1.0f, 0.48f), Vector2.zero, Vector2.zero);

            Button button = RuntimeUiFactory.CreateButton(card, "SelectButton", "입장", new Vector2(0.18f, 0.08f), new Vector2(0.82f, 0.22f), Vector2.zero, Vector2.zero);
            button.onClick.AddListener(() => RequestEnterGame(character));
        }
    }

    // 직업별 카드 색상을 반환합니다.
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

    // 선택한 캐릭터로 게임 입장 요청을 보냅니다.
    private void RequestEnterGame(CharacterData character)
    {
        statusText.text = $"{character.Name} 입장 요청 중...";
        NetworkManager.Instance.SendPacket(new EnterGameRequestPacket(CharacterSession.Instance.AccountId, character.CharacterId));
    }

    // 입장 성공 후 로딩 씬을 거쳐 게임 씬으로 이동합니다.
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
}
