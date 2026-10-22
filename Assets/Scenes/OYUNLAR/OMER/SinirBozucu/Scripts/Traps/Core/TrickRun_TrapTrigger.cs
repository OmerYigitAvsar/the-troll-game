using System.Collections;
using UnityEngine;

// Trigger'a bağlanan tek bir trap ve çalışma gecikmesini saklar.
[System.Serializable]
public class TrickRun_TrapActivation
{
    [Header("Target")]
    public TrickRun_TrapBase trap;

    [Header("Timing")]
    [Min(0f)]
    public float activationDelay;

    
}

public class TrickRun_TrapTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private bool activateOnlyOnce = true;

    [Header("Connected Traps")]
    [SerializeField] private TrickRun_TrapActivation[] trapActivations;
    
    [Header("Lock Settings")]
    [SerializeField] private bool startsLocked;

    [Header("Door Sequence (Optional)")]
    [Min(0)]
    [SerializeField] private int requiredDoorShiftCount;

    private bool hasTriggered;

    private bool isLocked;

    // Level her açıldığında Trigger'ı başlangıç durumuna getirir.
    private void OnEnable()
    {
        StopAllCoroutines();

        hasTriggered = false;

        // Trigger'ın başlangıçtaki kilit durumunu uygular.
        isLocked = startsLocked;
    }
    // Player bu nesnenin Trigger Collider alanına girdiğinde çalışır.
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Alana giren nesne Player değilse işlem yapmaz.
        if (!other.CompareTag("Player"))
            return;

        ActivateConnectedTraps();
    }

    // Bağlı bütün trapları kendi gecikme süreleriyle çalıştırır.
    public void ActivateConnectedTraps()
    {

        // Trigger kilitliyse bağlı trapları çalıştırmaz.
        if (isLocked)
            return;

        // Sıralı kapı tetikleyicilerinin yalnızca doğru aşamada çalışmasını sağlar.
        for (int i = 0; i < trapActivations.Length; i++)
        {
            TrickRun_ShiftingExitTrap doorTrap = trapActivations[i].trap as TrickRun_ShiftingExitTrap;
            if (doorTrap != null && doorTrap.CompletedShiftCount != requiredDoorShiftCount)
                return;
        }

        // Trigger yalnızca bir kez çalışacaksa tekrar kullanılmasını engeller.
        if (hasTriggered && activateOnlyOnce)
            return;

        hasTriggered = true;

        // Inspector'dan bağlanan bütün traplar için çalışma işlemini başlatır.
        for (int i = 0; i < trapActivations.Length; i++)
        {
            TrickRun_TrapActivation activation = trapActivations[i];

            // Listeye boş bir eleman eklenmişse onu atlar.
            if (activation.trap == null)
                continue;

            StartCoroutine(ActivateTrapAfterDelay(activation));
        }
    }

    // Belirlenen süreyi bekledikten sonra ilgili trapı etkinleştirir.
    private IEnumerator ActivateTrapAfterDelay(TrickRun_TrapActivation activation)
    {
        // Gecikme sıfırdan büyükse belirtilen süre kadar bekler.
        if (activation.activationDelay > 0f)
        {
            yield return new WaitForSeconds(activation.activationDelay);
        }

        activation.trap.ActivateTrap();
    }

    // Başka bir scriptin bu Trigger'ı tekrar kullanılabilir yapmasını sağlar.
    public void ResetTrigger()
    {
        StopAllCoroutines();
        hasTriggered = false;
    }

    // Başka scriptlerin Trigger'ın kilit durumunu değiştirmesini sağlar.
    public void SetLocked(bool locked)
    {
        isLocked = locked;
    }

    // Trigger'ın çalışmasını engeller.
    public void LockTrigger()
    {
        isLocked = true;
    }

    // Trigger'ın çalışmasına izin verir.
    public void UnlockTrigger()
    {
        isLocked = false;
    }
}
