using System.Collections.Generic;
using UnityEngine;

public class TrickRun_MovingPlatformCarrier : MonoBehaviour
{
    private readonly HashSet<Rigidbody2D> riders = new HashSet<Rigidbody2D>();
    private Vector3 previousPosition;

    private void OnEnable()
    {
        previousPosition = transform.position;
        riders.Clear();
    }

    private void FixedUpdate()
    {
        Vector3 movement = transform.position - previousPosition;
        if (movement.sqrMagnitude > 0f)
        {
            foreach (Rigidbody2D rider in riders)
            {
                if (rider != null)
                    rider.position += (Vector2)movement;
            }
        }

        previousPosition = transform.position;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Player"))
            return;

        bool playerIsAbove = false;
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y < -0.45f)
            {
                playerIsAbove = true;
                break;
            }
        }

        Rigidbody2D body = collision.collider.attachedRigidbody;
        if (body == null)
            return;

        if (playerIsAbove)
            riders.Add(body);
        else
            riders.Remove(body);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        Rigidbody2D body = collision.collider.attachedRigidbody;
        if (body != null)
            riders.Remove(body);
    }
}
