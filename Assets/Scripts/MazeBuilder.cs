using UnityEngine;

/// <summary>
/// 문자열 배치도(# = 벽)를 읽어 Cube로 미로를 생성한다.
/// 미로는 이 오브젝트의 자식으로 만들어지며, 배치도의 중앙이 이 오브젝트의 위치에 놓인다.
/// 에디터에서는 컴포넌트 우클릭 메뉴의 "Build Maze" / "Clear Maze"로 미리 생성/삭제할 수 있다.
/// </summary>
public class MazeBuilder : MonoBehaviour
{
    // 플레이어(1, 2, 1)가 여유 있게 지나도록 통로 폭 2, 벽 높이 3
    [SerializeField] private float cellSize = 2f;
    [SerializeField] private float wallHeight = 3f;

    // 28열 x 31행, '#' = 벽(Cube), '.' = 길/빈 공간. 위쪽 행이 +Z 방향.
    private static readonly string[] Layout =
    {
        "############################",
        "#............##............#",
        "#.####.#####.##.#####.####.#",
        "#.####.#####.##.#####.####.#",
        "#.####.#####.##.#####.####.#",
        "#..........................#",
        "#.####.##.########.##.####.#",
        "#.####.##.########.##.####.#",
        "#......##....##....##......#",
        "######.#####.##.#####.######",
        ".....#.#####.##.#####.#.....",
        ".....#.##..........##.#.....",
        ".....#.##.###..###.##.#.....",
        "######.##.########.##.######",
        "..........#......#..........",
        "######.##.#......#.##.######",
        "######.##.########.##.######",
        ".....#.##..........##.#.....",
        ".....#.##.########.##.#.....",
        "######.##.########.##.######",
        "#............##............#",
        "#.####.#####.##.#####.####.#",
        "#.####.#####.##.#####.####.#",
        "#...##................##...#",
        "###.##.##.########.##.##.###",
        "###.##.##.########.##.##.###",
        "#......##....##....##......#",
        "#.##########.##.##########.#",
        "#.##########.##.##########.#",
        "#..........................#",
        "############################",
    };

    private void Start()
    {
        if (transform.childCount == 0)
        {
            BuildMaze();
        }
    }

    [ContextMenu("Build Maze")]
    private void BuildMaze()
    {
        ClearMaze();

        int rows = Layout.Length;
        int cols = Layout[0].Length;

        CreateFloor(cols, rows);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                if (Layout[row][col] != '#')
                {
                    continue;
                }

                // 배치도 중앙이 원점. 첫 번째 행이 +Z 쪽에 오도록 뒤집는다.
                float x = (col - (cols - 1) * 0.5f) * cellSize;
                float z = ((rows - 1) * 0.5f - row) * cellSize;

                CreateCube($"Wall_{row}_{col}",
                    new Vector3(x, wallHeight * 0.5f, z),
                    new Vector3(cellSize, wallHeight, cellSize));
            }
        }
    }

    [ContextMenu("Clear Maze")]
    private void ClearMaze()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;

            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }
    }

    private void CreateFloor(int cols, int rows)
    {
        const float thickness = 0.1f;

        CreateCube("Floor",
            new Vector3(0f, -thickness * 0.5f, 0f),
            new Vector3(cols * cellSize, thickness, rows * cellSize));
    }

    private void CreateCube(string cubeName, Vector3 localPosition, Vector3 localScale)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = cubeName;
        cube.transform.SetParent(transform, false);
        cube.transform.localPosition = localPosition;
        cube.transform.localScale = localScale;
    }
}
