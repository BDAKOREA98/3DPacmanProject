using UnityEditor;
using UnityEngine;

/// <summary>
/// ItemSpawner 인스펙터에 씬에 미리 아이템을 만들고 지우는 버튼을 추가한다.
/// </summary>
[CustomEditor(typeof(ItemSpawner))]
public class ItemSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Build Items", GUILayout.Height(28)))
            {
                ((ItemSpawner)target).BuildItems();
            }

            if (GUILayout.Button("Clear Items", GUILayout.Height(28)))
            {
                ((ItemSpawner)target).ClearItems();
            }
        }
    }
}
