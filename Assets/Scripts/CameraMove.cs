using UnityEngine;

/// <summary>
/// 플레이어의 뒤쪽 일정 거리에서 따라가며 항상 플레이어 방향을 바라보는 카메라.
/// Offset은 플레이어 기준 로컬 좌표이므로 플레이어가 회전하면 카메라도 함께 돌아간다.
/// </summary>
public class CameraMove : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float rotateSpeed = 5f;
    [SerializeField] private bool useInitialOffset = false;

    // 플레이어 기준 로컬 Offset (x: 오른쪽, y: 위, z: 앞). 뒤쪽이므로 z는 음수.
    [SerializeField] private Vector3 offset = new Vector3(0f, 4f, -6f);

    // 플레이어 위치에서 이만큼 위쪽을 바라본다.
    [SerializeField] private float lookHeight = 1f;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning($"{nameof(CameraMove)}: target이 지정되지 않았습니다.", this);
            return;
        }

        // 시작 시 카메라의 현재 위치를 플레이어 기준 로컬 Offset으로 변환 (스케일은 무시)
        if (useInitialOffset)
        {
            offset = Quaternion.Inverse(target.rotation) * (transform.position - target.position);
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.position + target.rotation * offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition,
            Mathf.Clamp01(followSpeed * Time.deltaTime));

        Vector3 lookPoint = target.position + Vector3.up * lookHeight;
        Quaternion desiredRotation = Quaternion.LookRotation(lookPoint - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation,
            Mathf.Clamp01(rotateSpeed * Time.deltaTime));
    }
}
