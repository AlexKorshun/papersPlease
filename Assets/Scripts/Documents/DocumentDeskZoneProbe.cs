using System;
using UnityEngine;

public struct DeskZoneFlags
{
    public bool IsOverClosedDesk;
    public bool IsOverInspectDesk;
    public bool PhysicsOverClosedDesk;
    public bool PhysicsOverInspectDesk;
}

public static class DocumentDeskZoneProbe
{
    public static DeskZoneFlags Refresh(
        bool useCursorProbe,
        Camera mainCamera,
        float zoneOverlapProbeRadius,
        Transform transform,
        Func<Bounds> computeActiveSpriteBoundsWorld)
    {
        var flags = new DeskZoneFlags();

        if (useCursorProbe)
        {
            AccumulateTableZoneFlags(
                CollectDeskHitsAtCursorProbe(mainCamera, zoneOverlapProbeRadius, transform, computeActiveSpriteBoundsWorld),
                ref flags.IsOverClosedDesk, ref flags.IsOverInspectDesk);
            return flags;
        }

        AccumulateTableZoneFlags(
            CollectDeskHitsAtDocumentCenterProbe(zoneOverlapProbeRadius, transform, computeActiveSpriteBoundsWorld),
            ref flags.IsOverClosedDesk, ref flags.IsOverInspectDesk);
        AccumulateTableZoneFlags(
            CollectDeskHitsFromDocumentBounds(zoneOverlapProbeRadius, transform, computeActiveSpriteBoundsWorld),
            ref flags.PhysicsOverClosedDesk, ref flags.PhysicsOverInspectDesk);

        return flags;
    }

    public static TableTrigger.TableZone ResolveTargetZone(
        bool isDragging,
        TableTrigger.TableZone currentZone,
        bool isOverClosedDesk,
        bool isOverInspectDesk)
    {
        if (isOverInspectDesk && isOverClosedDesk)
            return TableTrigger.TableZone.InspectDesk;

        if (isOverInspectDesk)
        {
            if (!isDragging && currentZone == TableTrigger.TableZone.ClosedDesk)
                return TableTrigger.TableZone.ClosedDesk;
            return TableTrigger.TableZone.InspectDesk;
        }

        if (isOverClosedDesk) return TableTrigger.TableZone.ClosedDesk;
        return currentZone;
    }

    public static TableTrigger.TableZone ResolvePhysicsZone(
        TableTrigger.TableZone currentZone,
        bool physicsOverClosedDesk,
        bool physicsOverInspectDesk)
    {
        if (currentZone == TableTrigger.TableZone.ClosedDesk)
            return TableTrigger.TableZone.ClosedDesk;

        if (physicsOverInspectDesk && physicsOverClosedDesk)
            return TableTrigger.TableZone.InspectDesk;
        if (physicsOverInspectDesk) return TableTrigger.TableZone.InspectDesk;
        if (physicsOverClosedDesk) return TableTrigger.TableZone.ClosedDesk;
        return currentZone;
    }

    private static void AccumulateTableZoneFlags(Collider2D[] hits, ref bool closedDesk, ref bool inspectDesk)
    {
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null) continue;
            TableTrigger table = hit.GetComponent<TableTrigger>();
            if (table == null) continue;
            if (table.Zone == TableTrigger.TableZone.ClosedDesk) closedDesk = true;
            if (table.Zone == TableTrigger.TableZone.InspectDesk) inspectDesk = true;
        }
    }

    private static Collider2D[] CollectDeskHitsAtDocumentCenterProbe(
        float zoneOverlapProbeRadius,
        Transform transform,
        Func<Bounds> computeActiveSpriteBoundsWorld)
    {
        Bounds b = computeActiveSpriteBoundsWorld();
        Vector2 p = b.size.sqrMagnitude > 1e-8f
            ? new Vector2(b.center.x, b.center.y)
            : new Vector2(transform.position.x, transform.position.y);
        float r = Mathf.Max(0.01f, zoneOverlapProbeRadius);
        return Physics2D.OverlapCircleAll(p, r);
    }

    private static Collider2D[] CollectDeskHitsFromDocumentBounds(
        float zoneOverlapProbeRadius,
        Transform transform,
        Func<Bounds> computeActiveSpriteBoundsWorld)
    {
        Bounds b = computeActiveSpriteBoundsWorld();
        if (b.size.sqrMagnitude > 1e-8f)
        {
            Vector2 a = new Vector2(b.min.x, b.min.y);
            Vector2 c = new Vector2(b.max.x, b.max.y);
            return Physics2D.OverlapAreaAll(a, c);
        }

        float radius = Mathf.Max(0.01f, zoneOverlapProbeRadius);
        Vector2 probe = new Vector2(transform.position.x, transform.position.y);
        return Physics2D.OverlapCircleAll(probe, radius);
    }

    private static Collider2D[] CollectDeskHitsAtCursorProbe(
        Camera mainCamera,
        float zoneOverlapProbeRadius,
        Transform transform,
        Func<Bounds> computeActiveSpriteBoundsWorld)
    {
        if (mainCamera == null)
            return CollectDeskHitsFromDocumentBounds(zoneOverlapProbeRadius, transform, computeActiveSpriteBoundsWorld);

        Vector3 mp = Input.mousePosition;
        if (mp.x < 0f || mp.x >= Screen.width || mp.y < 0f || mp.y >= Screen.height)
            return CollectDeskHitsFromDocumentBounds(zoneOverlapProbeRadius, transform, computeActiveSpriteBoundsWorld);

        mp.x = Mathf.Clamp(mp.x, 0f, Mathf.Max(0f, Screen.width - 1f));
        mp.y = Mathf.Clamp(mp.y, 0f, Mathf.Max(0f, Screen.height - 1f));
        Vector3 w = mainCamera.ScreenToWorldPoint(mp);
        float r = Mathf.Max(0.01f, zoneOverlapProbeRadius);
        return Physics2D.OverlapCircleAll(new Vector2(w.x, w.y), r);
    }
}
