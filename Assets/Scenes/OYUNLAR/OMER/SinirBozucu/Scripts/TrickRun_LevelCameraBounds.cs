using UnityEngine;

public class TrickRun_LevelCameraBounds : MonoBehaviour
{
    [SerializeField] private float leftEdge;
    [SerializeField] private float rightEdge = 20f;
    [SerializeField] private float cameraY;
    [SerializeField] private bool followPlayerVertically;
    [SerializeField] private float bottomEdge;
    [SerializeField] private float topEdge;

    public float LeftEdge => leftEdge;
    public float RightEdge => rightEdge;
    public float CameraY => cameraY;
    public bool FollowPlayerVertically => followPlayerVertically;
    public float BottomEdge => bottomEdge;
    public float TopEdge => topEdge;

    public void Configure(float left, float right, float y)
    {
        leftEdge = left;
        rightEdge = right;
        cameraY = y;
    }

    public void ConfigureVertical(float left, float right, float centerY, float bottom, float top)
    {
        Configure(left, right, centerY);
        followPlayerVertically = true;
        bottomEdge = bottom;
        topEdge = top;
    }
}
