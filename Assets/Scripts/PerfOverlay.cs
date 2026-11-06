using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 렉의 원인을 찾기 위한 진단 화면. F3 키로 켜고 끈다. (기본은 꺼져 있고, 켜지 않으면 거의 비용이 없다)
/// FPS, 프레임 시간, CPU/GPU 시간, 렌더링 통계(Batches, SetPass, Triangles), 프레임당 GC 할당량을 보여 주고,
/// 프레임이 크게 튀면(100ms 이상) Console에 경고를 남겨 어떤 순간에 끊기는지 알 수 있게 한다.
/// Play 시작 시 자동으로 생성되므로 씬에 따로 배치할 필요가 없다.
/// </summary>
public class PerfOverlay : MonoBehaviour
{
    private const float SpikeThresholdMs = 100f;
    private const float WindowSeconds = 1f;

    private ProfilerRecorder _batches;
    private ProfilerRecorder _setPass;
    private ProfilerRecorder _triangles;
    private ProfilerRecorder _gcAllocInFrame;
    private ProfilerRecorder _mainThread;

    private readonly FrameTiming[] _timings = new FrameTiming[1];

    private bool _visible;
    private float _windowTime;
    private int _windowFrames;
    private float _worstMs;
    private float _shownFps;
    private float _shownAvgMs;
    private float _shownWorstMs;
    private long _gcBytesWindow;
    private long _shownGcBytes;
    private GUIStyle _style;
    private string _text = "";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (FindFirstObjectByType<PerfOverlay>() != null)
        {
            return;
        }

        GameObject host = new GameObject(nameof(PerfOverlay));
        DontDestroyOnLoad(host);
        host.AddComponent<PerfOverlay>();
    }

    private void OnEnable()
    {
        _batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
        _setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
        _triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
        _gcAllocInFrame = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        _mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
    }

    private void OnDisable()
    {
        _batches.Dispose();
        _setPass.Dispose();
        _triangles.Dispose();
        _gcAllocInFrame.Dispose();
        _mainThread.Dispose();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.f3Key.wasPressedThisFrame)
        {
            _visible = !_visible;
        }

        // 시간 배율(DEAD/CLEAR로 멈춘 상태)과 무관하게 실제 프레임 시간을 잰다.
        float frameMs = Time.unscaledDeltaTime * 1000f;

        _windowTime += Time.unscaledDeltaTime;
        _windowFrames++;
        _worstMs = Mathf.Max(_worstMs, frameMs);
        _gcBytesWindow += _gcAllocInFrame.Valid ? _gcAllocInFrame.LastValue : 0;

        if (frameMs >= SpikeThresholdMs)
        {
            Debug.LogWarning($"[Perf] 프레임 튐: {frameMs:0}ms (프레임 {Time.frameCount}, 게임 시간 {Time.timeSinceLevelLoad:0.0}초, " +
                             $"이 프레임 GC 할당 {(_gcAllocInFrame.Valid ? _gcAllocInFrame.LastValue : 0) / 1024}KB)");
        }

        if (_windowTime >= WindowSeconds)
        {
            _shownFps = _windowFrames / _windowTime;
            _shownAvgMs = _windowTime * 1000f / _windowFrames;
            _shownWorstMs = _worstMs;
            _shownGcBytes = _gcBytesWindow;

            _windowTime = 0f;
            _windowFrames = 0;
            _worstMs = 0f;
            _gcBytesWindow = 0;

            // 진단 화면 자체가 쓰레기(GC)를 만들지 않도록 글자는 1초에 한 번만 만들고, 화면에 보일 때만 만든다.
            if (_visible)
            {
                _text = BuildText();
            }
        }
    }

    private string BuildText()
    {
        // CPU/GPU 중 어느 쪽이 병목인지 확인 (Player Settings의 Frame Timing Stats가 켜져 있어야 값이 나온다)
        FrameTimingManager.CaptureFrameTimings();
        uint count = FrameTimingManager.GetLatestTimings(1, _timings);
        string cpuGpu = "CPU/GPU: (Frame Timing 사용 불가)";
        if (count > 0 && (_timings[0].gpuFrameTime > 0 || _timings[0].cpuMainThreadFrameTime > 0))
        {
            cpuGpu = $"CPU(Main) {_timings[0].cpuMainThreadFrameTime:0.0}ms  " +
                     $"CPU(Render) {_timings[0].cpuRenderThreadFrameTime:0.0}ms  GPU {_timings[0].gpuFrameTime:0.0}ms";
        }

        string mainThread = _mainThread.Valid
            ? $"Main Thread {_mainThread.LastValue / 1000000f:0.0}ms"
            : "Main Thread: -";

        return
            $"<b>FPS {_shownFps:0}</b>   평균 {_shownAvgMs:0.0}ms   최악 {_shownWorstMs:0.0}ms\n" +
            $"{mainThread}\n" +
            $"{cpuGpu}\n" +
            $"Batches {(_batches.Valid ? _batches.LastValue : 0)}   SetPass {(_setPass.Valid ? _setPass.LastValue : 0)}   " +
            $"Triangles {(_triangles.Valid ? _triangles.LastValue : 0) / 1000}K\n" +
            $"GC 할당 {_shownGcBytes / 1024}KB/초\n" +
            $"TimeScale {Time.timeScale:0.00}   FixedDt {Time.fixedDeltaTime:0.000}   물리 {Physics.simulationMode}   (F3: 끄기)";
    }

    private void OnGUI()
    {
        if (!_visible)
        {
            return;
        }

        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label) { fontSize = 18, richText = true };
            _style.normal.textColor = Color.white;
        }

        GUI.Label(new Rect(20f, 140f, 900f, 200f), _text, _style);
    }
}
