using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 화면 좌측 상단에 현재 Score를 표시한다.
/// Play 시작 시(그리고 씬을 다시 불러올 때마다) Canvas와 Text를 코드로 만들기 때문에
/// 씬에 따로 배치하거나 연결할 필요가 없다.
/// </summary>
public class ScoreUI : MonoBehaviour
{
    private const int FontSize = 40;
    private static readonly Vector2 Margin = new Vector2(30f, -30f);

    private Text _label;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        // 씬을 다시 불러와도(재시작) 새 씬에 다시 만들어지도록 등록
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Create();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Create();
    }

    private static void Create()
    {
        if (FindFirstObjectByType<ScoreUI>() != null)
        {
            return;
        }

        new GameObject(nameof(ScoreUI)).AddComponent<ScoreUI>();
    }

    private void Awake()
    {
        BuildUI();
    }

    private void OnEnable()
    {
        Item.Collected += OnItemCollected;
        Refresh();
    }

    private void OnDisable()
    {
        Item.Collected -= OnItemCollected;
    }

    private void OnItemCollected(int value)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (_label != null)
        {
            _label.text = $"Score: {Item.Score}";
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

        GameObject labelObject = new GameObject("ScoreText");
        labelObject.transform.SetParent(transform, false);

        _label = labelObject.AddComponent<Text>();
        _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _label.fontSize = FontSize;
        _label.fontStyle = FontStyle.Bold;
        _label.alignment = TextAnchor.UpperLeft;
        _label.color = Color.white;
        _label.raycastTarget = false;
        _label.horizontalOverflow = HorizontalWrapMode.Overflow;
        _label.verticalOverflow = VerticalWrapMode.Overflow;

        // 글자가 밝은 바닥/벽 위에서도 읽히도록 외곽선 추가
        Outline outline = labelObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);

        // 좌측 상단 앵커
        RectTransform rect = _label.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = Margin;
        rect.sizeDelta = new Vector2(500f, 80f);
    }
}
