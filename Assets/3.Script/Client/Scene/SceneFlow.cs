public static class SceneFlow
{
    public static string NextSceneName { get; private set; } = SceneNames.Login;

    // 로딩 씬이 끝난 뒤 이동할 다음 씬 이름을 저장합니다.
    public static void SetNextScene(string sceneName)
    {
        NextSceneName = sceneName;
    }
}
