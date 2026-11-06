using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 미로의 무작위 길 위에 점수가 높은 보너스 아이템을 만들고, 일정 시간(기본 30초) 뒤 사라지게 한다.
/// 사라지거나 먹힌 뒤에는 잠시 기다렸다가 다른 무작위 길에 다시 나타난다.
/// 위치 기준은 씬의 MazeBuilder를 따른다.
/// </summary>
public class BonusItemSpawner : MonoBehaviour
{
    [SerializeField] private int bonusScore = 200;
    [SerializeField] private float lifetime = 30f;
    [Tooltip("게임 시작 후 첫 등장까지, 그리고 사라진 뒤 다음 등장까지 기다리는 시간(초).")]
    [SerializeField] private float spawnInterval = 10f;
    [SerializeField] private float itemHeight = 1f;
    [SerializeField] private float itemSize = 1f;
    [SerializeField] private Color itemColor = new Color(1f, 0.45f, 0.1f);

    private const int MaxPickAttempts = 20;
    private const float FreeCheckRadius = 0.4f;

    private void Start()
    {
        MazeBuilder maze = FindFirstObjectByType<MazeBuilder>();
        if (maze == null)
        {
            Debug.LogWarning($"{nameof(BonusItemSpawner)}: 씬에서 MazeBuilder를 찾지 못했습니다.", this);
            return;
        }

        StartCoroutine(SpawnLoop(maze));
    }

    private IEnumerator SpawnLoop(MazeBuilder maze)
    {
        List<Vector2Int> pathCells = GetPathCells();
        if (pathCells.Count == 0)
        {
            yield break;
        }

        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            GameObject bonus = SpawnBonus(maze, pathCells);

            // 먹히거나 시간이 지나 사라질 때까지 대기
            while (bonus != null)
            {
                yield return null;
            }
        }
    }

    private GameObject SpawnBonus(MazeBuilder maze, List<Vector2Int> pathCells)
    {
        // 다른 아이템/적/플레이어와 겹치지 않는 칸을 우선 고른다.
        Vector3 position = Vector3.zero;
        for (int attempt = 0; attempt < MaxPickAttempts; attempt++)
        {
            Vector2Int cell = pathCells[Random.Range(0, pathCells.Count)];
            position = CellToWorld(maze, cell.x, cell.y);

            if (!Physics.CheckSphere(position, FreeCheckRadius, ~0, QueryTriggerInteraction.Collide))
            {
                break;
            }
        }

        GameObject bonus = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bonus.name = "BonusItem";
        bonus.transform.SetParent(transform, false);
        bonus.transform.position = position;

        // 부모(이 오브젝트)의 스케일과 무관하게 월드 기준으로 정확한 구 크기를 유지
        Vector3 parentScale = transform.lossyScale;
        bonus.transform.localScale = new Vector3(
            itemSize / parentScale.x, itemSize / parentScale.y, itemSize / parentScale.z);

        // 플레이어를 막지 않도록 Trigger로 설정
        bonus.GetComponent<Collider>().isTrigger = true;
        bonus.GetComponent<Renderer>().material.color = itemColor;

        bonus.AddComponent<BonusItem>().Initialize(bonusScore, lifetime);
        return bonus;
    }

    // 아이템이 놓이는 길 칸(배치도의 'o', 'P')만 후보로 삼는다. 모두 플레이어가 갈 수 있는 길이다.
    private static List<Vector2Int> GetPathCells()
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        for (int row = 0; row < MazeLayout.RowCount; row++)
        {
            for (int col = 0; col < MazeLayout.ColCount; col++)
            {
                char cell = MazeLayout.Rows[row][col];
                if (cell == MazeLayout.Item || cell == MazeLayout.BigItem)
                {
                    cells.Add(new Vector2Int(row, col));
                }
            }
        }

        return cells;
    }

    // MazeBuilder/ItemSpawner와 같은 좌표 계산 (배치도 중앙이 MazeBuilder 위치, 첫 행이 +Z)
    private Vector3 CellToWorld(MazeBuilder maze, int row, int col)
    {
        float cellSize = maze.CellSize;
        float x = (col - (MazeLayout.ColCount - 1) * 0.5f) * cellSize;
        float z = ((MazeLayout.RowCount - 1) * 0.5f - row) * cellSize;

        Vector3 world = maze.transform.TransformPoint(new Vector3(x, 0f, z));
        world.y = maze.transform.position.y + itemHeight;
        return world;
    }
}
