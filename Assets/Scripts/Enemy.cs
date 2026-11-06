using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어처럼 자신의 정면으로 계속 전진하며, 교차로에서 무작위로 90도 방향을 바꾸는 적.
/// 방향 결정은 MazeLayout 격자의 칸 중심에서 하고, 이동은 Rigidbody + BoxCast로 해서
/// 플레이어와 같은 방식으로 벽에 막힌다(벽에 파고들거나 통과하지 않는다).
/// 터널 행의 양 끝에서는 플레이어처럼 반대쪽으로 순간이동한다.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class Enemy : MonoBehaviour
{
    // (행 변화, 열 변화): 위(+Z), 아래(-Z), 왼쪽, 오른쪽
    private static readonly Vector2Int[] Directions =
    {
        new Vector2Int(-1, 0),
        new Vector2Int(1, 0),
        new Vector2Int(0, -1),
        new Vector2Int(0, 1),
    };

    /// <summary>적이 플레이어에게 닿았을 때 호출된다.</summary>
    public static event System.Action PlayerCaught;

    // 모든 Enemy의 Collider. 서로 부딪혀 길이 막히지 않도록 충돌을 무시시키는 데 쓴다.
    private static readonly List<Collider> AllColliders = new List<Collider>();

    private const float SkinWidth = 0.02f;         // 벽과 유지할 여유 거리
    private const float CheckDistance = 0.1f;      // 이동 거리에 더해 앞쪽을 미리 검사하는 거리
    private const float MaxFloorNormalY = 0.5f;    // 법선 |y|가 이 값 이상이면 바닥/천장으로 보고 무시
    private const float ArriveThreshold = 0.02f;
    private const float StoppedDistance = 0.0001f;

    private readonly List<Vector2Int> _candidates = new List<Vector2Int>(4);
    private readonly RaycastHit[] _hits = new RaycastHit[16];

    private Rigidbody _rigidbody;
    private BoxCollider _boxCollider;
    private Collider _playerCollider;
    private Renderer _renderer;
    private Renderer[] _modelRenderers;                       // 3D 모델을 붙였을 때 색을 입힐 대상
    private readonly MaterialPropertyBlock _tintBlock = new MaterialPropertyBlock();

    private MazeBuilder _maze;
    private float _moveSpeed;
    private float _centerHeight;

    // 큰 아이템을 먹은 동안(겁먹은 상태)의 설정
    private float _frightenedDuration = 8f;
    private float _frightenedSpeedMultiplier = 0.5f;
    private Color _frightenedColor = new Color(0.2f, 0.35f, 1f);
    private Color _normalColor = Color.white;
    private float _frightenedUntil;
    private bool _showingFrightened;

    private int _startRow;          // 처음 생성된 칸 (초기 위치)
    private int _startCol;

    private int _row;               // 마지막으로 도착한 칸의 행
    private int _col;               // 마지막으로 도착한 칸의 열
    private Vector2Int _direction;  // 현재 진행 방향 (행 변화, 열 변화)
    private Vector3 _worldDirection;
    private int _targetRow;
    private int _targetCol;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _boxCollider = GetComponent<BoxCollider>();
        _renderer = GetComponent<Renderer>();

        // 플레이어와 같은 물리 설정: 충돌해도 회전하지 않고, 마찰이 없고, 보간 사용
        _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        _boxCollider.isTrigger = false;
        _boxCollider.material = new PhysicsMaterial("NoFriction")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };

        // 적끼리는 서로 막지 않는다.
        AllColliders.RemoveAll(other => other == null);
        foreach (Collider other in AllColliders)
        {
            Physics.IgnoreCollision(_boxCollider, other);
        }

        AllColliders.Add(_boxCollider);
    }

    private void OnEnable()
    {
        Item.BigItemCollected += OnBigItemCollected;
    }

    private void OnDisable()
    {
        Item.BigItemCollected -= OnBigItemCollected;
    }

    private void OnDestroy()
    {
        AllColliders.Remove(_boxCollider);
    }

    /// <summary>큰 아이템을 먹었을 때의 느려짐 시간/배율/색을 설정한다. (Initialize 전에 호출)</summary>
    public void SetFrightenedSettings(float duration, float speedMultiplier, Color color)
    {
        _frightenedDuration = duration;
        _frightenedSpeedMultiplier = speedMultiplier;
        _frightenedColor = color;
    }

    /// <summary>
    /// 기본 큐브 대신 3D 모델을 보여 준다. 큐브는 충돌/물리용으로 남기고 화면에서만 숨긴다.
    /// 크기(worldScale)는 프리팹 원래 크기에 곱하는 배율, 위치(오프셋)는 월드 단위 값이다. (Initialize 전에 호출)
    /// </summary>
    public void AttachModel(GameObject prefab, Vector3 worldScale, Vector3 worldOffset, float yaw)
    {
        GameObject model = Instantiate(prefab, transform);
        model.name = "Model";

        // 모델에 딸린 Collider/Rigidbody는 적의 충돌 판정과 섞이지 않도록 제거한다.
        foreach (Collider modelCollider in model.GetComponentsInChildren<Collider>())
        {
            Destroy(modelCollider);
        }

        foreach (Rigidbody modelBody in model.GetComponentsInChildren<Rigidbody>())
        {
            Destroy(modelBody);
        }

        // 적 큐브의 스케일(1,2,1)이 모델을 찌그러뜨리지 않도록 상쇄해서 월드 기준 크기로 맞춘다.
        Vector3 parentScale = transform.lossyScale;
        // worldScale은 프리팹 원래 크기에 곱하는 배율이다. (1이면 에셋 크기 그대로, 2면 두 배)
        Vector3 baseScale = model.transform.localScale;
        model.transform.localScale = new Vector3(
            baseScale.x * worldScale.x / parentScale.x,
            baseScale.y * worldScale.y / parentScale.y,
            baseScale.z * worldScale.z / parentScale.z);
        model.transform.localPosition = new Vector3(
            worldOffset.x / parentScale.x, worldOffset.y / parentScale.y, worldOffset.z / parentScale.z);
        model.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        _modelRenderers = model.GetComponentsInChildren<Renderer>();

        if (_renderer != null)
        {
            _renderer.enabled = false;
        }
    }

    private bool IsFrightened => Time.time < _frightenedUntil;

    // 큰 아이템을 먹으면(이미 겁먹은 상태여도) 시간을 새로 시작한다.
    private void OnBigItemCollected()
    {
        _frightenedUntil = Time.time + _frightenedDuration;
    }

    public void Initialize(MazeBuilder maze, int row, int col, float moveSpeed, float centerHeight)
    {
        _maze = maze;
        _moveSpeed = moveSpeed;
        _centerHeight = centerHeight;

        // 적이 플레이어를 막지 않도록 서로의 충돌은 무시한다.
        PlayerMove player = FindFirstObjectByType<PlayerMove>();
        if (player != null && player.TryGetComponent(out _playerCollider))
        {
            Physics.IgnoreCollision(_boxCollider, _playerCollider);
        }

        if (_renderer != null && _modelRenderers == null)
        {
            _normalColor = _renderer.material.color;
        }

        _startRow = row;
        _startCol = col;

        _row = row;
        _col = col;
        Teleport(CellToWorld(_row, _col));

        _direction = Vector2Int.zero;
        ChooseNextTarget();
    }

    private void FixedUpdate()
    {
        if (_maze == null)
        {
            return;
        }

        UpdateFrightenedColor();

        if (CheckPlayerContact())
        {
            return;
        }

        Vector3 target = CellToWorld(_targetRow, _targetCol);
        Vector3 toTarget = target - _rigidbody.position;
        toTarget.y = 0f;

        // 목표 칸 중심까지 남은 거리 (진행 방향 기준)
        float remaining = Vector3.Dot(toTarget, _worldDirection);
        if (remaining <= ArriveThreshold)
        {
            Arrive(target);
            return;
        }

        float dt = Time.fixedDeltaTime;
        float speed = IsFrightened ? _moveSpeed * _frightenedSpeedMultiplier : _moveSpeed;
        float move = Mathf.Min(speed * dt, remaining);

        // 이번 이동 거리 안에 정면 벽이 있으면 벽 앞 여유 거리까지만 이동 (플레이어와 같은 방식)
        bool blocked = false;
        if (TryGetWallDistance(_worldDirection, move + CheckDistance + SkinWidth, out float wallDistance))
        {
            float freeDistance = Mathf.Max(0f, wallDistance - SkinWidth - SkinWidth);
            if (freeDistance < move)
            {
                move = freeDistance;
                blocked = true;
            }
        }

        Vector3 velocity = _worldDirection * (move / dt);
        velocity.y = _rigidbody.linearVelocity.y;
        _rigidbody.linearVelocity = velocity;

        // 예상치 못한 벽에 막혀 멈췄다면 마지막 칸으로 되돌아가 다른 길을 고른다.
        if (blocked && move <= StoppedDistance)
        {
            ReverseToLastCell();
        }
    }

    // 물리 충돌은 서로 무시하므로, Collider 영역이 겹치는지로 접촉을 판정한다.
    // 일반 상태에서 닿으면 플레이어가 잡히고, 겁먹은 상태에서 닿으면 이 적이 초기 위치로 돌아간다.
    // 이번 물리 프레임의 이동을 건너뛰어야 하면 true를 반환한다.
    private bool CheckPlayerContact()
    {
        if (_playerCollider == null || !_boxCollider.bounds.Intersects(_playerCollider.bounds))
        {
            return false;
        }

        if (IsFrightened)
        {
            ReturnToStart();
            return true;
        }

        PlayerCaught?.Invoke();
        return false;
    }

    private void ReturnToStart()
    {
        _frightenedUntil = 0f;     // 초기 위치로 돌아가면 다시 정상 상태
        UpdateFrightenedColor();

        _rigidbody.linearVelocity = Vector3.zero;

        _row = _startRow;
        _col = _startCol;
        Teleport(CellToWorld(_row, _col));

        _direction = Vector2Int.zero;
        ChooseNextTarget();
    }

    // 겁먹은 상태에서는 색을 바꿔 알아볼 수 있게 한다.
    private void UpdateFrightenedColor()
    {
        bool frightened = IsFrightened;
        if (frightened == _showingFrightened)
        {
            return;
        }

        _showingFrightened = frightened;

        // 3D 모델이 있으면 Material을 복제하지 않고 색만 덧입힌다(PropertyBlock). 원래 상태로 돌아가면 해제.
        if (_modelRenderers != null)
        {
            _tintBlock.Clear();
            if (frightened)
            {
                _tintBlock.SetColor("_BaseColor", _frightenedColor);
                _tintBlock.SetColor("_Color", _frightenedColor);
            }

            foreach (Renderer modelRenderer in _modelRenderers)
            {
                modelRenderer.SetPropertyBlock(frightened ? _tintBlock : null);
            }

            return;
        }

        if (_renderer != null)
        {
            _renderer.material.color = frightened ? _frightenedColor : _normalColor;
        }
    }

    // 에디터에서 Play 모드 진입 시(도메인 리로드를 끈 경우에도) 정적 값을 초기화
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        PlayerCaught = null;
        AllColliders.Clear();
    }

    private void Arrive(Vector3 target)
    {
        _row = _targetRow;
        _col = _targetCol;

        // 칸 중심으로 위치를 맞춘다. 오차가 아주 작으므로 보간을 깨지 않도록 Rigidbody 위치만 보정한다.
        _rigidbody.position = new Vector3(target.x, _rigidbody.position.y, target.z);

        ApplyTunnelWarp();
        ChooseNextTarget();
    }

    // 터널 행의 양 끝 칸에 도착하면 반대쪽 끝에서 한 칸 안쪽으로 이동 (플레이어의 TunnelWarp와 같은 규칙)
    private void ApplyTunnelWarp()
    {
        if (_row != MazeLayout.TunnelRow)
        {
            return;
        }

        int lastCol = MazeLayout.ColCount - 1;

        if (_col == 0 && _direction.y < 0)
        {
            _col = lastCol - 1;
        }
        else if (_col == lastCol && _direction.y > 0)
        {
            _col = 1;
        }
        else
        {
            return;
        }

        Teleport(CellToWorld(_row, _col));
    }

    private void ChooseNextTarget()
    {
        _candidates.Clear();

        Vector2Int reverse = -_direction;
        bool insideHouse = MazeLayout.IsGhostHouse(_row, _col);

        foreach (Vector2Int dir in Directions)
        {
            // 직전에 온 방향으로 되돌아가지 않는다 (막다른 길이면 아래에서 허용)
            if (dir == reverse && _direction != Vector2Int.zero)
            {
                continue;
            }

            if (CanEnter(_row + dir.x, _col + dir.y, insideHouse))
            {
                _candidates.Add(dir);
            }
        }

        if (_candidates.Count == 0)
        {
            // 막다른 길: 되돌아가기 허용
            if (_direction != Vector2Int.zero && CanEnter(_row + reverse.x, _col + reverse.y, insideHouse))
            {
                _candidates.Add(reverse);
            }
            else
            {
                return;
            }
        }

        SetDirection(_candidates[Random.Range(0, _candidates.Count)]);
        _targetRow = _row + _direction.x;
        _targetCol = _col + _direction.y;
    }

    private void ReverseToLastCell()
    {
        SetDirection(-_direction);
        _targetRow = _row;
        _targetCol = _col;
    }

    // 진행 방향을 정하고, 플레이어처럼 그 방향을 바라본다 (90도씩 즉시 회전)
    private void SetDirection(Vector2Int direction)
    {
        _direction = direction;
        _worldDirection = _maze.transform.TransformDirection(new Vector3(direction.y, 0f, -direction.x)).normalized;

        Quaternion rotation = Quaternion.LookRotation(_worldDirection, Vector3.up);
        _rigidbody.rotation = rotation;
        transform.rotation = rotation;
    }

    private void Teleport(Vector3 position)
    {
        // transform을 직접 옮겨 보간(Interpolate)이 가로지르지 않게 한다.
        _rigidbody.position = position;
        transform.position = position;
        Physics.SyncTransforms();
    }

    private static bool CanEnter(int row, int col, bool alreadyInsideHouse)
    {
        if (MazeLayout.IsWall(row, col))
        {
            return false;
        }

        // 유령집 밖으로 나온 뒤에는 다시 들어가지 않는다.
        return alreadyInsideHouse || !MazeLayout.IsGhostHouse(row, col);
    }

    // MazeBuilder/ItemSpawner와 같은 좌표 계산 (배치도 중앙이 MazeBuilder 위치, 첫 행이 +Z)
    private Vector3 CellToWorld(int row, int col)
    {
        float cellSize = _maze.CellSize;
        float x = (col - (MazeLayout.ColCount - 1) * 0.5f) * cellSize;
        float z = ((MazeLayout.RowCount - 1) * 0.5f - row) * cellSize;

        Vector3 world = _maze.transform.TransformPoint(new Vector3(x, 0f, z));
        world.y = _maze.transform.position.y + _centerHeight;
        return world;
    }

    /// <summary>
    /// 이 Collider와 같은 크기의 상자를 direction으로 쏴서 가장 가까운 벽까지의 거리를 구한다.
    /// 상자는 SkinWidth만큼 작게 만들어 자기 자신/바닥과 겹쳐 오탐하지 않게 한다.
    /// 플레이어나 다른 적(Rigidbody가 있는 물체), Trigger(아이템), 바닥/천장은 벽으로 보지 않는다.
    /// </summary>
    private bool TryGetWallDistance(Vector3 direction, float maxDistance, out float distance)
    {
        Vector3 scale = transform.lossyScale;
        Vector3 halfExtents = Vector3.Scale(_boxCollider.size * 0.5f, new Vector3(
            Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        halfExtents = Vector3.Max(halfExtents - Vector3.one * SkinWidth, Vector3.one * 0.001f);

        Vector3 center = transform.TransformPoint(_boxCollider.center);

        int count = Physics.BoxCastNonAlloc(center, halfExtents, direction, _hits, _rigidbody.rotation,
            maxDistance, ~0, QueryTriggerInteraction.Ignore);

        distance = float.MaxValue;
        bool found = false;

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = _hits[i];

            if (hit.collider == _boxCollider || hit.rigidbody != null)
            {
                continue;
            }

            if (Mathf.Abs(hit.normal.y) >= MaxFloorNormalY)
            {
                continue;
            }

            if (hit.distance < distance)
            {
                distance = hit.distance;
                found = true;
            }
        }

        return found;
    }
}
