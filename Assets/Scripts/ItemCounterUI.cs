using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 우측 상단에 남은 아이템 수를 표시한다.
/// Play 시작 시 Canvas와 Text를 코드로 만들기 때문에 씬에 따로 배치하거나 연결할 필요가 없다.
/// </summary>
public class ItemCounterUI : MonoBehaviour
{
    private const int FontSize = 40;
    private static readonly Vector2 Margin = new Vector2(-30f, -30f);

    private Text _label;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (FindFirstObjectByType<ItemCounterUI>() != null)
        {
            return;
        }

        new GameObject(nameof(ItemCounterUI)).AddComponent<ItemCounterUI>();
    }

    private void Awake()
    {
        BuildUI();
    }

    private void OnEnable()
    {
        Item.RemainingChanged += Refresh;
        Refresh(Item.Remaining);
    }

    private void OnDisable()
    {
        Item.RemainingChanged -= Refresh;
    }

    private void Refresh(int remaining)
    {
        if (_label != null)
        {
            _label.text = $"Items: {remaining}";
        }
    }

    private void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject labelObject = new GameObject("RemainingItemsText");
        labelObject.transform.SetParent(transform, false);

        _label = labelObject.AddComponent<Text>();
        _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _label.fontSize = FontSize;
        _label.fontStyle = FontStyle.Bold;
        _label.alignment = TextAnchor.UpperRight;
        _label.color = Color.white;
        _label.raycastTarget = false;
        _label.horizontalOverflow = HorizontalWrapMode.Overflow;
        _label.verticalOverflow = VerticalWrapMode.Overflow;

        // 글자가 밝은 바닥/벽 위에서도 읽히도록 외곽선 추가
        Outline outline = labelObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);

        // 우측 상단 앵커
        RectTransform rect = _label.rectTransform;
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;
        rect.anchoredPosition = Margin;
        rect.sizeDelta = new Vector2(500f, 80f);
    }
}
