using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LoadingSceneController : MonoBehaviour
{
    [SerializeField] private float minimumLoadingTime = 0.8f;

    private Slider progressSlider;
    private Text statusText;

    private void Start()
    {
        BuildUi();
        StartCoroutine(LoadNextSceneRoutine());
    }

    // 로딩 씬의 실제 Unity UI를 생성합니다.
    private void BuildUi()
    {
        Canvas canvas = RuntimeUiFactory.CreateCanvas("LoadingCanvas");
        RuntimeUiFactory.CreatePanel(canvas.transform, "Background", new Color(0.03f, 0.05f, 0.07f, 1.0f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        RuntimeUiFactory.CreateText(canvas.transform, "Title", "Loading", 48, TextAnchor.MiddleCenter, Color.white, new Vector2(0.0f, 0.54f), new Vector2(1.0f, 0.64f), Vector2.zero, Vector2.zero);
        statusText = RuntimeUiFactory.CreateText(canvas.transform, "Status", "다음 씬을 준비하고 있습니다.", 24, TextAnchor.MiddleCenter, new Color(0.75f, 0.85f, 0.92f, 1.0f), new Vector2(0.0f, 0.45f), new Vector2(1.0f, 0.52f), Vector2.zero, Vector2.zero);

        RectTransform barRoot = RuntimeUiFactory.CreatePanel(canvas.transform, "ProgressRoot", new Color(0.12f, 0.16f, 0.2f, 1.0f), new Vector2(0.32f, 0.38f), new Vector2(0.68f, 0.42f), Vector2.zero, Vector2.zero);
        GameObject sliderObject = new GameObject("ProgressSlider", typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(barRoot, false);
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.offsetMin = Vector2.zero;
        sliderRect.offsetMax = Vector2.zero;

        progressSlider = sliderObject.GetComponent<Slider>();
        progressSlider.minValue = 0.0f;
        progressSlider.maxValue = 1.0f;
        progressSlider.value = 0.0f;
    }

    // 최소 로딩 시간을 보장하면서 다음 씬을 비동기로 로드합니다.
    private IEnumerator LoadNextSceneRoutine()
    {
        string nextScene = SceneFlow.NextSceneName;
        statusText.text = $"{nextScene} 로딩 중...";

        AsyncOperation operation = SceneManager.LoadSceneAsync(nextScene);
        operation.allowSceneActivation = false;

        float elapsed = 0.0f;
        while (operation.progress < 0.9f || elapsed < minimumLoadingTime)
        {
            elapsed += Time.deltaTime;
            float timeProgress = Mathf.Clamp01(elapsed / minimumLoadingTime);
            float loadProgress = Mathf.Clamp01(operation.progress / 0.9f);
            progressSlider.value = Mathf.Min(timeProgress, loadProgress);
            yield return null;
        }

        progressSlider.value = 1.0f;
        operation.allowSceneActivation = true;
    }
}
