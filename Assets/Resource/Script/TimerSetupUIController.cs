using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimerSetupUIController : MonoBehaviour
{
    private const string AlertboxUrlPrefsKey = "TOONATION_ALERTBOX_URL";
    private const string SaveUrlPrefsKey = "TOONATION_SAVE_URL";
    private const string DelayPrefsKey = "TOONATION_APPLY_DELAY_SECONDS";
    private const string RouletteNamePrefsKey = "TOONATION_ROULETTE_NAMES";

    [Header("References")]
    [SerializeField] private ToonationReceiver receiver;
    [SerializeField] private RouletteTimerConnector rouletteConnector;
    [SerializeField] private SimpleTimerController timerController;

    [Header("Panels")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject timerPanel;
    [SerializeField] private GameObject setupPanel;
    [SerializeField] private GameObject detailSettingPanel;
    [SerializeField] private GameObject timerSettingPanel;

    [Header("Start UI")]
    [SerializeField] private Button normalModeButton;
    [SerializeField] private Button connectModeButton;

    [Header("Setup UI")]
    [SerializeField] private TMP_InputField alertboxUrlInput;
    [SerializeField] private Button connectButton;
    [SerializeField] private Button setupBackButton;

    [Tooltip("SetupPanel에서 연동 상세 설정을 여는 버튼")]
    [SerializeField] private Button detailSettingButton;

    [Header("Detail Setting UI")]
    [SerializeField] private TMP_InputField rouletteNameInput;
    [SerializeField] private TMP_InputField delayInput;
    [SerializeField] private Toggle saveUrlToggle;
    [SerializeField] private Button detailSaveButton;

    [Header("Timer Panel UI")]
    [SerializeField] private Button timerSettingOpenButton;

    [Header("Timer Setting UI")]
    [SerializeField] private Toggle timerSaveTimeToggle;
    [SerializeField] private Button timerSettingSaveButton;
    [SerializeField] private Button timerSettingBackButton;
    [SerializeField] private Button timerSettingHomeButton;

    [Header("Options")]
    [SerializeField] private bool autoConnectSavedUrl = true;
    [SerializeField] private string defaultRouletteNames = "노방종 룰렛";

    private string _pendingUrl;

    private void Awake()
    {
        if (normalModeButton != null)
            normalModeButton.onClick.AddListener(SelectNormalMode);

        if (connectModeButton != null)
            connectModeButton.onClick.AddListener(SelectConnectMode);

        if (connectButton != null)
            connectButton.onClick.AddListener(ConnectFromInput);

        if (setupBackButton != null)
            setupBackButton.onClick.AddListener(GoToStartPanelFromSetup);

        if (detailSettingButton != null)
            detailSettingButton.onClick.AddListener(OpenDetailSettingPanel);

        if (detailSaveButton != null)
            detailSaveButton.onClick.AddListener(SaveDetailSettingPanel);

        if (delayInput != null)
            delayInput.onEndEdit.AddListener(OnDelayInputEndEdit);

        if (rouletteNameInput != null)
            rouletteNameInput.onEndEdit.AddListener(OnRouletteNameInputEndEdit);

        if (saveUrlToggle != null)
            saveUrlToggle.onValueChanged.AddListener(OnSaveUrlToggleChanged);

        if (timerSettingOpenButton != null)
            timerSettingOpenButton.onClick.AddListener(OpenTimerSettingPanel);

        if (timerSettingSaveButton != null)
            timerSettingSaveButton.onClick.AddListener(SaveTimerSetting);

        if (timerSettingBackButton != null)
            timerSettingBackButton.onClick.AddListener(CloseTimerSettingPanel);

        if (timerSettingHomeButton != null)
            timerSettingHomeButton.onClick.AddListener(GoToStartPanelFromTimerSetting);
    }

    private void OnEnable()
    {
        if (receiver != null)
            receiver.OnStatusChanged += HandleStatusChanged;
    }

    private void OnDisable()
    {
        if (receiver != null)
            receiver.OnStatusChanged -= HandleStatusChanged;
    }

    private void OnDestroy()
    {
        if (normalModeButton != null)
            normalModeButton.onClick.RemoveListener(SelectNormalMode);

        if (connectModeButton != null)
            connectModeButton.onClick.RemoveListener(SelectConnectMode);

        if (connectButton != null)
            connectButton.onClick.RemoveListener(ConnectFromInput);

        if (setupBackButton != null)
            setupBackButton.onClick.RemoveListener(GoToStartPanelFromSetup);

        if (detailSettingButton != null)
            detailSettingButton.onClick.RemoveListener(OpenDetailSettingPanel);

        if (detailSaveButton != null)
            detailSaveButton.onClick.RemoveListener(SaveDetailSettingPanel);

        if (delayInput != null)
            delayInput.onEndEdit.RemoveListener(OnDelayInputEndEdit);

        if (rouletteNameInput != null)
            rouletteNameInput.onEndEdit.RemoveListener(OnRouletteNameInputEndEdit);

        if (saveUrlToggle != null)
            saveUrlToggle.onValueChanged.RemoveListener(OnSaveUrlToggleChanged);

        if (timerSettingOpenButton != null)
            timerSettingOpenButton.onClick.RemoveListener(OpenTimerSettingPanel);

        if (timerSettingSaveButton != null)
            timerSettingSaveButton.onClick.RemoveListener(SaveTimerSetting);

        if (timerSettingBackButton != null)
            timerSettingBackButton.onClick.RemoveListener(CloseTimerSettingPanel);

        if (timerSettingHomeButton != null)
            timerSettingHomeButton.onClick.RemoveListener(GoToStartPanelFromTimerSetting);
    }

    private void Start()
    {
        CloseAllPanels();

        LoadUrlSetting();
        LoadRouletteNameSetting();
        LoadDelaySetting();
        LoadSaveTimeToggleState();

        ApplyRouletteNameSetting();
        ApplyDelaySetting();

        string savedUrl = GetSavedUrlIfAllowed();

        if (!string.IsNullOrWhiteSpace(savedUrl) && autoConnectSavedUrl)
        {
            ConnectWithUrl(savedUrl);
        }
        else
        {
            OpenStartPanel();
        }
    }

    private void SelectNormalMode()
    {
        CloseAllPanels();

        if (timerPanel != null)
            timerPanel.SetActive(true);
    }

    private void SelectConnectMode()
    {
        CloseAllPanels();

        if (setupPanel != null)
            setupPanel.SetActive(true);
    }

    private void ConnectFromInput()
    {
        if (alertboxUrlInput == null)
            return;

        string url = alertboxUrlInput.text.Trim();

        if (string.IsNullOrWhiteSpace(url))
        {
            Debug.LogWarning("[Setup] URL을 입력해주세요.");
            return;
        }

        ApplyRouletteNameSetting();
        ApplyDelaySetting();
        ApplySaveUrlSettingFromToggle();

        ConnectWithUrl(url);
    }

    private void ConnectWithUrl(string url)
    {
        if (receiver == null)
        {
            return;
        }

        _pendingUrl = url;

        SetConnectButtonInteractable(false);

        receiver.SetAlertboxUrl(url);
        receiver.Connect();
    }

    private void HandleStatusChanged(string status)
    {
        if (status == "연결됨")
        {
            SaveUrlAfterConnected();
            OpenTimerPanelOnly();
            SetConnectButtonInteractable(true);
            return;
        }

        if (!string.IsNullOrEmpty(status) &&
            (status.Contains("오류") || status.Contains("실패")))
        {
            CloseAllPanels();

            if (setupPanel != null)
                setupPanel.SetActive(true);

            SetConnectButtonInteractable(true);
            return;
        }

        if (!string.IsNullOrEmpty(status) && status.Contains("끊김"))
        {
            SetConnectButtonInteractable(true);
        }
    }

    private void OpenDetailSettingPanel()
    {
        if (setupPanel != null)
            setupPanel.SetActive(false);

        if (detailSettingPanel != null)
            detailSettingPanel.SetActive(true);
    }

    private void SaveDetailSettingPanel()
    {
        ApplyRouletteNameSetting();
        ApplyDelaySetting();
        ApplySaveUrlSettingFromToggle();

        if (detailSettingPanel != null)
            detailSettingPanel.SetActive(false);

        if (setupPanel != null)
            setupPanel.SetActive(true);
    }

    private void OpenTimerSettingPanel()
    {
        LoadSaveTimeToggleState();

        if (timerSettingPanel != null)
            timerSettingPanel.SetActive(true);
    }

    private void SaveTimerSetting()
    {
        ApplyTimerSaveTimeSetting();

        if (timerSettingPanel != null)
            timerSettingPanel.SetActive(false);
    }

    private void CloseTimerSettingPanel()
    {
        LoadSaveTimeToggleState();

        if (timerSettingPanel != null)
            timerSettingPanel.SetActive(false);
    }

    private void GoToStartPanelFromTimerSetting()
    {
        ApplyTimerSaveTimeSetting();

        bool saveTime = timerController != null && timerController.IsSaveTimeEnabled();

        if (!saveTime && timerController != null)
            timerController.ResetTimer();

        DisconnectReceiver();
        OpenStartPanel();
    }

    private void GoToStartPanelFromSetup()
    {
        DisconnectReceiver();
        OpenStartPanel();
    }

    private void OpenStartPanel()
    {
        CloseAllPanels();

        if (startPanel != null)
            startPanel.SetActive(true);
    }

    private void OpenTimerPanelOnly()
    {
        CloseAllPanels();

        if (timerPanel != null)
            timerPanel.SetActive(true);
    }

    private void CloseAllPanels()
    {
        if (startPanel != null)
            startPanel.SetActive(false);

        if (timerPanel != null)
            timerPanel.SetActive(false);

        if (setupPanel != null)
            setupPanel.SetActive(false);

        if (detailSettingPanel != null)
            detailSettingPanel.SetActive(false);

        if (timerSettingPanel != null)
            timerSettingPanel.SetActive(false);
    }

    private void LoadUrlSetting()
    {
        bool saveUrl = PlayerPrefs.GetInt(SaveUrlPrefsKey, 0) == 1;

        if (saveUrlToggle != null)
            saveUrlToggle.SetIsOnWithoutNotify(saveUrl);

        if (!saveUrl)
        {
            PlayerPrefs.DeleteKey(AlertboxUrlPrefsKey);
            PlayerPrefs.Save();

            _pendingUrl = "";

            if (alertboxUrlInput != null)
                alertboxUrlInput.text = "";

            return;
        }

        string savedUrl = PlayerPrefs.GetString(AlertboxUrlPrefsKey, "");

        if (alertboxUrlInput != null)
            alertboxUrlInput.text = savedUrl;
    }

    private void LoadRouletteNameSetting()
    {
        string savedRouletteNames = PlayerPrefs.GetString(RouletteNamePrefsKey, defaultRouletteNames);

        if (rouletteNameInput != null)
            rouletteNameInput.text = savedRouletteNames;
    }

    private void LoadDelaySetting()
    {
        string savedDelay = PlayerPrefs.GetString(DelayPrefsKey, "");

        if (delayInput != null)
            delayInput.text = savedDelay;
    }

    private void LoadSaveTimeToggleState()
    {
        bool saveTime = timerController != null && timerController.IsSaveTimeEnabled();

        if (timerSaveTimeToggle != null)
            timerSaveTimeToggle.SetIsOnWithoutNotify(saveTime);
    }

    private string GetSavedUrlIfAllowed()
    {
        bool saveUrl = saveUrlToggle != null && saveUrlToggle.isOn;

        if (!saveUrl)
            return "";

        return PlayerPrefs.GetString(AlertboxUrlPrefsKey, "");
    }

    private void OnRouletteNameInputEndEdit(string _)
    {
        ApplyRouletteNameSetting();
    }

    private void ApplyRouletteNameSetting()
    {
        string rouletteNames = GetRouletteNameText();

        if (rouletteNameInput != null && string.IsNullOrWhiteSpace(rouletteNameInput.text))
            rouletteNameInput.text = rouletteNames;

        if (rouletteConnector != null)
            rouletteConnector.SetRoulettePrefixes(rouletteNames);

        PlayerPrefs.SetString(RouletteNamePrefsKey, rouletteNames);
        PlayerPrefs.Save();
    }

    private string GetRouletteNameText()
    {
        if (rouletteNameInput == null)
            return defaultRouletteNames;

        string text = rouletteNameInput.text.Trim();

        if (string.IsNullOrWhiteSpace(text))
            return defaultRouletteNames;

        return text;
    }

    private void OnDelayInputEndEdit(string _)
    {
        ApplyDelaySetting();
    }

    private void ApplyDelaySetting()
    {
        float delaySeconds = GetDelaySeconds();

        if (rouletteConnector != null)
            rouletteConnector.SetApplyDelaySeconds(delaySeconds);

        if (delaySeconds > 0f)
        {
            PlayerPrefs.SetString(
                DelayPrefsKey,
                delaySeconds.ToString(CultureInfo.InvariantCulture)
            );
        }
        else
        {
            PlayerPrefs.DeleteKey(DelayPrefsKey);
        }

        PlayerPrefs.Save();
    }

    private float GetDelaySeconds()
    {
        if (delayInput == null)
            return 0f;

        string text = delayInput.text.Trim();

        if (string.IsNullOrWhiteSpace(text))
            return 0f;

        if (!float.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float seconds))
        {
            return 0f;
        }

        return Mathf.Max(0f, seconds);
    }

    private void OnSaveUrlToggleChanged(bool isOn)
    {
        ApplySaveUrlSettingFromToggle();
    }

    private void ApplySaveUrlSettingFromToggle()
    {
        bool saveUrl = saveUrlToggle != null && saveUrlToggle.isOn;

        PlayerPrefs.SetInt(SaveUrlPrefsKey, saveUrl ? 1 : 0);

        if (!saveUrl)
            PlayerPrefs.DeleteKey(AlertboxUrlPrefsKey);

        PlayerPrefs.Save();
    }

    private void ApplyTimerSaveTimeSetting()
    {
        if (timerSaveTimeToggle == null)
            return;

        if (timerController != null)
            timerController.SetSaveTimeEnabled(timerSaveTimeToggle.isOn);
    }

    private void SaveUrlAfterConnected()
    {
        bool saveUrl = saveUrlToggle != null && saveUrlToggle.isOn;

        PlayerPrefs.SetInt(SaveUrlPrefsKey, saveUrl ? 1 : 0);

        if (saveUrl && !string.IsNullOrWhiteSpace(_pendingUrl))
        {
            PlayerPrefs.SetString(AlertboxUrlPrefsKey, _pendingUrl);
        }
        else
        {
            PlayerPrefs.DeleteKey(AlertboxUrlPrefsKey);
        }

        PlayerPrefs.Save();
    }

    private void DisconnectReceiver()
    {
        if (receiver != null && receiver.IsConnected)
            receiver.Disconnect();
    }


    private void SetConnectButtonInteractable(bool interactable)
    {
        if (connectButton != null)
            connectButton.interactable = interactable;
    }

}