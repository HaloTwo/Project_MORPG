using System.Collections.Generic;
using UnityEngine;

public sealed class MockLoginController : MonoBehaviour
{
    [SerializeField] private string mockLoginId = "test_user";

    private bool loginRequested;
    private bool hasCharacterList;
    private string statusMessage = "Login 버튼을 눌러 Mock 서버에서 캐릭터 3개를 받아오세요.";
    private List<CharacterData> characters = new List<CharacterData>();

    private void OnEnable()
    {
        PacketDispatcher dispatcher = NetworkManager.Instance.Dispatcher;
        dispatcher.LoginResponseReceived += HandleLoginResponse;
        dispatcher.CharacterListReceived += HandleCharacterList;
        dispatcher.EnterGameResponseReceived += HandleEnterGameResponse;
    }

    private void OnDisable()
    {
        NetworkManager networkManager = FindFirstObjectByType<NetworkManager>();
        if (networkManager == null)
        {
            return;
        }

        PacketDispatcher dispatcher = networkManager.Dispatcher;
        dispatcher.LoginResponseReceived -= HandleLoginResponse;
        dispatcher.CharacterListReceived -= HandleCharacterList;
        dispatcher.EnterGameResponseReceived -= HandleEnterGameResponse;
    }

    private void OnGUI()
    {
        DrawMockLoginWindow();
    }

    // 임시 로그인/캐릭터 선택 UI를 화면 왼쪽 위에 그립니다.
    private void DrawMockLoginWindow()
    {
        GUILayout.BeginArea(new Rect(20.0f, 20.0f, 360.0f, 280.0f), GUI.skin.box);
        GUILayout.Label("3D Quarter-View MORPG Mock Login");
        GUILayout.Space(6.0f);
        GUILayout.Label(statusMessage);
        GUILayout.Space(8.0f);

        if (GUILayout.Button("Login / 캐릭터 목록 받기", GUILayout.Height(32.0f)))
        {
            RequestLogin();
        }

        GUILayout.Space(8.0f);

        if (hasCharacterList)
        {
            foreach (CharacterData character in characters)
            {
                string label = $"{character.Name} / {character.GetClassNameKr()} / Lv.{character.Level}";
                if (GUILayout.Button(label, GUILayout.Height(30.0f)))
                {
                    RequestEnterGame(character.CharacterId);
                }
            }
        }

        GUILayout.EndArea();
    }

    // Mock 서버로 로그인 요청 패킷을 보냅니다.
    private void RequestLogin()
    {
        loginRequested = true;
        statusMessage = "로그인 요청 중...";
        NetworkManager.Instance.SendPacket(new LoginRequestPacket(mockLoginId, "mock-token"));
    }

    // 선택한 캐릭터로 게임 입장 요청 패킷을 보냅니다.
    private void RequestEnterGame(int characterId)
    {
        statusMessage = $"캐릭터 {characterId} 입장 요청 중...";
        NetworkManager.Instance.SendPacket(new EnterGameRequestPacket(CharacterSession.Instance.AccountId, characterId));
    }

    // 로그인 결과를 받아 계정 정보를 세션에 저장합니다.
    private void HandleLoginResponse(LoginResponsePacket packet)
    {
        if (!loginRequested)
        {
            return;
        }

        if (!packet.Success)
        {
            statusMessage = $"로그인 실패: {packet.Message}";
            return;
        }

        CharacterSession.Instance.SetAccount(packet.AccountId);
        statusMessage = $"로그인 성공. AccountId={packet.AccountId}";
    }

    // 서버에서 받은 캐릭터 목록을 UI에 표시할 수 있게 저장합니다.
    private void HandleCharacterList(CharacterListPacket packet)
    {
        characters = packet.Characters;
        hasCharacterList = true;
        CharacterSession.Instance.SetCharacters(packet.Characters);
        statusMessage = "캐릭터를 선택하세요: 전사 / 궁수 / 도적";
    }

    // 게임 입장 성공 시 현재 선택 캐릭터를 세션에 저장합니다.
    private void HandleEnterGameResponse(EnterGameResponsePacket packet)
    {
        if (!packet.Success || packet.Character == null)
        {
            statusMessage = $"입장 실패: {packet.Message}";
            return;
        }

        CharacterSession.Instance.SetSelectedCharacter(packet.Character);
        statusMessage = $"입장 완료: {packet.Character.Name} / {packet.Character.GetClassNameKr()}";
    }
}

