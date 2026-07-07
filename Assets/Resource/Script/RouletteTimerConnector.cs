using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class RouletteTimerConnector : MonoBehaviour
{
    [Header("Connection")]
    [SerializeField] private ToonationReceiver receiver;
    [SerializeField] private SimpleTimerController timer;

    [Header("Roulette")]
    [Tooltip("예: 노방종 룰렛, 방송연장 룰렛")]
    [SerializeField] private string roulettePrefixText = "노방종 룰렛";
    private const string DefaultRoulettePrefix = "노방종 룰렛";

    [Header("Delay")]
    [Tooltip("룰렛 결과 수신 후 타이머 적용까지 딜레이(초). 0이면 즉시 적용")]
    [SerializeField] private float applyDelaySec = 0f;

    [Header("Debug")]
    [SerializeField] private bool logParsedMessage = true;

    private readonly List<string> _roulettePrefixes = new List<string>();

    private void Awake()
    {
        SetRoulettePrefixes(roulettePrefixText);
    }

    private void OnEnable()
    {
        if (receiver != null)
            receiver.OnRawMessage += HandleRawMessage;
    }

    private void OnDisable()
    {
        if (receiver != null)
            receiver.OnRawMessage -= HandleRawMessage;
    }

    public void SetApplyDelaySeconds(float seconds)
    {
        applyDelaySec = Mathf.Max(0f, seconds);
        Debug.Log($"[Roulette] 적용 딜레이 설정: {applyDelaySec}초");
    }

    public void SetRoulettePrefixes(string prefixText)
    {
        roulettePrefixText = prefixText ?? "";

        if (string.IsNullOrWhiteSpace(roulettePrefixText))
            roulettePrefixText = DefaultRoulettePrefix;

        _roulettePrefixes.Clear();

        string[] parts = roulettePrefixText.Split(',', '\n', '\r');

        foreach (string part in parts)
        {
            string value = part.Trim();

            if (string.IsNullOrWhiteSpace(value))
                continue;

            if (!_roulettePrefixes.Contains(value))
                _roulettePrefixes.Add(value);
        }

        Debug.Log($"[Roulette] 인식할 룰렛 이름: {string.Join(", ", _roulettePrefixes)}");
    }

    private void HandleRawMessage(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return;

        string message = ExtractRouletteMessage(raw);

        if (string.IsNullOrEmpty(message))
            return;

        if (logParsedMessage)
            Debug.Log($"[Roulette] 추출 메시지: {message}");

        StartCoroutine(ApplyAfterDelay(message, applyDelaySec));
    }

    private IEnumerator ApplyAfterDelay(string message, float delay)
    {
        if (delay > 0f)
        {
            Debug.Log($"[Roulette] {delay}초 후 적용 예정: {message}");
            yield return new WaitForSecondsRealtime(delay);
        }

        ParseAndApply(message);
    }

    private string ExtractRouletteMessage(string raw)
    {
        string message = ExtractJsonStringField(raw, "message");
        string rouletteName = ExtractJsonStringField(raw, "roulette_name");
        string lotteryName = ExtractJsonStringField(raw, "lottery_name");
        string itemName = ExtractJsonStringField(raw, "roulette_item_name");
        string altItemName = ExtractJsonStringField(raw, "item_name");

        string name = !string.IsNullOrEmpty(rouletteName) ? rouletteName : lotteryName;
        string result = !string.IsNullOrEmpty(itemName) ? itemName : altItemName;

        if (!string.IsNullOrEmpty(message) && ContainsAllowedRouletteName(message))
            return message;

        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(result))
        {
            string combinedMessage = $"{name} - {result}";

            if (ContainsAllowedRouletteName(combinedMessage))
                return combinedMessage;
        }

        if (HasNoRouletteNameFilter() && !string.IsNullOrEmpty(result))
            return result;

        return null;
    }

    private string ExtractJsonStringField(string raw, string fieldName)
    {
        string pattern = $"\"{Regex.Escape(fieldName)}\"\\s*:\\s*\"((?:\\\\.|[^\"])*)\"";
        Match match = Regex.Match(raw, pattern);

        if (!match.Success)
            return null;

        string value = match.Groups[1].Value;
        value = value.Replace("\\/", "/");

        try
        {
            value = Regex.Unescape(value);
        }
        catch
        {
            // 무시
        }

        return value;
    }

    private bool HasNoRouletteNameFilter()
    {
        return _roulettePrefixes.Count <= 0;
    }

    private bool ContainsAllowedRouletteName(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        if (HasNoRouletteNameFilter())
            return true;

        foreach (string prefix in _roulettePrefixes)
        {
            if (message.IndexOf(prefix, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private void ParseAndApply(string message)
    {
        if (!ContainsAllowedRouletteName(message))
            return;

        string result = ExtractResultText(message);

        if (string.IsNullOrWhiteSpace(result))
            return;

        result = result.Trim();

        if (IsNoChangeResult(result))
        {
            Debug.Log($"[Roulette] 시간 변화 없음: {result}");
            return;
        }

        bool isAdd = result.Contains("추가");
        bool isSubtract = result.Contains("차감");

        if (!isAdd && !isSubtract)
        {
            Debug.LogWarning($"[Roulette] 추가/차감 키워드 없음: {result}");
            return;
        }

        int minutes = ParseMinutes(result);

        if (minutes <= 0)
        {
            Debug.LogWarning($"[Roulette] 시간 파싱 실패: {result}");
            return;
        }

        if (isSubtract)
            minutes *= -1;

        timer?.AddMinutes(minutes);

        Debug.Log($"[Roulette] 적용 완료: {message} → {(minutes >= 0 ? "+" : "")}{minutes}분");
    }

    private string ExtractResultText(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "";

        foreach (string prefix in _roulettePrefixes)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                continue;

            string pattern = Regex.Escape(prefix) + @"\s*[-–—:]\s*(.+)";
            Match match = Regex.Match(message, pattern, RegexOptions.IgnoreCase);

            if (match.Success)
                return match.Groups[1].Value.Trim();

            int idx = message.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);

            if (idx >= 0)
            {
                string after = message.Substring(idx + prefix.Length).Trim();
                after = after.TrimStart('-', '–', '—', ':').Trim();

                if (!string.IsNullOrWhiteSpace(after))
                    return after;
            }
        }

        return message.Trim();
    }

    private bool IsNoChangeResult(string result)
    {
        return result == "광" ||
               result == "꽝" ||
               result.Contains("광") ||
               result.Contains("꽝");
    }

    private int ParseMinutes(string text)
    {
        int total = 0;

        foreach (Match m in Regex.Matches(text, @"(\d+)\s*(일|시간|분)"))
        {
            int v = int.Parse(m.Groups[1].Value);

            switch (m.Groups[2].Value)
            {
                case "일":
                    total += v * 1440;
                    break;

                case "시간":
                    total += v * 60;
                    break;

                case "분":
                    total += v;
                    break;
            }
        }

        return total;
    }
}