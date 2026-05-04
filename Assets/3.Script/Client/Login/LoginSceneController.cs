using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LoginSceneController : MonoBehaviour
{
    [Header("Scene UI")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private InputField loginIdInput;
    [SerializeField] private InputField passwordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerButton;
    [SerializeField] private Text statusText;

    [Header("Register Dialog")]
    [SerializeField] private GameObject registerDialog;
    [SerializeField] private InputField registerIdInput;
    [SerializeField] private InputField registerPasswordInput;
    [SerializeField] private Button registerCreateButton;
    [SerializeField] private Button registerCancelButton;

    private bool receivedLogin;
    private bool receivedCharacters;
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
        NetworkManager.Instance.Connect();
        BindSceneUiIfNeeded();
        if (!IsUiAlive())
        {
            BuildUi();
        }

        WireUiEvents();
    }

    /// <summary>
    /// 로그인 씬의 실제 Unity UI를 생성합니다.
    /// 아이디와 비밀번호를 입력한 뒤 로그인 또는 회원가입 요청을 보낼 수 있습니다.
    /// </summary>
    private void BuildUi()
    {
        canvas = RuntimeUiFactory.CreateCanvas("LoginCanvas");
        RuntimeUiFactory.CreatePanel(canvas.transform, "Background", new Color(0.04f, 0.07f, 0.09f, 1.0f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        RectTransform panel = RuntimeUiFactory.CreatePanel(canvas.transform, "LoginPanel", new Color(0.08f, 0.12f, 0.16f, 0.94f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420.0f, -290.0f), new Vector2(420.0f, 290.0f));
        RuntimeUiFactory.CreateText(panel, "Title", "3D Quarter-View MORPG", 42, TextAnchor.MiddleCenter, Color.white, new Vector2(0.0f, 0.78f), new Vector2(1.0f, 0.95f), Vector2.zero, Vector2.zero);
        RuntimeUiFactory.CreateText(panel, "Subtitle", "Login / Register", 25, TextAnchor.MiddleCenter, new Color(0.75f, 0.85f, 0.92f, 1.0f), new Vector2(0.0f, 0.68f), new Vector2(1.0f, 0.78f), Vector2.zero, Vector2.zero);

        loginIdInput = RuntimeUiFactory.CreateInputField(panel, "LoginIdInput", "아이디", false, new Vector2(0.12f, 0.54f), new Vector2(0.88f, 0.64f), Vector2.zero, Vector2.zero);
        passwordInput = RuntimeUiFactory.CreateInputField(panel, "PasswordInput", "비밀번호", true, new Vector2(0.12f, 0.40f), new Vector2(0.88f, 0.50f), Vector2.zero, Vector2.zero);
        statusText = RuntimeUiFactory.CreateText(panel, "Status", "아이디와 비밀번호를 입력하세요. 테스트 계정: test_user / 1234", 21, TextAnchor.MiddleCenter, new Color(0.88f, 0.92f, 0.95f, 1.0f), new Vector2(0.08f, 0.24f), new Vector2(0.92f, 0.36f), Vector2.zero, Vector2.zero);

        loginButton = RuntimeUiFactory.CreateButton(panel, "LoginButton", "로그인", new Vector2(0.12f, 0.10f), new Vector2(0.48f, 0.21f), Vector2.zero, Vector2.zero);

        registerButton = RuntimeUiFactory.CreateButton(panel, "RegisterButton", "회원가입", new Vector2(0.52f, 0.10f), new Vector2(0.88f, 0.21f), Vector2.zero, Vector2.zero);
    }

    // 씬에 미리 배치된 UI 오브젝트가 있으면 이름 기준으로 찾아 연결합니다.
    private void BindSceneUiIfNeeded()
    {
        canvas = canvas != null ? canvas : GameObject.Find("LoginCanvas")?.GetComponent<Canvas>();
        loginIdInput = loginIdInput != null ? loginIdInput : GameObject.Find("LoginIdInput")?.GetComponent<InputField>();
        passwordInput = passwordInput != null ? passwordInput : GameObject.Find("PasswordInput")?.GetComponent<InputField>();
        loginButton = loginButton != null ? loginButton : GameObject.Find("LoginButton")?.GetComponent<Button>();
        registerButton = registerButton != null ? registerButton : GameObject.Find("RegisterButton")?.GetComponent<Button>();
        statusText = statusText != null ? statusText : GameObject.Find("Status")?.GetComponent<Text>();
        registerDialog = registerDialog != null ? registerDialog : GameObject.Find("RegisterDialog");
        registerIdInput = registerIdInput != null ? registerIdInput : GameObject.Find("RegisterIdInput")?.GetComponent<InputField>();
        registerPasswordInput = registerPasswordInput != null ? registerPasswordInput : GameObject.Find("RegisterPasswordInput")?.GetComponent<InputField>();
        registerCreateButton = registerCreateButton != null ? registerCreateButton : GameObject.Find("CreateAccountButton")?.GetComponent<Button>();
        registerCancelButton = registerCancelButton != null ? registerCancelButton : GameObject.Find("CancelButton")?.GetComponent<Button>();

        if (registerDialog != null)
        {
            registerDialog.SetActive(false);
        }
    }

    // 런타임 생성 UI와 씬 배치 UI 모두 같은 버튼 이벤트를 사용하도록 연결합니다.
    private void WireUiEvents()
    {
        if (loginButton != null)
        {
            loginButton.onClick.RemoveListener(RequestLogin);
            loginButton.onClick.AddListener(RequestLogin);
        }

        if (registerButton != null)
        {
            registerButton.onClick.RemoveListener(ShowRegisterDialog);
            registerButton.onClick.AddListener(ShowRegisterDialog);
        }

        if (registerCreateButton != null)
        {
            registerCreateButton.onClick.RemoveListener(RequestRegister);
            registerCreateButton.onClick.AddListener(RequestRegister);
        }

        if (registerCancelButton != null)
        {
            registerCancelButton.onClick.RemoveListener(HideRegisterDialog);
            registerCancelButton.onClick.AddListener(HideRegisterDialog);
        }
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
        SetStatus("로그인 요청 중...");
        NetworkManager.Instance.SendPacket(new LoginRequestPacket(loginId, password));
    }

    /// <summary>
    /// 입력한 아이디와 비밀번호로 회원가입 요청 패킷을 보냅니다.
    /// 서버는 같은 아이디가 이미 있으면 실패 응답을 내려줍니다.
    /// </summary>
    private void RequestRegister()
    {
        string loginId = registerIdInput == null ? string.Empty : registerIdInput.text.Trim();
        string password = registerPasswordInput == null ? string.Empty : registerPasswordInput.text;
        if (!ValidateCredentialInput(loginId, password))
        {
            return;
        }

        receivedLogin = false;
        receivedCharacters = false;
        SetStatus("회원가입 요청 중...");
        NetworkManager.Instance.SendPacket(new RegisterRequestPacket(loginId, password));
    }

    /// <summary>
    /// 로그인 응답을 받아 계정 정보를 저장합니다.
    /// 성공 후 캐릭터 목록까지 받으면 캐릭터 선택 씬으로 이동합니다.
    /// </summary>
    private void HandleLoginResponse(LoginResponsePacket packet)
    {
        if (!IsUiAlive())
        {
            return;
        }

        if (!packet.Success)
        {
            SetStatus(GetFriendlyMessage(packet.Message, "아이디 또는 비밀번호가 틀렸습니다."));
            return;
        }

        CharacterSession.Instance.SetAccount(packet.AccountId);
        receivedLogin = true;
        SetStatus("로그인 성공. 캐릭터 목록을 받는 중...");
        TryGoCharacterSelect();
    }

    /// <summary>
    /// 회원가입 응답을 받아 새 계정 정보를 저장합니다.
    /// 회원가입 직후에도 캐릭터 선택 화면으로 이동해 빈 슬롯 3칸을 볼 수 있게 합니다.
    /// </summary>
    private void HandleRegisterResponse(RegisterResponsePacket packet)
    {
        if (!IsUiAlive())
        {
            return;
        }

        if (!packet.Success)
        {
            SetStatus(GetFriendlyMessage(packet.Message, "이미 존재하는 아이디이거나 사용할 수 없는 계정입니다."));
            return;
        }

        HideRegisterDialog();
        CharacterSession.Instance.SetAccount(packet.AccountId);
        receivedLogin = true;
        SetStatus("회원가입 성공. 캐릭터 슬롯을 준비하는 중...");
        TryGoCharacterSelect();
    }

    /// <summary>
    /// 캐릭터 목록을 세션에 저장합니다.
    /// 새 계정은 비어 있는 목록을 받아 캐릭터 선택 씬에서 3개의 빈 슬롯으로 표시됩니다.
    /// </summary>
    private void HandleCharacterList(CharacterListPacket packet)
    {
        if (!IsUiAlive())
        {
            return;
        }

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
            SetStatus("아이디와 비밀번호를 모두 입력하세요.");
            return false;
        }

        return true;
    }

    /// 회원가입은 로그인 입력창을 재사용하지 않고 별도 팝업에서 계정 생성을 요청합니다.
    private void ShowRegisterDialog()
    {
        if (registerDialog != null)
        {
            registerDialog.SetActive(true);
            return;
        }

        RectTransform dimmed = RuntimeUiFactory.CreatePanel(canvas.transform, "RegisterDialog", new Color(0.0f, 0.0f, 0.0f, 0.68f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        registerDialog = dimmed.gameObject;

        RectTransform panel = RuntimeUiFactory.CreatePanel(dimmed, "RegisterPanel", new Color(0.08f, 0.12f, 0.16f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-360.0f, -260.0f), new Vector2(360.0f, 260.0f));
        RuntimeUiFactory.CreateText(panel, "Title", "회원가입", 38, TextAnchor.MiddleCenter, Color.white, new Vector2(0.0f, 0.78f), new Vector2(1.0f, 0.94f), Vector2.zero, Vector2.zero);
        RuntimeUiFactory.CreateText(panel, "Info", "새 계정에 사용할 아이디와 비밀번호를 입력하세요.", 20, TextAnchor.MiddleCenter, new Color(0.75f, 0.85f, 0.92f, 1.0f), new Vector2(0.08f, 0.66f), new Vector2(0.92f, 0.76f), Vector2.zero, Vector2.zero);

        registerIdInput = RuntimeUiFactory.CreateInputField(panel, "RegisterIdInput", "아이디", false, new Vector2(0.12f, 0.50f), new Vector2(0.88f, 0.61f), Vector2.zero, Vector2.zero);
        registerPasswordInput = RuntimeUiFactory.CreateInputField(panel, "RegisterPasswordInput", "비밀번호", true, new Vector2(0.12f, 0.35f), new Vector2(0.88f, 0.46f), Vector2.zero, Vector2.zero);

        registerCreateButton = RuntimeUiFactory.CreateButton(panel, "CreateAccountButton", "생성", new Vector2(0.12f, 0.13f), new Vector2(0.48f, 0.25f), Vector2.zero, Vector2.zero);
        registerCreateButton.onClick.AddListener(RequestRegister);

        registerCancelButton = RuntimeUiFactory.CreateButton(panel, "CancelButton", "취소", new Vector2(0.52f, 0.13f), new Vector2(0.88f, 0.25f), Vector2.zero, Vector2.zero);
        registerCancelButton.onClick.AddListener(HideRegisterDialog);
    }

    /// 회원가입 팝업을 닫고 임시 입력 참조를 정리합니다.
    private void HideRegisterDialog()
    {
        if (registerDialog != null)
        {
            registerDialog.SetActive(false);
        }
    }

    /// 서버 내부 메시지를 일반 사용자가 이해할 수 있는 문장으로 바꿔 UI에 표시합니다.
    private string GetFriendlyMessage(string serverMessage, string fallback)
    {
        switch (serverMessage)
        {
            case "InvalidAccount":
                return "아이디 또는 비밀번호가 틀렸습니다.";
            case "DuplicatedOrInvalidAccount":
                return "이미 존재하는 아이디이거나 사용할 수 없는 계정입니다.";
            case "LoginFailed":
                return "로그인에 실패했습니다.";
            case "RegisterFailed":
                return "회원가입에 실패했습니다.";
            default:
                return fallback;
        }
    }

    /// NetworkManager가 씬보다 오래 살아있으므로 같은 Dispatcher 참조로 이벤트를 정리합니다.
    private void SubscribeDispatcher()
    {
        if (subscribedDispatcher != null)
        {
            return;
        }

        subscribedDispatcher = NetworkManager.Instance.Dispatcher;
        subscribedDispatcher.LoginResponseReceived += HandleLoginResponse;
        subscribedDispatcher.RegisterResponseReceived += HandleRegisterResponse;
        subscribedDispatcher.CharacterListReceived += HandleCharacterList;
    }

    /// 씬 전환 후 파괴된 로그인 UI가 늦은 서버 응답을 받지 않도록 구독을 해제합니다.
    private void UnsubscribeDispatcher()
    {
        if (subscribedDispatcher == null)
        {
            return;
        }

        subscribedDispatcher.LoginResponseReceived -= HandleLoginResponse;
        subscribedDispatcher.RegisterResponseReceived -= HandleRegisterResponse;
        subscribedDispatcher.CharacterListReceived -= HandleCharacterList;
        subscribedDispatcher = null;
    }

    /// 로그인 Canvas와 상태 Text가 아직 살아있는지 확인합니다.
    private bool IsUiAlive()
    {
        return this != null && canvas != null && statusText != null;
    }

    /// 상태 문구 변경 전에 Text가 파괴됐는지 확인합니다.
    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}
