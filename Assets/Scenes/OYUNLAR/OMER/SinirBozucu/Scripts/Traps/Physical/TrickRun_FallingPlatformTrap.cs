using UnityEngine;

public class TrickRun_FallingPlatformTrap : TrickRun_TrapBase
{
    [Header("Platform Settings")]
    [SerializeField] private Rigidbody2D platformRigidbody;
    [SerializeField] private float gravityScale = 3f;

    private Vector3 startingPosition;
    private Quaternion startingRotation;

    // Platformun başlangıç konumunu ve dönüşünü kaydeder.
    private void Awake()
    {
        if (platformRigidbody == null)
            return;

        startingPosition = platformRigidbody.transform.position;
        startingRotation = platformRigidbody.transform.rotation;

        // Dusen platformun duvarlara surtunup havada takilmasini engeller.
        PhysicsMaterial2D frictionlessMaterial = new PhysicsMaterial2D("FallingPlatform_Frictionless")
        {
            friction = 0f,
            bounciness = 0f
        };

        foreach (Collider2D platformCollider in platformRigidbody.GetComponents<Collider2D>())
            platformCollider.sharedMaterial = frictionlessMaterial;
    }

    // TrickRun_TrapTrigger tarafından çalıştırıldığında platformu düşürür.
    protected override void OnTrapActivated()
    {
        if (platformRigidbody == null)
            return;

        // Platformu yer çekiminden etkilenen Dynamic nesneye dönüştürür.
        platformRigidbody.bodyType = RigidbodyType2D.Dynamic;
        platformRigidbody.gravityScale = gravityScale;
    }

    // Level yeniden başladığında platformu başlangıç durumuna döndürür.
    protected override void OnTrapReset()
    {
        // Rigidbody bağlantısı yapılmamışsa işlem yapmaz.
        if (platformRigidbody == null)
            return;

        // Platformu fizik kuvvetlerinden etkilenmeyen duruma getirir.
        platformRigidbody.bodyType = RigidbodyType2D.Kinematic;

        // Önceki düşüşten kalan hareket ve dönüş hızlarını temizler.
        platformRigidbody.linearVelocity = Vector2.zero;
        platformRigidbody.angularVelocity = 0f;

        // Platformu başlangıç konumuna ve dönüşüne geri getirir.
        platformRigidbody.transform.SetPositionAndRotation(
            startingPosition,
            startingRotation
        );
    }
}
