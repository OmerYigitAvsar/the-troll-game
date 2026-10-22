using UnityEngine;
using UnityEngine.UI;

public class TrickRun_LevelCounter : MonoBehaviour
{
    [SerializeField] private TrickRun_LevelManager levelManager;
    [SerializeField] private Text counterText;

    public void Configure(TrickRun_LevelManager manager, Text text)
    {
        levelManager = manager;
        counterText = text;
        Refresh();
    }

    private void OnEnable()
    {
        if (levelManager == null)
            levelManager = FindFirstObjectByType<TrickRun_LevelManager>();
        if (counterText == null)
            counterText = GetComponentInChildren<Text>(true);

        if (levelManager != null)
            levelManager.LevelChanged += SetLevel;

        Refresh();
    }

    private void OnDisable()
    {
        if (levelManager != null)
            levelManager.LevelChanged -= SetLevel;
    }

    private void Refresh()
    {
        if (levelManager != null)
            SetLevel(levelManager.CurrentLevelNumber);
    }

    private void SetLevel(int levelNumber)
    {
        if (counterText != null)
            counterText.text = $"SEVİYE {levelNumber}";
    }
}
