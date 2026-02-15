using UnityEngine;
using UniRx;
using System;

/// <summary>
/// スコア管理（テスト版）
/// </summary>
public class ScoreManager : MonoBehaviour
{
    private CompositeDisposable _disposables = new CompositeDisposable();

    void Start()
    {
        // 落下イベント購読
        GameEvents.OnCarFell
            .Subscribe(car => OnCarFellHandler(car))
            .AddTo(_disposables);

        // 衝突イベント購読
        GameEvents.OnCarCollision
            .Subscribe(collision => OnCarCollisionHandler(collision.attacker, collision.victim))
            .AddTo(_disposables);
    }

    void OnDestroy()
    {
        _disposables.Dispose();
    }

    private void OnCarFellHandler(GameObject car)
    {
        Debug.Log($"[ScoreManager] {car.name} が落下しました");
        // TODO: 落下回数記録
    }

    private void OnCarCollisionHandler(GameObject attacker, GameObject victim)
    {
        Debug.Log($"[ScoreManager] {attacker.name} が {victim.name} に衝突");
        // TODO: 衝突回数記録
    }
}