using UnityEngine;
using System.Collections.Generic;

public class RespawnManager : MonoBehaviour
{
    [Header("落下設定")]
    [Tooltip("このY座標以下に落ちたらリスポーンする")]
    [SerializeField] private float _fallThresholdY = -10f;
    [Tooltip("リスポーン後の無敵時間")]
    [SerializeField] private float _invincibleDuration = 3f;
    [Tooltip("リスポーン位置の高さオフセット")]
    [SerializeField] private float _respawnHeightOffset = 2f; // Checkpointより少し上に出す

    [Header("参照")]
    [SerializeField] private RaceManager _raceManager;

    // 初期位置（レース開始前の落下 or RaceManagerなし時のフォールバック）
    private Dictionary<GameObject, Vector3> _initialPositions = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, Quaternion> _initialRotations = new Dictionary<GameObject, Quaternion>();

    private List<GameObject> _cars = new List<GameObject>();
    private Dictionary<GameObject, float> _invincibleTimers = new Dictionary<GameObject, float>();

    void Start()
    {
        GameObject[] foundCars = GameObject.FindGameObjectsWithTag("Car");
        _cars.AddRange(foundCars);

        foreach (var car in _cars)
        {
            _invincibleTimers[car] = 0f;
            // 初期位置を保存
            _initialPositions[car] = car.transform.position;
            _initialRotations[car] = car.transform.rotation;
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
        // リスポーン位置・向きを決定
        Vector3 respawnPos;
        Quaternion respawnRot;
        GetRespawnTransform(car, out respawnPos, out respawnRot);

        // 位置・向きリセット
        car.transform.position = respawnPos;
        car.transform.rotation = respawnRot;

        // 速度リセット
        Rigidbody rb = car.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // AIの速度もリセット
        AIController ai = car.GetComponent<AIController>();
        if (ai != null)
        {
            ai.ResetSpeed();
        }

        // プレイヤーの速度もリセット
        CarController controller = car.GetComponent<CarController>();
        if (controller != null)
        {
            controller.ResetSpeed();
        }

        // 無敵設定
        SetInvincible(car);

        // イベント発行
        GameEvents.OnCarFell.OnNext(car);
    }

    /// <summary>
    /// リスポーン位置を決定
    /// 直前のCheckpoint → 初期位置 の優先度
    /// TODO: ゆくゆく「1位と最後尾の中間地点」に変更
    /// </summary>
    private void GetRespawnTransform(GameObject car, out Vector3 position, out Quaternion rotation)
    {
        if (_raceManager != null)
        {
            var progress = _raceManager.GetProgress(car);

            if (progress != null && progress.CurrentCheckpointIndex > 0)
            {
                // 直前に通過したCheckpointの位置にリスポーン
                List<Transform> checkpoints = _raceManager.GetCheckpoints();
                Transform lastCheckpoint = checkpoints[progress.CurrentCheckpointIndex - 1];

                position = lastCheckpoint.position + Vector3.up * _respawnHeightOffset;

                // 次のCheckpointの方向を向く
                if (progress.CurrentCheckpointIndex < checkpoints.Count)
                {
                    Transform nextCheckpoint = checkpoints[progress.CurrentCheckpointIndex];
                    Vector3 direction = (nextCheckpoint.position - lastCheckpoint.position);
                    direction.y = 0;
                    rotation = Quaternion.LookRotation(direction.normalized);
                }
                else
                {
                    rotation = lastCheckpoint.rotation;
                }

                return;
            }
        }

        // Checkpoint未通過 or RaceManagerなし → 初期位置に戻す
        position = _initialPositions[car];
        rotation = _initialRotations[car];
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