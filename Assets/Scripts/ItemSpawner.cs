#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

/// <summary>
/// MazeLayout의 'o' / 'P' 칸(길의 정가운데)에 구(Sphere) 아이템을 일정 간격으로 배치한다.
/// 칸 크기와 위치 기준은 씬의 MazeBuilder를 따라간다.
/// 아이템 Collider는 Trigger라서 플레이어를 막지 않고, 닿으면 Item 컴포넌트가 아이템을 먹는다.
/// 에디터에서는 컴포넌트 우클릭 메뉴의 "Build Items" / "Clear Items"로 미리 생성/삭제할 수 있다.
/// </summary>
public class ItemSpawner : MonoBehaviour
{
    [SerializeField] private float itemHeight = 1f;
    [SerializeField] private float itemSize = 0.5f;
    [SerializeField] private float bigItemSize = 1f;
    [SerializeField] private Color itemColor = new Color(1f, 0.92f, 0.4f);
    [SerializeField] private int itemScore = 10;
    [SerializeField] private int bigItemScore = 50;

    private void Start()
    {
        if (transform.childCount == 0)
        {
            BuildItems();
        }
    }

    [ContextMenu("Build Items")]
    public void BuildItems()
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

                // MazeBuilder와 같은 좌표 계산: 배치도 중앙이 MazeBuilder 위치, 첫 행이 +Z.
                // MazeBuilder의 Transform(위치/회전/스케일)을 그대로 적용해 벽과 어긋나지 않게 한다.
                float x = (col - (cols - 1) * 0.5f) * cellSize;
                float z = ((rows - 1) * 0.5f - row) * cellSize;
                Vector3 worldPosition = maze.transform.TransformPoint(new Vector3(x, 0f, z));
                worldPosition.y = maze.transform.position.y + itemHeight;

                bool isBig = cell == MazeLayout.BigItem;
                CreateItem($"Item_{row}_{col}", worldPosition,
                    isBig ? bigItemSize : itemSize,
                    isBig ? bigItemScore : itemScore);
            }
        }
    }

    [ContextMenu("Clear Items")]
    public void ClearItems()
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
#if UNITY_EDITOR
                Undo.DestroyObjectImmediate(child);
#else
                DestroyImmediate(child);
#endif
            }
        }

        MarkSceneChanged();
    }

    private void CreateItem(string itemName, Vector3 worldPosition, float size, int score)
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        item.name = itemName;
        RegisterCreated(item, "Build Items");
        item.transform.SetParent(transform, false);
        item.transform.position = worldPosition;

        // 부모(이 오브젝트)의 스케일과 무관하게 월드 기준으로 정확한 구 크기를 유지
        Vector3 parentScale = transform.lossyScale;
        item.transform.localScale = new Vector3(
            size / parentScale.x, size / parentScale.y, size / parentScale.z);

        // 플레이어를 막지 않도록 Trigger로 설정
        item.GetComponent<Collider>().isTrigger = true;

        item.GetComponent<Renderer>().material.color = itemColor;

        item.AddComponent<Item>().Initialize(score);
    }

    private void MarkSceneChanged()
    {
#if UNITY_EDITOR
        // 에디터에서 미리 만든 경우 씬을 저장 대상으로 표시
        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }

    private static void RegisterCreated(GameObject created, string label)
    {
#if UNITY_EDITOR
        // 에디터에서 만든 오브젝트를 Ctrl+Z로 되돌릴 수 있게 등록
        if (!Application.isPlaying)
        {
            Undo.RegisterCreatedObjectUndo(created, label);
        }
#endif
    }
}
