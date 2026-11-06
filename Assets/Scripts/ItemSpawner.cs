using UnityEngine;

/// <summary>
/// MazeLayout의 'o' / 'P' 칸(길의 정가운데)에 구(Sphere) 아이템을 일정 간격으로 배치한다.
/// 칸 크기와 위치 기준은 씬의 MazeBuilder를 따라간다.
/// 아이템 Collider는 Trigger라서 플레이어를 막지 않는다.
/// 에디터에서는 컴포넌트 우클릭 메뉴의 "Build Items" / "Clear Items"로 미리 생성/삭제할 수 있다.
/// </summary>
public class ItemSpawner : MonoBehaviour
{
    [SerializeField] private float itemHeight = 1f;
    [SerializeField] private float itemSize = 0.5f;
    [SerializeField] private float bigItemSize = 1f;
    [SerializeField] private Color itemColor = new Color(1f, 0.92f, 0.4f);

    private void Start()
    {
        if (transform.childCount == 0)
        {
            BuildItems();
        }
    }

    [ContextMenu("Build Items")]
    private void BuildItems()
    {
        ClearItems();

        MazeBuilder maze = FindFirstObjectByType<MazeBuilder>();
        if (maze == null)
        {
            Debug.LogWarning($"{nameof(ItemSpawner)}: 씬에서 MazeBuilder를 찾지 못했습니다.", this);
            return;
        }

        int rows = MazeLayout.RowCount;
        int cols = MazeLayout.ColCount;
        float cellSize = maze.CellSize;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                char cell = MazeLayout.Rows[row][col];
                if (cell != MazeLayout.Item && cell != MazeLayout.BigItem)
                {
                    continue;
                }

                // MazeBuilder와 같은 좌표 계산: 배치도 중앙이 MazeBuilder 위치, 첫 행이 +Z
                float x = (col - (cols - 1) * 0.5f) * cellSize;
                float z = ((rows - 1) * 0.5f - row) * cellSize;
                Vector3 worldPosition = maze.transform.position + new Vector3(x, itemHeight, z);

                float size = cell == MazeLayout.BigItem ? bigItemSize : itemSize;
                CreateItem($"Item_{row}_{col}", worldPosition, size);
            }
        }
    }

    [ContextMenu("Clear Items")]
    private void ClearItems()
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

    private void CreateItem(string itemName, Vector3 worldPosition, float size)
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        item.name = itemName;
        item.transform.SetParent(transform, false);
        item.transform.position = worldPosition;
        item.transform.localScale = Vector3.one * size;

        // 플레이어를 막지 않도록 Trigger로 설정
        item.GetComponent<Collider>().isTrigger = true;

        item.GetComponent<Renderer>().material.color = itemColor;
    }
}
