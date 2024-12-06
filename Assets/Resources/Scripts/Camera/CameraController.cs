using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

[System.Serializable]
public class CameraSettings
{
    public float maxCameraSpeed;
    public float minCameraSpeed;
    public float moveSpeed;

    public float zoomSpeed;
    public float maxZoomSensitivity;
    public float minZoomSensitivity;
    public float zoomSensitivity;

    public float maxCameraHeight;
    public float minCameraHeight;
    public float cameraHeight;

    public float maxRotationSpeed;
    public float minRotationSpeed;
    public float rotationSpeed;
}

public class CameraController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button _modeToggleButton;
    [SerializeField] private TMP_Text _modeToggleButtonText;
    [SerializeField] private Button _axisToggleButton;
    [SerializeField] private TMP_Text _axisToggleButtonText;

    [Header("Movement Settings")]
    [SerializeField, Range(0, 20f)] private float _maxCameraSpeed = 20f;
    [SerializeField, Range(0.01f, 1f)] private float _minCameraSpeed = 1f;
    [SerializeField] private Slider _speedSlider;

    [Header("Zoom Settings")]
    [SerializeField, Range(0.1f, 2f)] private float _zoomSpeed = 0.5f;

    [Header("Zoom Sensitivity")]
    [SerializeField, Range(1f, 100f)] private float _maxZoomSensitivity = 100f;
    [SerializeField, Range(1f, 5f)] private float _minZoomSensitivity = 5f;
    [SerializeField] private Slider _zoomSensitivitySlider;

    [Header("Height Slider")]
    [SerializeField, Range(0, 150)] private float _maxCameraHeight = 100f;
    [SerializeField, Range(0, 10f)] private float _minCameraHeight = 5f;
    [SerializeField] private Slider _heightSlider;

    [Header("Rotation Speed Settings")]
    [SerializeField, Range(0.1f, 5f)] private float _maxRotationSpeed = 5f;
    [SerializeField, Range(0.1f, 1f)] private float _minRotationSpeed = 0.1f;
    [SerializeField] private Slider _rotationSpeedSlider;

    [Header("Constraints")]
    [SerializeField] private float _errorIndentation = 10f;

    [Header("Colors")]
    [SerializeField] private Color _yAxisColor = new Color(0f, 0.588f, 0f);
    [SerializeField] private Color _xAxisColor = new Color(0.705f, 0f, 0f);

    private float _moveSpeed;
    private float _rotationSpeed;
    private float _zoomSensitivity;
    private float _height;

    private CameraMode _currentCameraMode = CameraMode.Move;
    private RotationAxis _currentRotationAxis = RotationAxis.Y;

    private bool _isDragging = false;

    private enum CameraMode { Move, Rotate, Zoom }
    private enum RotationAxis { X, Y }

    private float _previousPinchDistance;
    private bool _isPinching = false;

    private CameraSettings _cameraSettings;
    private string _settingsFilePath;

    private void Awake()
    {
        _settingsFilePath = Path.Combine(Application.persistentDataPath, "CameraSettings.json");
        LoadSettings();
    }

    private void Start()
    {
        SetSliders();

        if (_modeToggleButton != null)
        {
            _modeToggleButton.onClick.AddListener(ToggleCameraMode);
            UpdateModeToggleButtonText();
        }

        if (_axisToggleButton != null)
        {
            _axisToggleButton.onClick.AddListener(ToggleRotationAxis);
            UpdateAxisToggleButtonText();
            _axisToggleButton.gameObject.SetActive(false);
        }

        if (_speedSlider != null)
        {
            _speedSlider.onValueChanged.AddListener(SetSpeed);
        }

        if (_heightSlider != null)
        {
            _heightSlider.onValueChanged.AddListener(SetHeight);
        }

        if (_rotationSpeedSlider != null)
        {
            _rotationSpeedSlider.onValueChanged.AddListener(SetRotationSpeed);
        }

        if (_zoomSensitivitySlider != null)
        {
            _zoomSensitivitySlider.onValueChanged.AddListener(SetZoomSensitivity);
        }

        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnLeftClick += HandleLeftClick;
            InputManager.Instance.OnLeftRelease += HandleLeftRelease;
            InputManager.Instance.OnMouseMove += HandleMouseMove;
            InputManager.Instance.OnScroll += HandleScroll;

            InputManager.Instance.OnTouchBegin += HandleTouchBegin;
            InputManager.Instance.OnTouchCamera += HandleTouchMove;
            InputManager.Instance.OnTouchEnd += HandleTouchEnd;
        }
    }

    private void OnDestroy()
    {
        if (_modeToggleButton != null)
        {
            _modeToggleButton.onClick.RemoveListener(ToggleCameraMode);
        }

        if (_axisToggleButton != null)
        {
            _axisToggleButton.onClick.RemoveListener(ToggleRotationAxis);
        }

        if (_speedSlider != null)
        {
            _speedSlider.onValueChanged.RemoveListener(SetSpeed);
        }

        if (_heightSlider != null)
        {
            _heightSlider.onValueChanged.RemoveListener(SetHeight);
        }

        if (_rotationSpeedSlider != null)
        {
            _rotationSpeedSlider.onValueChanged.RemoveListener(SetRotationSpeed);
        }

        if (_zoomSensitivitySlider != null)
        {
            _zoomSensitivitySlider.onValueChanged.RemoveListener(SetZoomSensitivity);
        }

        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnLeftClick -= HandleLeftClick;
            InputManager.Instance.OnLeftRelease -= HandleLeftRelease;
            InputManager.Instance.OnMouseMove -= HandleMouseMove;
            InputManager.Instance.OnScroll -= HandleScroll;

            InputManager.Instance.OnTouchBegin -= HandleTouchBegin;
            InputManager.Instance.OnTouchCamera -= HandleTouchMove;
            InputManager.Instance.OnTouchEnd -= HandleTouchEnd;
        }
    }

    private void ToggleCameraMode()
    {
        switch (_currentCameraMode)
        {
            case CameraMode.Move:
                _currentCameraMode = CameraMode.Rotate;
                break;
            case CameraMode.Rotate:
                _currentCameraMode = CameraMode.Zoom;
                break;
            case CameraMode.Zoom:
                _currentCameraMode = CameraMode.Move;
                break;
        }

        UpdateModeToggleButtonText();

        if (_axisToggleButton != null)
        {
            _axisToggleButton.gameObject.SetActive(_currentCameraMode == CameraMode.Rotate);
        }

        SaveSettings();
    }

    private void UpdateModeToggleButtonText()
    {
        if (_modeToggleButtonText != null)
        {
            switch (_currentCameraMode)
            {
                case CameraMode.Move:
                    _modeToggleButtonText.text = "Мод: Передвижения";
                    break;
                case CameraMode.Rotate:
                    _modeToggleButtonText.text = "Мод: Поворот";
                    break;
                case CameraMode.Zoom:
                    _modeToggleButtonText.text = "Мод: Зум";
                    break;
            }
        }
    }

    private void ToggleRotationAxis()
    {
        if (_currentRotationAxis == RotationAxis.X)
        {
            _currentRotationAxis = RotationAxis.Y;
        }
        else
        {
            _currentRotationAxis = RotationAxis.X;
        }

        UpdateAxisToggleButtonText();
        SaveSettings();
    }

    private void UpdateAxisToggleButtonText()
    {
        if (_axisToggleButtonText != null)
        {
            _axisToggleButtonText.text = _currentRotationAxis == RotationAxis.X ? "X" : "Y";
            _axisToggleButtonText.color = _currentRotationAxis == RotationAxis.X ? _xAxisColor : _yAxisColor;
        }
    }

    private void ZoomCamera(float increment)
    {
        Vector3 pos = transform.position;
        pos.y -= increment;
        pos.y = Mathf.Clamp(pos.y, _minCameraHeight, _maxCameraHeight);
        transform.position = pos;

        if (_heightSlider != null)
        {
            _heightSlider.value = pos.y;
        }

        _height = pos.y;

        SaveSettings();
    }

    private void RotateCameraX(float angle)
    {
        Vector3 eulerAngles = transform.localEulerAngles;
        eulerAngles.x += angle;
        eulerAngles.x = ClampAngle(eulerAngles.x, 10f, 80f);
        transform.localEulerAngles = eulerAngles;

        SaveSettings();
    }

    private void RotateCameraY(float angle)
    {
        Vector3 eulerAngles = transform.eulerAngles;
        eulerAngles.y += angle;
        transform.eulerAngles = eulerAngles;

        SaveSettings();
    }

    private float ClampAngle(float angle, float min, float max)
    {
        if (angle > 180f)
            angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }

    private void LateUpdate()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, -_errorIndentation, 1000);
        pos.z = Mathf.Clamp(pos.z, -_errorIndentation, 1000);
        transform.position = pos;
    }

    private void SetHeight(float value)
    {
        Vector3 pos = transform.position;
        pos.y = value;
        pos.y = Mathf.Clamp(pos.y, _minCameraHeight, _maxCameraHeight);
        transform.position = pos;

        _height = pos.y;

        SaveSettings();
    }

    private void SetSpeed(float value)
    {
        _moveSpeed = value;
        SaveSettings();
    }

    private void SetRotationSpeed(float value)
    {
        _rotationSpeed = value;
        SaveSettings();
    }

    private void SetZoomSensitivity(float value)
    {
        _zoomSensitivity = value;
        SaveSettings();
    }

    private void SetSliders()
    {
        if (_heightSlider != null)
        {
            _heightSlider.maxValue = _maxCameraHeight;
            _heightSlider.minValue = _minCameraHeight;
            _heightSlider.value = Mathf.Clamp(transform.position.y, _minCameraHeight, _maxCameraHeight);
            _height = _heightSlider.value;
            SetHeight(_heightSlider.value);
        }

        if (_speedSlider != null)
        {
            _speedSlider.maxValue = _maxCameraSpeed;
            _speedSlider.minValue = _minCameraSpeed;
            _speedSlider.value = _moveSpeed;
            SetSpeed(_speedSlider.value);
        }

        if (_rotationSpeedSlider != null)
        {
            _rotationSpeedSlider.maxValue = _maxRotationSpeed;
            _rotationSpeedSlider.minValue = _minRotationSpeed;
            _rotationSpeedSlider.value = _rotationSpeed;
            SetRotationSpeed(_rotationSpeedSlider.value);
        }

        if (_zoomSensitivitySlider != null)
        {
            _zoomSensitivitySlider.maxValue = _maxZoomSensitivity;
            _zoomSensitivitySlider.minValue = _minZoomSensitivity;
            _zoomSensitivitySlider.value = _zoomSensitivity;
            SetZoomSensitivity(_zoomSensitivitySlider.value);
        }
    }

    private void HandleLeftClick()
    {
        _isDragging = true;
    }

    private void HandleLeftRelease()
    {
        _isDragging = false;
    }

    private void HandleMouseMove(Vector2 delta)
    {
        if (_isDragging && ModeSwitcher.Instance != null &&
            ModeSwitcher.Instance.CurrentMode != ModeSwitcher.Mode.Settings &&
            ModeSwitcher.Instance.CurrentMode == ModeSwitcher.Mode.Camera)
        {
            if (_currentCameraMode == CameraMode.Move)
            {
                Vector3 forward = transform.forward;
                Vector3 right = transform.right;

                forward.y = 0;
                right.y = 0;
                forward.Normalize();
                right.Normalize();

                Vector3 movement = (delta.x * -right + delta.y * -forward) * _moveSpeed * Time.deltaTime;
                transform.position += movement;
            }
            else if (_currentCameraMode == CameraMode.Rotate)
            {
                if (_currentRotationAxis == RotationAxis.Y)
                {
                    RotateCameraY(delta.x * _rotationSpeed * Time.deltaTime);
                }
                else if (_currentRotationAxis == RotationAxis.X)
                {
                    RotateCameraX(-delta.y * _rotationSpeed * Time.deltaTime);
                }
            }
        }
    }

    private void HandleScroll(float scrollDelta)
    {
        if (ModeSwitcher.Instance != null &&
            ModeSwitcher.Instance.CurrentMode != ModeSwitcher.Mode.Settings &&
            ModeSwitcher.Instance.CurrentMode == ModeSwitcher.Mode.Camera)
        {
            ZoomCamera(scrollDelta * _zoomSpeed * 10f);
        }
    }

    private void HandleTouchBegin()
    {
        _isDragging = true;
    }

    private void HandleTouchMove(Vector2 touchDelta)
    {
        if (_isDragging && ModeSwitcher.Instance != null &&
            ModeSwitcher.Instance.CurrentMode != ModeSwitcher.Mode.Settings &&
            ModeSwitcher.Instance.CurrentMode == ModeSwitcher.Mode.Camera)
        {
            if (_currentCameraMode == CameraMode.Move)
            {
                Vector3 forward = transform.forward;
                Vector3 right = transform.right;

                forward.y = 0;
                right.y = 0;
                forward.Normalize();
                right.Normalize();

                Vector3 movement = (touchDelta.x * -right + touchDelta.y * -forward) * _moveSpeed * Time.deltaTime;
                transform.position += movement;
            }
            else if (_currentCameraMode == CameraMode.Rotate)
            {
                if (_currentRotationAxis == RotationAxis.Y)
                {
                    RotateCameraY(touchDelta.x * _rotationSpeed * Time.deltaTime);
                }
                else if (_currentRotationAxis == RotationAxis.X)
                {
                    RotateCameraX(-touchDelta.y * _rotationSpeed * Time.deltaTime);
                }
            }
            else if (_currentCameraMode == CameraMode.Zoom)
            {
                ZoomCamera(touchDelta.y * _zoomSensitivity * Time.deltaTime);
            }
        }
    }

    private void HandleTouchEnd()
    {
        _isDragging = false;
    }

    private void Update()
    {
        if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
            {
                _previousPinchDistance = Vector2.Distance(touch0.position, touch1.position);
                _isPinching = true;
            }
            else if (_isPinching && (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved))
            {
                float currentDistance = Vector2.Distance(touch0.position, touch1.position);
                float delta = currentDistance - _previousPinchDistance;
                _previousPinchDistance = currentDistance;

                Zoom(delta * _zoomSensitivity * Time.deltaTime);
            }

            if (touch0.phase == TouchPhase.Ended || touch0.phase == TouchPhase.Canceled ||
                touch1.phase == TouchPhase.Ended || touch1.phase == TouchPhase.Canceled)
            {
                _isPinching = false;
            }
        }
    }

    private void Zoom(float increment)
    {
        if (_isDragging && ModeSwitcher.Instance != null &&
            ModeSwitcher.Instance.CurrentMode != ModeSwitcher.Mode.Settings &&
            ModeSwitcher.Instance.CurrentMode == ModeSwitcher.Mode.Camera &&
            _currentCameraMode == CameraMode.Zoom)
        {
            ZoomCamera(increment);
        }
    }

    private void SaveSettings()
    {
        _cameraSettings = new CameraSettings
        {
            maxCameraSpeed = _maxCameraSpeed,
            minCameraSpeed = _minCameraSpeed,
            moveSpeed = _moveSpeed,

            zoomSpeed = _zoomSpeed,
            maxZoomSensitivity = _maxZoomSensitivity,
            minZoomSensitivity = _minZoomSensitivity,
            zoomSensitivity = _zoomSensitivity,

            maxCameraHeight = _maxCameraHeight,
            minCameraHeight = _minCameraHeight,
            cameraHeight = _height,

            maxRotationSpeed = _maxRotationSpeed,
            minRotationSpeed = _minRotationSpeed,
            rotationSpeed = _rotationSpeed
        };

        string json = JsonUtility.ToJson(_cameraSettings, true);
        File.WriteAllText(_settingsFilePath, json);
    }

    private void LoadSettings()
    {
        if (File.Exists(_settingsFilePath))
        {
            try
            {
                string json = File.ReadAllText(_settingsFilePath);
                _cameraSettings = JsonUtility.FromJson<CameraSettings>(json);

                _maxCameraSpeed = _cameraSettings.maxCameraSpeed;
                _minCameraSpeed = _cameraSettings.minCameraSpeed;
                _moveSpeed = _cameraSettings.moveSpeed;

                _zoomSpeed = _cameraSettings.zoomSpeed;
                _maxZoomSensitivity = _cameraSettings.maxZoomSensitivity;
                _minZoomSensitivity = _cameraSettings.minZoomSensitivity;
                _zoomSensitivity = _cameraSettings.zoomSensitivity;

                _maxCameraHeight = _cameraSettings.maxCameraHeight;
                _minCameraHeight = _cameraSettings.minCameraHeight;
                _height = _cameraSettings.cameraHeight;

                _maxRotationSpeed = _cameraSettings.maxRotationSpeed;
                _minRotationSpeed = _cameraSettings.minRotationSpeed;
                _rotationSpeed = _cameraSettings.rotationSpeed;

                Vector3 pos = transform.position;
                pos.y = Mathf.Clamp(_height, _minCameraHeight, _maxCameraHeight);
                transform.position = pos;

                SetSliders();
            }
            catch (IOException e)
            {
                _cameraSettings = new CameraSettings
                {
                    maxCameraSpeed = _maxCameraSpeed,
                    minCameraSpeed = _minCameraSpeed,
                    moveSpeed = _speedSlider != null ? _speedSlider.value : _moveSpeed,

                    zoomSpeed = _zoomSpeed,
                    maxZoomSensitivity = _maxZoomSensitivity,
                    minZoomSensitivity = _minZoomSensitivity,
                    zoomSensitivity = _zoomSensitivitySlider != null ? _zoomSensitivitySlider.value : _zoomSensitivity,

                    maxCameraHeight = _maxCameraHeight,
                    minCameraHeight = _minCameraHeight,
                    cameraHeight = _heightSlider != null ? _heightSlider.value : _height,

                    maxRotationSpeed = _maxRotationSpeed,
                    minRotationSpeed = _minRotationSpeed,
                    rotationSpeed = _rotationSpeedSlider != null ? _rotationSpeedSlider.value : _rotationSpeed
                };
                SaveSettings();
            }
        }
        else
        {
            _cameraSettings = new CameraSettings
            {
                maxCameraSpeed = _maxCameraSpeed,
                minCameraSpeed = _minCameraSpeed,
                moveSpeed = _speedSlider != null ? _speedSlider.value : _moveSpeed,

                zoomSpeed = _zoomSpeed,
                maxZoomSensitivity = _maxZoomSensitivity,
                minZoomSensitivity = _minZoomSensitivity,
                zoomSensitivity = _zoomSensitivitySlider != null ? _zoomSensitivitySlider.value : _zoomSensitivity,

                maxCameraHeight = _maxCameraHeight,
                minCameraHeight = _minCameraHeight,
                cameraHeight = _heightSlider != null ? _heightSlider.value : _height,

                maxRotationSpeed = _maxRotationSpeed,
                minRotationSpeed = _minRotationSpeed,
                rotationSpeed = _rotationSpeedSlider != null ? _rotationSpeedSlider.value : _rotationSpeed
            };
            SaveSettings();
        }
    }
}
