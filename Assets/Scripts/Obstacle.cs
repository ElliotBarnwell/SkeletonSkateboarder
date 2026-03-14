using UnityEngine;

// Attach this to every obstacle prefab
public class Obstacle : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.TriggerGameOver();
        }
    }

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            GameManager.Instance.TriggerGameOver();
        }
    }
}