using System;
using UnityEngine;

/// <summary>
/// 플레이어가 닿으면 사라지며 점수를 올리는 아이템.
/// Collider는 Trigger여야 하며, ItemSpawner가 아이템을 만들 때 자동으로 붙인다.
/// </summary>
public class Item : MonoBehaviour
{
    /// <summary>획득한 아이템 점수가 누적된 값.</summary>
    public static int Score { get; private set; }

    /// <summary>아이템을 먹을 때마다 해당 아이템의 점수와 함께 호출된다.</summary>
    public static event Action<int> Collected;

    private int _value;

    public void Initialize(int value)
    {
        _value = value;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerMove>() == null)
        {
            return;
        }

        Score += _value;
        Collected?.Invoke(_value);

        Destroy(gameObject);
    }

    // 에디터에서 Play 모드 진입 시(도메인 리로드를 끈 경우에도) 점수를 초기화
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Score = 0;
        Collected = null;
    }
}
