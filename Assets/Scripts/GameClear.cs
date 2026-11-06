using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 아이템을 모두 먹으면 화면 가운데에 CLEAR 문구를 띄우고 게임을 멈춘다.
/// Play 시작 시 자동으로 생성되므로 씬에 따로 배치하거나 연결할 필요가 없다.
/// </summary>
public class GameClear : MonoBehaviour
{
    private bool _sawItems;
    private bool _cleared;

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
        // 이전 Play에서 멈춘 채 끝났더라도 항상 정상 속도로 시작
        Time.timeScale = 1f;

        if (FindFirstObjectByType<GameClear>() != null)
        {
            return;
        }

        new GameObject(nameof(GameClear)).AddComponent<GameClear>();
    }

    private void Start()
    {
        Debug.Log($"{nameof(GameClear)}: 준비됨. 남은 아이템 {Item.Remaining}개");
    }

    // 이벤트 순서에 의존하지 않도록 매 프레임 남은 아이템 수를 직접 확인한다.
    private void Update()
    {
        if (_cleared)
        {
            return;
        }

        if (Item.Remaining > 0)
        {
            if (!_sawItems)
            {
                Debug.Log($"{nameof(GameClear)}: 아이템 {Item.Remaining}개 확인, 클리어 감시 시작");
            }

            _sawItems = true;
            return;
        }

        // 아이템이 한 번이라도 있었는데 모두 사라졌다면 클리어
        if (_sawItems)
        {
            Clear();
        }
    }

    private void Clear()
    {
        _cleared = true;

        // 게임 종료: 플레이어, 적 등 모든 물리/이동을 멈춘다. (UI 생성에 문제가 있어도 게임은 멈춘다)
        Time.timeScale = 0f;
        Debug.Log($"{nameof(GameClear)}: 모든 아이템을 먹었습니다. Score = {Item.Score}");

        ShowClear();
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
