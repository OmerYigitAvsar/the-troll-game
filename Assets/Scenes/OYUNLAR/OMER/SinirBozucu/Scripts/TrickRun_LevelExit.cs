using UnityEngine;

public class TrickRun_LevelExit : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TrickRun_LevelManager levelManager;

    private bool isCompleted;

    // Bir nesne kapının Trigger alanına girdiğinde çalışır.
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Kapıya giren nesne Player değilse işlem yapmaz.
        if (!other.CompareTag("Player"))
            return;

        CompleteLevel();
    }

    // TrickRun_LevelManager'a mevcut bölümün tamamlandığını bildirir.
    private void CompleteLevel()
    {
        // Kapının aynı anda birden fazla çalışmasını engeller.
        if (isCompleted)
            return;

        isCompleted = true;

        // Mevcut Level'ı kapatıp sonraki Level'ı açar.
        levelManager.CompleteCurrentLevel();
    }

    // Kapı yeniden açıldığında tekrar kullanılabilmesini sağlar.
    private void OnEnable()
    {
        isCompleted = false;
    }
}