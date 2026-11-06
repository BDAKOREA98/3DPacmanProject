using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어가 항상 자신의 정면(transform.forward)으로 자동 전진한다.
/// A/D 키를 누를 때마다 Y축 기준으로 정확히 90도씩 즉시 회전한다.
/// </summary>
public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private const float TurnAngle = 90f;

    private void Update()
    {
        HandleTurn();
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
            transform.Rotate(0f, -TurnAngle, 0f);
        }
        else if (keyboard.dKey.wasPressedThisFrame)
        {
            transform.Rotate(0f, TurnAngle, 0f);
        }
    }

    private void MoveForward()
    {
        transform.position += moveSpeed * Time.deltaTime * transform.forward;
    }
}
