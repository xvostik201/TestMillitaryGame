using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ModeSwitcher : MonoBehaviour
{
    public enum Mode { Camera, Draw, Texture, Spawner, Settings }

    [SerializeField] private Mode _currentMode = Mode.Camera;

    [Header("Allowed Modes")]
    [SerializeField] private List<Mode> _allowedModes = new List<Mode> { Mode.Camera };

    [Header("Text")]
    [SerializeField] private TMP_Text _modeText;

    [Header("Button GameObjects")]
    [SerializeField] private GameObject _camSettingsBT;
    [SerializeField] private GameObject _drawSettingsBT;
    [SerializeField] private GameObject _brushSettingsBT;
    [SerializeField] private GameObject _textureSettingsBT;
    [SerializeField] private GameObject _spawnerModeBT;

    [Header("Confirmation Panel")]
    [SerializeField] private GameObject _confirmationPanel;

    public Mode CurrentMode => _currentMode;

    private Mode _previousMode;
    private GameObject _previousOpenGameObject;

    private Stack<GameObject> _openUIStack = new Stack<GameObject>();

    public static ModeSwitcher Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        if (_allowedModes.Count == 0)
        {
            _allowedModes.Add(Mode.Camera);
        }

        _currentMode = _allowedModes[0];
        SetModeUI();
    }

    private void OnEnable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnEscapePressed += HandleEscapePressed;
        }
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnEscapePressed -= HandleEscapePressed;
        }
    }

    private void HandleEscapePressed()
    {
        if (_openUIStack.Count > 0)
        {
            GameObject lastOpenUI = _openUIStack.Pop();
            if (lastOpenUI != null)
            {
                lastOpenUI.SetActive(false);
            }
        }
        else
        {
            if (_confirmationPanel != null)
            {
                _confirmationPanel.SetActive(true);
                _openUIStack.Push(_confirmationPanel);
            }
        }
    }

    public void SwitchMode()
    {
        if (_allowedModes.Count == 0)
            return;

        int currentIndex = _allowedModes.IndexOf(_currentMode);
        if (currentIndex == -1)
        {
            _currentMode = _allowedModes[0];
        }
        else
        {
            _currentMode = _allowedModes[(currentIndex + 1) % _allowedModes.Count];
        }

        _modeText.text = _currentMode.ToString();
        ClosePreviousUI();
        SetModeUI();
    }

    private void SetModeUI()
    {

        if (_camSettingsBT != null) _camSettingsBT.SetActive(false);
        if (_drawSettingsBT != null) _drawSettingsBT.SetActive(false);
        if (_brushSettingsBT != null) _brushSettingsBT.SetActive(false);
        if (_textureSettingsBT != null) _textureSettingsBT.SetActive(false);
        if (_spawnerModeBT != null) _spawnerModeBT.SetActive(false);

        switch (_currentMode)
        {
            case Mode.Camera:
                if (_camSettingsBT != null)
                    _camSettingsBT.SetActive(true);
                _modeText.text = "Камера";
                break;
            case Mode.Draw:
                if (_drawSettingsBT != null)
                    _drawSettingsBT.SetActive(true);
                if (_brushSettingsBT != null)
                    _brushSettingsBT.SetActive(true);
                _modeText.text = "Рисование";
                break;
            case Mode.Texture:
                if (_drawSettingsBT != null)
                    _drawSettingsBT.SetActive(true);
                if (_brushSettingsBT != null)
                    _brushSettingsBT.SetActive(true);
                if (_textureSettingsBT != null)
                    _textureSettingsBT.SetActive(true);
                _modeText.text = "Рисование текстур";
                break;
            case Mode.Spawner:
                if (_spawnerModeBT != null)
                    _spawnerModeBT.SetActive(true);
                _modeText.text = "Спавнер обьектов";
                break;
            default:
                break;
        }
    }

    private void ClosePreviousUI()
    {
        if (_previousOpenGameObject != null)
        {
            _previousOpenGameObject.SetActive(false);
            UnregisterOpenUI(_previousOpenGameObject);
            _previousOpenGameObject = null;
        }
    }

    public void SettingsMode(GameObject settingsPanel)
    {
        if (settingsPanel == null)
            return;

        _previousMode = _currentMode != Mode.Settings ? _currentMode : _previousMode;
        _currentMode = settingsPanel.activeInHierarchy ? _previousMode : Mode.Settings;
        bool active = _currentMode == Mode.Settings;
        settingsPanel.SetActive(active);
        _previousOpenGameObject = settingsPanel;
        if (active)
        {
            RegisterOpenUI(settingsPanel);
        }
        else
        {
            UnregisterOpenUI(settingsPanel);
        }

        _modeText.text = _currentMode.ToString();
    }

    public void SetPreviewMode()
    {
        _currentMode = _previousMode;
        SetModeUI();
    }

    public void RegisterOpenUI(GameObject ui)
    {
        if (ui != null && !_openUIStack.Contains(ui))
        {
            _openUIStack.Push(ui);
        }
    }

    public void UnregisterOpenUI(GameObject ui)
    {
        if (ui != null && _openUIStack.Contains(ui))
        {
            Stack<GameObject> tempStack = new Stack<GameObject>();
            while (_openUIStack.Count > 0)
            {
                GameObject top = _openUIStack.Pop();
                if (top != ui)
                {
                    tempStack.Push(top);
                }
                else
                {
                    break;
                }
            }

            while (tempStack.Count > 0)
            {
                _openUIStack.Push(tempStack.Pop());
            }
        }
    }

    public void ConfirmExit()
    {
        if (_confirmationPanel != null)
        {
            _confirmationPanel.SetActive(false);
            if (_openUIStack.Count > 0 && _openUIStack.Peek() == _confirmationPanel)
            {
                _openUIStack.Pop();
            }
        }

        SceneManager.LoadScene(0);
    }

    public void CancelExit()
    {
        if (_confirmationPanel != null && _confirmationPanel.activeSelf)
        {
            _confirmationPanel.SetActive(false);
            if (_openUIStack.Count > 0 && _openUIStack.Peek() == _confirmationPanel)
            {
                _openUIStack.Pop();
            }
        }
    }
}
