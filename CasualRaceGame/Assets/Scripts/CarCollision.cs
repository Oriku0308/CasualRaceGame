using UnityEngine;

/// <summary>
/// 車同士の衝突処理
/// </summary>
public class CarCollision : MonoBehaviour
{
    // TODO: 仮でSerializeで済ませているが、多分シングルトン的設計する
    [SerializeField] private RespawnManager _respawnManager;

    [Header("衝突設定")]
    [SerializeField] private float _pushForce = 15f;
    [SerializeField] private float _lastAttackerDuration = 3f; // 最終衝突者の有効時間
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

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Car"))
        {
            GameObject other = collision.gameObject;
            Rigidbody otherRb = other.GetComponent<Rigidbody>();

            // 無敵チェック
            if (_respawnManager.IsInvincible(other)) return;

            // クールダウンチェック
            if (Time.time - _lastCollisionTime < _collisionCooldown)
            {
                return;
            }

            // 相対速度で判定
            Vector3 relativeVelocity = _rb.linearVelocity - otherRb.linearVelocity;
            Vector3 collisionDirection = (other.transform.position - transform.position).normalized;
            float approachSpeed = Vector3.Dot(relativeVelocity, collisionDirection);

            // 自分が近づいてない（離れてる or 相手が近づいてる）なら処理しない
            if (approachSpeed <= 0)
            {
                return;
            }

            _lastCollisionTime = Time.time;

            // 衝突点を取得
            Vector3 collisionPoint = collision.contacts[0].point;

            // 相手が吹っ飛ぶ方向：衝突点から相手の中心へ
            Vector3 pushDirection = (other.transform.position - collisionPoint).normalized;

            // 相手を吹っ飛ばす
            ApplyPushToOther(other, pushDirection);

            // 自分も吹っ飛ぶ
            ApplyPushToSelf(other);

            // 衝突記録
            RecordCollision(other);
        }
    }

    /// <summary>
    /// 相手を吹っ飛ばす
    /// </summary>
    private void ApplyPushToOther(GameObject other, Vector3 pushDirection)
    {
        Rigidbody otherRb = other.GetComponent<Rigidbody>();

        // 速度加算
        otherRb.linearVelocity += pushDirection * _pushForce;

        // 向き変更（Y軸のみ）
        Vector3 flatPushDirection = new Vector3(pushDirection.x, 0, pushDirection.z).normalized;
        other.transform.rotation = Quaternion.LookRotation(flatPushDirection);
    }

    /// <summary>
    /// 自分も吹っ飛ぶ
    /// </summary>
    private void ApplyPushToSelf(GameObject other)
    {
        Rigidbody otherRb = other.GetComponent<Rigidbody>();
        float otherSpeed = otherRb.linearVelocity.magnitude;

        // 前方向に加速（相手の速度の0.2倍）
        _rb.linearVelocity += transform.forward * (otherSpeed * 0.2f);

        // 相手が停止中なら向き変更しない
        if (otherSpeed < 0.1f)
        {
            return;
        }

        // 向き変更：相手の進行方向（速度ベクトル）を向く
        Vector3 otherDirection = otherRb.linearVelocity.normalized;

        // Y軸回転のみ
        Vector3 flatOtherDirection = new Vector3(otherDirection.x, 0, otherDirection.z).normalized;
        transform.rotation = Quaternion.LookRotation(flatOtherDirection);
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