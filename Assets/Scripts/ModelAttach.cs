using UnityEngine;

/// <summary>
/// 큐브 같은 기본 오브젝트에 3D 모델(프리팹)을 붙이는 공용 도우미. 플레이어와 적이 함께 쓴다.
/// 큐브는 충돌/물리용으로 남기고, 모델은 스케일을 상쇄한 중간 오브젝트 아래에 넣어
/// 큐브의 비균일 스케일(예: 1,2,1)이 모델을 찌그러뜨리지 않게 한다.
/// </summary>
public static class ModelAttach
{
    /// <summary>
    /// owner 아래에 모델을 만들고 그 모델의 Renderer 목록을 돌려준다.
    /// scale은 프리팹 원래 크기에 곱하는 배율, offset은 owner 중심 기준 월드 단위 위치,
    /// eulerRotation은 owner 정면 기준 오일러 각도다.
    /// </summary>
    public static Renderer[] Attach(Transform owner, GameObject prefab, Vector3 scale, Vector3 offset, Vector3 eulerRotation)
    {
        // 월드 스케일이 정확히 (1,1,1)이 되도록 owner의 스케일을 상쇄한 중간 오브젝트.
        // 그 아래의 모델은 어떻게 회전해도 늘어나지 않는다.
        Vector3 ownerScale = owner.lossyScale;

        Transform modelRoot = new GameObject("ModelRoot").transform;
        modelRoot.SetParent(owner, false);
        modelRoot.localPosition = new Vector3(
            offset.x / ownerScale.x, offset.y / ownerScale.y, offset.z / ownerScale.z);
        modelRoot.localRotation = Quaternion.identity;
        modelRoot.localScale = new Vector3(1f / ownerScale.x, 1f / ownerScale.y, 1f / ownerScale.z);

        GameObject model = Object.Instantiate(prefab, modelRoot);
        model.name = "Model";

        // 모델에 딸린 Collider/Rigidbody는 owner의 충돌 판정과 섞이지 않도록 제거한다.
        foreach (Collider modelCollider in model.GetComponentsInChildren<Collider>())
        {
            Object.Destroy(modelCollider);
        }

        foreach (Rigidbody modelBody in model.GetComponentsInChildren<Rigidbody>())
        {
            Object.Destroy(modelBody);
        }

        Vector3 baseScale = model.transform.localScale;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.Euler(eulerRotation);
        model.transform.localScale = Vector3.Scale(baseScale, scale);

        return model.GetComponentsInChildren<Renderer>();
    }
}
