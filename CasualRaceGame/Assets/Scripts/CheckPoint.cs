using UnityEngine;

/// <summary>
/// チェックポイント通過判定
/// </summary>
public class Checkpoint : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private int _checkpointIndex = 0;  // チェックポイント番号

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            // イベント発行
            GameEvents.OnCheckpointPassed.OnNext((other.gameObject, _checkpointIndex));

            Debug.Log($"{other.name} が Checkpoint {_checkpointIndex} を通過");
        }
    }
}