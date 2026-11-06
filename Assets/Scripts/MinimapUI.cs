using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 우측 상단(남은 아이템 수 아래)에 미로 전체가 보이는 미니맵을 표시한다.
/// 이름에 "Minimap"이 들어간 카메라(없으면 새로 만든다)를 위에서 내려다보는 직교 카메라로 설정해
/// RenderTexture에 그리고, 그 결과를 UI에 보여 준다.
/// 플레이어(노란 원)와 적(각자의 색 원)은 미니맵에만 보이는 표시로 나타낸다.
/// Play 시작 시(그리고 씬을 다시 불러올 때마다) 자동으로 만들어지므로 씬에 따로 배치할 필요가 없다.
/// </summary>
public class MinimapUI : MonoBehaviour
{
    private const int TextureSize = 512;
    private const float UiSize = 300f;
    private const float BorderSize = 4f;
    private static readonly Vector2 Margin = new Vector2(-30f, -110f);   // 남은 아이템 수 글자 아래

    private const int MarkerLayer = 31;        // 미니맵 전용 표시가 쓰는 레이어 (이름을 만들 필요 없음)
    private const float CameraHeight = 60f;
    private const float MarkerHeight = 15f;
    private const float MapMargin = 1.05f;     // 미로 가장자리 여유
    private static readonly Color PlayerMarkerColor = new Color(1f, 0.92f, 0.1f);

    private Camera _camera;
    private RenderTexture _renderTexture;
    private Vector3 _mapCenter;

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
        if (FindFirstObjectByType<MinimapUI>() != null)
        {
            return;
        }

