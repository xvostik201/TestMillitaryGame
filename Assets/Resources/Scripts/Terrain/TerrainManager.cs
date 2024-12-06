using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using TMPro;

public class TerrainManager : MonoBehaviour
{
    [SerializeField] private TerrainData _defaultTerrainData;
    [SerializeField] private GameObject _defaultTerrainGameObject;
    private Terrain _terrainInstance;

    [Header("UI Elements")]
    [SerializeField] private TMP_InputField _saveInputField;
    [SerializeField] private GameObject _savePanel;
    [SerializeField] private GameObject _loadPanel;
    [SerializeField] private GameObject _buttonPrefab;
    [SerializeField] private Transform _contentParent;

    private ObjectSpawner _objectSpawner;

    public static TerrainManager Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        CreateTerrain();
        PopulateLoadList();
    }

    private void CreateTerrain()
    {
        TerrainData clonedTerrainData = CloneTerrainData(_defaultTerrainData);

        GameObject terrainGO = Terrain.CreateTerrainGameObject(clonedTerrainData);
        _terrainInstance = terrainGO.GetComponent<Terrain>();

        _terrainInstance.transform.position = _defaultTerrainGameObject.transform.position;

        _defaultTerrainGameObject.SetActive(false);

        UpdateReferences();
    }

    private void UpdateReferences()
    {
        TerrainEditor terrainEditor = FindObjectOfType<TerrainEditor>();
        if (terrainEditor != null)
            terrainEditor.SetTerrain(_terrainInstance);

        _objectSpawner = FindObjectOfType<ObjectSpawner>();
        if (_objectSpawner != null)
            _objectSpawner.SetSpawnParent(_terrainInstance.transform);
    }

    public TerrainData CloneTerrainData(TerrainData sourceTerrainData)
    {
        TerrainData newTerrainData = new TerrainData();

        newTerrainData.heightmapResolution = sourceTerrainData.heightmapResolution;
        newTerrainData.alphamapResolution = sourceTerrainData.alphamapResolution;
        newTerrainData.baseMapResolution = sourceTerrainData.baseMapResolution;
        newTerrainData.SetDetailResolution(sourceTerrainData.detailResolution, 8);
        newTerrainData.size = sourceTerrainData.size;

        int heightRes = sourceTerrainData.heightmapResolution;
        float[,] heights = sourceTerrainData.GetHeights(0, 0, heightRes, heightRes);
        newTerrainData.SetHeights(0, 0, heights);

        newTerrainData.terrainLayers = sourceTerrainData.terrainLayers;

        int alphaRes = sourceTerrainData.alphamapResolution;
        float[,,] alphamaps = sourceTerrainData.GetAlphamaps(0, 0, alphaRes, alphaRes);
        newTerrainData.SetAlphamaps(0, 0, alphamaps);

        newTerrainData.detailPrototypes = sourceTerrainData.detailPrototypes;
        int detailRes = sourceTerrainData.detailResolution;
        newTerrainData.SetDetailResolution(detailRes, 8);
        for (int i = 0; i < sourceTerrainData.detailPrototypes.Length; i++)
        {
            int[,] details = sourceTerrainData.GetDetailLayer(0, 0, detailRes, detailRes, i);
            newTerrainData.SetDetailLayer(0, 0, i, details);
        }

        newTerrainData.treePrototypes = sourceTerrainData.treePrototypes;
        newTerrainData.treeInstances = sourceTerrainData.treeInstances;

        return newTerrainData;
    }

    public void SaveTerrainButton()
    {
        string terrainName = _saveInputField.text.Trim();
        if (!string.IsNullOrEmpty(terrainName))
        {
            SaveTerrainData(terrainName);
            _savePanel.SetActive(false);
            _saveInputField.text = "";
        }
        else
        {
            Debug.LogWarning("Имя террейна не может быть пустым!");
        }
    }

    public void ShowLoadPanel()
    {
        _loadPanel.SetActive(true);
    }

    public void HideLoadPanel()
    {
        _loadPanel.SetActive(false);
    }

    public GameObject GetCurrentTerrain()
    {
        return _terrainInstance.gameObject;
    }

    private void PopulateLoadList()
    {
        string[] files = Directory.GetFiles(Application.persistentDataPath, "*_heights.json");
        foreach (string file in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            string terrainName = fileName.Replace("_heights", "");

            GameObject buttonObj = Instantiate(_buttonPrefab, _contentParent);
            Button button = buttonObj.GetComponent<Button>();
            TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();
            buttonText.text = terrainName;

            button.onClick.AddListener(() => LoadTerrainButton(terrainName));
        }
    }
    private void DestroyGameObjectList()
    {
        foreach(Transform child in _contentParent)
        {
            Destroy(child.gameObject);
        }
    }
    public void LoadTerrainButton(string terrainName)
    {
        LoadTerrainData(terrainName);
        HideLoadPanel();
    }

    public void SaveTerrainData(string terrainName)
    {
        SaveTerrainHeights(terrainName);
        SaveTerrainTextures(terrainName);
        SaveSpawnedObjects(terrainName);
        DestroyGameObjectList();
        PopulateLoadList();
    }

    public void LoadTerrainData(string terrainName)
    {
        if (_terrainInstance != null)
            Destroy(_terrainInstance.gameObject);

        CreateTerrain();

        LoadTerrainHeights(terrainName);
        LoadTerrainTextures(terrainName);

        UpdateReferences();

        LoadSpawnedObjects(terrainName);
    }

    public void SaveTerrainHeights(string terrainName)
    {
        TerrainData terrainData = _terrainInstance.terrainData;
        int width = terrainData.heightmapResolution;
        int height = terrainData.heightmapResolution;
        float[,] heights = terrainData.GetHeights(0, 0, width, height);

        float[] heights1D = new float[width * height];
        for (int i = 0; i < width; i++)
            for (int j = 0; j < height; j++)
                heights1D[i * height + j] = heights[i, j];

        string json = JsonUtility.ToJson(new HeightDataWrapper
        {
            heights = heights1D,
            width = width,
            height = height
        });

        string path = Path.Combine(Application.persistentDataPath, terrainName + "_heights.json");
        File.WriteAllText(path, json);
    }

    public void LoadTerrainHeights(string terrainName)
    {
        string path = Path.Combine(Application.persistentDataPath, terrainName + "_heights.json");
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            HeightDataWrapper dataWrapper = JsonUtility.FromJson<HeightDataWrapper>(json);

            float[,] heights = new float[dataWrapper.width, dataWrapper.height];
            for (int i = 0; i < dataWrapper.width; i++)
                for (int j = 0; j < dataWrapper.height; j++)
                    heights[i, j] = dataWrapper.heights[i * dataWrapper.height + j];

            _terrainInstance.terrainData.SetHeights(0, 0, heights);
        }
    }

    public void SaveTerrainTextures(string terrainName)
    {
        TerrainData terrainData = _terrainInstance.terrainData;
        int width = terrainData.alphamapWidth;
        int height = terrainData.alphamapHeight;
        int layers = terrainData.alphamapLayers;
        float[,,] alphamaps = terrainData.GetAlphamaps(0, 0, width, height);

        float[] alphamaps1D = new float[width * height * layers];
        int index = 0;
        for (int i = 0; i < width; i++)
            for (int j = 0; j < height; j++)
                for (int k = 0; k < layers; k++)
                    alphamaps1D[index++] = alphamaps[i, j, k];

        string json = JsonUtility.ToJson(new TextureDataWrapper
        {
            alphamaps = alphamaps1D,
            width = width,
            height = height,
            layers = layers
        });

        string path = Path.Combine(Application.persistentDataPath, terrainName + "_textures.json");
        File.WriteAllText(path, json);
    }

    public void LoadTerrainTextures(string terrainName)
    {
        string path = Path.Combine(Application.persistentDataPath, terrainName + "_textures.json");
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            TextureDataWrapper dataWrapper = JsonUtility.FromJson<TextureDataWrapper>(json);

            float[,,] alphamaps = new float[dataWrapper.width, dataWrapper.height, dataWrapper.layers];
            int index = 0;
            for (int i = 0; i < dataWrapper.width; i++)
                for (int j = 0; j < dataWrapper.height; j++)
                    for (int k = 0; k < dataWrapper.layers; k++)
                        alphamaps[i, j, k] = dataWrapper.alphamaps[index++];

            _terrainInstance.terrainData.SetAlphamaps(0, 0, alphamaps);
        }
    }

    public void SaveSpawnedObjects(string terrainName)
    {
        if (_objectSpawner != null)
        {
            List<ObjectSpawner.SpawnedObjectData> spawnedObjectsData = _objectSpawner.GetSpawnedObjectsData();

            string json = JsonUtility.ToJson(new SpawnedObjectsWrapper { objects = spawnedObjectsData });
            string path = Path.Combine(Application.persistentDataPath, terrainName + "_objects.json");
            File.WriteAllText(path, json);
        }
    }

    public void LoadSpawnedObjects(string terrainName)
    {
        if (_objectSpawner != null)
        {
            string path = Path.Combine(Application.persistentDataPath, terrainName + "_objects.json");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                SpawnedObjectsWrapper wrapper = JsonUtility.FromJson<SpawnedObjectsWrapper>(json);

                _objectSpawner.LoadSpawnedObjects(wrapper.objects);
            }
            else
            {
                _objectSpawner.ClearSpawnedObjects();
            }
        }
    }

    [System.Serializable]
    public class HeightDataWrapper
    {
        public float[] heights;
        public int width;
        public int height;
    }

    [System.Serializable]
    public class TextureDataWrapper
    {
        public float[] alphamaps;
        public int width;
        public int height;
        public int layers;
    }

    [System.Serializable]
    public class SpawnedObjectsWrapper
    {
        public List<ObjectSpawner.SpawnedObjectData> objects;
    }
}
