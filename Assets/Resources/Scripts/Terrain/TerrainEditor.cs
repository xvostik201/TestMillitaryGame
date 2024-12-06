using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.IO;

[System.Serializable]
public class TerrainEditorSettings
{
    public float brushStrength;
    public float brushSize;
    public float stepOfDraw;
    public int selectedTextureIndex;
}

public class TerrainEditor : MonoBehaviour
{
    private Terrain _terrain;

    [Header("Brush settings")]
    [SerializeField] private float _brushStrength = 0.1f;
    [SerializeField] private float _brushSize = 5f;
    [SerializeField] private float _stepOfDraw = 0.1f;

    [Header("Painting textures")]
    [SerializeField] private int _selectedTextureIndex = 0;
    private TerrainLayer[] _terrainLayers;

    [Header("UI Elements")]
    [SerializeField] private Image _currentBrushImage;
    [SerializeField] private Sprite[] _brushIcons;

    [SerializeField] private Image _currentTextureImage;
    [SerializeField] private Sprite[] _textureIcons;

    [SerializeField] private GameObject _brushScrollView;
    [SerializeField] private GameObject _textureScrollView;

    [Header("Size slider")]
    [SerializeField, Range(1f, 30f)] private float _maxBrushSize = 30f;
    [SerializeField, Range(0.1f, 1f)] private float _minBrushSize = 1f;
    [SerializeField] private Slider _sizeSlider;

    [Header("Strength slider")]
    [SerializeField, Range(0.01f, 5f)] private float _maxBrushStrength = 5f;
    [SerializeField, Range(0.001f, 0.1f)] private float _minBrushStrength = 0.01f;
    [SerializeField] private Slider _strengthSlider;

    [Header("Step of draw slider")]
    [SerializeField, Range(0.01f, 1f)] private float _maxStepOfDraw = 5f;
    [SerializeField, Range(0.01f, 1f)] private float _minStepOfDraw = 0.01f;
    [SerializeField] private Slider _stepOfDrawSlider;

    private ModeSwitcher _modeSwitcher;
    private float _drawTimer = 0f;
    private int _currentTextureIndex;
    private enum BrushType { Circle, Square, Eraser }
    private BrushType _currentBrush = BrushType.Circle;

    private bool _isDrawing = false;
    private Vector3 _lastDrawPosition;

    private TerrainEditorSettings _settings;
    private string _settingsFilePath;

    private void Awake()
    {
        _settingsFilePath = Path.Combine(Application.persistentDataPath, "TerrainEditorSettings.json");
        LoadSettings();
    }

