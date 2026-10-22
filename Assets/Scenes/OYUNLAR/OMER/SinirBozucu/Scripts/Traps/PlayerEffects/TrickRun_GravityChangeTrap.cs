using System.Collections;
using UnityEngine;

public class TrickRun_GravityChangeTrap : TrickRun_TrapBase
{
    [Header("Player Reference")]
    [SerializeField] private TrickRun_PlayerController playerController;

    [Header("Gravity Settings")]
    [SerializeField] private float gravityMultiplier = 0.5f;
    [SerializeField] private float effectDuration;
    [SerializeField] private float rotationDuration = 0.22f;

    private Quaternion startingRotation;

    private void Awake()
    {
        if (playerController != null)
            startingRotation = playerController.transform.rotation;
    }

    // Trap çalıştığında Player'ın yer çekimini değiştirir.
    protected override void OnTrapActivated()
    {
        if (playerController == null)
            return;

        playerController.SetGravityMultiplier(gravityMultiplier);
        StartCoroutine(RotatePlayer(gravityMultiplier < 0f ? 180f : 0f));

        // Süre belirlenmişse yer çekimini daha sonra normale döndürür.
        if (effectDuration > 0f)
        {
            StartCoroutine(ResetGravityAfterDuration());
        }
    }

    // Level yeniden başladığında yer çekimini normale döndürür.
    protected override void OnTrapReset()
    {
        if (playerController != null)
        {
            playerController.SetGravityMultiplier(1f);
            playerController.transform.rotation = startingRotation;
        }
    }

    // Belirlenen sürenin sonunda yer çekimini normale döndürür.
    private IEnumerator ResetGravityAfterDuration()
    {
        yield return new WaitForSeconds(effectDuration);

        playerController.SetGravityMultiplier(1f);
        yield return RotatePlayer(0f);
    }

    private IEnumerator RotatePlayer(float targetAngle)
    {
        Quaternion from = playerController.transform.rotation;
        Quaternion to = startingRotation * Quaternion.Euler(0f, 0f, targetAngle);
        float elapsed = 0f;
        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;
            playerController.transform.rotation = Quaternion.Slerp(from, to, Mathf.Clamp01(elapsed / rotationDuration));
            yield return null;
        }
        playerController.transform.rotation = to;
    }
}
