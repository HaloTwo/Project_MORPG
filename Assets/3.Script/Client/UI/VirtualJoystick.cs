using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public static VirtualJoystick Instance { get; private set; }

    [SerializeField] private float radius = 90.0f;

    private RectTransform root;
    private RectTransform handle;
    private Vector2 input;

    public Vector2 InputVector => input;

    private void Awake()
    {
        Instance = this;
        root = GetComponent<RectTransform>();
        BuildVisuals();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // 조이스틱 배경과 손잡이 이미지를 생성합니다.
    private void BuildVisuals()
    {
        Image background = gameObject.GetComponent<Image>();
        if (background == null)
        {
            background = gameObject.AddComponent<Image>();
        }
        background.color = new Color(1.0f, 1.0f, 1.0f, 0.18f);

        GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObject.transform.SetParent(transform, false);
        handle = handleObject.GetComponent<RectTransform>();
        handle.anchorMin = new Vector2(0.5f, 0.5f);
        handle.anchorMax = new Vector2(0.5f, 0.5f);
        handle.sizeDelta = new Vector2(78.0f, 78.0f);
        handle.anchoredPosition = Vector2.zero;

        Image handleImage = handleObject.GetComponent<Image>();
        handleImage.color = new Color(1.0f, 1.0f, 1.0f, 0.55f);
    }

    // 조이스틱 영역을 누르면 입력을 시작합니다.
    public void OnPointerDown(PointerEventData eventData)
    {
        UpdateInput(eventData);
    }

    // 드래그 위치를 -1~1 이동 입력으로 변환합니다.
    public void OnDrag(PointerEventData eventData)
    {
        UpdateInput(eventData);
    }

    // 손을 떼면 조이스틱 입력을 0으로 되돌립니다.
    public void OnPointerUp(PointerEventData eventData)
    {
        input = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
    }

    // 화면 좌표를 조이스틱 내부 좌표로 바꾸고 입력 벡터를 갱신합니다.
    private void UpdateInput(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            return;
        }

        Vector2 clamped = Vector2.ClampMagnitude(localPoint, radius);
        input = clamped / radius;
        handle.anchoredPosition = clamped;
    }
}
