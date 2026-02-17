using UnityEngine;
using TMPro;

/// <summary>
/// HUD表示
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private GameObject _playerCar;
    [SerializeField] private RaceManager _raceManager;

    [Header("UI要素")]
    [SerializeField] private GameObject _hudPanel;
    [SerializeField] private TextMeshProUGUI _rankText;
    [SerializeField] private TextMeshProUGUI _speedText;
    [SerializeField] private TextMeshProUGUI _boostText;
    [SerializeField] private TextMeshProUGUI _countdownText;
    [SerializeField] private TextMeshProUGUI _raceEndTimerText;

    private CarBase _playerCarBase;

    void Start()
    {
        _playerCarBase = _playerCar.GetComponent<CarBase>();
        _raceEndTimerText.gameObject.SetActive(false);
    }

    void Update()
    {
        UpdateCountdown();
        UpdateRaceEndTimer();

        if (_raceManager.IsRaceEnded())
        {
            _hudPanel.SetActive(false);
            return;
        }

        if (_raceManager.IsRaceStarted())
        {
            UpdateRank();
            UpdateSpeed();
            UpdateBoost();
        }
    }

    private void UpdateCountdown()
    {
        int countdown = _raceManager.GetCountdownValue();

        if (countdown > 0)
        {
            _countdownText.gameObject.SetActive(true);
            _countdownText.text = countdown.ToString();
        }
        else if (_raceManager.IsRaceStarted() && _countdownText.gameObject.activeSelf)
        {
            // GO!を一瞬表示してから消す
            _countdownText.text = "GO!";
            Invoke(nameof(HideCountdown), 1f);
        }
    }

    private void HideCountdown()
    {
        _countdownText.gameObject.SetActive(false);
    }

    private void UpdateRaceEndTimer()
    {
        float timer = _raceManager.GetRaceEndTimer();

        if (timer > 0)
        {
            _raceEndTimerText.gameObject.SetActive(true);
            _raceEndTimerText.text = $"残り {Mathf.CeilToInt(timer)}秒";
        }
        else if (_raceManager.IsRaceEnded())
        {
            _raceEndTimerText.text = "終了！";
        }
    }

    private void UpdateRank()
    {
        int rank = _raceManager.GetRank(_playerCar);
        int total = _raceManager.GetCarCount();
        _rankText.text = $"{rank} / {total}";
    }

    private void UpdateSpeed()
    {
        float speed = _playerCarBase.GetSpeed();
        _speedText.text = $"{speed:F0} km/h";
    }

    private void UpdateBoost()
    {
        float boost = _playerCarBase.GetBoostMultiplier();
        if (boost > 1f)
        {
            _boostText.text = $"x{boost:F1}";
            _boostText.gameObject.SetActive(true);
        }
        else
        {
            _boostText.gameObject.SetActive(false);
        }
    }
}