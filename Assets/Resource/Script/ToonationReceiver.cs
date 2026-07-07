using System;
using System.Collections;
using System.Text.RegularExpressions;
using NativeWebSocket;
using UnityEngine;
using UnityEngine.Networking;

public class ToonationReceiver : MonoBehaviour
{
    [Header("Toonation")]
    [Tooltip("https://toon.at/widget/alertbox/xxxx 형식 또는 xxxx 키만 입력 가능")]
    [SerializeField] private string alertboxUrl = "";

    [Header("Debug")]
    [SerializeField] private bool logRawMessage = false;
    [SerializeField] private bool fallbackToUrlKeyWhenPayloadMissing = true;

    public event Action<string> OnRawMessage;
    public event Action<string> OnStatusChanged;

    private WebSocket _ws;
    private Coroutine _connectRoutine;
    private Coroutine _pingRoutine;

    public bool IsConnected => _ws != null && _ws.State == WebSocketState.Open;

    public void SetAlertboxUrl(string url)
    {
        alertboxUrl = url?.Trim() ?? "";
    }

    public void Connect()
    {
        if (string.IsNullOrWhiteSpace(alertboxUrl))
        {
            Debug.LogError("[Toonation] Alertbox URL이 비어있습니다.");
            return;
        }

        if (_connectRoutine != null)
            StopCoroutine(_connectRoutine);

        _connectRoutine = StartCoroutine(ConnectRoutine(alertboxUrl));
    }

    public async void Disconnect()
    {
        if (_pingRoutine != null)
        {
            StopCoroutine(_pingRoutine);
            _pingRoutine = null;
        }

        if (_ws != null)
        {
            try
            {
                await _ws.Close();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Toonation] 연결 종료 중 오류: {ex.Message}");
            }

            _ws = null;
        }

        ChangeStatus("연결 끊김");
    }

    private IEnumerator ConnectRoutine(string input)
    {
        string normalizedUrl = NormalizeAlertboxUrl(input);
        string keyFromUrl = ExtractKey(input);
        string payload = null;

        if (!string.IsNullOrEmpty(normalizedUrl))
        {
            using UnityWebRequest request = UnityWebRequest.Get(normalizedUrl);
            request.timeout = 10;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string html = request.downloadHandler.text;
                payload = ExtractPayloadFromHtml(html);

                if (string.IsNullOrEmpty(payload))
                    Debug.LogWarning("[Toonation] Alertbox 페이지에서 payload를 찾지 못했습니다.");
            }
            else
            {
                Debug.LogWarning($"[Toonation] Alertbox 페이지 요청 실패: {request.error}");
            }
        }

        if (string.IsNullOrEmpty(payload) && fallbackToUrlKeyWhenPayloadMissing)
        {
            payload = keyFromUrl;
            Debug.LogWarning("[Toonation] payload 추출 실패 → URL key로 WebSocket 연결을 시도합니다.");
        }

        if (string.IsNullOrEmpty(payload))
        {
            Debug.LogError("[Toonation] payload/key 추출 실패");
            yield break;
        }

        ConnectWebSocket(payload);
    }

    private async void ConnectWebSocket(string payload)
    {
        if (_ws != null)
        {
            try
            {
                await _ws.Close();
            }
            catch
            {
                // 이전 연결 종료 실패는 무시
            }

            _ws = null;
        }

        string wsUrl = $"wss://ws.toon.at/{payload}";

        _ws = new WebSocket(wsUrl);

        _ws.OnOpen += () =>
        {
            ChangeStatus("연결됨");

            if (_pingRoutine != null)
                StopCoroutine(_pingRoutine);

            _pingRoutine = StartCoroutine(PingRoutine());
        };

        _ws.OnMessage += bytes =>
        {
            string raw = System.Text.Encoding.UTF8.GetString(bytes);

            if (logRawMessage)
                Debug.Log($"[Toonation RAW] {raw}");

            OnRawMessage?.Invoke(raw);
        };

        _ws.OnError += error =>
        {
            Debug.LogError($"[Toonation] 오류: {error}");
            ChangeStatus($"오류: {error}");
        };

        _ws.OnClose += code =>
        {
            ChangeStatus("연결 끊김");
        };

        try
        {
            await _ws.Connect();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Toonation] 연결 실패: {ex.Message}");
            ChangeStatus("연결 실패");
        }
    }

    private IEnumerator PingRoutine()
    {
        while (IsConnected)
        {
            yield return new WaitForSecondsRealtime(30f);

            if (IsConnected)
                _ = _ws.SendText("PING");
        }
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        _ws?.DispatchMessageQueue();
#endif
    }

    private void OnDestroy()
    {
        Disconnect();
    }

    private void ChangeStatus(string status)
    {
        OnStatusChanged?.Invoke(status);
    }

    private string NormalizeAlertboxUrl(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        string value = input.Trim();

        if (value.StartsWith("http://") || value.StartsWith("https://"))
            return value;

        return $"https://toon.at/widget/alertbox/{value}";
    }

    private string ExtractKey(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        string value = input.Trim();

        Match match = Regex.Match(value, @"alertbox/([^/?#\s]+)");
        if (match.Success)
            return match.Groups[1].Value;

        return value;
    }

    private string ExtractPayloadFromHtml(string html)
    {
        if (string.IsNullOrEmpty(html))
            return null;

        // window.payload = JSON.parse("...") 안에 유니코드 이스케이프된 JWT 토큰 추출
        // eyJ 로 시작하는 Base64 JWT 패턴
        var jwtMatch = Regex.Match(html, @"(eyJ[A-Za-z0-9+/=._\-]{20,})");
        if (jwtMatch.Success)
        {
            return jwtMatch.Groups[1].Value;
        }

        // fallback 패턴들
        string[] patterns =
        {
            @"wss://ws\.toon\.at/([A-Za-z0-9+/=._\-]+)",
            @"wss:\\/\\/ws\.toon\.at\\/([A-Za-z0-9+/=._\-]+)",
            "\"payload\"\\s*:\\s*\"([^\"]+)\"",
            "payload\\s*[:=]\\s*[\"\']([^\"\']*)[\"\']",
        };

        foreach (string pattern in patterns)
        {
            Match match = Regex.Match(html, pattern);
            if (!match.Success) continue;
            string payload = match.Groups[1].Value;
            payload = payload.Replace("\\/", "/");
            payload = Regex.Unescape(payload);
            return payload;
        }

        return null;
    }
}