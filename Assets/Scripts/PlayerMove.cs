using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어가 항상 자신의 정면(transform.forward)으로 자동 전진한다.
/// A/D 키를 누를 때마다 Y축 기준으로 정확히 90도씩 즉시 회전한다.
/// Rigidbody로 이동하여 벽과 충돌하면 막히지만, 충돌로 인한 회전은 발생하지 않는다.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private const float TurnAngle = 90f;

    private Rigidbody _rigidbody;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();

        // 충돌해도 회전하지 않도록 모든 회전 축 고정 (이동 방향 전환은 코드로만 수행)
        _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

        // 카메라가 따라갈 때 물리 갱신 주기 때문에 떨리지 않도록 보간 사용
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Update()
    {
        HandleTurn();
    }

    private void FixedUpdate()
    {
        MoveForward();
    }

    private void HandleTurn()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        // wasPressedThisFrame: 눌린 프레임에만 true (Input.GetKeyDown과 동일한 동작)
        if (keyboard.aKey.wasPressedThisFrame)
        {
            Turn(-TurnAngle);
        }
        else if (keyboard.dKey.wasPressedThisFrame)
        {
            Turn(TurnAngle);
        }
    }

    private void Turn(float angle)
    {
        Quaternion rotation = _rigidbody.rotation * Quaternion.Euler(0f, angle, 0f);

        _rigidbody.rotation = rotation;
        transform.rotation = rotation;
    }

    private void MoveForward()
    {
        // 정면 방향 속도만 지정하고, 중력(Y)은 물리에 맡긴다. 벽에 막히면 속도가 자연히 줄어든다.
        Vector3 velocity = transform.forward * moveSpeed;
        velocity.y = _rigidbody.linearVelocity.y;

        _rigidbody.linearVelocity = velocity;
    }
}
