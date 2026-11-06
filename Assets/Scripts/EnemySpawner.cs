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
    [Tooltip("큰 아이템을 먹은 동안 적(3D 모델)에 덧입히는 색.")]
    [SerializeField] private Color frightenedColor = new Color(0.2f, 0.35f, 1f);

    [Header("3D 모델 (선택): 지정하면 큐브 대신 이 모델이 보인다")]
    [SerializeField] private GameObject modelPrefab;
    [Tooltip("모델(Mesh)만 키우는 배율. 1이면 에셋 크기 그대로. 충돌 크기(Enemy Scale)는 그대로다.")]
    [SerializeField] private Vector3 modelScale = Vector3.one;
    [Tooltip("적 중심(큐브 중앙) 기준 모델 위치(월드 단위). 모델의 발 위치가 피벗이면 (0, -1, 0).")]
    [SerializeField] private Vector3 modelOffset = new Vector3(0f, -1f, 0f);
    [Tooltip("모델이 누워 있거나 다른 방향을 볼 때 돌리는 각도(오일러). 누워 있으면 X를 -90 또는 90, 정면이 다르면 Y를 90/180/-90으로 바꿔 보세요.")]
    [SerializeField] private Vector3 modelRotation = new Vector3(-90f, 0f, 0f);

    [Header("적")]
    [SerializeField] private Vector3 enemyScale = new Vector3(1f, 2f, 1f);

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

        Enemy enemy = enemyObject.AddComponent<Enemy>();
        enemy.SetFrightenedSettings(frightenedDuration, frightenedSpeedMultiplier, frightenedColor);

        if (modelPrefab != null)
        {
            enemy.AttachModel(modelPrefab, modelScale, modelOffset, modelRotation);
        }

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
