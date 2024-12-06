using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System;

public class MainMenu : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject _mainMenuPanel;
    [SerializeField] private GameObject _terrainListPanel;

    [Header("Buttons")]
    [SerializeField] private Button _startGameButton;
    [SerializeField] private Button _mapEditorButton;
    [SerializeField] private Button _exitButton;

    [Header("Terrain List")]
    [SerializeField] private Transform _terrainListContent;
    [SerializeField] private GameObject _terrainButtonPrefab;

    [Header("Back Button")]
    [SerializeField] private Button _backButton;

    private string _selectedTerrainName;
    private string _settingsFilePath;

    private void Awake()
    {
        _settingsFilePath = Path.Combine(Application.persistentDataPath, "GameSettings.json");
    }

    private void Start()
    {
        _terrainListPanel.SetActive(false);

        _startGameButton.onClick.AddListener(OnStartGameButtonClicked);
        _mapEditorButton.onClick.AddListener(OnMapEditorButtonClicked);
        _exitButton.onClick.AddListener(OnExitButtonClicked);

        if (_backButton != null)
        {
            _backButton.onClick.AddListener(OnBackButtonClicked);
        }
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
        if (_terrainListPanel.activeSelf)
        {
            OnBackButtonClicked();
        }
    }

    private void OnStartGameButtonClicked()
    {
        _mainMenuPanel.SetActive(false);
        _terrainListPanel.SetActive(true);
        PopulateTerrainList();
    }

    private void PopulateTerrainList()
    {
        foreach (Transform child in _terrainListContent)
        {
            Destroy(child.gameObject);
        }

        List<string> savedTerrains = GetAllSavedTerrains();

        if (!savedTerrains.Contains("Default"))
        {
            savedTerrains.Insert(0, "Default");
        }

        if (savedTerrains.Count == 0)
        {
            GameObject noTerrainsText = new GameObject("NoTerrainsText");
            noTerrainsText.transform.SetParent(_terrainListContent, false);
            TextMeshProUGUI text = noTerrainsText.AddComponent<TextMeshProUGUI>();
            text.text = "Нет сохраненных карт";
            text.alignment = TextAlignmentOptions.Center;
        }
        else
        {
            foreach (string terrainName in savedTerrains)
            {
                GameObject buttonObj = Instantiate(_terrainButtonPrefab, _terrainListContent);
                buttonObj.name = terrainName + "Button";

                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = terrainName;
                }

                Button button = buttonObj.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() => OnTerrainButtonClicked(terrainName));
                }
            }
        }
    }

    private List<string> GetAllSavedTerrains()
    {
        HashSet<string> savedTerrainsSet = new HashSet<string>();
        string[] files = Directory.GetFiles(Application.persistentDataPath, "*_heights.json");
        foreach (string file in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            string terrainName = fileName.Replace("_heights", "");
            savedTerrainsSet.Add(terrainName);
        }
        return new List<string>(savedTerrainsSet);
    }

    private void OnTerrainButtonClicked(string terrainName)
    {
        _selectedTerrainName = terrainName;
        HighlightSelectedTerrainButton(terrainName);
        OnLaunchButtonClicked();
    }

    private void HighlightSelectedTerrainButton(string selectedName)
    {
        foreach (Transform child in _terrainListContent)
        {
            Button button = child.GetComponent<Button>();
            if (button != null)
            {
                ColorBlock colors = button.colors;
                TextMeshProUGUI buttonText = child.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null && buttonText.text == selectedName)
                {
                    colors.normalColor = Color.green;
                }
                else
                {
                    colors.normalColor = Color.white;
                }
                button.colors = colors;
            }
        }
    }

    private void OnLaunchButtonClicked()
    {
        if (!string.IsNullOrEmpty(_selectedTerrainName))
        {
            SelectedTerrainData data = new SelectedTerrainData
            {
                selectedTerrainName = _selectedTerrainName
            };

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(_settingsFilePath, json);

            SceneManager.LoadScene(1);
        }
    }

    private void OnMapEditorButtonClicked()
    {
        SceneManager.LoadScene(2);
    }

    private void OnExitButtonClicked()
    {
        Application.Quit();
    }

    private void OnBackButtonClicked()
    {
        _terrainListPanel.SetActive(false);
        _mainMenuPanel.SetActive(true);
        _selectedTerrainName = null;

        foreach (Transform child in _terrainListContent)
        {
            Button button = child.GetComponent<Button>();
            if (button != null)
            {
                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                button.colors = colors;
            }
        }
    }

[Serializable]
public class SelectedTerrainData
{
    public string selectedTerrainName;
}

}
