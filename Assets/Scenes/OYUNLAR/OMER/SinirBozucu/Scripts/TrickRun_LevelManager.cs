using System;
using UnityEngine;
using UnityEngine.UI;

public class TrickRun_LevelManager : MonoBehaviour
{
    [Header("Level Settings")]
    [SerializeField] private GameObject[] levels;
    [Min(0)]
    [SerializeField] private int startingLevelIndex;

    [Header("Player Settings")]
    [SerializeField] private Transform player;

    private int currentLevelIndex;

    [Header("Main Menu (optional)")]
    [SerializeField] private GameObject mainMenuRoot;
    [SerializeField] private GameObject homePanel, groupsPanel, levelSelectionPanel, resetPanel, completionPanel;
    [SerializeField] private GameObject gameplayUI, menuReturnButton;
    [SerializeField] private Text continueLabel, saveSummary, groupTitle;
    [SerializeField] private Button[] groupButtons, levelButtons;
    private const string SavePrefix = "TrickRun.Progress.v1.";
    private static readonly string[] GroupNames =
    {
        "Yükselen Diken", "Hareketli Diken", "Düşen Platform", "Hareketli Platform",
        "Kaybolan Zemin", "Sahte Kapılar", "Ters Kontroller", "Zıplama Yasağı",
        "Yerçekimi", "Final Mücadelesi"
    };
    public static string GetGroupName(int index) => index >= 0 && index < GroupNames.Length ? GroupNames[index] : "???";
    private int resumeIndex, unlockedCount = 1, savedAttempts, selectedGroup;
    private bool hasSave, inMenu, progressLoaded;
    public bool UsesMainMenu => mainMenuRoot != null;
    public bool IsInMenu => UsesMainMenu && inMenu;

    private void LoadProgress()
    {
        hasSave = PlayerPrefs.GetInt(SavePrefix + "HasSave", 0) == 1;
        unlockedCount = Mathf.Clamp(PlayerPrefs.GetInt(SavePrefix + "Unlocked", 1), 1, levels.Length);
        resumeIndex = Mathf.Clamp(PlayerPrefs.GetInt(SavePrefix + "Level", 0), 0, unlockedCount - 1);
        savedAttempts = Mathf.Max(0, PlayerPrefs.GetInt(SavePrefix + "Attempts", 0));
        progressLoaded = true;
    }

    private void SaveProgress()
    {
        if (!UsesMainMenu || !progressLoaded) return;
        PlayerPrefs.SetInt(SavePrefix + "HasSave", hasSave ? 1 : 0);
        PlayerPrefs.SetInt(SavePrefix + "Unlocked", unlockedCount);
        PlayerPrefs.SetInt(SavePrefix + "Level", resumeIndex);
        PlayerPrefs.SetInt(SavePrefix + "Attempts", savedAttempts);
        PlayerPrefs.Save();
    }

    public void RecordAttempts(int attempts)
    {
        if (!UsesMainMenu || !progressLoaded || inMenu) return;
        savedAttempts = Mathf.Max(0, attempts);
        SaveProgress();
    }

    private void OnApplicationPause(bool paused) { if (paused) SaveProgress(); }
    private void OnApplicationQuit() { SaveProgress(); }

    private void Update()
    {
        if (!UsesMainMenu || !progressLoaded) return;
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;
        if (!inMenu) ShowMainMenu();
        else if (levelSelectionPanel.activeSelf) ShowGroups();
        else if (!homePanel.activeSelf) ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        if (!UsesMainMenu) return;
        inMenu = true;
        // Deactivate gameplay without changing Time.timeScale or reparenting scene objects.
        foreach (var level in levels) if (level != null) level.SetActive(false);
        player.gameObject.SetActive(false);
        gameplayUI.SetActive(false);
        menuReturnButton.SetActive(false);
        SetMenuPanel(homePanel);
        continueLabel.text = hasSave ? "DEVAM ET" : "YENİ OYUN";
        saveSummary.text = hasSave ? $"Bölüm {resumeIndex + 1}  ·  Deneme {savedAttempts}" : "Her deneme yeni bir başlangıç.";
        SaveProgress();
    }

