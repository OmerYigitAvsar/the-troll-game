using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TrickRun_PlayerDeath : MonoBehaviour
{
    [Header("Restart Settings")]
    [SerializeField] private float restartDelay = 0.4f;

    [Header("References")]
    [SerializeField] private TrickRun_LevelManager levelManager;
    [SerializeField] private Text attemptCounterText;

    private TrickRun_PlayerController playerController;
    private Rigidbody2D playerRigidbody;
    private bool isRestarting;
    private int attemptCount;

    // Oyun başladığında Player üzerindeki gerekli bileşenleri alır.
    private void Awake()
    {
        playerController = GetComponent<TrickRun_PlayerController>();
        playerRigidbody = GetComponent<Rigidbody2D>();
        RefreshAttemptCounter();
    }

    // Player, Trigger özelliği açık bir nesneye girdiğinde çalışır.
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Temas edilen nesne Hazard etiketine sahipse Player başarısız olur.
        if (other.CompareTag("Hazard"))
        {
            Die();
        }
    }

    // Player kontrolünü durdurur ve yeniden başlatma işlemini başlatır.
    public void Die()
    {
        // İşlem zaten başladıysa tekrar çalışmasını engeller.
        if (isRestarting || (levelManager != null && levelManager.IsInMenu))
            return;

        isRestarting = true;
        attemptCount++;
        RefreshAttemptCounter();

        if (levelManager != null) levelManager.RecordAttempts(attemptCount);

        // Yeniden başlatma sırasında Player hareketini kapatır.
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // Player'ın mevcut hareket hızını sıfırlar.
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
        }

        StartCoroutine(RestartLevel());
    }

    public void ConfigureAttemptCounter(Text counterText)
    {
        attemptCounterText = counterText;
        RefreshAttemptCounter();
    }

    public void PrepareForMenuStart(int savedCount)
    {
        StopAllCoroutines();
        isRestarting = false;
        attemptCount = Mathf.Max(0, savedCount);
        if (playerController == null) playerController = GetComponent<TrickRun_PlayerController>();
        if (playerController != null) playerController.enabled = true;
        RefreshAttemptCounter();
    }

    private void RefreshAttemptCounter()
    {
        if (attemptCounterText != null)
            attemptCounterText.text = $"DENEME: {attemptCount}";
    }

    // Kısa bir süre bekleyip mevcut Level'ı yeniden başlatır.
    private IEnumerator RestartLevel()
    {
        yield return new WaitForSeconds(restartDelay);

        // Player'ı mevcut Level'ın SpawnPoint konumuna gönderir.
        levelManager.RestartCurrentLevel();

        // Player hareketini tekrar açar.
        if (playerController != null)
        {
            playerController.enabled = true;
        }

        // Player'ın tekrar başarısız olabilmesine izin verir.
        isRestarting = false;
    }
}