    private void Start()
    {
        _modeSwitcher = FindObjectOfType<ModeSwitcher>();
        SetSliders();
        UpdateBrushIcon();
        UpdateTextureIcon();


        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnLeftClick += StartDrawing;
            InputManager.Instance.OnLeftRelease += StopDrawing;
            InputManager.Instance.OnMouseMove += HandleMouseMove;
            InputManager.Instance.OnTouchBegin += StartDrawing;
            InputManager.Instance.OnTouchDraw += HandleTouchMove;
            InputManager.Instance.OnTouchEnd += StopDrawing;
        }
    }

    private void OnDestroy()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnLeftClick -= StartDrawing;
            InputManager.Instance.OnLeftRelease -= StopDrawing;
            InputManager.Instance.OnMouseMove -= HandleMouseMove;
            InputManager.Instance.OnTouchBegin -= StartDrawing;
            InputManager.Instance.OnTouchDraw -= HandleTouchMove;
            InputManager.Instance.OnTouchEnd -= StopDrawing;
        }
    }

    public void SetTerrain(Terrain newTerrain)
    {
        _terrain = newTerrain;
        _terrainLayers = _terrain.terrainData.terrainLayers;
    }

    private void Update()
    {
        if (_terrain == null || _modeSwitcher == null)
            return;
        if (_modeSwitcher.CurrentMode == ModeSwitcher.Mode.Draw || _modeSwitcher.CurrentMode == ModeSwitcher.Mode.Texture)
        {
            HandleTerrainEditing();
        }
    }

    private void HandleTerrainEditing()
    {
        _drawTimer += Time.deltaTime;
    }

    private void StartDrawing()
    {
        _isDrawing = true;
        _drawTimer = _stepOfDraw;
    }

    private void StopDrawing()
    {
        _isDrawing = false;
        _drawTimer = 0f;
    }

    private void HandleMouseMove(Vector2 delta)
    {
        if (_isDrawing && _drawTimer >= _stepOfDraw && !IsPointerOverUI())
        {
            _drawTimer = 0f;
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject == _terrain.gameObject)
                {
                    if (_modeSwitcher.CurrentMode == ModeSwitcher.Mode.Draw)
                        EditTerrainAtPoint(hit.point);
                    else if (_modeSwitcher.CurrentMode == ModeSwitcher.Mode.Texture)
                        PaintTextureAtPoint(hit.point);
                }
            }
        }
    }

    private void HandleTouchMove(Vector2 touchDelta)
    {
        if (_isDrawing && _drawTimer >= _stepOfDraw && !IsPointerOverUI())
        {
            _drawTimer = 0f;
            Ray ray = Camera.main.ScreenPointToRay(touchDelta);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject == _terrain.gameObject)
                {
                    if (_modeSwitcher.CurrentMode == ModeSwitcher.Mode.Draw)
                        EditTerrainAtPoint(hit.point);
                    else if (_modeSwitcher.CurrentMode == ModeSwitcher.Mode.Texture)
                        PaintTextureAtPoint(hit.point);
                }
            }
        }
    }
    private bool IsPointerOverUI()
    {
#if UNITY_EDITOR
        return EventSystem.current.IsPointerOverGameObject();
#else
        if (Input.touchCount > 0)
        {
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        }
        return false;
#endif
    }
    private void EditTerrainAtPoint(Vector3 point)
    {
        TerrainData terrainData = _terrain.terrainData;

        int terrainWidth = terrainData.heightmapResolution;
        int terrainHeight = terrainData.heightmapResolution;

        Vector3 terrainPos = _terrain.transform.position;

        float relativeX = (point.x - terrainPos.x) / terrainData.size.x;
        float relativeZ = (point.z - terrainPos.z) / terrainData.size.z;

        int xBase = Mathf.RoundToInt(relativeX * terrainWidth);
        int zBase = Mathf.RoundToInt(relativeZ * terrainHeight);

        int brushSizeInt = Mathf.RoundToInt(_brushSize);
        int offset = brushSizeInt / 2;

        int xStart = Mathf.Clamp(xBase - offset, 0, terrainWidth - 1);
        int zStart = Mathf.Clamp(zBase - offset, 0, terrainHeight - 1);

        int xEnd = Mathf.Clamp(xBase + offset, 0, terrainWidth - 1);
        int zEnd = Mathf.Clamp(zBase + offset, 0, terrainHeight - 1);

        int width = xEnd - xStart + 1;
        int height = zEnd - zStart + 1;

        float[,] heights = terrainData.GetHeights(xStart, zStart, width, height);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                float xCoord = x - width / 2f;
                float zCoord = z - height / 2f;
                float distance = Mathf.Sqrt(xCoord * xCoord + zCoord * zCoord);

                bool withinBrushArea = false;
                float strength = 0f;

                if (_currentBrush == BrushType.Circle)
                {
                    if (distance <= _brushSize / 2f)
                    {
                        withinBrushArea = true;
                        strength = _brushStrength * (1 - (distance / (_brushSize / 2f)));
                    }
                }
                else if (_currentBrush == BrushType.Square)
                {
                    if (Mathf.Abs(xCoord) <= _brushSize / 2f && Mathf.Abs(zCoord) <= _brushSize / 2f)
                    {
                        withinBrushArea = true;
                        strength = _brushStrength;
                    }
                }
                else if (_currentBrush == BrushType.Eraser)
                {
                    if (distance <= _brushSize / 2f)
                    {
                        withinBrushArea = true;
                        strength = -_brushStrength * (1 - (distance / (_brushSize / 2f)));
                    }
                }

                if (withinBrushArea)
                {
                    heights[z, x] += strength;
                    heights[z, x] = Mathf.Clamp01(heights[z, x]);
                }
            }
        }

        terrainData.SetHeights(xStart, zStart, heights);
    }

    private void PaintTextureAtPoint(Vector3 point)
    {
        TerrainData terrainData = _terrain.terrainData;

        int alphamapWidth = terrainData.alphamapWidth;
        int alphamapHeight = terrainData.alphamapHeight;

        Vector3 terrainPos = _terrain.transform.position;

        float relativeX = (point.x - terrainPos.x) / terrainData.size.x;
        float relativeZ = (point.z - terrainPos.z) / terrainData.size.z;

        int x = Mathf.FloorToInt(relativeX * alphamapWidth);
        int z = Mathf.FloorToInt(relativeZ * alphamapHeight);

        int brushSize = Mathf.RoundToInt(_brushSize);
        int halfBrushSize = brushSize / 2;

        int xStart = Mathf.Clamp(x - halfBrushSize, 0, alphamapWidth - 1);
        int zStart = Mathf.Clamp(z - halfBrushSize, 0, alphamapHeight - 1);

        int xEnd = Mathf.Clamp(x + halfBrushSize, 0, alphamapWidth);
        int zEnd = Mathf.Clamp(z + halfBrushSize, 0, alphamapHeight);

        int width = xEnd - xStart;
        int height = zEnd - zStart;

        float[,,] alphamaps = terrainData.GetAlphamaps(xStart, zStart, width, height);

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                float xCoord = i - width / 2f;
                float zCoord = j - height / 2f;
                float distance = Mathf.Sqrt(xCoord * xCoord + zCoord * zCoord);

                bool withinBrushArea = false;
                float strength = 0f;

                if (_currentBrush == BrushType.Circle)
                {
                    if (distance <= _brushSize / 2f)
                    {
                        withinBrushArea = true;
                        strength = _brushStrength * (1 - (distance / (_brushSize / 2f)));
                    }
                }
                else if (_currentBrush == BrushType.Square)
                {
                    if (Mathf.Abs(xCoord) <= _brushSize / 2f && Mathf.Abs(zCoord) <= _brushSize / 2f)
                    {
                        withinBrushArea = true;
                        strength = _brushStrength;
                    }
                }

                if (withinBrushArea)
                {
                    float total = 0f;
                    for (int layer = 0; layer < terrainData.alphamapLayers; layer++)
                    {
                        if (layer == _selectedTextureIndex)
                        {
                            alphamaps[j, i, layer] += strength;
                        }
                        else
                        {
                            alphamaps[j, i, layer] -= strength / (terrainData.alphamapLayers - 1);
                        }
                        alphamaps[j, i, layer] = Mathf.Clamp01(alphamaps[j, i, layer]);
                        total += alphamaps[j, i, layer];
                    }
                    for (int layer = 0; layer < terrainData.alphamapLayers; layer++)
                    {
                        alphamaps[j, i, layer] /= total;
                    }
                }
            }
        }

        terrainData.SetAlphamaps(xStart, zStart, alphamaps);
    }

    public void SetBrushSize()
    {
        _brushSize = _sizeSlider.value;
        SaveSettings();
    }

    public void SetBrushStrength()
    {
        _brushStrength = _strengthSlider.value;
        SaveSettings();
    }

    public void SetStepOfDraw()
    {
        _stepOfDraw = _stepOfDrawSlider.value;
        SaveSettings();
    }

    private void SetSliders()
    {
        if (_sizeSlider != null)
        {
            _sizeSlider.maxValue = _maxBrushSize;
            _sizeSlider.minValue = _minBrushSize;
            _sizeSlider.value = _brushSize;
            SetBrushSize();
        }

        if (_strengthSlider != null)
        {
            _strengthSlider.maxValue = _maxBrushStrength;
            _strengthSlider.minValue = _minBrushStrength;
            _strengthSlider.value = _brushStrength;
            SetBrushStrength();
        }

        if (_stepOfDrawSlider != null)
        {
            _stepOfDrawSlider.maxValue = _maxStepOfDraw;
            _stepOfDrawSlider.minValue = _minStepOfDraw;
            _stepOfDrawSlider.value = _stepOfDraw;
            SetStepOfDraw();
        }
    }

    public void SelectBrush(int brushIndex)
    {
        _currentBrush = (BrushType)brushIndex;
        UpdateBrushIcon();
        _brushScrollView.SetActive(false);
        _modeSwitcher.SetPreviewMode();
    }

    private void UpdateBrushIcon()
    {
        if (_currentBrushImage != null && _brushIcons.Length > (int)_currentBrush)
        {
            _currentBrushImage.sprite = _brushIcons[(int)_currentBrush];
        }
    }

    private void UpdateTextureIcon()
    {
        if (_currentTextureImage != null && _textureIcons.Length > _selectedTextureIndex)
        {
            _currentTextureImage.sprite = _textureIcons[_selectedTextureIndex];
        }
    }

    public void SelectTexture(int textureIndex)
    {
        _currentTextureIndex = textureIndex;
        if (textureIndex >= 0 && textureIndex < _terrainLayers.Length)
        {
            _selectedTextureIndex = textureIndex;
        }
        UpdateTextureIcon();
        _modeSwitcher.SetPreviewMode();
        _textureScrollView.SetActive(false);
        SaveSettings();
    }

    private void SaveSettings()
    {
        _settings = new TerrainEditorSettings
        {
            brushStrength = _brushStrength,
            brushSize = _brushSize,
            stepOfDraw = _stepOfDraw,
            selectedTextureIndex = _selectedTextureIndex
        };

        string json = JsonUtility.ToJson(_settings, true);
        File.WriteAllText(_settingsFilePath, json);
        
    }

    private void LoadSettings()
    {
        if (File.Exists(_settingsFilePath))
        {
            try
            {
                string json = File.ReadAllText(_settingsFilePath);
                _settings = JsonUtility.FromJson<TerrainEditorSettings>(json);

                _brushStrength = _settings.brushStrength;
                _brushSize = _settings.brushSize;
                _stepOfDraw = _settings.stepOfDraw;
                _selectedTextureIndex = _settings.selectedTextureIndex;

                if (_terrain != null)
                {
                    _terrainLayers = _terrain.terrainData.terrainLayers;
                }

                SetSliders();
                UpdateBrushIcon();
                UpdateTextureIcon();
            }
            catch (IOException)
            {
                _settings = new TerrainEditorSettings
                {
                    brushStrength = _brushStrength,
                    brushSize = _brushSize,
                    stepOfDraw = _stepOfDraw,
                    selectedTextureIndex = _selectedTextureIndex
                };
                SaveSettings();
            }
        }
        else
        {
            _settings = new TerrainEditorSettings
            {
                brushStrength = _brushStrength,
                brushSize = _brushSize,
                stepOfDraw = _stepOfDraw,
                selectedTextureIndex = _selectedTextureIndex
            };
            SaveSettings();
        }
    }
}
