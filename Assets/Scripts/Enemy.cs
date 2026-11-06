using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어처럼 자신의 정면으로 계속 전진하며, 교차로에서 무작위로 90도 방향을 바꾸는 적.
/// MazeLayout 격자를 따라 칸 단위로 이동하므로 벽을 통과하지 않는다.
/// 터널 행의 양 끝에서는 플레이어처럼 반대쪽으로 순간이동한다.
/// </summary>
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

    private const float ArriveThreshold = 0.001f;

    private readonly List<Vector2Int> _candidates = new List<Vector2Int>(4);

    private MazeBuilder _maze;
    private float _moveSpeed;
    private float _centerHeight;

    private int _row;               // 현재 칸의 행
    private int _col;               // 현재 칸의 열
    private Vector2Int _direction;  // 현재 진행 방향 (행 변화, 열 변화)
    private int _targetRow;
    private int _targetCol;

    public void Initialize(MazeBuilder maze, int row, int col, float moveSpeed, float centerHeight)
    {
        _maze = maze;
        _moveSpeed = moveSpeed;
        _centerHeight = centerHeight;

        _row = row;
        _col = col;
        transform.position = CellToWorld(_row, _col);

        _direction = Vector2Int.zero;
        ChooseNextTarget();
    }

    private void Update()
    {
        if (_maze == null)
        {
            return;
        }

        Vector3 target = CellToWorld(_targetRow, _targetCol);
        transform.position = Vector3.MoveTowards(transform.position, target, _moveSpeed * Time.deltaTime);

        if ((transform.position - target).sqrMagnitude <= ArriveThreshold * ArriveThreshold)
        {
            _row = _targetRow;
            _col = _targetCol;
            transform.position = target;

            ApplyTunnelWarp();
            ChooseNextTarget();
        }
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

        transform.position = CellToWorld(_row, _col);
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

        _direction = _candidates[Random.Range(0, _candidates.Count)];
        _targetRow = _row + _direction.x;
        _targetCol = _col + _direction.y;

        // 플레이어처럼 진행 방향을 바라본다 (90도씩 즉시 회전)
        Vector3 worldDirection = _maze.transform.TransformDirection(new Vector3(_direction.y, 0f, -_direction.x));
        transform.rotation = Quaternion.LookRotation(worldDirection, Vector3.up);
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
}
