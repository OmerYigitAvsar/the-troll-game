using UnityEngine;

public class TrickRun_SetTriggerLockTrap : TrickRun_TrapBase
{
    [Header("Target Triggers")]
    [SerializeField] private TrickRun_TrapTrigger[] targetTriggers;

    [Header("Lock Operation")]
    [SerializeField] private bool lockTriggers;

    // Trap çalıştığında hedef Trigger'ların kilit durumunu değiştirir.
    protected override void OnTrapActivated()
    {
        for (int i = 0; i < targetTriggers.Length; i++)
        {
            if (targetTriggers[i] != null)
            {
                targetTriggers[i].SetLocked(lockTriggers);
            }
        }
    }

    // Hedef Trigger'lar Level açılırken kendilerini sıfırladığı için işlem yapmaz.
    protected override void OnTrapReset()
    {
    }
}