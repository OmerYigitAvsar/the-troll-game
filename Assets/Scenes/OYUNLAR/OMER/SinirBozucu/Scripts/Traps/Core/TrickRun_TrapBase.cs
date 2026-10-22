using UnityEngine;

public abstract class TrickRun_TrapBase : MonoBehaviour
{
    [Header("Activation Settings")]
    [SerializeField] private bool allowMultipleActivations;

    private bool hasActivated;

    // Trap GameObject'i her açıldığında başlangıç durumuna döner.
    protected virtual void OnEnable()
    {
        ResetTrap();
    }

    // Trigger tarafından çağrılarak tuzağı etkinleştirir.
    public void ActivateTrap()
    {
        // Trap daha önce çalıştıysa ve tekrar çalışmasına izin verilmiyorsa durur.
        if (hasActivated && !allowMultipleActivations)
            return;

        hasActivated = true;

        // Her trap türünün kendine özel çalışma işlemini başlatır.
        OnTrapActivated();
    }

    // Level yeniden başladığında tuzağı başlangıç durumuna getirir.
    public void ResetTrap()
    {
        // Trap üzerinde çalışan bütün Coroutine işlemlerini durdurur.
        StopAllCoroutines();

        hasActivated = false;

        // Her trap türünün kendine özel sıfırlama işlemini gerçekleştirir.
        OnTrapReset();
    }

    // Bu fonksiyon her farklı trap scriptinde özel olarak doldurulacak.
    protected abstract void OnTrapActivated();

    // Bu fonksiyon trapın başlangıç hâline nasıl döneceğini belirleyecek.
    protected abstract void OnTrapReset();
}