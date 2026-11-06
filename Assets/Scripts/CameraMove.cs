using UnityEngine;

/// <summary>
/// 플레이어의 자식으로 붙은 카메라가 플레이어 회전 시 부드럽게 따라 돌도록 한다.
/// 자식 카메라는 부모 회전을 그대로 받아 즉시 돌아가므로, 매 프레임 부모가 만든 포즈를 버리고
/// 직접 보관한 위치/회전을 Lerp로 목표 포즈에 접근시켜 적용한다.
/// 시작 시 카메라의 배치(위치/각도)를 플레이어 기준으로 기억해 그대로 유지한다.
/// </summary>
public class CameraMove : MonoBehaviour
{
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float rotateSpeed = 5f;

    private Transform _target;
    private Vector3 _offset;            // 플레이어 기준 로컬 Offset (스케일 제외)
    private Quaternion _rotationOffset; // 플레이어 기준 로컬 회전

    private Vector3 _currentPosition;
    private Quaternion _currentRotation;

    private void Start()
    {
        _target = transform.parent;

        if (_target == null)
        {
            Debug.LogWarning($"{nameof(CameraMove)}: 카메라가 플레이어의 자식이 아닙니다.", this);
            enabled = false;
            return;
        }

        _offset = Quaternion.Inverse(_target.rotation) * (transform.position - _target.position);
        _rotationOffset = Quaternion.Inverse(_target.rotation) * transform.rotation;

        _currentPosition = transform.position;
        _currentRotation = transform.rotation;
    }

    /// <summary>
    /// 플레이어가 순간이동했을 때 카메라가 먼 거리를 날아가지 않고 즉시 목표 포즈로 이동하게 한다.
    /// </summary>
    public void SnapToTarget()
    {
        if (_target == null)
        {
            return;
        }

        _currentPosition = _target.position + _target.rotation * _offset;
        _currentRotation = _target.rotation * _rotationOffset;

        transform.SetPositionAndRotation(_currentPosition, _currentRotation);
    }

    private void LateUpdate()
    {
        Vector3 desiredPosition = _target.position + _target.rotation * _offset;
        Quaternion desiredRotation = _target.rotation * _rotationOffset;

        _currentPosition = Vector3.Lerp(_currentPosition, desiredPosition,
            Mathf.Clamp01(followSpeed * Time.deltaTime));
        _currentRotation = Quaternion.Slerp(_currentRotation, desiredRotation,
            Mathf.Clamp01(rotateSpeed * Time.deltaTime));

        transform.SetPositionAndRotation(_currentPosition, _currentRotation);
    }
}
