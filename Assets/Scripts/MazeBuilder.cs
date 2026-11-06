#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

/// <summary>
/// MazeLayout 배치도의 벽(#)을 읽어 Cube로 미로를 생성한다.
/// 미로는 이 오브젝트의 자식으로 만들어지며, 배치도의 중앙이 이 오브젝트의 위치에 놓인다.
/// 에디터에서는 컴포넌트 우클릭 메뉴의 "Build Maze" / "Clear Maze"로 미리 생성/삭제할 수 있다.
/// </summary>
public class MazeBuilder : MonoBehaviour
{
    // 플레이어(1, 2, 1)가 여유 있게 지나도록 통로 폭 2, 벽 높이 3
    [SerializeField] private float cellSize = 2f;
    [SerializeField] private float wallHeight = 3f;

    // 벽과 바닥의 색이 달라야 화면과 미니맵에서 구분된다.
    [SerializeField] private Color wallColor = new Color(0.1f, 0.25f, 0.95f);
    [SerializeField] private Color floorColor = new Color(0.03f, 0.03f, 0.08f);

    public float CellSize => cellSize;

    private Material _wallMaterial;
    private Material _floorMaterial;

    private void Start()
    {
        if (transform.childCount == 0)
        {
            BuildMaze();
        }
    }

    [ContextMenu("Build Maze")]
    public void BuildMaze()
    {
        ClearMaze();

        int rows = MazeLayout.RowCount;
        int cols = MazeLayout.ColCount;

        // 모든 벽/바닥이 같은 Material을 공유한다. (큐브마다 Material이 복제되지 않는다)
        _wallMaterial = GetMaterial("MazeWall", wallColor);
        _floorMaterial = GetMaterial("MazeFloor", floorColor);

        CreateFloor(cols, rows);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                if (MazeLayout.Rows[row][col] != MazeLayout.Wall)
                {
                    continue;
                }

                // 배치도 중앙이 원점. 첫 번째 행이 +Z 쪽에 오도록 뒤집는다.
                float x = (col - (cols - 1) * 0.5f) * cellSize;
                float z = ((rows - 1) * 0.5f - row) * cellSize;

                CreateCube($"Wall_{row}_{col}",
                    new Vector3(x, wallHeight * 0.5f, z),
                    new Vector3(cellSize, wallHeight, cellSize),
                    _wallMaterial);
            }
        }

        MarkSceneChanged();
    }

    [ContextMenu("Clear Maze")]
    public void ClearMaze()
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

    private void CreateFloor(int cols, int rows)
    {
        const float thickness = 0.1f;

        // 윗면을 아주 조금(0.02) 위로 둬서, 같은 높이(Y=0)의 Plane과 겹쳐 깜빡이는 현상을 막는다.
        const float topHeight = 0.02f;

        CreateCube("Floor",
            new Vector3(0f, topHeight - thickness * 0.5f, 0f),
            new Vector3(cols * cellSize, thickness, rows * cellSize),
            _floorMaterial);
    }

    private void CreateCube(string cubeName, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = cubeName;
        cube.transform.SetParent(transform, false);
        cube.transform.localPosition = localPosition;
        cube.transform.localScale = localScale;

        cube.GetComponent<Renderer>().sharedMaterial = material;

        RegisterCreated(cube, "Build Maze");
    }

    /// <summary>
    /// 기본 큐브 Material을 복사해 색만 바꾼 Material을 돌려준다.
    /// 에디터에서 미리 만들 때는 Assets/Materials 에 에셋으로 저장해 씬에 계속 남게 한다.
    /// </summary>
    private static Material GetMaterial(string materialName, Color color)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            const string folder = "Assets/Materials";
            string path = $"{folder}/{materialName}.mat";

            Material asset = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (asset == null)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    AssetDatabase.CreateFolder("Assets", "Materials");
                }

                asset = CreateColoredMaterial(materialName);
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.color = color;
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            return asset;
        }
#endif
        Material material = CreateColoredMaterial(materialName);
        material.color = color;
        return material;
    }

    // 렌더 파이프라인(URP)의 기본 큐브 Material과 같은 셰이더로 새 Material을 만든다.
    private static Material CreateColoredMaterial(string materialName)
    {
        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Material source = temp.GetComponent<Renderer>().sharedMaterial;

        if (Application.isPlaying)
        {
            Destroy(temp);
        }
        else
        {
            DestroyImmediate(temp);
        }

        return new Material(source) { name = materialName };
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
