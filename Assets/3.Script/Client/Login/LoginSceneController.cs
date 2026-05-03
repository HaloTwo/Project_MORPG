using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LoginSceneController : MonoBehaviour
{
    private InputField loginIdInput;
    private InputField passwordInput;
    private Text statusText;
    private bool receivedLogin;
    private bool receivedCharacters;

    private void OnEnable()
    {
        PacketDispatcher dispatcher = NetworkManager.Instance.Dispatcher;
        dispatcher.LoginResponseReceived += HandleLoginResponse;
        dispatcher.RegisterResponseReceived += HandleRegisterResponse;
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
        dispatcher.RegisterResponseReceived -= HandleRegisterResponse;
        dispatcher.CharacterListReceived -= HandleCharacterList;
    }

    private void Start()
    {
        NetworkManager.Instance.Connect();
        BuildUi();
    }

    /// <summary>
    /// 로그인 씬의 실제 Unity UI를 생성합니다.
    /// 아이디와 비밀번호를 입력한 뒤 로그인 또는 회원가입 요청을 보낼 수 있습니다.
    /// </summary>
    private void BuildUi()
    {
        Canvas canvas = RuntimeUiFactory.CreateCanvas("LoginCanvas");
        RuntimeUiFactory.CreatePanel(canvas.transform, "Background", new Color(0.04f, 0.07f, 0.09f, 1.0f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        RectTransform panel = RuntimeUiFactory.CreatePanel(canvas.transform, "LoginPanel", new Color(0.08f, 0.12f, 0.16f, 0.94f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420.0f, -290.0f), new Vector2(420.0f, 290.0f));
        RuntimeUiFactory.CreateText(panel, "Title", "3D Quarter-View MORPG", 42, TextAnchor.MiddleCenter, Color.white, new Vector2(0.0f, 0.78f), new Vector2(1.0f, 0.95f), Vector2.zero, Vector2.zero);
        RuntimeUiFactory.CreateText(panel, "Subtitle", "Login / Register", 25, TextAnchor.MiddleCenter, new Color(0.75f, 0.85f, 0.92f, 1.0f), new Vector2(0.0f, 0.68f), new Vector2(1.0f, 0.78f), Vector2.zero, Vector2.zero);

        loginIdInput = RuntimeUiFactory.CreateInputField(panel, "LoginIdInput", "아이디", false, new Vector2(0.12f, 0.54f), new Vector2(0.88f, 0.64f), Vector2.zero, Vector2.zero);
        passwordInput = RuntimeUiFactory.CreateInputField(panel, "PasswordInput", "비밀번호", true, new Vector2(0.12f, 0.40f), new Vector2(0.88f, 0.50f), Vector2.zero, Vector2.zero);
        statusText = RuntimeUiFactory.CreateText(panel, "Status", "아이디와 비밀번호를 입력하세요. 테스트 계정: test_user / 1234", 21, TextAnchor.MiddleCenter, new Color(0.88f, 0.92f, 0.95f, 1.0f), new Vector2(0.08f, 0.24f), new Vector2(0.92f, 0.36f), Vector2.zero, Vector2.zero);

        Button loginButton = RuntimeUiFactory.CreateButton(panel, "LoginButton", "로그인", new Vector2(0.12f, 0.10f), new Vector2(0.48f, 0.21f), Vector2.zero, Vector2.zero);
        loginButton.onClick.AddListener(RequestLogin);

        Button registerButton = RuntimeUiFactory.CreateButton(panel, "RegisterButton", "회원가입", new Vector2(0.52f, 0.10f), new Vector2(0.88f, 0.21f), Vector2.zero, Vector2.zero);
        registerButton.onClick.AddListener(RequestRegister);
    }

    /// <summary>
    /// 입력한 아이디와 비밀번호로 로그인 요청 패킷을 보냅니다.
    /// 서버는 계정이 존재하고 비밀번호가 맞는지 검사한 뒤 결과를 내려줍니다.
    /// </summary>
    private void RequestLogin()
    {
        string loginId = GetLoginId();
        string password = GetPassword();
        if (!ValidateCredentialInput(loginId, password))
        {
            return;
        }

        receivedLogin = false;
        receivedCharacters = false;
        statusText.text = "로그인 요청 중...";
        NetworkManager.Instance.SendPacket(new LoginRequestPacket(loginId, password));
    }

    /// <summary>
    /// 입력한 아이디와 비밀번호로 회원가입 요청 패킷을 보냅니다.
    /// 서버는 같은 아이디가 이미 있으면 실패 응답을 내려줍니다.
    /// </summary>
    private void RequestRegister()
    {
        string loginId = GetLoginId();
        string password = GetPassword();
        if (!ValidateCredentialInput(loginId, password))
        {
            return;
        }

        receivedLogin = false;
        receivedCharacters = false;
        statusText.text = "회원가입 요청 중...";
        NetworkManager.Instance.SendPacket(new RegisterRequestPacket(loginId, password));
    }

    /// <summary>
    /// 로그인 응답을 받아 계정 정보를 저장합니다.
    /// 성공 후 캐릭터 목록까지 받으면 캐릭터 선택 씬으로 이동합니다.
    /// </summary>
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

    /// <summary>
    /// 회원가입 응답을 받아 새 계정 정보를 저장합니다.
    /// 회원가입 직후에도 캐릭터 선택 화면으로 이동해 빈 슬롯 3칸을 볼 수 있게 합니다.
    /// </summary>
    private void HandleRegisterResponse(RegisterResponsePacket packet)
    {
        if (!packet.Success)
        {
            statusText.text = $"회원가입 실패: {packet.Message}";
            return;
        }

        CharacterSession.Instance.SetAccount(packet.AccountId);
        receivedLogin = true;
        statusText.text = "회원가입 성공. 캐릭터 슬롯을 준비하는 중...";
        TryGoCharacterSelect();
    }

    /// <summary>
    /// 캐릭터 목록을 세션에 저장합니다.
    /// 새 계정은 비어 있는 목록을 받아 캐릭터 선택 씬에서 3개의 빈 슬롯으로 표시됩니다.
    /// </summary>
    private void HandleCharacterList(CharacterListPacket packet)
    {
        CharacterSession.Instance.SetCharacters(packet.Characters);
        receivedCharacters = true;
        TryGoCharacterSelect();
    }

    /// <summary>
    /// 로그인 또는 회원가입 결과와 캐릭터 목록이 모두 준비되면 로딩 씬으로 이동합니다.
    /// </summary>
    private void TryGoCharacterSelect()
    {
        if (!receivedLogin || !receivedCharacters)
        {
            return;
        }

        SceneFlow.SetNextScene(SceneNames.CharacterSelect);
        SceneManager.LoadScene(SceneNames.Loading);
    }

    /// <summary>
    /// 아이디 입력창 값을 서버 요청에 사용할 문자열로 정리합니다.
    /// </summary>
    private string GetLoginId()
    {
        return loginIdInput == null ? string.Empty : loginIdInput.text.Trim();
    }

    /// <summary>
    /// 비밀번호 입력창 값을 서버 요청에 사용할 문자열로 정리합니다.
    /// </summary>
    private string GetPassword()
    {
        return passwordInput == null ? string.Empty : passwordInput.text;
    }

    /// <summary>
    /// 서버로 보내기 전에 빈 입력인지 먼저 검사해 불필요한 요청을 줄입니다.
    /// 실제 중복 아이디와 비밀번호 검증은 반드시 서버가 최종 판단합니다.
    /// </summary>
    private bool ValidateCredentialInput(string loginId, string password)
    {
        if (string.IsNullOrWhiteSpace(loginId) || string.IsNullOrWhiteSpace(password))
        {
            statusText.text = "아이디와 비밀번호를 모두 입력하세요.";
            return false;
        }

        return true;
    }
}
