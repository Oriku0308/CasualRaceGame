using UnityEngine;
using UniRx;
using System;
using System.Collections.Generic;
using System.Linq;

public class ScoreManager : MonoBehaviour
{
    [Serializable]
    public class CarScoreData
    {
        public GameObject Car;
        public int CollisionCount;
        public int FellCount;
        public int KnockOffCount;
        public int GoalRank;
        public float FirstPlaceTime;

        public CarScoreData(GameObject car)
        {
            Car = car;
        }
    }

    private Dictionary<GameObject, CarScoreData> _scoreDataMap = new Dictionary<GameObject, CarScoreData>();
    private CompositeDisposable _disposables = new CompositeDisposable();

    // 1位維持時間の計測用
    private GameObject _currentFirstPlace = null;
    private bool _raceActive = false;

    void Start()
    {
        RegisterAllCars();

        GameEvents.OnCarFell
            .Subscribe(car => OnCarFellHandler(car))
            .AddTo(_disposables);

        GameEvents.OnCarCollision
            .Subscribe(data => OnCarCollisionHandler(data.attacker, data.victim))
            .AddTo(_disposables);

        GameEvents.OnCarGoaled
            .Subscribe(car => OnCarGoaledHandler(car))
            .AddTo(_disposables);

        GameEvents.OnFirstPlaceChanged
            .Subscribe(car => OnFirstPlaceChangedHandler(car))
            .AddTo(_disposables);

        GameEvents.OnRaceStarted
            .Subscribe(_ => _raceActive = true)
            .AddTo(_disposables);

        GameEvents.OnRaceEnded
            .Subscribe(_ => _raceActive = false)
            .AddTo(_disposables);
    }

    void Update()
    {
        UpdateFirstPlaceTime();
    }

    void OnDestroy()
    {
        _disposables.Dispose();
    }

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
    }

    private void OnCarFellHandler(GameObject car)
    {
        if (!_scoreDataMap.ContainsKey(car)) return;

        _scoreDataMap[car].FellCount++;

        CarCollision carCollision = car.GetComponent<CarCollision>();
        if (carCollision != null)
        {
            GameObject attacker = carCollision.GetLastAttacker();
            if (attacker != null && _scoreDataMap.ContainsKey(attacker))
            {
                _scoreDataMap[attacker].KnockOffCount++;
            }
        }
    }

    private void OnCarGoaledHandler(GameObject car)
    {
        if (!_scoreDataMap.ContainsKey(car)) return;

        // RaceManagerのGoalOrderを取得
        RaceManager raceManager = GetComponent<RaceManager>();
        if (raceManager != null)
        {
            var progress = raceManager.GetProgress(car);
            if (progress != null)
            {
                _scoreDataMap[car].GoalRank = progress.GoalOrder;
            }
        }
    }

    private void OnFirstPlaceChangedHandler(GameObject car)
    {
        _currentFirstPlace = car;
    }

    // 1位維持時間
    private void UpdateFirstPlaceTime()
    {
        if (!_raceActive) return;
        if (_currentFirstPlace == null) return;
        if (!_scoreDataMap.ContainsKey(_currentFirstPlace)) return;

        _scoreDataMap[_currentFirstPlace].FirstPlaceTime += Time.deltaTime;
    }

    // スコア取得
    public CarScoreData GetScoreData(GameObject car)
    {
        return _scoreDataMap.ContainsKey(car) ? _scoreDataMap[car] : null;
    }

    public Dictionary<GameObject, CarScoreData> GetAllScoreData()
    {
        return _scoreDataMap;
    }

    // ポイント計算

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

        // ゴール順位1位
        AwardGoalPointToFirst(points);

        // 1位維持時間1位
        AwardPointToTopFloat(points, data => data.FirstPlaceTime);

        return points;
    }

    private void AwardPointToTopScorers(Dictionary<GameObject, int> points, Func<CarScoreData, int> selector)
    {
        if (_scoreDataMap.Count == 0) return;

        int maxValue = _scoreDataMap.Values.Max(selector);
        if (maxValue <= 0) return;

        foreach (var kvp in _scoreDataMap)
        {
            if (selector(kvp.Value) == maxValue)
            {
                points[kvp.Key]++;
            }
        }
    }

    private void AwardPointToTopFloat(Dictionary<GameObject, int> points, Func<CarScoreData, float> selector)
    {
        if (_scoreDataMap.Count == 0) return;

        float maxValue = _scoreDataMap.Values.Max(selector);
        if (maxValue <= 0f) return;

        foreach (var kvp in _scoreDataMap)
        {
            if (Mathf.Approximately(selector(kvp.Value), maxValue))
            {
                points[kvp.Key]++;
            }
        }
    }

    private void AwardGoalPointToFirst(Dictionary<GameObject, int> points)
    {
        foreach (var kvp in _scoreDataMap)
        {
            if (kvp.Value.GoalRank == 1)
            {
                points[kvp.Key]++;
            }
        }
    }
}