using UnityEngine;
using UniRx;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// スコア管理
/// 各車の5項目のスコアを記録・集計する
/// </summary>
public class ScoreManager : MonoBehaviour
{
    /// <summary>
    /// 各車のスコアデータ
    /// </summary>
    [Serializable]
    public class CarScoreData
    {
        public GameObject Car;
        public int CollisionCount;      // 衝突回数
        public int FellCount;           // 落下回数
        public int KnockOffCount;       // 落下させた回数
        public int GoalRank;            // ゴール順位（0=未ゴール）
        public float FirstPlaceTime;    // 1位維持時間

        public CarScoreData(GameObject car)
        {
            Car = car;
        }
    }

    // 全車のスコアデータ
    private Dictionary<GameObject, CarScoreData> _scoreDataMap = new Dictionary<GameObject, CarScoreData>();

    private CompositeDisposable _disposables = new CompositeDisposable();

    void Start()
    {
        // シーン内の全車を登録
        RegisterAllCars();

        // イベント購読
        GameEvents.OnCarFell
            .Subscribe(car => OnCarFellHandler(car))
            .AddTo(_disposables);

        GameEvents.OnCarCollision
            .Subscribe(data => OnCarCollisionHandler(data.attacker, data.victim))
            .AddTo(_disposables);

        // TODO: RaceManager実装時に追加
        // GameEvents.OnCarGoaled.Subscribe(...)
        // 1位維持時間の計測
    }

    void OnDestroy()
    {
        _disposables.Dispose();
    }

    /// <summary>
    /// シーン内の"Car"タグを持つ全オブジェクトを登録
    /// </summary>
    private void RegisterAllCars()
    {
        GameObject[] cars = GameObject.FindGameObjectsWithTag("Car");

        foreach (var car in cars)
        {
            _scoreDataMap[car] = new CarScoreData(car);
        }

        Debug.Log($"[ScoreManager] {_scoreDataMap.Count}台の車を登録");
    }

    // イベントハンドラ
    private void OnCarCollisionHandler(GameObject attacker, GameObject victim)
    {
        if (!_scoreDataMap.ContainsKey(attacker)) return;

        _scoreDataMap[attacker].CollisionCount++;

        Debug.Log($"[ScoreManager] {attacker.name} の衝突回数: {_scoreDataMap[attacker].CollisionCount}");
    }

    private void OnCarFellHandler(GameObject car)
    {
        if (!_scoreDataMap.ContainsKey(car)) return;

        // 落下回数を記録
        _scoreDataMap[car].FellCount++;

        // 落下させた判定（最終衝突者チェック）
        CarCollision carCollision = car.GetComponent<CarCollision>();
        if (carCollision != null)
        {
            GameObject attacker = carCollision.GetLastAttacker();
            if (attacker != null && _scoreDataMap.ContainsKey(attacker))
            {
                _scoreDataMap[attacker].KnockOffCount++;
                Debug.Log($"[ScoreManager] {attacker.name} が {car.name} を落下させた！ (計{_scoreDataMap[attacker].KnockOffCount}回)");
            }
        }

        Debug.Log($"[ScoreManager] {car.name} の落下回数: {_scoreDataMap[car].FellCount}");
    }

    // スコア取得（UI・結果画面用）

    /// <summary>
    /// 指定した車のスコアデータを取得
    /// </summary>
    public CarScoreData GetScoreData(GameObject car)
    {
        return _scoreDataMap.ContainsKey(car) ? _scoreDataMap[car] : null;
    }

    /// <summary>
    /// 全車のスコアデータを取得
    /// </summary>
    public Dictionary<GameObject, CarScoreData> GetAllScoreData()
    {
        return _scoreDataMap;
    }

    // TODO: ポイント計算（現在は仮実装）

    /// <summary>
    /// 各項目の1位を判定し、総合ポイントを計算
    /// </summary>
    public Dictionary<GameObject, int> CalculateFinalPoints()
    {
        var points = new Dictionary<GameObject, int>();

        foreach (var car in _scoreDataMap.Keys)
        {
            points[car] = 0;
        }

        // 衝突回数1位
        AwardPointToTopScorers(points, data => data.CollisionCount);

        // 落下させた回数1位
        AwardPointToTopScorers(points, data => data.KnockOffCount);

        // 落下回数1位
        AwardPointToTopScorers(points, data => data.FellCount);

        // TODO: ゴール順位1位（GoalRank == 1）
        // TODO: 1位維持時間1位

        return points;
    }

    /// <summary>
    /// 指定項目の最大値を持つ車にポイントを付与（同率対応）
    /// </summary>
    private void AwardPointToTopScorers(Dictionary<GameObject, int> points, Func<CarScoreData, int> selector)
    {
        if (_scoreDataMap.Count == 0) return;

        int maxValue = _scoreDataMap.Values.Max(selector);

        // 全員0なら誰にもポイントなし
        if (maxValue <= 0) return;

        foreach (var kvp in _scoreDataMap)
        {
            if (selector(kvp.Value) == maxValue)
            {
                points[kvp.Key]++;
            }
        }
    }
}