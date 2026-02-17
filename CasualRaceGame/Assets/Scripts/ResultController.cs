using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// リザルト画面の表示
/// </summary>
public class ResultController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private ScoreManager _scoreManager;
    [SerializeField] private RaceManager _raceManager;

    [Header("UI要素")]
    [SerializeField] private GameObject _resultPanel;
    [SerializeField] private Transform _resultContent; // 各車の行を並べる親オブジェクト
    [SerializeField] private GameObject _resultRowPrefab; // 1行分のプレハブ

    private CompositeDisposable _disposables = new CompositeDisposable();

    void Start()
    {
        _resultPanel.SetActive(false);

        GameEvents.OnRaceEnded
            .Subscribe(_ => ShowResult())
            .AddTo(_disposables);
    }

    void OnDestroy()
    {
        _disposables.Dispose();
    }

    private void ShowResult()
    {
        _resultPanel.SetActive(true);

        var allScoreData = _scoreManager.GetAllScoreData();
        var points = _scoreManager.CalculateFinalPoints();

        // 総合ポイント順にソート
        var sorted = points
            .OrderByDescending(kvp => kvp.Value)
            .ThenBy(kvp => allScoreData[kvp.Key].GoalRank)
            .ToList();

        // 各項目の最大値を取得（1位マーク用）
        int maxCollision = allScoreData.Values.Max(d => d.CollisionCount);
        int maxKnockOff = allScoreData.Values.Max(d => d.KnockOffCount);
        int maxFell = allScoreData.Values.Max(d => d.FellCount);
        float maxFirstPlace = allScoreData.Values.Max(d => d.FirstPlaceTime);

        int rank = 1;
        foreach (var kvp in sorted)
        {
            GameObject car = kvp.Key;
            int totalPoints = kvp.Value;
            var data = allScoreData[car];

            // 行を生成
            GameObject row = Instantiate(_resultRowPrefab, _resultContent);
            ResultRow resultRow = row.GetComponent<ResultRow>();

            if (resultRow != null)
            {
                resultRow.SetData(
                    rank,
                    car.name,
                    totalPoints,
                    data.GoalRank,
                    data.CollisionCount,
                    data.KnockOffCount,
                    data.FellCount,
                    data.FirstPlaceTime,
                    // 各項目が1位かどうか
                    data.GoalRank == 1,
                    maxCollision > 0 && data.CollisionCount == maxCollision,
                    maxKnockOff > 0 && data.KnockOffCount == maxKnockOff,
                    maxFell > 0 && data.FellCount == maxFell,
                    maxFirstPlace > 0 && Mathf.Approximately(data.FirstPlaceTime, maxFirstPlace)
                );
            }

            rank++;
        }
    }
}