using UnityEngine;
using System.Collections.Generic;

public class RespawnManager : MonoBehaviour
{
    [Header("落下設定")]
    [SerializeField] private float _fallThresholdY = -10f;
    [SerializeField] private float _invincibleDuration = 3f;
    [SerializeField] private Vector3 _respawnPosition = new Vector3(0, 1, 0);

    private List<GameObject> _cars = new List<GameObject>();
    private Dictionary<GameObject, float> _invincibleTimers = new Dictionary<GameObject, float>();

    void Start()
    {
        GameObject[] foundCars = GameObject.FindGameObjectsWithTag("Car");
        _cars.AddRange(foundCars);

        foreach (var car in _cars)
        {
            _invincibleTimers[car] = 0f;
        }
    }

    void Update()
    {
        CheckFall();
        UpdateInvincibleTimers();
    }

    private void CheckFall()
    {
        foreach (var car in _cars)
        {
            if (car.transform.position.y < _fallThresholdY)
            {
                RespawnCar(car);
            }
        }
    }

    private void RespawnCar(GameObject car)
    {
        // 位置リセット
        car.transform.position = _respawnPosition;

        // 速度リセット
        Rigidbody rb = car.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 向きリセット
        car.transform.rotation = Quaternion.identity;

        // 無敵設定
        SetInvincible(car);

        // ★イベント発行（追加）
        GameEvents.OnCarFell.OnNext(car);
    }

    private void SetInvincible(GameObject car)
    {
        _invincibleTimers[car] = _invincibleDuration;
    }

    private void UpdateInvincibleTimers()
    {
        List<GameObject> keys = new List<GameObject>(_invincibleTimers.Keys);

        foreach (var car in keys)
        {
            if (_invincibleTimers[car] > 0)
            {
                _invincibleTimers[car] -= Time.deltaTime;
            }
        }
    }

    public bool IsInvincible(GameObject car)
    {
        return _invincibleTimers.ContainsKey(car) && _invincibleTimers[car] > 0;
    }
}