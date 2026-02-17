using UnityEngine;
using UniRx;

/// <summary>
/// ゲーム内で発生する各種イベントを定義
/// </summary>
public static class GameEvents
{
    /// <summary>
    /// 1位が変わったときのイベント
    /// </summary>
    public static Subject<GameObject> OnFirstPlaceChanged = new Subject<GameObject>();

    /// <summary>
    /// 車が落下したときのイベント
    /// </summary>
    public static Subject<GameObject> OnCarFell = new Subject<GameObject>();

    /// <summary>
    /// 車同士が衝突したときのイベント
    /// (attacker: 衝突を与えた車, victim: 衝突を受けた車)
    /// </summary>
    public static Subject<(GameObject attacker, GameObject victim)> OnCarCollision =
        new Subject<(GameObject, GameObject)>();

    /// <summary>
    /// チェックポイントを通過したときのイベント
    /// (car: 通過した車, checkpointIndex: チェックポイント番号)
    /// </summary>
    public static Subject<(GameObject car, int checkpointIndex)> OnCheckpointPassed =
        new Subject<(GameObject, int)>();

    /// <summary>
    /// ゴールしたときのイベント
    /// </summary>
    public static Subject<GameObject> OnCarGoaled = new Subject<GameObject>();

    /// <summary>
    /// レースが開始したときのイベント
    /// </summary>
    public static Subject<Unit> OnRaceStarted = new Subject<Unit>();

    /// <summary>
    /// レースが終了したときのイベント
    /// </summary>
    public static Subject<Unit> OnRaceEnded = new Subject<Unit>();
}