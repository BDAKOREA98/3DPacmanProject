#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// 같은 색의 오브젝트들이 하나의 Material을 공유하도록 도와 주는 도우미.
/// renderer.material.color를 오브젝트마다 쓰면 Material이 개수만큼 복제되어 메모리와 렌더링 비용이 커진다.
/// 에디터에서 미리 만들 때는 Assets/Materials 에 에셋으로 저장해 씬에 계속 남게 한다.
/// </summary>
public static class MaterialUtil
{
    /// <summary>기본 큐브/구 Material을 복사해 색만 바꾼 Material을 돌려준다.</summary>
    public static Material GetShared(string materialName, Color color)
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

    // 렌더 파이프라인(URP)의 기본 Material과 같은 셰이더로 새 Material을 만든다.
    private static Material CreateColoredMaterial(string materialName)
    {
        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Material source = temp.GetComponent<Renderer>().sharedMaterial;

        if (Application.isPlaying)
        {
            Object.Destroy(temp);
        }
        else
        {
            Object.DestroyImmediate(temp);
        }

        return new Material(source) { name = materialName };
    }
}
