using UnityEngine;

public class TrickRun_CameraBoundsTrap : TrickRun_TrapBase
{
    [SerializeField] private TrickRun_LevelCameraBounds levelBounds;
    [SerializeField] private Transform player;
    [SerializeField] private GameObject firstRoomRightWall;
    [SerializeField] private GameObject finalRightWall;
    [SerializeField] private float initialRightEdge;
    [SerializeField] private float expandedRightEdge;

    private string firstWallOriginalName;
    private string finalWallOriginalName;

    private void Awake()
    {
        if (firstRoomRightWall != null) firstWallOriginalName = firstRoomRightWall.name;
        if (finalRightWall != null) finalWallOriginalName = finalRightWall.name;
    }

    protected override void OnTrapActivated()
    {
        ApplyExpandedBounds();
    }

    protected override void OnTrapReset()
    {
        if (levelBounds == null) return;

        if (firstRoomRightWall != null)
        {
            firstRoomRightWall.SetActive(true);
            firstRoomRightWall.name = string.IsNullOrEmpty(firstWallOriginalName) ? "RightWall" : firstWallOriginalName;
        }
        if (finalRightWall != null)
            finalRightWall.name = string.IsNullOrEmpty(finalWallOriginalName) ? "FinalRightWall" : finalWallOriginalName;

        levelBounds.Configure(levelBounds.LeftEdge, initialRightEdge, levelBounds.CameraY);
        RefreshCameraWalls();
    }

    private void ApplyExpandedBounds()
    {
        if (levelBounds == null) return;

        if (firstRoomRightWall != null)
        {
            firstRoomRightWall.name = "RemovedRoomDivider";
            firstRoomRightWall.SetActive(false);
        }
        if (finalRightWall != null) finalRightWall.name = "RightWall";

        levelBounds.Configure(levelBounds.LeftEdge, expandedRightEdge, levelBounds.CameraY);
        RefreshCameraWalls();
    }

    private void RefreshCameraWalls()
    {
        TrickRun_CameraFollower follower = Object.FindFirstObjectByType<TrickRun_CameraFollower>();
        if (follower != null)
            follower.RefreshLevelBounds(player, levelBounds);
    }
}
