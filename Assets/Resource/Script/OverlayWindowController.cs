using System.Collections;
using UnityEngine;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
#endif

public class OverlayWindowController : MonoBehaviour
{
    [Header("Window Size")]
    [SerializeField] private int windowWidth = 300;
    [SerializeField] private int timerWindowHeight = 50;
    [SerializeField] private int setupWindowHeight = 120;

    [Header("Options")]
    [SerializeField] private bool alwaysOnTop = true;
    [SerializeField] private bool setupModeOnStart = true;

    // X 버튼 제거용
    private const int GWL_STYLE = -16;
    private const uint WS_SYSMENU = 0x00080000;

    private bool _setupMode;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

    private const uint SWP_NOSIZE     = 0x0001;
    private const uint SWP_NOMOVE     = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private IntPtr _windowHandle;
#endif

    private IEnumerator Start()
    {
        _setupMode = setupModeOnStart;
        ApplyWindowSize();

        yield return null;
        yield return new WaitForSecondsRealtime(0.3f);

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        _windowHandle = Process.GetCurrentProcess().MainWindowHandle;
        ApplyTopMost();
#endif
    }

    private void Update()
    {
        int targetHeight = GetCurrentHeight();

        // 해상도가 달라졌을 때만 재적용 (위치는 건드리지 않음)
        if (Screen.fullScreenMode != FullScreenMode.Windowed ||
            Screen.width != windowWidth ||
            Screen.height != targetHeight)
        {
            ApplyWindowSize();
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        // 항상 위 고정만 유지 (위치는 이동 안 함)
        if (alwaysOnTop && _windowHandle != IntPtr.Zero)
            ApplyTopMost();
#endif
    }

    public void SetSetupMode(bool enabled)
    {
        _setupMode = enabled;
        ApplyWindowSize();
        // 위치 재적용 없음 — 사용자가 옮긴 위치 유지
    }

    private int GetCurrentHeight() =>
        _setupMode ? setupWindowHeight : timerWindowHeight;

    private void ApplyWindowSize()
    {
        Screen.fullScreenMode = FullScreenMode.Windowed;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    if (_windowHandle != IntPtr.Zero)
    {
        SetWindowPos(
            _windowHandle,
            alwaysOnTop ? HWND_TOPMOST : HWND_NOTOPMOST,
            0,
            0,
            windowWidth,
            GetCurrentHeight(),
            SWP_NOMOVE | SWP_NOACTIVATE | SWP_SHOWWINDOW
        );

        return;
    }
#endif

        Screen.SetResolution(windowWidth, GetCurrentHeight(), FullScreenMode.Windowed);
    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    /// <summary>항상 위 고정 (위치/크기 변경 없음)</summary>
    private void ApplyTopMost()
    {
        if (_windowHandle == IntPtr.Zero) return;
        SetWindowPos(_windowHandle, HWND_TOPMOST, 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
    }
#endif
}
