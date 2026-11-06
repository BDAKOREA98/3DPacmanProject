using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 적에게 닿으면 화면에 DEAD를 표시하고, 잠시 뒤 현재 씬을 다시 불러와 게임을 처음부터 다시 시작한다.
/// Play 시작 시(그리고 씬을 다시 불러올 때마다) 자동으로 생성되므로 씬에 따로 배치할 필요가 없다.
/// 재시작을 하려면 현재 씬이 Build Settings의 Scenes In Build에 들어 있어야 한다.
/// </summary>
public class PlayerDeath : MonoBehaviour
{
    private const float RestartDelay = 2f;

    private bool _dead;

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
        if (FindFirstObjectByType<PlayerDeath>() != null)
        {
            return;
        }

        new GameObject(nameof(PlayerDeath)).AddComponent<PlayerDeath>();
    }

    private void OnEnable()
    {
        Enemy.PlayerCaught += OnPlayerCaught;
    }

    private void OnDisable()
    {
        Enemy.PlayerCaught -= OnPlayerCaught;
    }

    private void OnPlayerCaught()
    {
        // 여러 적이 동시에 닿아도 한 번만 처리
        if (_dead)
        {
            return;
        }

        _dead = true;

        // 게임 정지 (이후 대기 시간은 timeScale의 영향을 받지 않는 실제 시간으로 잰다)
        Time.timeScale = 0f;
        Debug.Log($"{nameof(PlayerDeath)}: 적에게 닿았습니다. {RestartDelay}초 뒤 다시 시작합니다.");

        ShowDead();
        StartCoroutine(RestartAfterDelay());
    }

    private IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSecondsRealtime(RestartDelay);

        Restart();
    }

    private static void Restart()
    {
        Item.ResetScore();
        Time.timeScale = 1f;

        int buildIndex = SceneManager.GetActiveScene().buildIndex;
        if (buildIndex < 0)
        {
            Debug.LogError($"{nameof(PlayerDeath)}: 현재 씬이 Build Settings에 없어 다시 시작할 수 없습니다. " +
                           "File > Build Profiles(Build Settings)의 Scene List에 현재 씬을 추가해 주세요.");
            return;
        }

        SceneManager.LoadScene(buildIndex);
    }

    private void ShowDead()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // 화면 전체를 어둡게
        GameObject dim = new GameObject("Dim");
        dim.transform.SetParent(transform, false);
        Image dimImage = dim.AddComponent<Image>();
        dimImage.color = new Color(0.25f, 0f, 0f, 0.6f);
        dimImage.raycastTarget = false;
        Stretch(dimImage.rectTransform);

        // 가운데 DEAD 문구
        GameObject labelObject = new GameObject("DeadText");
        labelObject.transform.SetParent(transform, false);

        Text label = labelObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 160;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(1f, 0.2f, 0.2f);
        label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.text = "DEAD\n<size=60><color=white>Restarting...</color></size>";

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
