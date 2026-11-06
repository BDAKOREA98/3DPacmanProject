using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 아이템을 모두 먹으면 화면 가운데에 CLEAR 문구를 띄우고 게임을 멈춘다.
/// Play 시작 시 자동으로 생성되므로 씬에 따로 배치하거나 연결할 필요가 없다.
/// </summary>
public class GameClear : MonoBehaviour
{
    private bool _collectedAny;
    private bool _cleared;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        // 이전 Play에서 멈춘 채 끝났더라도 항상 정상 속도로 시작
        Time.timeScale = 1f;

        if (FindFirstObjectByType<GameClear>() != null)
        {
            return;
        }

        new GameObject(nameof(GameClear)).AddComponent<GameClear>();
    }

    private void OnEnable()
    {
        Item.Collected += OnItemCollected;
        Item.RemainingChanged += OnRemainingChanged;
    }

    private void OnDisable()
    {
        Item.Collected -= OnItemCollected;
        Item.RemainingChanged -= OnRemainingChanged;
    }

    private void OnItemCollected(int value)
    {
        _collectedAny = true;
    }

    private void OnRemainingChanged(int remaining)
    {
        // 실제로 아이템을 먹어서 0이 된 경우에만 클리어 (시작 시 아직 아이템이 없는 상태는 제외)
        if (_cleared || !_collectedAny || remaining > 0)
        {
            return;
        }

        _cleared = true;
        ShowClear();

        // 게임 종료: 플레이어, 적 등 모든 물리/이동을 멈춘다.
        Time.timeScale = 0f;
    }

    private void OnDestroy()
    {
        // 씬 전환 등으로 사라질 때 멈춘 상태가 남지 않도록 복구
        if (_cleared)
        {
            Time.timeScale = 1f;
        }
    }

    private void ShowClear()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // 화면 전체를 살짝 어둡게
        GameObject dim = new GameObject("Dim");
        dim.transform.SetParent(transform, false);
        Image dimImage = dim.AddComponent<Image>();
        dimImage.color = new Color(0f, 0f, 0f, 0.55f);
        dimImage.raycastTarget = false;
        Stretch(dimImage.rectTransform);

        // 가운데 CLEAR 문구와 점수
        GameObject labelObject = new GameObject("ClearText");
        labelObject.transform.SetParent(transform, false);

        Text label = labelObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 160;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(1f, 0.92f, 0.4f);
        label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.text = $"CLEAR!\n<size=60><color=white>Score: {Item.Score}</color></size>";

        Outline outline = labelObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(4f, -4f);

        Stretch(label.rectTransform);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
