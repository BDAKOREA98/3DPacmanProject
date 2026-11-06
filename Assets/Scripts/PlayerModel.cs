using UnityEngine;

/// <summary>
/// 플레이어(큐브)에 3D 모델을 입힌다. 큐브는 충돌/물리용으로 남기고 화면에서만 숨긴다.
/// 플레이어 오브젝트에 붙이고 Inspector의 Model Prefab에 모델을 지정하면 된다.
/// Model Prefab을 비워 두면 아무 일도 하지 않는다(큐브 그대로).
/// </summary>
public class PlayerModel : MonoBehaviour
{
    [SerializeField] private GameObject modelPrefab;

    [Tooltip("모델(Mesh)만 키우는 배율. 1이면 에셋 크기 그대로. 충돌 크기는 그대로다.")]
    [SerializeField] private Vector3 modelScale = Vector3.one;

    [Tooltip("플레이어 중심(큐브 중앙) 기준 모델 위치(월드 단위). 모델의 발 위치가 피벗이면 (0, -1, 0).")]
    [SerializeField] private Vector3 modelOffset = new Vector3(0f, -1f, 0f);

    [Tooltip("모델이 누워 있거나 다른 방향을 볼 때 돌리는 각도(오일러). 누워 있으면 X를 -90 또는 90, 정면이 다르면 Y를 90/180/-90으로 바꿔 보세요.")]
    [SerializeField] private Vector3 modelRotation = new Vector3(-90f, 0f, 0f);

    private void Awake()
    {
        if (modelPrefab == null)
        {
            return;
        }

        ModelAttach.Attach(transform, modelPrefab, modelScale, modelOffset, modelRotation);

        // 큐브는 화면에서만 숨긴다. (Collider와 Rigidbody는 그대로)
        if (TryGetComponent(out Renderer cubeRenderer))
        {
            cubeRenderer.enabled = false;
        }
    }
}
