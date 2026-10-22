using System.Collections;
using UnityEngine;

public class TrickRun_AppearingObjectsTrap : TrickRun_TrapBase
{
    [Header("Objects To Appear")]
    [SerializeField] private GameObject[] objectsToAppear;

    [Header("Appearance Settings")]
    [SerializeField] private float appearanceInterval;

    // TrickRun_TrapTrigger tarafından çalıştırıldığında nesneleri ortaya çıkarır.
    protected override void OnTrapActivated()
    {
        StartCoroutine(ShowObjects());
    }

    // Level başladığında bütün hedef nesneleri gizler.
    protected override void OnTrapReset()
    {
        if (objectsToAppear == null)
            return;

        for (int i = 0; i < objectsToAppear.Length; i++)
        {
            if (objectsToAppear[i] != null)
            {
                objectsToAppear[i].SetActive(false);
            }
        }
    }

    // Hedef nesneleri belirlenen aralıklarla görünür yapar.
    private IEnumerator ShowObjects()
    {
        if (objectsToAppear == null)
            yield break;

        for (int i = 0; i < objectsToAppear.Length; i++)
        {
            if (objectsToAppear[i] != null)
            {
                objectsToAppear[i].SetActive(true);
            }

            // Sonraki nesne ortaya çıkmadan önce bekler.
            if (appearanceInterval > 0f)
            {
                yield return new WaitForSeconds(appearanceInterval);
            }
        }
    }
}
