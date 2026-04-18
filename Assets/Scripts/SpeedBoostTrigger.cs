using UnityEngine;

/// Attached to each speed bar. Fires a boost on TrackScroller the moment
/// the bar's world Z position passes behind the player.
///
/// No Rigidbody or collider needed — mirrors the distance-check pattern
/// used by Coin.cs.
public class SpeedBoostTrigger : MonoBehaviour
{
    public float boostAmount = 10f;

    private Transform _player;
    private bool      _triggered;

    void Start()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null)
            _player = p.transform;
    }

    void Update()
    {
        if (_triggered || _player == null) return;

        if (transform.position.z < _player.position.z)
        {
            _triggered = true;
            TrackScroller.Instance?.ApplyBoost(boostAmount);
        }
    }
}
