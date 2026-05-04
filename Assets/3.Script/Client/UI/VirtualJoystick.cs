using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif
using System.Collections.Generic;

public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, ICanvasRaycastFilter
{
    public static VirtualJoystick Instance { get; private set; }

    [SerializeField] private float radius = 90.0f;
    [SerializeField] private float cameraRotateSensitivity = 0.18f;
    [SerializeField] private float cameraPitchSensitivity = 0.08f;
    [SerializeField] private float pinchZoomSensitivity = 0.035f;
    [SerializeField] private int initialTrailPoolSize = 18;
    [SerializeField] private float trailLifeTime = 0.26f;
    [SerializeField] private Vector2 topUiSafeAreaMin = new Vector2(0.76f, 0.74f);

    private RectTransform root;
    private RectTransform visualRoot;
    private RectTransform handle;
    private CanvasGroup visualGroup;
    private QuarterViewCameraController cameraController;
    private Sprite circleSprite;
    private readonly List<TrailDot> trailPool = new List<TrailDot>();
    private Vector2 input;
    private bool isJoystickActive;
    private bool isCameraDragActive;
    private int activePointerId = int.MinValue;
    private float previousPinchDistance;

    public Vector2 InputVector => input;

    // 오른쪽 위 메뉴 버튼 같은 실제 UI 영역은 전체 화면 입력 레이어가 클릭을 빼앗지 않게 합니다.
    public bool IsRaycastLocationValid(Vector2 screenPosition, Camera eventCamera)
    {
        if (screenPosition.x >= Screen.width * topUiSafeAreaMin.x && screenPosition.y >= Screen.height * topUiSafeAreaMin.y)
        {
            return false;
        }

        return true;
    }

    private sealed class TrailDot
    {
        public RectTransform Rect;
        public CanvasGroup CanvasGroup;
        public float RemainingLifeTime;
    }

    private void Awake()
    {
        Instance = this;
        root = GetComponent<RectTransform>();
        BuildVisuals();
        WarmTrailPool();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        UpdateTrailDots();
        UpdatePinchZoom();
    }

    // 조이스틱 배경과 손잡이 이미지를 원형 이미지로 생성하고, 기본 상태에서는 숨깁니다.
    private void BuildVisuals()
    {
        circleSprite = CreateCircleSprite(128, Color.white);

        Image inputSurface = gameObject.GetComponent<Image>();
        if (inputSurface == null)
        {
            inputSurface = gameObject.AddComponent<Image>();
        }
        inputSurface.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);

        GameObject visualObject = new GameObject("VisualRoot", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        visualObject.transform.SetParent(transform, false);
        visualRoot = visualObject.GetComponent<RectTransform>();
        visualRoot.anchorMin = new Vector2(0.5f, 0.5f);
        visualRoot.anchorMax = new Vector2(0.5f, 0.5f);
        visualRoot.sizeDelta = new Vector2(radius * 2.0f, radius * 2.0f);
        visualRoot.anchoredPosition = Vector2.zero;

        visualGroup = visualObject.GetComponent<CanvasGroup>();
        visualGroup.alpha = 0.0f;
        visualGroup.blocksRaycasts = false;

        Image background = visualObject.GetComponent<Image>();
        background.sprite = circleSprite;
        background.color = new Color(1.0f, 1.0f, 1.0f, 0.22f);

        GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObject.transform.SetParent(visualRoot, false);
        handle = handleObject.GetComponent<RectTransform>();
        handle.anchorMin = new Vector2(0.5f, 0.5f);
        handle.anchorMax = new Vector2(0.5f, 0.5f);
        handle.sizeDelta = new Vector2(78.0f, 78.0f);
        handle.anchoredPosition = Vector2.zero;

        Image handleImage = handleObject.GetComponent<Image>();
        handleImage.sprite = circleSprite;
        handleImage.color = new Color(1.0f, 1.0f, 1.0f, 0.62f);
    }

    // 화면을 처음 누른 위치에 따라 이동 조이스틱 또는 카메라 드래그 모드로 진입합니다.
    public void OnPointerDown(PointerEventData eventData)
    {
        if (activePointerId != int.MinValue)
        {
            return;
        }

        activePointerId = eventData.pointerId;
        if (IsBottomLeftQuarter(eventData.position))
        {
            BeginJoystick(eventData);
            return;
        }

        BeginCameraDrag();
    }

    // 이동 영역에서는 조이스틱 입력을 갱신하고, 나머지 영역에서는 카메라를 좌우 회전시킵니다.
    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId)
        {
            return;
        }

        if (isJoystickActive)
        {
            UpdateInput(eventData);
            return;
        }

        if (isCameraDragActive)
        {
            RotateCamera(eventData.delta);
            SpawnTrailDot(eventData.position, eventData.pressEventCamera);
        }
    }

    // 손을 떼면 현재 입력 모드를 종료하고 조이스틱을 숨깁니다.
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId)
        {
            return;
        }

        input = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
        isJoystickActive = false;
        isCameraDragActive = false;
        activePointerId = int.MinValue;

        if (visualGroup != null)
        {
            visualGroup.alpha = 0.0f;
        }
    }

    // 왼쪽 아래 4분면에서 시작한 터치만 이동 입력으로 사용합니다.
    private bool IsBottomLeftQuarter(Vector2 screenPosition)
    {
        return screenPosition.x <= Screen.width * 0.5f && screenPosition.y <= Screen.height * 0.5f;
    }

    // 터치 시작 지점에 원형 조이스틱을 표시하고 그 위치를 기준점으로 삼습니다.
    private void BeginJoystick(PointerEventData eventData)
    {
        isJoystickActive = true;
        isCameraDragActive = false;
        previousPinchDistance = 0.0f;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(root, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            visualRoot.anchoredPosition = localPoint;
        }

        if (visualGroup != null)
        {
            visualGroup.alpha = 1.0f;
        }

        UpdateInput(eventData);
    }

    // 카메라 드래그는 조이스틱을 표시하지 않고 카메라 컨트롤러만 찾습니다.
    private void BeginCameraDrag()
    {
        isJoystickActive = false;
        isCameraDragActive = true;
        previousPinchDistance = 0.0f;

        if (cameraController == null && Camera.main != null)
        {
            cameraController = Camera.main.GetComponent<QuarterViewCameraController>();
        }
    }

    // 화면 좌표를 조이스틱 내부 좌표로 바꾸고 입력 벡터를 갱신합니다.
    private void UpdateInput(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(visualRoot, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            return;
        }

        Vector2 clamped = Vector2.ClampMagnitude(localPoint, radius);
        input = clamped / radius;
        handle.anchoredPosition = clamped;
    }

    // 화면 드래그량을 카메라 yaw/pitch 회전값으로 변환합니다.
    private void RotateCamera(Vector2 screenDelta)
    {
        if (cameraController == null)
        {
            return;
        }

        cameraController.AddOrbit(screenDelta.x * cameraRotateSensitivity, -screenDelta.y * cameraPitchSensitivity);
    }

    // 두 손가락 핀치 제스처로 플레이어 중심 줌 인/아웃을 처리합니다.
    private void UpdatePinchZoom()
    {
        if (isJoystickActive)
        {
            previousPinchDistance = 0.0f;
            return;
        }

        if (cameraController == null && Camera.main != null)
        {
            cameraController = Camera.main.GetComponent<QuarterViewCameraController>();
        }

        if (cameraController == null || !TryReadTwoFingerGesture(out float currentDistance, out Vector2 averageDelta))
        {
            previousPinchDistance = 0.0f;
            return;
        }

        if (previousPinchDistance > 0.0f)
        {
            float pinchDelta = currentDistance - previousPinchDistance;
            cameraController.AddZoom(-pinchDelta * pinchZoomSensitivity);
            cameraController.AddOrbit(averageDelta.x * cameraRotateSensitivity, -averageDelta.y * cameraPitchSensitivity);
        }

        previousPinchDistance = currentDistance;
    }

    // 현재 입력 시스템에서 두 손가락의 거리와 평균 이동량을 읽습니다.
    private bool TryReadTwoFingerGesture(out float distanceValue, out Vector2 averageDelta)
    {
        distanceValue = 0.0f;
        averageDelta = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null)
        {
            TouchControl first = Touchscreen.current.touches[0];
            TouchControl second = Touchscreen.current.touches[1];
            if (first.press.isPressed && second.press.isPressed)
            {
                Vector2 firstPosition = first.position.ReadValue();
                Vector2 secondPosition = second.position.ReadValue();
                distanceValue = Vector2.Distance(firstPosition, secondPosition);
                averageDelta = (first.delta.ReadValue() + second.delta.ReadValue()) * 0.5f;
                return true;
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.touchCount >= 2)
        {
            Touch first = Input.GetTouch(0);
            Touch second = Input.GetTouch(1);
            distanceValue = Vector2.Distance(first.position, second.position);
            averageDelta = (first.deltaPosition + second.deltaPosition) * 0.5f;
            return true;
        }
#endif

        return false;
    }

    // 드래그 잔상 오브젝트를 미리 만들어두고 필요할 때 재사용합니다.
    private void WarmTrailPool()
    {
        for (int i = 0; i < initialTrailPoolSize; i++)
        {
            TrailDot dot = CreateTrailDot();
            dot.Rect.gameObject.SetActive(false);
            trailPool.Add(dot);
        }
    }

    // 잔상 하나를 풀에서 꺼내 화면 좌표 위치에 배치합니다.
    private void SpawnTrailDot(Vector2 screenPosition, Camera eventCamera)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPosition, eventCamera, out Vector2 localPoint))
        {
            return;
        }

        TrailDot dot = GetTrailDot();
        dot.Rect.anchoredPosition = localPoint;
        dot.Rect.localScale = Vector3.one;
        dot.RemainingLifeTime = trailLifeTime;
        dot.CanvasGroup.alpha = 0.34f;
        dot.Rect.gameObject.SetActive(true);
    }

    // 활성 잔상의 투명도와 크기를 줄이다가 시간이 끝나면 다시 풀로 돌립니다.
    private void UpdateTrailDots()
    {
        for (int i = 0; i < trailPool.Count; i++)
        {
            TrailDot dot = trailPool[i];
            if (!dot.Rect.gameObject.activeSelf)
            {
                continue;
            }

            dot.RemainingLifeTime -= Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(dot.RemainingLifeTime / trailLifeTime);
            dot.CanvasGroup.alpha = normalized * 0.34f;
            dot.Rect.localScale = Vector3.one * Mathf.Lerp(0.55f, 1.0f, normalized);

            if (dot.RemainingLifeTime <= 0.0f)
            {
                dot.Rect.gameObject.SetActive(false);
            }
        }
    }

    // 풀에 남는 잔상이 없으면 하나만 추가 생성합니다.
    private TrailDot GetTrailDot()
    {
        for (int i = 0; i < trailPool.Count; i++)
        {
            if (!trailPool[i].Rect.gameObject.activeSelf)
            {
                return trailPool[i];
            }
        }

        TrailDot dot = CreateTrailDot();
        trailPool.Add(dot);
        return dot;
    }

    // 드래그 경로를 보여줄 작은 원형 UI 오브젝트를 생성합니다.
    private TrailDot CreateTrailDot()
    {
        GameObject dotObject = new GameObject("CameraDragTrail", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        dotObject.transform.SetParent(transform, false);

        RectTransform rect = dotObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(26.0f, 26.0f);

        Image image = dotObject.GetComponent<Image>();
        image.sprite = circleSprite;
        image.color = new Color(0.62f, 0.86f, 1.0f, 1.0f);

        return new TrailDot
        {
            Rect = rect,
            CanvasGroup = dotObject.GetComponent<CanvasGroup>(),
            RemainingLifeTime = 0.0f
        };
    }

    // 런타임 UI만으로 원형 조이스틱을 만들기 위한 간단한 원형 스프라이트 생성 함수입니다.
    private Sprite CreateCircleSprite(int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "RuntimeCircleJoystick";
        texture.wrapMode = TextureWrapMode.Clamp;

        float center = (size - 1) * 0.5f;
        float radiusPixels = size * 0.5f;
        Color transparent = new Color(color.r, color.g, color.b, 0.0f);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01(radiusPixels - distance);
                Color pixel = Color.Lerp(transparent, color, alpha);
                pixels[y * size + x] = pixel;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0.0f, 0.0f, size, size), new Vector2(0.5f, 0.5f), 100.0f);
    }
}
