using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 시작 시 미로 가운데 유령집 안에서 Enemy를 한 번만 한꺼번에 생성한다.
/// 생성된 Enemy는 플레이어처럼 전진하며 교차로에서 무작위로 방향을 바꾼다.
/// 위치 기준은 씬의 MazeBuilder를 따른다.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private int enemyCount = 4;
    [SerializeField] private float moveSpeed = 4f;
    [Header("큰 아이템을 먹었을 때")]
    [SerializeField] private float frightenedDuration = 8f;
    [Range(0.1f, 1f)]
    [SerializeField] private float frightenedSpeedMultiplier = 0.5f;
    [SerializeField] private Color frightenedColor = new Color(0.2f, 0.35f, 1f);

    [Header("적")]
    [SerializeField] private Vector3 enemyScale = new Vector3(1f, 2f, 1f);
    [SerializeField] private Color[] enemyColors =
    {
        new Color(1f, 0.2f, 0.2f),
        new Color(1f, 0.55f, 0.8f),
        new Color(0.2f, 0.9f, 1f),
        new Color(1f, 0.6f, 0.15f),
    };

    private void Start()
    {
        MazeBuilder maze = FindFirstObjectByType<MazeBuilder>();
        if (maze == null)
        {
            Debug.LogWarning($"{nameof(EnemySpawner)}: 씬에서 MazeBuilder를 찾지 못했습니다.", this);
            return;
        }

        // 게임 시작 시 한 번만 생성 (이후 다시 생성하지 않는다)
        SpawnAll(maze);
    }

    private void SpawnAll(MazeBuilder maze)
    {
        List<Vector2Int> spawnCells = GetHouseCells();
        Shuffle(spawnCells);

        for (int i = 0; i < enemyCount; i++)
        {
            Vector2Int cell = spawnCells[i % spawnCells.Count];
            SpawnEnemy(maze, i, cell.x, cell.y);
        }
    }

    private void SpawnEnemy(MazeBuilder maze, int index, int row, int col)
    {
        GameObject enemyObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        enemyObject.name = $"Enemy_{index}";
        enemyObject.transform.SetParent(transform, false);

        // 부모(이 오브젝트)의 스케일과 무관하게 월드 기준 크기를 유지
        Vector3 parentScale = transform.lossyScale;
        enemyObject.transform.localScale = new Vector3(
            enemyScale.x / parentScale.x, enemyScale.y / parentScale.y, enemyScale.z / parentScale.z);

        if (enemyColors.Length > 0)
        {
            enemyObject.GetComponent<Renderer>().material.color = enemyColors[index % enemyColors.Length];
        }

        Enemy enemy = enemyObject.AddComponent<Enemy>();
        enemy.SetFrightenedSettings(frightenedDuration, frightenedSpeedMultiplier, frightenedColor);
        enemy.Initialize(maze, row, col, moveSpeed, enemyScale.y * 0.5f);
    }

    // 유령집 내부 칸 목록 (x = 행, y = 열)
    private static List<Vector2Int> GetHouseCells()
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        for (int row = MazeLayout.HouseTop; row <= MazeLayout.HouseBottom; row++)
        {
            for (int col = MazeLayout.HouseLeft; col <= MazeLayout.HouseRight; col++)
            {
                if (!MazeLayout.IsWall(row, col))
                {
                    cells.Add(new Vector2Int(row, col));
                }
            }
        }

        return cells;
    }

    private static void Shuffle(List<Vector2Int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
