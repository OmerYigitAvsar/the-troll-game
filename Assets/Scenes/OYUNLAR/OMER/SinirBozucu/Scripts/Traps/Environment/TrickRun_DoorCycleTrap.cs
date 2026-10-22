using UnityEngine;

public class TrickRun_DoorCycleTrap : TrickRun_TrapBase
{
    [SerializeField] private GameObject fakeDoor;
    [SerializeField] private GameObject realDoor;
    [SerializeField] private Transform leftPoint;
    [SerializeField] private Transform rightPoint;
    [SerializeField] private GameObject[] trapPhases;
    [SerializeField, Min(1)] private int requiredContacts = 6;
    [SerializeField] private TrickRun_PlayerController playerController;

    private int contactCount;

    protected override void OnTrapActivated()
    {
        if (fakeDoor == null || contactCount >= requiredContacts)
            return;

        contactCount++;
        if (contactCount >= requiredContacts)
        {
            fakeDoor.SetActive(false);
            if (realDoor != null) realDoor.SetActive(true);
            SetPhase(-1);
            ApplyCompletionState();
            return;
        }

        SetPhase(contactCount);
        Transform destination = contactCount % 2 == 1 ? leftPoint : rightPoint;
        if (destination != null)
        {
            fakeDoor.transform.position = destination.position;
            fakeDoor.transform.rotation = destination.rotation;
        }
    }

    protected override void OnTrapReset()
    {
        contactCount = 0;
        ResetAllPhaseTraps();
        if (fakeDoor != null)
        {
            fakeDoor.SetActive(true);
            if (rightPoint != null)
            {
                fakeDoor.transform.position = rightPoint.position;
                fakeDoor.transform.rotation = rightPoint.rotation;
            }
        }
        if (realDoor != null) realDoor.SetActive(false);
        SetPhase(0);
    }

    private void ResetAllPhaseTraps()
    {
        if (trapPhases == null) return;
        foreach (GameObject phase in trapPhases)
        {
            if (phase == null) continue;
            foreach (TrickRun_TrapBase trap in phase.GetComponentsInChildren<TrickRun_TrapBase>(true))
                if (trap != null) trap.ResetTrap();
        }
    }

    private void SetPhase(int activeIndex)
    {
        if (trapPhases == null) return;
        for (int i = 0; i < trapPhases.Length; i++)
        {
            if (trapPhases[i] == null) continue;
            if (trapPhases[i].activeSelf && i != activeIndex)
            {
                foreach (TrickRun_TrapBase trap in trapPhases[i].GetComponentsInChildren<TrickRun_TrapBase>(true))
                    if (trap != null) trap.ResetTrap();
            }
            trapPhases[i].SetActive(i == activeIndex);
        }

        if (activeIndex < 0) return;

        if (playerController != null)
            playerController.SetJumpingEnabled(true);
    }

    private void ApplyCompletionState()
    {
        if (playerController != null)
            playerController.SetJumpingEnabled(true);
    }
}