        new GameObject(nameof(MinimapUI)).AddComponent<MinimapUI>();
    }

    private void Awake()
    {
        _renderTexture = new RenderTexture(TextureSize, TextureSize, 16) { name = "MinimapRenderTexture" };
        BuildUI();
    }

    private void Start()
    {
        MazeBuilder maze = FindFirstObjectByType<MazeBuilder>();
        PlayerMove player = FindFirstObjectByType<PlayerMove>();

        if (maze == null || player == null)
        {
            Debug.LogWarning($"{nameof(MinimapUI)}: MazeBuilder 또는 PlayerMove를 찾지 못해 미니맵을 만들지 않습니다.", this);
            enabled = false;
            return;
        }

        _mapCenter = maze.transform.position;

        SetUpCamera(maze);
        HideMarkersFromMainCamera();

        AddMarker(player.transform, PlayerMarkerColor, 6f);
        StartCoroutine(AddEnemyMarkers());
    }

    // 플레이어의 자식이라 함께 움직이거나 회전/스케일의 영향을 받지 않도록, 매 프레임 월드 기준 위치로 고정한다.
    private void LateUpdate()
    {
        if (_camera != null)
        {
            _camera.transform.SetPositionAndRotation(
                _mapCenter + Vector3.up * CameraHeight,
                Quaternion.Euler(90f, 0f, 0f));   // 위가 +Z(미로의 위쪽)가 되는 북쪽 고정 시점
        }
    }

    private void OnDestroy()
    {
        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
        }
    }

    // ---------------- 카메라 ----------------

    private void SetUpCamera(MazeBuilder maze)
    {
        _camera = FindMinimapCamera();

        if (_camera == null)
        {
            _camera = new GameObject("MinimapCamera").AddComponent<Camera>();
        }

        // 플레이어의 자식이면 플레이어의 회전/스케일(1,2,1)이 화면을 왜곡하므로 떼어 낸다.
        if (_camera.transform.parent != null)
        {
            _camera.transform.SetParent(null, true);
        }

        // 미로 전체가 들어오도록 직교 크기를 맞춘다.
        Vector3 scale = maze.transform.lossyScale;
        float worldWidth = MazeLayout.ColCount * maze.CellSize * Mathf.Abs(scale.x);
        float worldDepth = MazeLayout.RowCount * maze.CellSize * Mathf.Abs(scale.z);

        _camera.orthographic = true;
        _camera.orthographicSize = Mathf.Max(worldWidth, worldDepth) * 0.5f * MapMargin;
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = new Color(0.03f, 0.03f, 0.08f, 1f);
        _camera.cullingMask = ~0;
        _camera.nearClipPlane = 0.3f;
        _camera.farClipPlane = CameraHeight * 2f;
        _camera.depth = -10f;
        _camera.targetTexture = _renderTexture;
        _camera.enabled = true;

        // 오디오 리스너가 둘 이상 생기지 않게 한다.
        if (_camera.TryGetComponent(out AudioListener listener))
        {
            listener.enabled = false;
        }
    }

    private static Camera FindMinimapCamera()
    {
        foreach (Camera candidate in FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (candidate.name.Contains("Minimap"))
            {
                return candidate;
            }
        }

        return null;
    }

    // 미니맵 전용 표시(원)가 메인 화면에는 보이지 않게 한다.
    private static void HideMarkersFromMainCamera()
    {
        Camera main = Camera.main;
        if (main != null)
        {
            main.cullingMask &= ~(1 << MarkerLayer);
        }
    }

    // ---------------- 표시(마커) ----------------

    // 적은 EnemySpawner가 Start에서 만들므로 한 프레임 기다린 뒤 표시를 붙인다.
    private IEnumerator AddEnemyMarkers()
    {
        yield return null;

        foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            Color color = Color.red;
            if (enemy.TryGetComponent(out Renderer enemyRenderer))
            {
                color = enemyRenderer.material.color;
            }

            AddMarker(enemy.transform, color, 5f);
        }
    }

    private void AddMarker(Transform parent, Color color, float worldSize)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "MinimapMarker";
        marker.layer = MarkerLayer;
        Destroy(marker.GetComponent<Collider>());

        marker.transform.SetParent(parent, false);

        // 벽보다 위에 두고, 부모의 스케일과 무관하게 월드 기준 크기를 유지
        marker.transform.position = new Vector3(parent.position.x, _mapCenter.y + MarkerHeight, parent.position.z);
        Vector3 parentScale = parent.lossyScale;
        marker.transform.localScale = new Vector3(
            worldSize / parentScale.x, worldSize / parentScale.y, worldSize / parentScale.z);

        Renderer markerRenderer = marker.GetComponent<Renderer>();
        markerRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        markerRenderer.receiveShadows = false;

        Material material = markerRenderer.material;
        material.color = color;
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color);   // 조명과 상관없이 선명하게 보이도록
    }

    // ---------------- UI ----------------

    private void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // 테두리
        GameObject frameObject = new GameObject("MinimapFrame");
        frameObject.transform.SetParent(transform, false);
        Image frame = frameObject.AddComponent<Image>();
        frame.color = Color.white;
        frame.raycastTarget = false;

        RectTransform frameRect = frame.rectTransform;
        frameRect.anchorMin = Vector2.one;
        frameRect.anchorMax = Vector2.one;
        frameRect.pivot = Vector2.one;
        frameRect.anchoredPosition = Margin;
        frameRect.sizeDelta = new Vector2(UiSize + BorderSize * 2f, UiSize + BorderSize * 2f);

        // 미니맵 화면
        GameObject mapObject = new GameObject("MinimapImage");
        mapObject.transform.SetParent(frameObject.transform, false);
        RawImage map = mapObject.AddComponent<RawImage>();
        map.texture = _renderTexture;
        map.raycastTarget = false;

        RectTransform mapRect = map.rectTransform;
        mapRect.anchorMin = new Vector2(0.5f, 0.5f);
        mapRect.anchorMax = new Vector2(0.5f, 0.5f);
        mapRect.pivot = new Vector2(0.5f, 0.5f);
        mapRect.anchoredPosition = Vector2.zero;
        mapRect.sizeDelta = new Vector2(UiSize, UiSize);
    }
}
