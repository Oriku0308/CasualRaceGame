using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("移動速度のパラメータ")]
    [Tooltip("前進加速度")]
    [SerializeField] private float _acceleration = 5f;
    [Tooltip("加速が鈍る割合")]
    [SerializeField] private float _speedResistance = 0.05f;
    [Tooltip("速度抵抗の曲線の強さ（0.1=緩やか、2.0=急激）")]
    [SerializeField] private float _resistanceCurve = 1.0f;
    [Tooltip("後退加速度")]
    [SerializeField] private float _reverseAcceleration = 2f;
    [Tooltip("自然減速")]
    [SerializeField] private float _deceleration = 10f;
    [Tooltip("ブレーキ力")]
    [SerializeField] private float _brakeForce = 15f;
    [Tooltip("最大後退速度")]
    [SerializeField] private float _maxReverseSpeed = 5f;
    [Tooltip("回転速度")]
    [SerializeField] private float _turnSpeed = 100f;

    [Header("浮遊設定")]
    [SerializeField] private float _hoverHeight = 0.3f;  // 地面からの高さ
    [SerializeField] private float _hoverForce = 50f;    // 浮遊力


    private Rigidbody _rb;
    private float _currentSpeed = 0f;
    private float _inputVertical;
    private float _inputHorizontal;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        GetInput();
    }

    void FixedUpdate()
    {
        Move();
        Turn();
        ApplyHoverForce();
    }

    private void GetInput()
    {
        _inputVertical = Input.GetAxisRaw("Vertical");    // W/S
        _inputHorizontal = Input.GetAxisRaw("Horizontal"); // A/D
    }

    private void Move()
    {
        if (_inputVertical > 0)  // W: 前進
        {
            if (_currentSpeed < 0)
            {
                // 後退中ならブレーキ
                _currentSpeed += _brakeForce * Time.fixedDeltaTime;
                _currentSpeed = Mathf.Min(_currentSpeed, 0);
            }
            else
            {
                // 前進加速（速度に応じて減衰）
                float speedFactor = 1f / (1f + Mathf.Pow(_currentSpeed * _speedResistance, _resistanceCurve));
                _currentSpeed += _acceleration * speedFactor * Time.fixedDeltaTime;
            }
        }
        else if (_inputVertical < 0)  // S: ブレーキ/後退
        {
            if (_currentSpeed > 0)
            {
                // 前進中ならブレーキ
                _currentSpeed -= _brakeForce * Time.fixedDeltaTime;
                _currentSpeed = Mathf.Max(_currentSpeed, 0);
            }
            else
            {
                // 後退加速（後退は上限あり）
                _currentSpeed -= _reverseAcceleration * Time.fixedDeltaTime;
                _currentSpeed = Mathf.Max(_currentSpeed, -_maxReverseSpeed);
            }
        }
        else  // 何も押してない: 自然減速
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

        Vector3 newVelocity = transform.forward * _currentSpeed;
        newVelocity.y = _rb.linearVelocity.y;  // Y速度は保持
        _rb.linearVelocity = newVelocity;
    }

    private void Turn()
    {
        if (Mathf.Abs(_inputHorizontal) > 0.01f)
        {
            float turn = _inputHorizontal * _turnSpeed * Time.fixedDeltaTime;
            transform.Rotate(0, turn, 0);
        }
    }

    private void ApplyHoverForce()
    {
        RaycastHit hit;
        // 下方向にRayを飛ばして地面との距離を測る
        if (Physics.Raycast(transform.position, -Vector3.up, out hit, 2f))
        { 
            // 目標高さとの差分
            float currentHeight = hit.distance;
            float heightDifference = _hoverHeight - currentHeight;

            // 高さを維持する力を加える
            _rb.AddForce(Vector3.up * heightDifference * _hoverForce);
        }
    }

    public float GetSpeed()
    {
        return _currentSpeed;
    }
}