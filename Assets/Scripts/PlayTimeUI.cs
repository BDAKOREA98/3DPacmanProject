using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 화면 가운데 상단에 현재 플레이한 시간(분:초)을 표시한다.
/// 씬이 시작(또는 재시작)된 뒤 흐른 게임 시간이라서, 게임이 멈추면(DEAD/CLEAR) 시간도 멈춘다.
/// Play 시작 시(그리고 씬을 다시 불러올 때마다) 자동으로 만들어지므로 씬에 따로 배치할 필요가 없다.
/// </summary>
public class PlayTimeUI : MonoBehaviour
{
    private const int FontSize = 40;
    private static readonly Vector2 Margin = new Vector2(0f, -30f);

    private Text _label;
    private int _shownSeconds = -1;

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
        if (FindFirstObjectByType<PlayTimeUI>() != null)
        {
            return;
        }

        new GameObject(nameof(PlayTimeUI)).AddComponent<PlayTimeUI>();
    }

    private void Awake()
    {
        BuildUI();
        Refresh(0);
    }

    private void Update()
    {
        // 게임 시간(Time.timeScale 반영): 씬을 불러온 뒤 흐른 시간
        int seconds = Mathf.FloorToInt(Time.timeSinceLevelLoad);

        // 표시가 바뀔 때(초가 넘어갈 때)만 글자를 갱신
        if (seconds != _shownSeconds)
        {
            Refresh(seconds);
        }
    }

    private void Refresh(int totalSeconds)
    {
        _shownSeconds = totalSeconds;

        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        _label.text = $"Time: {minutes:00}:{seconds:00}";
    }

    private void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject labelObject = new GameObject("PlayTimeText");
        labelObject.transform.SetParent(transform, false);

        _label = labelObject.AddComponent<Text>();
        _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _label.fontSize = FontSize;
        _label.fontStyle = FontStyle.Bold;
        _label.alignment = TextAnchor.UpperCenter;
        _label.color = Color.white;
        _label.raycastTarget = false;
        _label.horizontalOverflow = HorizontalWrapMode.Overflow;
        _label.verticalOverflow = VerticalWrapMode.Overflow;

        // 글자가 밝은 바닥/벽 위에서도 읽히도록 외곽선 추가
        Outline outline = labelObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);

        // 가운데 상단 앵커
        RectTransform rect = _label.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Margin;
        rect.sizeDelta = new Vector2(500f, 80f);
    }
}
