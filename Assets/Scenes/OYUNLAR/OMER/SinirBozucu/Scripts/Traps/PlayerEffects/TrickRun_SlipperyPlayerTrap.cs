    using System.Collections;
    using UnityEngine;

    public class TrickRun_SlipperyPlayerTrap : TrickRun_TrapBase
    {
        [Header("Player Reference")]
        [SerializeField] private TrickRun_PlayerController playerController;

        [Header("Effect Settings")]
        [SerializeField] private float effectDuration;
        [SerializeField] private bool enableSlippery = true;
        [SerializeField] private bool clearOnDisable;
        private bool appliedEffect;

        [Header("Slippery Tuning")]
        [Tooltip("How quickly the player reaches the target speed while input is held.")]
        [Min(0.1f)] [SerializeField] private float acceleration = 12f;
        [Tooltip("How quickly the player stops after releasing the controls. Lower values slide longer.")]
        [Min(0.05f)] [SerializeField] private float deceleration = 3f;
        [Tooltip("Maximum horizontal speed while this slippery effect is active.")]
        [Range(0.25f, 2f)] [SerializeField] private float speedMultiplier = 1f;

        // Trap çalıştığında Player'ın hareketini kaygan hâle getirir.
        protected override void OnTrapActivated()
        {
            if (playerController == null)
                return;

            playerController.SetSlipperyMovement(enableSlippery, acceleration, deceleration, speedMultiplier);
            appliedEffect = enableSlippery;

            // Süre belirlenmişse etkiyi süre sonunda kapatır.
            if (effectDuration > 0f)
            {
                StartCoroutine(RemoveEffectAfterDuration());
            }
        }

        // Level yeniden başladığında kayganlık etkisini kaldırır.
        protected override void OnTrapReset()
        {
            appliedEffect = false;
            if (playerController != null)
            {
                playerController.SetSlipperyMovement(false);
            }
        }

        private void OnDisable()
        {
            if (!clearOnDisable) return;
            StopAllCoroutines();
            if (appliedEffect && playerController != null) playerController.SetSlipperyMovement(false);
            appliedEffect = false;
        }

        // Belirlenen sürenin sonunda kayganlığı kaldırır.
        private IEnumerator RemoveEffectAfterDuration()
        {
            yield return new WaitForSeconds(effectDuration);

            playerController.SetSlipperyMovement(false);
            appliedEffect = false;
        }
    }
