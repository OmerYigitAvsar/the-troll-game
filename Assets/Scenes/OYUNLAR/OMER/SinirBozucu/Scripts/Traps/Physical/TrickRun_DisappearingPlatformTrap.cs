using System.Collections;
using UnityEngine;

public class TrickRun_DisappearingPlatformTrap : TrickRun_TrapBase
{
    [Header("Platforms")]
    [SerializeField] private GameObject[] platforms;

    [Header("Connected Trigger Colliders")]
    [SerializeField] private Collider2D[] triggerColliders;

    [Header("Disappear Settings")]
    [SerializeField] private float disappearInterval = 0.05f;

    private bool waitingForLevel30Return;
    private bool disappearanceStarted;

    // TrickRun_TrapTrigger tarafından çalıştırıldığında kaybolma işlemini başlatır.
    protected override void OnTrapActivated()
    {
        if (IsInsideLevel30() && !CanActivateDuringLevel30Return())
        {
            waitingForLevel30Return = true;
            return;
        }

        disappearanceStarted = true;
        StartCoroutine(HidePlatforms());
    }

    private void Update()
    {
        if (!waitingForLevel30Return || disappearanceStarted || !CanActivateDuringLevel30Return())
            return;

        waitingForLevel30Return = false;
        disappearanceStarted = true;
        StartCoroutine(HidePlatforms());
    }

    // Level yeniden başladığında platformları ve Trigger'ları geri açar.
    protected override void OnTrapReset()
    {
        waitingForLevel30Return = false;
        disappearanceStarted = false;

        // Bütün platformları yeniden görünür ve kullanılabilir yapar.
        for (int i = 0; i < platforms.Length; i++)
        {
            if (platforms[i] != null)
            {
                platforms[i].SetActive(true);
            }
        }

        // Kapatılan Trigger Collider alanlarını yeniden etkinleştirir.
        for (int i = 0; i < triggerColliders.Length; i++)
        {
            if (triggerColliders[i] != null)
            {
                triggerColliders[i].enabled = true;
            }
        }
    }

    private bool IsInsideLevel30()
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.name.StartsWith("Seviye_030_") || current.name.StartsWith("Level_030_"))
                return true;
            current = current.parent;
        }
        return false;
    }

    private bool CanActivateDuringLevel30Return()
    {
        Transform level = transform;
        while (level != null && level.GetComponent<TrickRun_LevelCameraBounds>() == null)
            level = level.parent;
        if (level == null) return true;

        TrickRun_ShiftingExitTrap door = level.GetComponentInChildren<TrickRun_ShiftingExitTrap>(true);
        TrickRun_PlayerController player = FindFirstObjectByType<TrickRun_PlayerController>();
        Rigidbody2D playerBody = player != null ? player.GetComponent<Rigidbody2D>() : null;
        if (door == null || playerBody == null || door.CompletedShiftCount < 1 || playerBody.linearVelocity.x >= -0.1f)
            return false;

        if (platforms == null || platforms.Length == 0 || platforms[0] == null)
            return true;
        Collider2D platformCollider = platforms[0].GetComponent<Collider2D>();
        float allowedDistance = platformCollider != null ? platformCollider.bounds.extents.x + 1.5f : 3f;
        return Mathf.Abs(player.transform.position.x - platforms[0].transform.position.x) <= allowedDistance;
    }

    // Trigger alanlarını kapatıp platformları sırayla görünmez yapar.
    private IEnumerator HidePlatforms()
    {
        // Kaybolacak yolların üzerindeki Trigger alanlarını hemen kapatır.
        for (int i = 0; i < triggerColliders.Length; i++)
        {
            if (triggerColliders[i] != null)
            {
                triggerColliders[i].enabled = false;
            }
        }

        // Platformları belirtilen aralıklarla sırayla kapatır.
        for (int i = 0; i < platforms.Length; i++)
        {
            if (platforms[i] != null)
            {
                platforms[i].SetActive(false);
            }

            // Bir sonraki platform kapanmadan önce bekler.
            if (disappearInterval > 0f)
            {
                yield return new WaitForSeconds(disappearInterval);
            }
        }
    }
}
