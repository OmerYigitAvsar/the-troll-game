using System.Collections;
using UnityEngine;

public class TrickRun_SlowPlayerTrap : TrickRun_TrapBase
{
    [Header("Player Reference")]
    [SerializeField] private TrickRun_PlayerController playerController;

    [Header("Slow Settings")]
    [Range(0.1f, 2f)]
    [SerializeField] private float speedMultiplier = 0.5f;

    [SerializeField] private float effectDuration;

    // Trap çalıştığında Player'ın hareket hızını azaltır.
    protected override void OnTrapActivated()
    {
        if (playerController == null)
            return;

        playerController.SetMovementSpeedMultiplier(speedMultiplier);

        // Bir süre belirlendiyse etkiyi süre sonunda kaldırır.
        if (effectDuration > 0f)
        {
            StartCoroutine(RemoveSlowAfterDuration());
        }
    }

    // Level yeniden başladığında Player'ın hızını normale döndürür.
    protected override void OnTrapReset()
    {
        if (playerController != null)
        {
            playerController.SetMovementSpeedMultiplier(1f);
        }
    }

    // Belirlenen sürenin sonunda yavaşlatma etkisini kaldırır.
    private IEnumerator RemoveSlowAfterDuration()
    {
        yield return new WaitForSeconds(effectDuration);

        playerController.SetMovementSpeedMultiplier(1f);
    }
}
