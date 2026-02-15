using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform _target;  // PlayerCar
    [SerializeField] private Vector3 _offset = new Vector3(0, 5, -10);  // カメラの相対位置
    [SerializeField] private float _followSpeed = 5f;  // 位置追従の滑らかさ
    [SerializeField] private float _rotationSpeed = 5f;  // 回転追従の滑らかさ

    void LateUpdate()
    {
        if (_target == null) return;

        FollowTarget();
        RotateToTarget();
    }

    private void FollowTarget()
    {
        // 車の向きに基づいたオフセット位置を計算
        Vector3 targetPosition = _target.position + _target.TransformDirection(_offset);

        // 滑らかに移動
        transform.position = Vector3.Lerp(transform.position, targetPosition, _followSpeed * Time.deltaTime);
    }

    private void RotateToTarget()
    {
        // 車の向きに基づいた回転を計算
        Quaternion targetRotation = Quaternion.LookRotation(_target.position - transform.position + Vector3.up * _offset.y);

        // 滑らかに回転
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
    }
}