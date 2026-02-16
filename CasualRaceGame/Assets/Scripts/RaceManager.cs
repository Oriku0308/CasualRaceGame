using UnityEngine;
using UniRx;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class RaceManager : MonoBehaviour
{
    [Header("チェックポイント")]
    [SerializeField] private List<Transform> _checkpoints = new List<Transform>();

    [Header("周回設定")]
    [SerializeField] private int _totalLaps = 1;

    [Header("レース設定")]
    [SerializeField] private float _countdownDuration = 3f;
    [SerializeField] private float _raceEndDelay = 10f; // 1位ゴール後の制限時間

    private Dictionary<GameObject, CarProgress> _progressMap = new Dictionary<GameObject, CarProgress>();
    private List<GameObject> _rankings = new List<GameObject>();
    private List<GameObject> _allCars = new List<GameObject>();

    private int _countdownValue = 0; // 現在のカウントダウン値（0=カウントダウン終了）
    private float _raceEndTimer = 0f;
    private int _goalCount = 0;
    private bool _raceStarted = false;
    private bool _raceEnded = false;


    public class CarProgress
    {
        public int CurrentCheckpointIndex;
        public int CurrentLap;
        public bool HasGoaled;
        public int GoalOrder;

        public CarProgress()
        {
            CurrentCheckpointIndex = 0;
            CurrentLap = 0;
            HasGoaled = false;
            GoalOrder = 0;
        }
    }

    void Start()
    {
        RegisterAllCars();

        GameEvents.OnCheckpointPassed
            .Subscribe(data => OnCheckpointPassedHandler(data.car, data.checkpointIndex))
            .AddTo(this);

        GameEvents.OnCarGoaled
            .Subscribe(car => OnCarGoaledHandler(car))
            .AddTo(this);

        // カウントダウン開始
        StartCoroutine(CountdownCoroutine());
    }

    void Update()
    {
        if (_raceStarted && !_raceEnded)
        {
            UpdateRankings();
        }
    }

    private void RegisterAllCars()
    {
        GameObject[] cars = GameObject.FindGameObjectsWithTag("Car");
        foreach (var car in cars)
        {
            _progressMap[car] = new CarProgress();
            _allCars.Add(car);

            // 開始まで操作不可
            CarBase carBase = car.GetComponent<CarBase>();
            if (carBase != null) carBase.SetCanMove(false);
        }
        Debug.Log($"[RaceManager] {_progressMap.Count}台の車を登録, チェックポイント{_checkpoints.Count}個");
    }

    // レース開始

    private IEnumerator CountdownCoroutine()
    {
        float timer = _countdownDuration;

        while (timer > 0)
        {
            _countdownValue = Mathf.CeilToInt(timer);
            Debug.Log($"[RaceManager] {_countdownValue}...");
            yield return new WaitForSeconds(1f);
            timer -= 1f;
        }

        _countdownValue = 0;
        Debug.Log("[RaceManager] GO!");
        StartRace();
    }

    private void StartRace()
    {
        _raceStarted = true;

        // 全車の操作を有効化
        foreach (var car in _allCars)
        {
            CarBase carBase = car.GetComponent<CarBase>();
            if (carBase != null) carBase.SetCanMove(true);
        }

        GameEvents.OnRaceStarted.OnNext(Unit.Default);
    }

    // レース終了

    private void OnCarGoaledHandler(GameObject car)
    {
        // 最初のゴールで終了タイマー開始
        if (_goalCount == 1 && !_raceEnded)
        {
            StartCoroutine(RaceEndCoroutine());
        }
    }

    private IEnumerator RaceEndCoroutine()
    {
        Debug.Log($"[RaceManager] 1位ゴール！残り{_raceEndDelay}秒で終了");

        _raceEndTimer = _raceEndDelay;
        while (_raceEndTimer > 0)
        {
            _raceEndTimer -= Time.deltaTime;
            yield return null;
        }

        EndRace();
    }

    private void EndRace()
    {
        if (_raceEnded) return;
        _raceEnded = true;

        // 未ゴール車も順位確定
        UpdateRankings();

        // 全車の操作を無効化
        foreach (var car in _allCars)
        {
            CarBase carBase = car.GetComponent<CarBase>();
            if (carBase != null) carBase.SetCanMove(false);
        }

        GameEvents.OnRaceEnded.OnNext(Unit.Default);
        Debug.Log("[RaceManager] レース終了！");
    }

    // チェックポイント・順位

    private void OnCheckpointPassedHandler(GameObject car, int checkpointIndex)
    {
        if (!_raceStarted || _raceEnded) return;
        if (!_progressMap.ContainsKey(car)) return;

        var progress = _progressMap[car];
        if (progress.HasGoaled) return;
        if (checkpointIndex != progress.CurrentCheckpointIndex) return;

        progress.CurrentCheckpointIndex++;

        if (progress.CurrentCheckpointIndex >= _checkpoints.Count)
        {
            progress.CurrentLap++;
            if (progress.CurrentLap >= _totalLaps)
            {
                _goalCount++;
                progress.HasGoaled = true;
                progress.GoalOrder = _goalCount;
                GameEvents.OnCarGoaled.OnNext(car);
                Debug.Log($"[RaceManager] {car.name} が{_goalCount}着でゴール！");
            }
            else
            {
                progress.CurrentCheckpointIndex = 0;
            }
        }
    }

    private void UpdateRankings()
    {
        _rankings = _progressMap
            .OrderByDescending(kvp => GetSortScore(kvp.Key, kvp.Value))
            .Select(kvp => kvp.Key)
            .ToList();
    }

    private float GetSortScore(GameObject car, CarProgress progress)
    {
        if (progress.HasGoaled)
        {
            return 1000000f - progress.GoalOrder;
        }

        float score = progress.CurrentLap * 10000f;
        score += progress.CurrentCheckpointIndex * 100f;

        if (progress.CurrentCheckpointIndex < _checkpoints.Count)
        {
            Transform nextCP = _checkpoints[progress.CurrentCheckpointIndex];
            float distance = Vector3.Distance(car.transform.position, nextCP.position);
            score += Mathf.Clamp(99f - distance, 0f, 99f);
        }

        return score;
    }

    // 外部参照用
    public int GetCountdownValue() => _countdownValue;

    public float GetRaceEndTimer() => _raceEndTimer;

    public bool IsRaceStarted() => _raceStarted;
    public bool IsRaceEnded() => _raceEnded;

    public int GetRank(GameObject car)
    {
        int index = _rankings.IndexOf(car);
        return index >= 0 ? index + 1 : _progressMap.Count;
    }

    public List<GameObject> GetRankings() => _rankings;
    public int GetCarCount() => _progressMap.Count;
    public List<Transform> GetCheckpoints() => _checkpoints;

    public Transform GetNextCheckpoint(GameObject car)
    {
        if (!_progressMap.ContainsKey(car)) return null;
        var progress = _progressMap[car];
        if (progress.HasGoaled) return null;
        if (progress.CurrentCheckpointIndex >= _checkpoints.Count) return null;
        return _checkpoints[progress.CurrentCheckpointIndex];
    }

    public CarProgress GetProgress(GameObject car)
    {
        return _progressMap.ContainsKey(car) ? _progressMap[car] : null;
    }

    public int GetCheckpointCount() => _checkpoints.Count;
}