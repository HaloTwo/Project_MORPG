using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class RuntimeUiFactory
{
    private static Font cachedFont;

    // 씬에 Canvas와 EventSystem을 준비합니다.
    public static Canvas CreateCanvas(string name)
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920.0f, 1080.0f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    // Unity UI 버튼 클릭을 받기 위해 EventSystem을 보장합니다.
    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

#if ENABLE_INPUT_SYSTEM
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
#else
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
#endif
    }

    // 부모 안에 RectTransform 기반 패널을 만듭니다.
    public static RectTransform CreatePanel(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);

        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Image image = panelObject.GetComponent<Image>();
        image.color = color;
        return rect;
    }

    // Unity 기본 Text 컴포넌트로 라벨을 만듭니다.
    public static Text CreateText(Transform parent, string name, string text, int fontSize, TextAnchor alignment, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Text label = textObject.GetComponent<Text>();
        label.text = text;
        label.font = GetDefaultFont();
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = color;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        return label;
    }

    // 클릭 가능한 버튼과 내부 텍스트를 함께 만듭니다.
    public static Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.12f, 0.18f, 0.24f, 0.96f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.18f, 0.28f, 0.36f, 1.0f);
        colors.pressedColor = new Color(0.08f, 0.12f, 0.18f, 1.0f);
        button.colors = colors;

        CreateText(buttonObject.transform, "Label", label, 28, TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return button;
    }

    /// <summary>
    /// 로그인과 회원가입에 사용할 런타임 InputField를 생성합니다.
    /// 사용자가 입력한 값은 클라이언트가 서버 요청 패킷을 만들 때만 사용합니다.
    /// </summary>
    public static InputField CreateInputField(Transform parent, string name, string placeholder, bool password, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject inputObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
        inputObject.transform.SetParent(parent, false);

        RectTransform rect = inputObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Image image = inputObject.GetComponent<Image>();
        image.color = new Color(0.04f, 0.07f, 0.09f, 0.96f);

        Text text = CreateText(inputObject.transform, "Text", string.Empty, 24, TextAnchor.MiddleLeft, Color.white, Vector2.zero, Vector2.one, new Vector2(18.0f, 0.0f), new Vector2(-18.0f, 0.0f));
        Text placeholderText = CreateText(inputObject.transform, "Placeholder", placeholder, 24, TextAnchor.MiddleLeft, new Color(0.55f, 0.62f, 0.68f, 1.0f), Vector2.zero, Vector2.one, new Vector2(18.0f, 0.0f), new Vector2(-18.0f, 0.0f));

        InputField inputField = inputObject.GetComponent<InputField>();
        inputField.textComponent = text;
        inputField.placeholder = placeholderText;
        inputField.contentType = password ? InputField.ContentType.Password : InputField.ContentType.Standard;
        inputField.lineType = InputField.LineType.SingleLine;
        inputField.caretColor = Color.white;
        inputField.selectionColor = new Color(0.2f, 0.45f, 0.7f, 0.55f);
        return inputField;
    }

    /// 서버 연결이 실패하거나 끊겼을 때 보여줄 종료 확인 팝업을 만듭니다.
    public static void ShowServerDisconnectedDialog(UnityEngine.Events.UnityAction onConfirm)
    {
        Canvas canvas = CreateCanvas("ServerDisconnectedCanvas");
        canvas.sortingOrder = 1000;

        CreatePanel(canvas.transform, "DimmedBackground", new Color(0.0f, 0.0f, 0.0f, 0.72f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        RectTransform panel = CreatePanel(canvas.transform, "DialogPanel", new Color(0.08f, 0.11f, 0.14f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-360.0f, -170.0f), new Vector2(360.0f, 170.0f));

        CreateText(panel, "Title", "서버가 끊겼습니다.", 34, TextAnchor.MiddleCenter, Color.white, new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.86f), Vector2.zero, Vector2.zero);
        CreateText(panel, "Message", "확인을 누르면 게임을 종료합니다.", 22, TextAnchor.MiddleCenter, new Color(0.78f, 0.86f, 0.92f, 1.0f), new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.56f), Vector2.zero, Vector2.zero);

        Button confirmButton = CreateButton(panel, "ConfirmButton", "확인", new Vector2(0.34f, 0.12f), new Vector2(0.66f, 0.28f), Vector2.zero, Vector2.zero);
        confirmButton.onClick.AddListener(onConfirm);
    }

    /// 빌드에서는 애플리케이션을 종료하고, 에디터에서는 플레이 모드를 중지합니다.
    public static void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// 기본 UI 폰트를 가져옵니다.
    private static Font GetDefaultFont()
    {
        if (cachedFont == null)
        {
            cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        return cachedFont;
    }
}

