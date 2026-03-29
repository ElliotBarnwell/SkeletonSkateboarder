using UnityEngine;

public class Coin : MonoBehaviour
{
    public int   pointValue    = 10;
    public float collectRadius = 1.5f;
    public float spinSpeed     = 180f;

    private Transform player;

    void Start()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

        if (player == null) return;

        if (Vector3.Distance(transform.position, player.position) < collectRadius)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.AddScore(pointValue);

            Destroy(gameObject);
        }
    }
}
