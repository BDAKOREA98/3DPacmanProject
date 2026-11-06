using System;
using UnityEngine;

/// <summary>
/// 플레이어가 닿으면 사라지며 점수를 올리는 아이템.
/// Collider는 Trigger여야 하며, ItemSpawner가 아이템을 만들 때 자동으로 붙인다.
/// 씬에 남아 있는 아이템 수를 Remaining으로 추적한다.
/// </summary>
public class Item : MonoBehaviour
{
    /// <summary>획득한 아이템 점수가 누적된 값.</summary>
    public static int Score { get; private set; }

    /// <summary>현재 씬에 남아 있는 아이템 수.</summary>
    public static int Remaining { get; private set; }

    /// <summary>아이템을 먹을 때마다 해당 아이템의 점수와 함께 호출된다.</summary>
    public static event Action<int> Collected;

    /// <summary>큰 아이템(파워 아이템)을 먹을 때 호출된다.</summary>
    public static event Action BigItemCollected;

    /// <summary>남은 아이템 수가 바뀔 때마다 새 개수와 함께 호출된다.</summary>
    public static event Action<int> RemainingChanged;

    // 에디터에서 Build Items로 미리 만든 아이템도 값이 저장되도록 직렬화한다.
    [SerializeField] private int value = 10;
    [SerializeField] private bool isBig;

    /// <summary>점수를 0으로 되돌린다(게임 재시작 시).</summary>
    public static void ResetScore()
    {
        Score = 0;
    }

    /// <summary>점수를 더하고 Collected 이벤트를 호출한다. (남은 아이템 수에는 영향을 주지 않는 보너스 아이템 등에도 사용)</summary>
    public static void AddScore(int value)
    {
        Score += value;
        Collected?.Invoke(value);
    }

    public void Initialize(int itemValue, bool big)
    {
        value = itemValue;
        isBig = big;
    }

    private void Awake()
    {
        Remaining++;
        RemainingChanged?.Invoke(Remaining);
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        Remaining--;
        RemainingChanged?.Invoke(Remaining);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerMove>() == null)
        {
            return;
        }

        AddScore(value);

        if (isBig)
        {
            BigItemCollected?.Invoke();
        }

        Destroy(gameObject);
    }

    // 에디터에서 Play 모드 진입 시(도메인 리로드를 끈 경우에도) 정적 값을 초기화
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Score = 0;
        Remaining = 0;
        Collected = null;
        BigItemCollected = null;
        RemainingChanged = null;
    }
}
