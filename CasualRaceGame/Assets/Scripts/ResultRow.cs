using UnityEngine;
using TMPro;

/// <summary>
/// リザルト画面の1行分
/// </summary>
public class ResultRow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _rankText;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _pointsText;
    [SerializeField] private TextMeshProUGUI _goalRankText;
    [SerializeField] private TextMeshProUGUI _collisionText;
    [SerializeField] private TextMeshProUGUI _knockOffText;
    [SerializeField] private TextMeshProUGUI _fellText;
    [SerializeField] private TextMeshProUGUI _firstPlaceTimeText;

    [Header("1位マークの色")]
    [SerializeField] private Color _topColor = Color.yellow;

    public void SetData(
        int rank, string carName, int totalPoints,
        int goalRank, int collision, int knockOff, int fell, float firstPlaceTime,
        bool isTopGoal, bool isTopCollision, bool isTopKnockOff, bool isTopFell, bool isTopFirstPlace)
    {
        _rankText.text = $"{rank}位";
        _nameText.text = carName;
        _pointsText.text = $"{totalPoints}pt";

        _goalRankText.text = goalRank > 0 ? $"{goalRank}着" : "未完走";
        _collisionText.text = $"{collision}回";
        _knockOffText.text = $"{knockOff}回";
        _fellText.text = $"{fell}回";
        _firstPlaceTimeText.text = $"{firstPlaceTime:F1}秒";

        // 1位マーク
        if (isTopGoal) _goalRankText.color = _topColor;
        if (isTopCollision) _collisionText.color = _topColor;
        if (isTopKnockOff) _knockOffText.color = _topColor;
        if (isTopFell) _fellText.color = _topColor;
        if (isTopFirstPlace) _firstPlaceTimeText.color = _topColor;
    }
}