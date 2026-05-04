using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class LoginSceneUiBuilder
{
    private const string LoginScenePath = "Assets/1.Scene/LoginScene.unity";

    [MenuItem("MORPG/Build Login Scene UI")]
    public static void BuildLoginSceneUi()
    {
        EditorSceneManager.OpenScene(LoginScenePath);
        RuntimeUiFactory.EnsureEventSystem();

        DeleteIfExists("LoginCanvas");

        LoginSceneController controller = Object.FindFirstObjectByType<LoginSceneController>();
        if (controller == null)
        {
            GameObject controllerObject = new GameObject("LoginSceneController");
            controller = controllerObject.AddComponent<LoginSceneController>();
        }

        Canvas canvas = RuntimeUiFactory.CreateCanvas("LoginCanvas");
        RuntimeUiFactory.CreatePanel(canvas.transform, "Background", new Color(0.04f, 0.07f, 0.09f, 1.0f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        RectTransform panel = RuntimeUiFactory.CreatePanel(canvas.transform, "LoginPanel", new Color(0.08f, 0.12f, 0.16f, 0.94f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420.0f, -290.0f), new Vector2(420.0f, 290.0f));
        RuntimeUiFactory.CreateText(panel, "Title", "3D Quarter-View MORPG", 42, TextAnchor.MiddleCenter, Color.white, new Vector2(0.0f, 0.78f), new Vector2(1.0f, 0.95f), Vector2.zero, Vector2.zero);
        RuntimeUiFactory.CreateText(panel, "Subtitle", "Login / Register", 25, TextAnchor.MiddleCenter, new Color(0.75f, 0.85f, 0.92f, 1.0f), new Vector2(0.0f, 0.68f), new Vector2(1.0f, 0.78f), Vector2.zero, Vector2.zero);

        InputField loginIdInput = RuntimeUiFactory.CreateInputField(panel, "LoginIdInput", "아이디", false, new Vector2(0.12f, 0.54f), new Vector2(0.88f, 0.64f), Vector2.zero, Vector2.zero);
        InputField passwordInput = RuntimeUiFactory.CreateInputField(panel, "PasswordInput", "비밀번호", true, new Vector2(0.12f, 0.40f), new Vector2(0.88f, 0.50f), Vector2.zero, Vector2.zero);
        Text statusText = RuntimeUiFactory.CreateText(panel, "Status", "아이디와 비밀번호를 입력하세요. 테스트 계정: test_user / 1234", 21, TextAnchor.MiddleCenter, new Color(0.88f, 0.92f, 0.95f, 1.0f), new Vector2(0.08f, 0.24f), new Vector2(0.92f, 0.36f), Vector2.zero, Vector2.zero);
        Button loginButton = RuntimeUiFactory.CreateButton(panel, "LoginButton", "로그인", new Vector2(0.12f, 0.10f), new Vector2(0.48f, 0.21f), Vector2.zero, Vector2.zero);
        Button registerButton = RuntimeUiFactory.CreateButton(panel, "RegisterButton", "회원가입", new Vector2(0.52f, 0.10f), new Vector2(0.88f, 0.21f), Vector2.zero, Vector2.zero);

        RectTransform dialog = RuntimeUiFactory.CreatePanel(canvas.transform, "RegisterDialog", new Color(0.0f, 0.0f, 0.0f, 0.68f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        RectTransform registerPanel = RuntimeUiFactory.CreatePanel(dialog, "RegisterPanel", new Color(0.08f, 0.12f, 0.16f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-360.0f, -260.0f), new Vector2(360.0f, 260.0f));
        RuntimeUiFactory.CreateText(registerPanel, "Title", "회원가입", 38, TextAnchor.MiddleCenter, Color.white, new Vector2(0.0f, 0.78f), new Vector2(1.0f, 0.94f), Vector2.zero, Vector2.zero);
        RuntimeUiFactory.CreateText(registerPanel, "Info", "새 계정에 사용할 아이디와 비밀번호를 입력하세요.", 20, TextAnchor.MiddleCenter, new Color(0.75f, 0.85f, 0.92f, 1.0f), new Vector2(0.08f, 0.66f), new Vector2(0.92f, 0.76f), Vector2.zero, Vector2.zero);
        InputField registerIdInput = RuntimeUiFactory.CreateInputField(registerPanel, "RegisterIdInput", "아이디", false, new Vector2(0.12f, 0.50f), new Vector2(0.88f, 0.61f), Vector2.zero, Vector2.zero);
        InputField registerPasswordInput = RuntimeUiFactory.CreateInputField(registerPanel, "RegisterPasswordInput", "비밀번호", true, new Vector2(0.12f, 0.35f), new Vector2(0.88f, 0.46f), Vector2.zero, Vector2.zero);
        Button createButton = RuntimeUiFactory.CreateButton(registerPanel, "CreateAccountButton", "생성", new Vector2(0.12f, 0.13f), new Vector2(0.48f, 0.25f), Vector2.zero, Vector2.zero);
        Button cancelButton = RuntimeUiFactory.CreateButton(registerPanel, "CancelButton", "취소", new Vector2(0.52f, 0.13f), new Vector2(0.88f, 0.25f), Vector2.zero, Vector2.zero);
        dialog.gameObject.SetActive(false);

        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("canvas").objectReferenceValue = canvas;
        serializedController.FindProperty("loginIdInput").objectReferenceValue = loginIdInput;
        serializedController.FindProperty("passwordInput").objectReferenceValue = passwordInput;
        serializedController.FindProperty("loginButton").objectReferenceValue = loginButton;
        serializedController.FindProperty("registerButton").objectReferenceValue = registerButton;
        serializedController.FindProperty("statusText").objectReferenceValue = statusText;
        serializedController.FindProperty("registerDialog").objectReferenceValue = dialog.gameObject;
        serializedController.FindProperty("registerIdInput").objectReferenceValue = registerIdInput;
        serializedController.FindProperty("registerPasswordInput").objectReferenceValue = registerPasswordInput;
        serializedController.FindProperty("registerCreateButton").objectReferenceValue = createButton;
        serializedController.FindProperty("registerCancelButton").objectReferenceValue = cancelButton;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[LoginSceneUiBuilder] LoginScene UI built as editable scene objects.");
    }

    private static void DeleteIfExists(string objectName)
    {
        GameObject existing = GameObject.Find(objectName);
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }
    }
}
