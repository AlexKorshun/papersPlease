using UnityEngine;

public static class DocumentRigidbodyByZone
{
    public static void ApplyModeForZone(
        Rigidbody2D documentRigidbody,
        TableTrigger.TableZone physicsZone,
        RigidbodyType2D savedBodyType,
        float savedGravityScale)
    {
        if (documentRigidbody == null) return;

        if (physicsZone == TableTrigger.TableZone.InspectDesk)
        {
            if (documentRigidbody.bodyType == RigidbodyType2D.Kinematic) return;
            documentRigidbody.velocity = Vector2.zero;
            documentRigidbody.angularVelocity = 0f;
            documentRigidbody.bodyType = RigidbodyType2D.Kinematic;
        }
        else
        {
            if (documentRigidbody.bodyType == savedBodyType &&
                Mathf.Approximately(documentRigidbody.gravityScale, savedGravityScale))
                return;

            documentRigidbody.bodyType = savedBodyType;
            documentRigidbody.gravityScale = savedGravityScale;
            documentRigidbody.position = documentRigidbody.transform.position;
            documentRigidbody.rotation = documentRigidbody.transform.eulerAngles.z;
            documentRigidbody.velocity = Vector2.zero;
            documentRigidbody.angularVelocity = 0f;
            Physics2D.SyncTransforms();
        }
    }
}
