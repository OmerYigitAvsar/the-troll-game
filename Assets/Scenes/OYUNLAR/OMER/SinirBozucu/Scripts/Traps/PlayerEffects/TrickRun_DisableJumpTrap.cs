using System.Collections;
using UnityEngine;

public class TrickRun_DisableJumpTrap : TrickRun_TrapBase
{
    [Header("Player Reference")]
    [SerializeField] private TrickRun_PlayerController playerController;

    [Header("Effect Settings")]
    [SerializeField] private float effectDuration;

    // Trap çalıştığında Player'ın zıplamasını kapatır.
    protected override void OnTrapActivated()
    {
        if (playerController == null)
            return;

        playerController.SetJumpingEnabled(false);

        // Süre belirlenmişse zıplamayı süre sonunda geri açar.
        if (effectDuration > 0f)
        {
            StartCoroutine(EnableJumpAfterDuration());
        }
    }

    public void SetJumpEnabled(bool enabled)
    {
        if (playerController != null)
            playerController.SetJumpingEnabled(enabled);
    }

    // Level yeniden başladığında zıplamayı tekrar açar.
    protected override void OnTrapReset()
    {
        if (playerController != null)
        {
            playerController.SetJumpingEnabled(true);
        }
    }

    // Belirlenen sürenin sonunda zıplamayı yeniden etkinleştirir.
    private IEnumerator EnableJumpAfterDuration()
    {
        yield return new WaitForSeconds(effectDuration);

        playerController.SetJumpingEnabled(true);
    }
}
