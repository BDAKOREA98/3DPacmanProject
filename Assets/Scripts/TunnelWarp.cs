using UnityEngine;

/// <summary>
/// 팩맨처럼 터널 행의 한쪽 끝으로 나가면 반대쪽 끝으로 순간이동시킨다.
/// Play 시작 시 자동으로 생성되므로 씬에 따로 배치하거나 연결할 필요가 없다.
/// 위치 기준은 씬의 MazeBuilder(위치/회전/스케일 포함)를 따른다.
/// </summary>
public class TunnelWarp : MonoBehaviour
{
    private MazeBuilder _maze;
    private Rigidbody _player;
    private CameraMove _camera;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (FindFirstObjectByType<TunnelWarp>() != null)
        {
            return;
        }

        new GameObject(nameof(TunnelWarp)).AddComponent<TunnelWarp>();
    }

    private void Start()
    {
        _maze = FindFirstObjectByType<MazeBuilder>();
        PlayerMove playerMove = FindFirstObjectByType<PlayerMove>();

        if (_maze == null || playerMove == null)
        {
            Debug.LogWarning($"{nameof(TunnelWarp)}: MazeBuilder 또는 PlayerMove를 찾지 못해 비활성화합니다.", this);
            enabled = false;
            return;
        }

        _player = playerMove.GetComponent<Rigidbody>();
        _camera = playerMove.GetComponentInChildren<CameraMove>();
    }

    private void FixedUpdate()
    {
        float cellSize = _maze.CellSize;
        Transform mazeTransform = _maze.transform;

        // 미로 로컬 좌표(칸 크기 단위)로 변환해 터널 행 위에 있는지, 양 끝에 도달했는지 검사
        Vector3 local = mazeTransform.InverseTransformPoint(_player.position);

        float tunnelZ = ((MazeLayout.RowCount - 1) * 0.5f - MazeLayout.TunnelRow) * cellSize;
        if (Mathf.Abs(local.z - tunnelZ) > cellSize * 0.5f)
        {
            return;
        }

        float edgeX = (MazeLayout.ColCount - 1) * 0.5f * cellSize;   // 맨 왼쪽/오른쪽 칸의 중심

        if (local.x <= -edgeX)
        {
            Warp(local, edgeX - cellSize);   // 왼쪽 끝 -> 오른쪽 끝에서 한 칸 안쪽
        }
        else if (local.x >= edgeX)
        {
            Warp(local, -edgeX + cellSize);  // 오른쪽 끝 -> 왼쪽 끝에서 한 칸 안쪽
        }
    }

    private void Warp(Vector3 local, float targetLocalX)
    {
        local.x = targetLocalX;
        Vector3 worldPosition = _maze.transform.TransformPoint(local);

        // transform을 직접 옮겨 보간(Interpolate)이 먼 거리를 가로지르지 않게 한다.
        _player.position = worldPosition;
        _player.transform.position = worldPosition;
        Physics.SyncTransforms();

        // 이동 방향/속도는 그대로 유지(같은 방향으로 계속 전진). 카메라는 즉시 따라 붙인다.
        if (_camera != null)
        {
            _camera.SnapToTarget();
        }
    }
}
