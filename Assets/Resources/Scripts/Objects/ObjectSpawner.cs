using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.IO;

public class ObjectSpawner : MonoBehaviour
{
    [System.Serializable]
    public class ObjectSpawnerSettings
    {
        public float rotationSliderValue;
    }
    [Header("Spawn settings")]
    [SerializeField] private GameObject[] _objectsToSpawn;
    [SerializeField] private Transform _spawnParent;

    [Header("UI Elements")]
    [SerializeField] private GameObject _spawnerScrollBar;
    [SerializeField] private GameObject _spawnScrollBarContent;
    [SerializeField] private GameObject _buttonPrefab;
    [SerializeField] private Slider _rotationSlider;

    private GameObject _selectedObjectPrefab;
    private List<SpawnedObjectData> _spawnedObjectsData = new List<SpawnedObjectData>();
    private GameObject _currentSelectedObject;
    private SpawnedObjectData _currentSelectedData;
    private string _settingsFilePath;
    private ObjectSpawnerSettings _settings;
    private void Start()
    {
        PopulateSpawnPanel();
        if(_rotationSlider != null)
        {
            _rotationSlider.onValueChanged.AddListener(UpdateObjectRotation);
            SetSlider();
        }
    }

    private void SetSlider()
    {
        _rotationSlider.minValue = 0f;
        _rotationSlider.maxValue = 360f;
        _rotationSlider.value = 0f;
    }

    private void Update()
    {
        if(_spawnerScrollBar == null)
            return;
        HandleObjectPlacement();
    }

    private void HandleObjectPlacement()
    {
        if (_selectedObjectPrefab == null)
            return;

        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI() && ModeSwitcher.Instance.CurrentMode == ModeSwitcher.Mode.Spawner)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject.GetComponent<Terrain>() != null)
                    SpawnObjectAtPosition(hit.point);
            }
        }
    }

    private void SpawnObjectAtPosition(Vector3 position)
    {
        float initialRotationY = _rotationSlider.value;
        Quaternion rotation = Quaternion.Euler(0, initialRotationY, 0);
        GameObject newObj = Instantiate(_selectedObjectPrefab, position, rotation, _spawnParent);
        newObj.name = _selectedObjectPrefab.name;

        SpawnedObjectData data = new SpawnedObjectData
        {
            prefabName = _selectedObjectPrefab.name,
            position = position,
            rotationY = initialRotationY
        };
        _spawnedObjectsData.Add(data);

        ObjectSelector selector = newObj.AddComponent<ObjectSelector>();
        selector.Initialize(this, data);
    }

    private void PopulateSpawnPanel()
    {
        if (_spawnerScrollBar == null) return;
        foreach (GameObject obj in _objectsToSpawn)
        {
            GameObject buttonObj = Instantiate(_buttonPrefab, _spawnScrollBarContent.transform);
            Button button = buttonObj.GetComponent<Button>();
            TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();
            buttonText.text = obj.name;

            button.onClick.AddListener(() => SelectObjectToSpawn(obj));
            button.onClick.AddListener(() => ModeSwitcher.Instance.SettingsMode(_spawnerScrollBar));
        }
    }

    public void SelectObjectToSpawn(GameObject obj)
    {
        _selectedObjectPrefab = obj;
        DeselectCurrentObject();
    }

    private bool IsPointerOverUI()
    {
        return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }

    public void SelectObjectForEditing(GameObject obj, SpawnedObjectData data)
    {
        _currentSelectedObject = obj;
        _currentSelectedData = data;
        _rotationSlider.value = data.rotationY;
        _rotationSlider.gameObject.SetActive(true);
    }

    public void DeselectCurrentObject()
    {
        _currentSelectedObject = null;
        _currentSelectedData = null;
    }

    public void UpdateObjectRotation(float value)
    {
        if (_currentSelectedObject != null && _currentSelectedData != null)
        {
            Vector3 eulerAngles = _currentSelectedObject.transform.eulerAngles;
            eulerAngles.y = value;
            _currentSelectedObject.transform.eulerAngles = eulerAngles;

            _currentSelectedData.rotationY = value;
        }
    }

    public void SetSpawnParent(Transform parent)
    {
        _spawnParent = parent;
    }

    public List<SpawnedObjectData> GetSpawnedObjectsData()
    {
        return _spawnedObjectsData;
    }

    public void LoadSpawnedObjects(List<SpawnedObjectData> objectsData)
    {
        ClearSpawnedObjects();

        foreach (SpawnedObjectData data in objectsData)
        {
            GameObject prefab = GetPrefabByName(data.prefabName);
            if (prefab != null)
            {
                Quaternion rotation = Quaternion.Euler(0, data.rotationY, 0);
                GameObject newObj = Instantiate(prefab, data.position, rotation, _spawnParent);
                newObj.name = prefab.name;

                ObjectSelector selector = newObj.AddComponent<ObjectSelector>();
                selector.Initialize(this, data);

                _spawnedObjectsData.Add(data);
            }
            else
            {
                Debug.LogWarning($"{data.prefabName} не найден");
            }
        }
    }

    public void ClearSpawnedObjects()
    {
        if (_spawnParent != null)
        {
            foreach (Transform child in _spawnParent)
                Destroy(child.gameObject);
        }
        _spawnedObjectsData.Clear();
        DeselectCurrentObject();
    }

    private GameObject GetPrefabByName(string name)
    {
        foreach (GameObject obj in _objectsToSpawn)
        {
            if (obj.name == name)
                return obj;
        }
        return null;
    }
    private void SaveSettings()
    {
        _settings = new ObjectSpawnerSettings
        {
            rotationSliderValue = _rotationSlider != null ? _rotationSlider.value : 0f
        };

        string json = JsonUtility.ToJson(_settings, true);
        try
        {
            File.WriteAllText(_settingsFilePath, json);
        }
        catch (IOException)
        {
        }
    }
    private void LoadSettings()
    {
        if (File.Exists(_settingsFilePath))
        {
            try
            {
                string json = File.ReadAllText(_settingsFilePath);
                _settings = JsonUtility.FromJson<ObjectSpawnerSettings>(json);

                _rotationSlider.value = _settings.rotationSliderValue;

                SetSlider();
            }
            catch (IOException)
            {
                _settings = new ObjectSpawnerSettings
                {
                    rotationSliderValue = _rotationSlider != null ? _rotationSlider.value : 0f
                };
                SaveSettings();
            }
        }
        else
        {
            _settings = new ObjectSpawnerSettings
            {
                rotationSliderValue = _rotationSlider != null ? _rotationSlider.value : 0f
            };
            SaveSettings();
        }
    }

    [System.Serializable]
    public class SpawnedObjectData
    {
        public string prefabName;
        public Vector3 position;
        public float rotationY;
    }
}
