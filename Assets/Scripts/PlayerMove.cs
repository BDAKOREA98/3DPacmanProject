using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// WASD 입력으로 오브젝트를 전후좌우(XZ 평면)로 이동시킨다.
/// 인스펙터 연결 없이 코드에서 InputAction을 직접 만든다.
/// </summary>
public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private InputAction _moveAction;

    private void Awake()
    {
        _moveAction = new InputAction("Move", InputActionType.Value);
        _moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
    }

    private void OnEnable() => _moveAction.Enable();

    private void OnDisable() => _moveAction.Disable();

    private void OnDestroy() => _moveAction.Dispose();

    private void Update()
    {
        Vector2 input = _moveAction.ReadValue<Vector2>();
        Vector3 direction = new Vector3(input.x, 0f, input.y);

        transform.position += moveSpeed * Time.deltaTime * direction;
    }
}
