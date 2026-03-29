using UnityEngine;

public class Coin : MonoBehaviour
{
    public int pointValue = 10;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(pointValue);

        Destroy(gameObject);
    }
}
