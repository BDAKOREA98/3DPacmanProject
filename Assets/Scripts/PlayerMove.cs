using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어가 항상 자신의 정면(transform.forward)으로 자동 전진한다.
/// A/D 키를 누를 때마다 Y축 기준으로 정확히 90도씩 즉시 회전한다.
/// BoxCast로 벽을 검사해 정면 벽 앞에서 멈추고, 벽이 너무 가까운 방향으로는 회전하지 않는다.
/// 입력은 Update, 물리 검사와 이동은 FixedUpdate에서 처리한다.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class PlayerMove : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("벽 검사")]
    [Tooltip("벽으로 취급할 레이어. 바닥은 법선 방향으로도 걸러지므로 Everything이어도 된다.")]
    [SerializeField] private LayerMask wallMask = ~0;

    [Tooltip("이번 프레임 이동 거리에 더해 앞쪽을 미리 검사하는 거리.")]
    [SerializeField] private float checkDistance = 0.1f;

    [Tooltip("A/D로 회전한 뒤 바라볼 방향에 이 거리 안으로 벽이 있으면 회전을 무시한다.")]
    [SerializeField] private float turnCheckDistance = 1.5f;

    [Tooltip("벽과 항상 유지할 여유 거리. 벽에 파고들거나 떨리는 것을 막는다.")]
    [SerializeField] private float skinWidth = 0.02f;

    private const float TurnAngle = 90f;
    private const float MaxFloorNormalY = 0.5f; // 법선의 |y|가 이 값 이상이면 바닥/천장으로 보고 무시
    private const int MaxQueuedTurns = 4;

    private readonly RaycastHit[] _hits = new RaycastHit[16];
    private readonly Queue<float> _turnQueue = new Queue<float>();

    private Rigidbody _rigidbody;
    private BoxCollider _boxCollider;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _boxCollider = GetComponent<BoxCollider>();

        // 충돌해도 회전하지 않도록 모든 회전 축 고정 (이동 방향 전환은 코드로만 수행)
        _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

        // 카메라가 따라갈 때 물리 갱신 주기 때문에 떨리지 않도록 보간 사용
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        RemoveFriction();
    }

    // 벽에 붙었을 때 마찰 때문에 움직이지 않는 문제 방지: 마찰 0 (상대 마찰과 Minimum으로 결합)
    private void RemoveFriction()
    {
        _boxCollider.material = new PhysicsMaterial("NoFriction")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };
    }

    private void Update()
    {
        ReadTurnInput();
    }

    private void FixedUpdate()
    {
        ProcessTurnQueue();
        MoveForward();
    }

    // ---------------- 입력 (Update) ----------------

    private void ReadTurnInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        // wasPressedThisFrame: 눌린 프레임에만 true (Input.GetKeyDown과 동일한 동작)
        if (keyboard.aKey.wasPressedThisFrame)
        {
            EnqueueTurn(-TurnAngle);
        }
        else if (keyboard.dKey.wasPressedThisFrame)
        {
            EnqueueTurn(TurnAngle);
        }
    }

    private void EnqueueTurn(float angle)
    {
        // 다음 FixedUpdate에서 검사 후 처리. 한 물리 프레임 사이의 입력도 놓치지 않는다.
        if (_turnQueue.Count < MaxQueuedTurns)
        {
            _turnQueue.Enqueue(angle);
        }
    }

    // ---------------- 회전 (FixedUpdate) ----------------

    private void ProcessTurnQueue()
    {
        while (_turnQueue.Count > 0)
        {
            float angle = _turnQueue.Dequeue();

            // 회전을 수행하기 전에, 회전 후 바라볼 방향에 벽이 가까운지 먼저 검사
            if (CanTurn(angle))
            {
                Turn(angle);
            }
        }
    }

    private bool CanTurn(float angle)
    {
        Quaternion newRotation = _rigidbody.rotation * Quaternion.Euler(0f, angle, 0f);
        Vector3 newForward = newRotation * Vector3.forward;

        // 회전 후의 방향/Collider 방향으로 BoxCast. turnCheckDistance 안에 벽이 있으면 회전 불가
        // (박스가 skinWidth만큼 작으므로 검사 거리에 skinWidth를 더한다)
        return !TryGetWallDistance(newRotation, newForward, turnCheckDistance + skinWidth, out _);
    }

    private void Turn(float angle)
    {
        Quaternion rotation = _rigidbody.rotation * Quaternion.Euler(0f, angle, 0f);

        _rigidbody.rotation = rotation;
        transform.rotation = rotation;

        // 같은 FixedUpdate 안의 이후 검사가 회전된 Collider를 기준으로 하도록 동기화
        Physics.SyncTransforms();
    }

    // ---------------- 이동 (FixedUpdate) ----------------

    private void MoveForward()
    {
        float step = moveSpeed * Time.fixedDeltaTime;
        float speed = moveSpeed;

        // 이번 프레임 이동 거리 + 미리보기 거리 안에 정면 벽이 있으면 벽 앞 여유 거리까지만 이동
        if (TryGetWallDistance(_rigidbody.rotation, transform.forward, step + checkDistance + skinWidth, out float distance))
        {
            // BoxCast 박스는 skinWidth만큼 작으므로 실제 여유 거리 = distance - skinWidth
            float freeDistance = Mathf.Max(0f, distance - skinWidth - skinWidth);
            speed = Mathf.Min(moveSpeed, freeDistance / Time.fixedDeltaTime);
        }

        // 정면 방향 속도만 지정하고, 중력(Y)은 물리에 맡긴다.
        Vector3 velocity = transform.forward * speed;
        velocity.y = _rigidbody.linearVelocity.y;

        _rigidbody.linearVelocity = velocity;
    }

    // ---------------- 벽 검사 ----------------

    /// <summary>
    /// 플레이어 BoxCollider와 같은 크기의 상자를 direction으로 쏴서 가장 가까운 벽까지의 거리를 구한다.
    /// 상자는 skinWidth만큼 작게 만들어 자기 자신/바닥과 겹쳐 오탐하지 않게 한다.
    /// </summary>
    private bool TryGetWallDistance(Quaternion orientation, Vector3 direction, float maxDistance, out float distance)
    {
        Vector3 scale = transform.lossyScale;
        Vector3 halfExtents = Vector3.Scale(_boxCollider.size * 0.5f, new Vector3(
            Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        halfExtents = Vector3.Max(halfExtents - Vector3.one * skinWidth, Vector3.one * 0.001f);

        Vector3 center = transform.TransformPoint(_boxCollider.center);

        int count = Physics.BoxCastNonAlloc(center, halfExtents, direction, _hits, orientation,
            maxDistance, wallMask, QueryTriggerInteraction.Ignore);

        distance = float.MaxValue;
        bool found = false;

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = _hits[i];

            // 자기 자신 제외. Rigidbody가 있는 물체(적 등)는 벽이 아니므로 이동/회전을 막지 않는다.
            // (적이 벽으로 취급되면 플레이어가 적에게 닿기 전에 멈춰 버려 접촉 판정이 일어나지 않는다.)
            if (hit.collider == _boxCollider || hit.rigidbody != null)
            {
                continue;
            }

            // 바닥/천장(법선이 위아래를 향함)은 벽이 아니므로 이동이나 회전을 막지 않는다.
            if (Mathf.Abs(hit.normal.y) >= MaxFloorNormalY)
            {
                continue;
            }

            if (hit.distance < distance)
            {
                distance = hit.distance;
                found = true;
            }
        }

        return found;
    }
}
