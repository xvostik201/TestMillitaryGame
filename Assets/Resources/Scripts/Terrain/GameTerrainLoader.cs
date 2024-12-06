using UnityEngine;
using System.IO;
using System.Collections.Generic;
using static MainMenu;

public class GameTerrainLoader : MonoBehaviour
{
    [Header("Terrain Settings")]
    [SerializeField] private TerrainData _defaultTerrainData;
    [SerializeField] private GameObject _defaultTerrainGameObject;

    [Header("References")]
    [SerializeField] private TerrainEditor _terrainEditor;
    [SerializeField] private ObjectSpawner _objectSpawner;

    private Terrain _terrainInstance;
    private string _settingsFilePath;

    private void Awake()
    {
        _settingsFilePath = Path.Combine(Application.persistentDataPath, "GameSettings.json");
    }

    private void Start()
    {
        LoadTerrain();
    }

    private void LoadTerrain()
    {
        string terrainName = "Default";

        if (File.Exists(_settingsFilePath))
        {
            string json = File.ReadAllText(_settingsFilePath);
            SelectedTerrainData data = JsonUtility.FromJson<SelectedTerrainData>(json);
            if (data != null && !string.IsNullOrEmpty(data.selectedTerrainName))
            {
                terrainName = data.selectedTerrainName;
            }
        }

        if (terrainName != "Default")
        {
            LoadSavedTerrain(terrainName);
        }
        else
        {
            CreateDefaultTerrain();
        }
    }

    private void CreateDefaultTerrain()
    {
        TerrainData clonedData = CloneTerrainData(_defaultTerrainData);
        GameObject terrainGO = Terrain.CreateTerrainGameObject(clonedData);
        _terrainInstance = terrainGO.GetComponent<Terrain>();

        _terrainInstance.transform.position = _defaultTerrainGameObject.transform.position;
        _defaultTerrainGameObject.SetActive(false);

        UpdateReferences();
    }

    private TerrainData CloneTerrainData(TerrainData source)
    {
        TerrainData clone = new TerrainData
        {
            heightmapResolution = source.heightmapResolution,
            alphamapResolution = source.alphamapResolution,
            baseMapResolution = source.baseMapResolution,
            size = source.size
        };

        float[,] heights = source.GetHeights(0, 0, source.heightmapResolution, source.heightmapResolution);
        clone.SetHeights(0, 0, heights);

        clone.terrainLayers = source.terrainLayers;
        float[,,] alphamaps = source.GetAlphamaps(0, 0, source.alphamapWidth, source.alphamapHeight);
        clone.SetAlphamaps(0, 0, alphamaps);

        clone.detailPrototypes = source.detailPrototypes;
        clone.SetDetailResolution(source.detailResolution, 8);
        for (int i = 0; i < source.detailPrototypes.Length; i++)
        {
            int[,] details = source.GetDetailLayer(0, 0, source.detailResolution, source.detailResolution, i);
            clone.SetDetailLayer(0, 0, i, details);
        }

        clone.treePrototypes = source.treePrototypes;
        clone.treeInstances = source.treeInstances;

        return clone;
    }

    private void LoadSavedTerrain(string terrainName)
    {
        if (_terrainInstance != null)
        {
            Destroy(_terrainInstance.gameObject);
        }

        CreateDefaultTerrain();

        LoadTerrainHeights(terrainName);
        LoadTerrainTextures(terrainName);
        LoadSpawnedObjects(terrainName);
    }

    private void LoadTerrainHeights(string terrainName)
    {
        string path = Path.Combine(Application.persistentDataPath, $"{terrainName}_heights.json");
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

    private void LoadTerrainTextures(string terrainName)
    {
        string path = Path.Combine(Application.persistentDataPath, $"{terrainName}_textures.json");
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

    private void LoadSpawnedObjects(string terrainName)
    {
        string path = Path.Combine(Application.persistentDataPath, $"{terrainName}_objects.json");
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            SpawnedObjectsWrapper wrapper = JsonUtility.FromJson<SpawnedObjectsWrapper>(json);

            if (_objectSpawner != null)
            {
                _objectSpawner.LoadSpawnedObjects(wrapper.objects);
            }
            else
            {
                _objectSpawner.ClearSpawnedObjects();
            }
        }
    }

    private void UpdateReferences()
    {
        if (_terrainEditor != null)
        {
            _terrainEditor.SetTerrain(_terrainInstance);
        }

        if (_objectSpawner != null)
        {
            _objectSpawner.SetSpawnParent(_terrainInstance.transform);
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
