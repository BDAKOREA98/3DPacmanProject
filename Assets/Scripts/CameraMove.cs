using UnityEngine;

/// <summary>
/// 플레이어를 일정한 Offset으로 부드럽게 따라가는 카메라.
/// 플레이어의 회전에는 영향을 받지 않는다(위치만 추적).
/// </summary>
public class CameraMove : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private bool useInitialOffset = true;
    [SerializeField] private Vector3 offset = new Vector3(0f, 8f, -8f);

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning($"{nameof(CameraMove)}: target이 지정되지 않았습니다.", this);
            return;
        }

        // 게임 시작 시 카메라의 현재 위치를 기준으로 Offset 자동 계산
        if (useInitialOffset)
        {
            offset = transform.position - target.position;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        // 플레이어 회전과 무관하게 월드 좌표 기준 Offset만 적용
        Vector3 desiredPosition = target.position + offset;
        float t = Mathf.Clamp01(followSpeed * Time.deltaTime);

        transform.position = Vector3.Lerp(transform.position, desiredPosition, t);
    }
}
