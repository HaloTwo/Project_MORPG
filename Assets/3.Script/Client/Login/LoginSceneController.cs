using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LoginSceneController : MonoBehaviour
{
    private Text statusText;
    private bool receivedLogin;
    private bool receivedCharacters;

    private void OnEnable()
    {
        PacketDispatcher dispatcher = NetworkManager.Instance.Dispatcher;
        dispatcher.LoginResponseReceived += HandleLoginResponse;
        dispatcher.CharacterListReceived += HandleCharacterList;
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
    }

    private void Start()
    {
        NetworkManager.Instance.Connect();
        BuildUi();
    }

    // 로그인 씬의 실제 Unity UI를 생성합니다.
    private void BuildUi()
    {
        Canvas canvas = RuntimeUiFactory.CreateCanvas("LoginCanvas");
        RuntimeUiFactory.CreatePanel(canvas.transform, "Background", new Color(0.04f, 0.07f, 0.09f, 1.0f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        RectTransform panel = RuntimeUiFactory.CreatePanel(canvas.transform, "LoginPanel", new Color(0.08f, 0.12f, 0.16f, 0.94f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-360.0f, -230.0f), new Vector2(360.0f, 230.0f));
        RuntimeUiFactory.CreateText(panel, "Title", "3D Quarter-View MORPG", 44, TextAnchor.MiddleCenter, Color.white, new Vector2(0.0f, 0.72f), new Vector2(1.0f, 0.95f), Vector2.zero, Vector2.zero);
        RuntimeUiFactory.CreateText(panel, "Subtitle", "Mock Login", 26, TextAnchor.MiddleCenter, new Color(0.75f, 0.85f, 0.92f, 1.0f), new Vector2(0.0f, 0.58f), new Vector2(1.0f, 0.72f), Vector2.zero, Vector2.zero);
        statusText = RuntimeUiFactory.CreateText(panel, "Status", "로그인 버튼을 누르면 Mock 서버에서 캐릭터 정보를 가져옵니다.", 22, TextAnchor.MiddleCenter, new Color(0.88f, 0.92f, 0.95f, 1.0f), new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.54f), Vector2.zero, Vector2.zero);

        Button loginButton = RuntimeUiFactory.CreateButton(panel, "LoginButton", "Login", new Vector2(0.23f, 0.12f), new Vector2(0.77f, 0.28f), Vector2.zero, Vector2.zero);
        loginButton.onClick.AddListener(RequestLogin);
    }

    // 로그인 요청 패킷을 보냅니다.
    private void RequestLogin()
    {
        receivedLogin = false;
        receivedCharacters = false;
        statusText.text = "로그인 요청 중...";
        NetworkManager.Instance.SendPacket(new LoginRequestPacket("test_user", "mock-token"));
    }

    // 로그인 응답을 받아 계정 정보를 저장합니다.
    private void HandleLoginResponse(LoginResponsePacket packet)
    {
        if (!packet.Success)
        {
            statusText.text = $"로그인 실패: {packet.Message}";
            return;
        }

        CharacterSession.Instance.SetAccount(packet.AccountId);
        receivedLogin = true;
        statusText.text = "로그인 성공. 캐릭터 목록을 받는 중...";
        TryGoCharacterSelect();
    }

    // 캐릭터 목록을 세션에 저장합니다.
    private void HandleCharacterList(CharacterListPacket packet)
    {
        CharacterSession.Instance.SetCharacters(packet.Characters);
        receivedCharacters = true;
        TryGoCharacterSelect();
    }

    // 로그인과 캐릭터 목록이 모두 준비되면 로딩 씬으로 이동합니다.
    private void TryGoCharacterSelect()
    {
        if (!receivedLogin || !receivedCharacters)
        {
            return;
        }

        SceneFlow.SetNextScene(SceneNames.CharacterSelect);
        SceneManager.LoadScene(SceneNames.Loading);
    }
}
