using UnityEngine;

/// <summary>
/// 車の共通処理（移動・浮遊・衝撃・下位ブースト）
/// CarControllerとAIControllerがこれを継承する
/// </summary>
public abstract class CarBase : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] protected RaceManager _raceManager;

    [Header("移動速度のパラメータ")]
    [Tooltip("加速の基本値")]
    [SerializeField] protected float _acceleration = 5f;
    [Tooltip("速度に応じた抵抗の強さ")]
    [SerializeField] protected float _speedResistance = 0.05f;
    [Tooltip("抵抗の曲線の形状")]
    [SerializeField] protected float _resistanceCurve = 1.0f;
    [Tooltip("減速の基本値")]
    [SerializeField] protected float _reverseAcceleration = 2f;
    [Tooltip("自然減速の基本値")]
    [SerializeField] protected float _deceleration = 10f;
    [Tooltip("ブレーキの基本値")]
    [SerializeField] protected float _brakeForce = 15f;
    [Tooltip("最大逆走速度")]
    [SerializeField] protected float _maxReverseSpeed = 5f;
    [Header("旋回設定")]
    [SerializeField] protected float _turnSpeed = 100f;

    [Header("横移動設定")]
    [SerializeField] protected float _strafeSpeed = 10f;

    [Header("浮遊設定")]
    [SerializeField] private float _hoverHeight = 0.3f;
    [SerializeField] private float _hoverForce = 50f;

    [Header("衝撃設定")]
    [SerializeField] private float _externalVelocityDecay = 5f;

    [Header("下位ブースト設定")]
    [Tooltip("ブーストが適用される最低順位")]
    [SerializeField] private int _boostStartRank = 4;
    [Tooltip("1順位ごとのブースト倍率の増加量")]
    [SerializeField] private float _boostPerRank = 0.1f;

    protected Rigidbody _rb;
    protected float _currentSpeed = 0f;
    protected float _strafeInput = 0f;
    private Vector3 _externalVelocity = Vector3.zero;
    private float _boostMultiplier = 1f;
    private bool _canMove = false;

    protected virtual void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }

    protected virtual void FixedUpdate()
    {
        if (!_canMove)
        {
            ApplyHoverForce();
            return;
        }

        UpdateBoost();
        UpdateMovement();
    }

    /// <summary>
    /// 下位ブースト倍率を更新
    /// 4位=1.1倍、5位=1.2倍、6位=1.3倍...
    /// </summary>
    private void UpdateBoost()
    {
        if (_raceManager == null)
        {
            _boostMultiplier = 1f;
            return;
        }

        int rank = _raceManager.GetRank(gameObject);

        if (rank >= _boostStartRank)
        {
            _boostMultiplier = 1f + (rank - _boostStartRank + 1) * _boostPerRank;
        }
        else
        {
            _boostMultiplier = 1f;
        }
    }

    protected void Accelerate()
    {
        if (_currentSpeed < 0)
        {
            _currentSpeed += _brakeForce * Time.fixedDeltaTime;
            _currentSpeed = Mathf.Min(_currentSpeed, 0);
        }
        else
        {
            float speedFactor = 1f / (1f + Mathf.Pow(_currentSpeed * _speedResistance, _resistanceCurve));
            _currentSpeed += _acceleration * speedFactor * _boostMultiplier * Time.fixedDeltaTime;
        }
    }

    protected void Brake()
    {
        if (_currentSpeed > 0)
        {
            _currentSpeed -= _brakeForce * Time.fixedDeltaTime;
            _currentSpeed = Mathf.Max(_currentSpeed, 0);
        }
        else
        {
            _currentSpeed -= _reverseAcceleration * Time.fixedDeltaTime;
            _currentSpeed = Mathf.Max(_currentSpeed, -_maxReverseSpeed);
        }
    }

    protected void Decelerate()
    {
        if (_currentSpeed > 0)
        {
            _currentSpeed -= _deceleration * Time.fixedDeltaTime;
            _currentSpeed = Mathf.Max(_currentSpeed, 0);
        }
        else if (_currentSpeed < 0)
        {
            _currentSpeed += _deceleration * Time.fixedDeltaTime;
            _currentSpeed = Mathf.Min(_currentSpeed, 0);
        }
    }

    protected void Turn(float input)
    {
        if (Mathf.Abs(input) > 0.01f)
        {
            float turn = input * _turnSpeed * Time.fixedDeltaTime;
            transform.Rotate(0, turn, 0);
        }
    }

    private void UpdateMovement()
    {
        Vector3 newVelocity = transform.forward * _currentSpeed;
        newVelocity += transform.right * _strafeInput * _strafeSpeed;
        newVelocity += _externalVelocity;
        newVelocity.y = _rb.linearVelocity.y;
        _rb.linearVelocity = newVelocity;

        _externalVelocity = Vector3.MoveTowards(_externalVelocity, Vector3.zero, _externalVelocityDecay * Time.fixedDeltaTime);

        ApplyHoverForce();
    }

    private void ApplyHoverForce()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, -Vector3.up, out hit, 2f))
        {
            float heightDifference = _hoverHeight - hit.distance;
            _rb.AddForce(Vector3.up * heightDifference * _hoverForce);
        }
    }

    public void AddExternalVelocity(Vector3 velocity)
    {
        _externalVelocity += velocity;
    }

    public void ResetSpeed()
    {
        _currentSpeed = 0f;
        _externalVelocity = Vector3.zero;
    }

    public float GetSpeed()
    {
        return _currentSpeed;
    }

    public float GetBoostMultiplier()
    {
        return _boostMultiplier;
    }

    public void SetCanMove(bool canMove)
    {
        _canMove = canMove;
    }

    public bool CanMove()
    {
        return _canMove;
    }
}