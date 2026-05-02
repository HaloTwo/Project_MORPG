using UnityEngine;
using UnityEngine.UI;

public sealed class GameHudController : MonoBehaviour
{
    private Text characterText;

    private void Start()
    {
        BuildUi();
        RefreshCharacterInfo();
    }

    // 게임 씬의 조이스틱과 캐릭터 정보 UI를 생성합니다.
    private void BuildUi()
    {
        Canvas canvas = RuntimeUiFactory.CreateCanvas("GameHudCanvas");

        RectTransform joystickRoot = RuntimeUiFactory.CreatePanel(canvas.transform, "VirtualJoystick", new Color(1.0f, 1.0f, 1.0f, 0.12f), new Vector2(0.04f, 0.06f), new Vector2(0.20f, 0.34f), Vector2.zero, Vector2.zero);
        joystickRoot.gameObject.AddComponent<VirtualJoystick>();

        RectTransform infoPanel = RuntimeUiFactory.CreatePanel(canvas.transform, "InfoPanel", new Color(0.04f, 0.07f, 0.09f, 0.72f), new Vector2(0.02f, 0.88f), new Vector2(0.36f, 0.98f), Vector2.zero, Vector2.zero);
        characterText = RuntimeUiFactory.CreateText(infoPanel, "CharacterText", "Character", 22, TextAnchor.MiddleLeft, Color.white, new Vector2(0.05f, 0.0f), new Vector2(0.95f, 1.0f), Vector2.zero, Vector2.zero);
    }

    // 선택한 캐릭터 이름과 직업을 HUD에 표시합니다.
    private void RefreshCharacterInfo()
    {
        CharacterData character = CharacterSession.Instance.SelectedCharacter;
        if (character == null)
        {
            characterText.text = "No Character";
            return;
        }

        characterText.text = $"{character.Name} / {character.GetClassNameKr()} / Lv.{character.Level}";
    }
}
