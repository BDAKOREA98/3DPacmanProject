using UnityEngine;

/// <summary>
/// 높은 점수를 주는 보너스 아이템. 일정 시간이 지나면 스스로 사라진다.
/// 일반 Item과 달리 "남은 아이템 수"에 포함되지 않으므로 클리어 조건에 영향을 주지 않는다.
/// BonusItemSpawner가 만들 때 자동으로 붙인다.
/// </summary>
public class BonusItem : MonoBehaviour
{
    private int _score;

    public void Initialize(int score, float lifetime)
    {
        _score = score;

        // 먹지 않아도 lifetime(초) 뒤에 사라진다.
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerMove>() == null)
        {
            return;
        }

        Item.AddScore(_score);

        Destroy(gameObject);
    }
}
