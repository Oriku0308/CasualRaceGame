using UnityEngine;

/// <summary>
/// AI車の制御
/// </summary>
public class AIController : CarBase
{
    [Header("AI設定")]
    [Tooltip("チェックポイント到達とみなす距離")]
    [SerializeField] private float _arrivalDistance = 5f;
    [Tooltip("ターン開始とみなす角度")]
    [SerializeField] private float _brakeAngle = 90f;
    [Header("個性設定")]
    [Tooltip("速度のばらつき")]
    [SerializeField] private float _speedVariation = 0.2f;  // 速度のばらつき
    [Tooltip("走行ラインのずれ幅")]
    [SerializeField] private float _pathOffset = 3f;         // 走行ラインのずれ幅

    private float _speedMultiplier;
    private Vector3 _targetOffset;

    protected override void Start()
    {
        base.Start();

        // 各AIに個性を与える
        _speedMultiplier = 1f + Random.Range(-_speedVariation, _speedVariation);

        // 走行ラインのオフセット（左右にずらす）
        float offsetX = Random.Range(-_pathOffset, _pathOffset);
        _targetOffset = new Vector3(offsetX, 0, 0);
    }

    protected override void FixedUpdate()
    {
        Transform target = base._raceManager.GetNextCheckpoint(gameObject);

        if (target == null)
        {
            Decelerate();
            base.FixedUpdate();
            return;
        }

        // 目標位置にオフセットを加える（走行ラインをずらす）
        Vector3 targetPos = target.position + target.right * _targetOffset.x;
        Vector3 directionToTarget = (targetPos - transform.position);
        directionToTarget.y = 0;

        if (directionToTarget.magnitude > 0.1f)
        {
            TurnToward(directionToTarget.normalized);
            DecideAcceleration(directionToTarget.normalized);
        }

        base.FixedUpdate();
    }

    private new void Accelerate()
    {
        float speedFactor = 1f / (1f + Mathf.Pow(_currentSpeed * _speedResistance, _resistanceCurve));
        _currentSpeed += _acceleration * speedFactor * _speedMultiplier * Time.fixedDeltaTime;
    }

    private void TurnToward(Vector3 targetDirection)
    {
        float angleToTarget = Vector3.SignedAngle(transform.forward, targetDirection, Vector3.up);
        float turnInput = Mathf.Clamp(angleToTarget / (_turnSpeed * Time.fixedDeltaTime), -1f, 1f);
        Turn(turnInput);
    }

    private void DecideAcceleration(Vector3 targetDirection)
    {
        float angleToTarget = Vector3.Angle(transform.forward, targetDirection);

        if (angleToTarget < _brakeAngle)
            Accelerate();
        else
            Decelerate();
    }
}