    private void SetMenuPanel(GameObject panel)
    {
        homePanel.SetActive(panel == homePanel);
        groupsPanel.SetActive(panel == groupsPanel);
        levelSelectionPanel.SetActive(panel == levelSelectionPanel);
        resetPanel.SetActive(panel == resetPanel);
        completionPanel.SetActive(panel == completionPanel);
    }

    public void ContinueFromMenu() { StartSelectedLevel(hasSave ? resumeIndex : 0); }
    public void ShowGroups()
    {
        if (!IsInMenu) return;
        SetMenuPanel(groupsPanel);
        for (int i = 0; i < groupButtons.Length; i++)
        {
            int first = i * 5 + 1, last = Mathf.Min(first + 4, levels.Length);
            bool available = first <= unlockedCount;
            groupButtons[i].interactable = available;
            var label = groupButtons[i].GetComponentInChildren<Text>();
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 16;
            label.resizeTextMaxSize = 24;
            label.text = (available ? GetGroupName(i) : "???") + $"\n{first}–{last}" + (available ? "" : "\nKİLİTLİ");
        }
    }

    public void SelectGroup(int index)
    {
        if (!IsInMenu || index < 0 || index >= groupButtons.Length || index * 5 >= unlockedCount) return;
        selectedGroup = index;
        SetMenuPanel(levelSelectionPanel);
        groupTitle.text = GetGroupName(index);
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelIndex = selectedGroup * 5 + i;
            levelButtons[i].gameObject.SetActive(levelIndex < levels.Length);
            levelButtons[i].interactable = levelIndex < unlockedCount;
            levelButtons[i].GetComponentInChildren<Text>().text = $"BÖLÜM {levelIndex + 1}" + (levelIndex < unlockedCount ? "" : "\nKİLİTLİ");
        }
    }

    public void SelectLevelSlot(int slot)
    {
        if (slot >= 0 && slot < 5) StartSelectedLevel(selectedGroup * 5 + slot);
    }

    private void StartSelectedLevel(int index)
    {
        if (!IsInMenu || index < 0 || index >= levels.Length || index >= unlockedCount) return;
        player.gameObject.SetActive(true);
        var death = player.GetComponent<TrickRun_PlayerDeath>();
        if (death != null) death.PrepareForMenuStart(savedAttempts);
        var control = player.GetComponent<TrickRun_PlayerController>();
        if (control != null)
        {
            control.SetMobileLeftHeld(false); control.SetMobileRightHeld(false);
            control.ReleaseMobileJump();
            control.SetSlipperyMovement(false); control.SetControlsReversed(false);
            control.SetJumpingEnabled(true); control.SetMovementSpeedMultiplier(1f); control.SetGravityMultiplier(1f);
        }
        inMenu = false;
        SetMenuPanel(null);
        gameplayUI.SetActive(true); menuReturnButton.SetActive(true);
        hasSave = true;
        OpenLevel(index);
    }

    public void ShowResetConfirmation() { if (IsInMenu) SetMenuPanel(resetPanel); }
    public void ConfirmReset()
    {
        if (!IsInMenu) return;
        resumeIndex = 0; savedAttempts = 0; hasSave = false;
        // Unlocked is intentionally preserved, even after resetting the current run.
        SaveProgress();
        StartSelectedLevel(0);
    }

    public event Action<int> LevelChanged;
    public int CurrentLevelNumber => GetLevelNumber(currentLevelIndex);

    // Oyun başladığında ilk bölümü açar.
    private void Start()
    {
        if (UsesMainMenu)
        {
            LoadProgress();
            ShowMainMenu();
            return;
        }
        int safeStartingIndex = Mathf.Clamp(
            startingLevelIndex,
            0,
            Mathf.Max(0, levels.Length - 1)
        );

        OpenLevel(safeStartingIndex);
    }

    // Belirtilen sıradaki Level nesnesini açar.
    private void OpenLevel(int levelIndex)
    {
        // Geçersiz bir bölüm numarası verilmişse işlemi durdurur.
        if (levelIndex < 0 || levelIndex >= levels.Length)
        {
            Debug.LogError("Açılmak istenen bölüm bulunamadı!");
            return;
        }

        // Açılan bölümün numarasını saklar.
        currentLevelIndex = levelIndex;

        // Mevcut bölümü açar, diğer bütün bölümleri kapatır.
        for (int i = 0; i < levels.Length; i++)
        {
            bool shouldBeActive = i == currentLevelIndex;
            levels[i].SetActive(shouldBeActive);
        }

        MovePlayerToSpawnPoint();
        LevelChanged?.Invoke(CurrentLevelNumber);
        if (UsesMainMenu) { resumeIndex = levelIndex; SaveProgress(); }
    }

    private int GetLevelNumber(int index)
    {
        if (levels == null || index < 0 || index >= levels.Length || levels[index] == null)
            return index + 1;

        string levelName = levels[index].name;
        string[] parts = levelName.Split('_');
        if (parts.Length > 1 && int.TryParse(parts[1], out int parsedNumber))
            return parsedNumber;

        return index + 1;
    }

    // Player'ı mevcut bölümdeki SpawnPoint konumuna taşır.
    private void MovePlayerToSpawnPoint()
    {
        // Level içerisindeki SpawnPoint nesnesini bulur.
        Transform spawnPoint =
            levels[currentLevelIndex].transform.Find("SpawnPoint") ??
            levels[currentLevelIndex].transform.Find("Başlangıç Noktası");

        // SpawnPoint bulunamadıysa hata verir.
        if (spawnPoint == null)
        {
            Debug.LogError(
                levels[currentLevelIndex].name +
                " içerisinde SpawnPoint bulunamadı!"
            );

            return;
        }

        // Player'ın önceki hareket hızını sıfırlar.
        Rigidbody2D playerRigidbody =
            player.GetComponent<Rigidbody2D>();

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
        }

        // Player'ı bölümün başlangıç noktasına taşır.
        player.position = spawnPoint.position;

        TrickRun_CameraFollower cameraFollower = FindFirstObjectByType<TrickRun_CameraFollower>();
        if (cameraFollower != null)
        {
            TrickRun_LevelCameraBounds cameraBounds =
                levels[currentLevelIndex].GetComponent<TrickRun_LevelCameraBounds>();

            cameraFollower.ConfigureForLevel(player, cameraBounds);
        }
    }

    // Kapı tarafından çağrılarak sonraki bölümü açar.
    public void CompleteCurrentLevel()
    {
        if (IsInMenu) return;
        int nextLevelIndex = currentLevelIndex + 1;
        if (UsesMainMenu)
        {
            unlockedCount = Mathf.Max(unlockedCount, Mathf.Min(nextLevelIndex + 1, levels.Length));
            SaveProgress();
            if (nextLevelIndex >= levels.Length)
            {
                ShowMainMenu();
                SetMenuPanel(completionPanel);
                return;
            }
        }

        // Son bölüm tamamlandıysa şimdilik ilk bölüme döner.
        if (nextLevelIndex >= levels.Length)
        {
            OpenLevel(0);
            return;
        }

        OpenLevel(nextLevelIndex);
    }

    // Mevcut Level içerisindeki nesneleri sıfırlar ve Player'ı başlangıca taşır.
    public void RestartCurrentLevel()
    {
        if (IsInMenu) return;
        // Level'ı kapatıp tekrar açarak tuzakların sıfırlanmasını sağlar.
        levels[currentLevelIndex].SetActive(false);
        levels[currentLevelIndex].SetActive(true);

        // Player'ı SpawnPoint konumuna geri gönderir.
        MovePlayerToSpawnPoint();
    }
}
