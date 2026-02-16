using UnityEngine;

/// <summary>
/// 車同士の衝突処理
/// </summary>
public class CarCollision : MonoBehaviour
{
    // TODO: 仮でSerializeで済ませているが、多分シングルトン的設計する
    [SerializeField] private RespawnManager _respawnManager;

    [Header("衝突設定")]
    [Tooltip("衝突時の基本吹っ飛ばし力")]
    [SerializeField] private float _pushForce = 15f;
    [Tooltip("衝突時の最低吹っ飛ばし力")]
    [SerializeField] private float _minPushForce = 5f;  // 最低吹っ飛ばし力
    [Header("衝突管理")]
    [SerializeField] private float _lastAttackerDuration = 3f; // 最終衝突者の有効時間
    [Tooltip("連続衝突防止のクールダウン時間")]
    [SerializeField] private float _collisionCooldown = 0.1f; // 連続衝突防止のクールダウン時間

    private Rigidbody _rb;
 

    // 衝突回数
    private int _collisionCount = 0;

    // 最終衝突者（落下させた判定用）
    private GameObject _lastAttacker = null;
    private float _lastAttackerTimer = 0f;
    private float _lastCollisionTime = 0f;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        UpdateLastAttackerTimer();
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Car"))
        {
            GameObject other = collision.gameObject;
            Rigidbody otherRb = other.GetComponent<Rigidbody>();

            if (_respawnManager.IsInvincible(other)) return;
            if (_respawnManager.IsInvincible(gameObject)) return;
            if (Time.time - _lastCollisionTime < _collisionCooldown) return;

            // Unityが提供する衝突時の相対速度を使う
            float impactSpeed = (_rb.linearVelocity - otherRb.linearVelocity).magnitude;
            Debug.Log($"[衝突] {gameObject.name} impactSpeed: {impactSpeed:F2}");

            // 衝撃が弱すぎたら無視
            if (impactSpeed < 1f) return;

            // 速度が高い方だけが処理する（二重処理防止）
            float mySpeed = _rb.linearVelocity.magnitude;
            float otherSpeed = otherRb.linearVelocity.magnitude;
            if (mySpeed < otherSpeed - 0.1f) return;
            if (Mathf.Abs(mySpeed - otherSpeed) <= 0.1f && gameObject.GetInstanceID() < other.GetInstanceID()) return;

            _lastCollisionTime = Time.time;

            CarCollision otherCollision = other.GetComponent<CarCollision>();
            if (otherCollision != null) otherCollision.SetCollisionCooldown();

            // 吹っ飛ばし方向：自分から相手への方向
            Vector3 pushDirection = (other.transform.position - transform.position).normalized;
            Vector3 flatPushDirection = new Vector3(pushDirection.x, 0, pushDirection.z).normalized;

            ApplyPushToOther(other, impactSpeed);
            ApplyPushToSelf(other, impactSpeed);
            RecordCollision(other);
        }
    }

    /// <summary>
    /// 外部からクールダウンを設定（相手側から呼ばれる）
    /// </summary>
    public void SetCollisionCooldown()
    {
        _lastCollisionTime = Time.time;
    }

    private void ApplyPushToOther(GameObject other, float impactSpeed)
    {
        Vector3 myDirection = _rb.linearVelocity.normalized;
        Vector3 flatDirection = new Vector3(myDirection.x, 0, myDirection.z).normalized;

        if (flatDirection.sqrMagnitude < 0.01f) return;

        float pushPower = Mathf.Max(impactSpeed * _pushForce, _minPushForce);

        other.transform.rotation = Quaternion.LookRotation(flatDirection);

        CarBase otherCar = other.GetComponent<CarBase>();
        if (otherCar != null) otherCar.AddExternalVelocity(flatDirection * pushPower);
    }

    private void ApplyPushToSelf(GameObject other, float impactSpeed)
    {
        Rigidbody otherRb = other.GetComponent<Rigidbody>();
        Vector3 otherDirection = otherRb.linearVelocity.normalized;
        Vector3 flatDirection = new Vector3(otherDirection.x, 0, otherDirection.z).normalized;

        if (flatDirection.sqrMagnitude < 0.01f) return;

        float bouncePower = Mathf.Max(impactSpeed * _pushForce * 0.3f, _minPushForce * 0.3f);

        transform.rotation = Quaternion.LookRotation(flatDirection);

        CarBase selfCar = GetComponent<CarBase>();
        if (selfCar != null) selfCar.AddExternalVelocity(flatDirection * bouncePower);
    }

    private void RecordCollision(GameObject victim)
    {
        // 自分の衝突回数カウント
        _collisionCount++;

        // 相手に「最終衝突者」として自分を記録
        CarCollision victimCollision = victim.GetComponent<CarCollision>();
        if (victimCollision != null)
        {
            victimCollision.SetLastAttacker(gameObject);
        }

        // イベント発行
        GameEvents.OnCarCollision.OnNext((gameObject, victim));
    }

    /// <summary>
    /// 最終衝突者を設定
    /// </summary>
    public void SetLastAttacker(GameObject attacker)
    {
        _lastAttacker = attacker;
        _lastAttackerTimer = _lastAttackerDuration;
    }

    /// <summary>
    /// 最終衝突者タイマー更新
    /// </summary>
    private void UpdateLastAttackerTimer()
    {
        if (_lastAttackerTimer > 0)
        {
            _lastAttackerTimer -= Time.fixedDeltaTime;

            if (_lastAttackerTimer <= 0)
            {
                ClearLastAttacker();
            }
        }
    }

    /// <summary>
    /// 最終衝突者クリア
    /// </summary>
    private void ClearLastAttacker()
    {
        _lastAttacker = null;
        _lastAttackerTimer = 0f;
    }

    /// <summary>
    /// 最終衝突者取得
    /// </summary>
    public GameObject GetLastAttacker()
    {
        return _lastAttacker;
    }

    /// <summary>
    /// 衝突回数取得
    /// </summary>
    public int GetCollisionCount()
    {
        return _collisionCount;
    }
}