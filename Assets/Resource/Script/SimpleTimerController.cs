using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SimpleTimerController : MonoBehaviour
{
    public const string SaveTimePrefsKey = "TIMER_SAVE_TIME";
    private const string RemainingSecondsPrefsKey = "TIMER_REMAINING_SECONDS";

    [Header("Timer UI")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Minute Input Panel")]
    [SerializeField] private GameObject minutePanel;
    [SerializeField] private TMP_InputField minuteInput;
    [SerializeField] private Button plusButton;
    [SerializeField] private Button minusButton;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button closeButton;

    [Header("Pause")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private TextMeshProUGUI pauseButtonText;

    [Header("Timer")]
    [SerializeField] private double initialSeconds = 0;
    [SerializeField] private bool runOnStart = false;
    [SerializeField] private bool autoStartWhenTimeAdded = true;
    [SerializeField] private bool clearInputOnOpen = true;

    private double _remainingSeconds;
    private bool _isRunning;
    private bool _isPausedByUser;
    private bool _saveTimeEnabled;
    private bool _initialized;

    private float _saveTimeElapsed;

    // 1: 시간 추가, -1: 시간 차감
    private int _pendingSign = 1;

    private void Awake()
    {
        _saveTimeEnabled = PlayerPrefs.GetInt(SaveTimePrefsKey, 0) == 1;

        if (plusButton != null)
            plusButton.onClick.AddListener(OpenAddPanel);

        if (minusButton != null)
            minusButton.onClick.AddListener(OpenSubtractPanel);

        if (applyButton != null)
            applyButton.onClick.AddListener(ApplyMinuteInput);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseMinutePanel);

        if (pauseButton != null)
            pauseButton.onClick.AddListener(TogglePause);

        if (minuteInput != null)
            minuteInput.onSubmit.AddListener(OnMinuteInputSubmit);
    }

    private void Start()
    {
        if (_saveTimeEnabled && TryLoadSavedTime(out double savedSeconds))
        {
            _remainingSeconds = Math.Max(0.0, savedSeconds);
        }
        else
        {
            _remainingSeconds = Math.Max(0.0, initialSeconds);
        }

        _isRunning = runOnStart && _remainingSeconds > 0;
        _isPausedByUser = false;
        _initialized = true;

        if (minutePanel != null)
            minutePanel.SetActive(false);

        UpdateDisplay();
        UpdatePauseButtonText();
    }

    private void Update()
    {
        if (!_isRunning)
            return;

        _remainingSeconds -= Time.unscaledDeltaTime;

        if (_remainingSeconds <= 0)
        {
            _remainingSeconds = 0;
            _isRunning = false;
            _isPausedByUser = false;

            UpdatePauseButtonText();
            SaveRemainingTime();
        }

        UpdateDisplay();
        SaveTimeTick();
    }

    private void OnDestroy()
    {
        if (plusButton != null)
            plusButton.onClick.RemoveListener(OpenAddPanel);

        if (minusButton != null)
            minusButton.onClick.RemoveListener(OpenSubtractPanel);

        if (applyButton != null)
            applyButton.onClick.RemoveListener(ApplyMinuteInput);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseMinutePanel);

        if (pauseButton != null)
            pauseButton.onClick.RemoveListener(TogglePause);

        if (minuteInput != null)
            minuteInput.onSubmit.RemoveListener(OnMinuteInputSubmit);
    }

    private void OnApplicationQuit()
    {
        SaveRemainingTime();
    }

    private void OpenAddPanel()
    {
        OpenMinutePanel(1);
    }

    private void OpenSubtractPanel()
    {
        OpenMinutePanel(-1);
    }

    private void OnMinuteInputSubmit(string _)
    {
        ApplyMinuteInput();
    }
    private void OpenMinutePanel(int sign)
    {
        _pendingSign = sign;

        if (minutePanel != null)
            minutePanel.SetActive(true);

        if (minuteInput != null)
        {
            if (clearInputOnOpen)
                minuteInput.text = "";

            minuteInput.ActivateInputField();
            EventSystem.current?.SetSelectedGameObject(minuteInput.gameObject);
        }
    }

    private void CloseMinutePanel()
    {
        if (minutePanel != null)
            minutePanel.SetActive(false);

        if (minuteInput != null)
            minuteInput.DeactivateInputField();
    }

    private void ApplyMinuteInput()
    {
        int minutes = GetInputMinutes();

        if (minutes <= 0)
        {
            Debug.LogWarning("[Timer] 입력된 분 값이 없습니다.");
            return;
        }

        AddMinutes(minutes * _pendingSign);
        CloseMinutePanel();
    }

    private int GetInputMinutes()
    {
        if (minuteInput == null)
            return 0;

        if (!int.TryParse(minuteInput.text, out int minutes))
            return 0;

        return Mathf.Max(0, minutes);
    }

    public void TogglePause()
    {
        if (_remainingSeconds <= 0)
            return;

        if (_isRunning)
        {
            _isRunning = false;
            _isPausedByUser = true;
        }
        else
        {
            _isRunning = true;
            _isPausedByUser = false;
        }

        UpdatePauseButtonText();
        SaveRemainingTime();
    }

    public void ResetTimer()
    {
        _remainingSeconds = Math.Max(0.0, initialSeconds);
        _isRunning = false;
        _isPausedByUser = false;

        UpdateDisplay();
        UpdatePauseButtonText();
        SaveRemainingTime();
    }

    /// <summary>
    /// 분 단위로 시간 추가/차감합니다.
    /// 양수면 추가, 음수면 차감입니다.
    /// </summary>
    public void AddMinutes(int minutes)
    {
        bool wasZero = _remainingSeconds <= 0;

        _remainingSeconds += minutes * 60.0;

        if (_remainingSeconds < 0)
            _remainingSeconds = 0;

        if (_remainingSeconds <= 0)
        {
            _isRunning = false;
            _isPausedByUser = false;
        }
        else if (autoStartWhenTimeAdded && wasZero && !_isPausedByUser)
        {
            _isRunning = true;
        }

        UpdateDisplay();
        UpdatePauseButtonText();
        SaveRemainingTime();
    }

    public void SetSaveTimeEnabled(bool enabled)
    {
        _saveTimeEnabled = enabled;
        PlayerPrefs.SetInt(SaveTimePrefsKey, enabled ? 1 : 0);

        if (enabled)
        {
            if (_initialized)
                SaveRemainingTime();
            else
                PlayerPrefs.Save();
        }
        else
        {
            PlayerPrefs.DeleteKey(RemainingSecondsPrefsKey);
            PlayerPrefs.Save();
        }
    }

    public bool IsSaveTimeEnabled()
    {
        return _saveTimeEnabled;
    }

    private void SaveTimeTick()
    {
        if (!_saveTimeEnabled)
            return;

        _saveTimeElapsed += Time.unscaledDeltaTime;

        if (_saveTimeElapsed < 1f)
            return;

        _saveTimeElapsed = 0f;
        SaveRemainingTime();
    }

    private void SaveRemainingTime()
    {
        if (!_saveTimeEnabled)
            return;

        PlayerPrefs.SetString(
            RemainingSecondsPrefsKey,
            _remainingSeconds.ToString(CultureInfo.InvariantCulture)
        );

        PlayerPrefs.Save();
    }

    private bool TryLoadSavedTime(out double seconds)
    {
        seconds = 0;

        if (!PlayerPrefs.HasKey(RemainingSecondsPrefsKey))
            return false;

        string value = PlayerPrefs.GetString(RemainingSecondsPrefsKey, "0");

        return double.TryParse(
            value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out seconds
        );
    }

    private void UpdateDisplay()
    {
        if (timerText != null)
            timerText.text = FormatTime(_remainingSeconds);
    }

    private void UpdatePauseButtonText()
    {
        if (pauseButtonText == null)
            return;

        if (_remainingSeconds <= 0)
        {
            pauseButtonText.text = "Stop";
            return;
        }

        pauseButtonText.text = _isRunning ? "Stop" : "Resume";
    }

    private string FormatTime(double totalSeconds)
    {
        int t = Mathf.CeilToInt((float)totalSeconds);

        int days = t / 86400;
        int hours = (t % 86400) / 3600;
        int minutes = (t % 3600) / 60;
        int seconds = t % 60;

        return $"{days}일 {hours:D2}:{minutes:D2}:{seconds:D2}";
    }
}