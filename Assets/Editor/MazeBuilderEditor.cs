using UnityEditor;
using UnityEngine;

/// <summary>
/// MazeBuilder 인스펙터에 씬에 미리 미로를 만들고 지우는 버튼을 추가한다.
/// </summary>
[CustomEditor(typeof(MazeBuilder))]
public class MazeBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Build Maze", GUILayout.Height(28)))
            {
                ((MazeBuilder)target).BuildMaze();
            }

            if (GUILayout.Button("Clear Maze", GUILayout.Height(28)))
            {
                ((MazeBuilder)target).ClearMaze();
            }
        }
    }
}
