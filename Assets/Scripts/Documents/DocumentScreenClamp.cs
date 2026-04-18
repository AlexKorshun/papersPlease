using System;
using UnityEngine;

public static class DocumentScreenClamp
{
    public static void GetCameraWorldScreenRect(Camera cam, out float minX, out float maxX, out float minY, out float maxY)
    {
        minX = maxX = minY = maxY = 0f;
        if (cam == null) return;

        if (cam.orthographic)
        {
            float height = cam.orthographicSize * 2f;
            float width = height * cam.aspect;
            Vector3 c = cam.transform.position;
            minX = c.x - width * 0.5f;
            maxX = c.x + width * 0.5f;
            minY = c.y - height * 0.5f;
            maxY = c.y + height * 0.5f;
        }
        else
        {
            float zDist = Mathf.Abs(cam.transform.position.z);
            Vector3 bl = cam.ViewportToWorldPoint(new Vector3(0f, 0f, zDist));
            Vector3 tr = cam.ViewportToWorldPoint(new Vector3(1f, 1f, zDist));
            minX = Mathf.Min(bl.x, tr.x);
            maxX = Mathf.Max(bl.x, tr.x);
            minY = Mathf.Min(bl.y, tr.y);
            maxY = Mathf.Max(bl.y, tr.y);
        }
    }

    public static void ClampDocument(
        Transform transform,
        Camera mainCamera,
        bool clampEnabled,
        float screenClampPaddingWorld,
        TableTrigger.TableZone currentZone,
        float inspectMinVisibleWidthFraction,
        float inspectMinVisibleHeightFraction,
        Func<Bounds> computeActiveSpriteBoundsWorld)
    {
        if (!clampEnabled || mainCamera == null) return;

        GetCameraWorldScreenRect(mainCamera, out float sminX, out float smaxX, out float sminY, out float smaxY);
        float pad = screenClampPaddingWorld;
        sminX += pad;
        smaxX -= pad;
        sminY += pad;
        smaxY -= pad;

        if (smaxX < sminX || smaxY < sminY) return;

        if (currentZone == TableTrigger.TableZone.InspectDesk)
            ClampInspectMinVisibleOverlap(transform, sminX, smaxX, sminY, smaxY, inspectMinVisibleWidthFraction, inspectMinVisibleHeightFraction, computeActiveSpriteBoundsWorld);
        else
            ClampFullyInsideScreenRect(transform, sminX, smaxX, sminY, smaxY, computeActiveSpriteBoundsWorld);
    }

    private static void ClampFullyInsideScreenRect(
        Transform transform,
        float sminX, float smaxX, float sminY, float smaxY,
        Func<Bounds> computeActiveSpriteBoundsWorld)
    {
        Bounds doc = computeActiveSpriteBoundsWorld();
        Vector3 pos = transform.position;

        if (doc.size.sqrMagnitude < 1e-8f)
        {
            pos.x = Mathf.Clamp(pos.x, sminX, smaxX);
            pos.y = Mathf.Clamp(pos.y, sminY, smaxY);
            transform.position = pos;
            return;
        }

        Vector3 delta = Vector3.zero;
        if (doc.min.x < sminX) delta.x += sminX - doc.min.x;
        if (doc.max.x > smaxX) delta.x += smaxX - doc.max.x;
        if (doc.min.y < sminY) delta.y += sminY - doc.min.y;
        if (doc.max.y > smaxY) delta.y += smaxY - doc.max.y;

        if (delta.sqrMagnitude > 0f)
            transform.position = pos + delta;
    }

    private static void ClampInspectMinVisibleOverlap(
        Transform transform,
        float sminX, float smaxX, float sminY, float smaxY,
        float inspectMinVisibleWidthFraction,
        float inspectMinVisibleHeightFraction,
        Func<Bounds> computeActiveSpriteBoundsWorld)
    {
        float screenW = smaxX - sminX;
        float screenH = smaxY - sminY;
        float span = Mathf.Max(screenW, screenH) * 2f + 5f;

        for (int pass = 0; pass < 4; pass++)
        {
            Vector3 pos = transform.position;
            transform.position = pos;
            Bounds doc = computeActiveSpriteBoundsWorld();

            if (doc.size.sqrMagnitude < 1e-8f)
            {
                pos.x = Mathf.Clamp(pos.x, sminX, smaxX);
                pos.y = Mathf.Clamp(pos.y, sminY, smaxY);
                transform.position = pos;
                return;
            }

            float needX = Mathf.Max(0.01f, inspectMinVisibleWidthFraction * doc.size.x);
            float needY = Mathf.Max(0.01f, inspectMinVisibleHeightFraction * doc.size.y);
            needX = Mathf.Min(needX, Mathf.Max(0.02f, screenW));
            needY = Mathf.Min(needY, Mathf.Max(0.02f, screenH));

            float slackScreen = Mathf.Max(1e-4f, Mathf.Min(screenW, screenH) * 1e-5f);
            float ix = Intersection1D(doc.min.x, doc.max.x, sminX, smaxX);
            float iy = Intersection1D(doc.min.y, doc.max.y, sminY, smaxY);
            if (ix >= needX - slackScreen && iy >= needY - slackScreen)
                return;

            float ox0 = doc.min.x - pos.x;
            float ox1 = doc.max.x - pos.x;
            float oy0 = doc.min.y - pos.y;
            float oy1 = doc.max.y - pos.y;

            span = Mathf.Max(doc.size.x, doc.size.y, screenW, screenH) * 2f + 5f;

            InspectFindFeasibleAxisInterval(ox0, ox1, sminX, smaxX, needX, pos.x, span, out float minX, out float maxX);
            InspectFindFeasibleAxisInterval(oy0, oy1, sminY, smaxY, needY, pos.y, span, out float minY, out float maxY);

            Vector3 next = pos;
            next.x = Mathf.Clamp(pos.x, minX, maxX);
            next.y = Mathf.Clamp(pos.y, minY, maxY);

            if ((next - pos).sqrMagnitude < 1e-12f)
            {
                transform.position = next;
                return;
            }

            transform.position = next;
        }
    }

    private static float Intersection1D(float a0, float a1, float b0, float b1)
    {
        return Mathf.Max(0f, Mathf.Min(a1, b1) - Mathf.Max(a0, b0));
    }

    private static bool InspectAxisFeasible(
        float px, float ox0, float ox1, float s0, float s1, float need)
    {
        float ix = Intersection1D(px + ox0, px + ox1, s0, s1);
        float slack = Mathf.Max(1e-4f, need * 5e-5f);
        return ix >= need - slack;
    }

    private static float InspectBinarySearchBoundary(
        float lo, float hi, bool wantLeftEdge, Func<float, bool> feasible)
    {
        for (int i = 0; i < 36; i++)
        {
            float mid = (lo + hi) * 0.5f;
            if (wantLeftEdge)
            {
                if (feasible(mid)) hi = mid;
                else lo = mid;
            }
            else
            {
                if (feasible(mid)) lo = mid;
                else hi = mid;
            }
        }
        return wantLeftEdge ? hi : lo;
    }

    private static void InspectFindFeasibleAxisInterval(
        float ox0, float ox1, float s0, float s1, float need, float desiredP, float span,
        out float minP, out float maxP)
    {
        minP = desiredP;
        maxP = desiredP;

        Func<float, bool> feas = p => InspectAxisFeasible(p, ox0, ox1, s0, s1, need);

        if (!feas(desiredP))
        {
            float center = (s0 + s1 - ox0 - ox1) * 0.5f;
            float p = desiredP;
            for (int k = 0; k < 48; k++)
            {
                p = Mathf.Lerp(p, center, 0.25f);
                if (feas(p))
                {
                    desiredP = p;
                    break;
                }
            }
        }

        if (!feas(desiredP))
        {
            minP = maxP = Mathf.Clamp(desiredP, s0 - ox1, s1 - ox0);
            return;
        }

        float delta = Mathf.Max(0.02f, span * 0.05f);
        float loInfeasible = desiredP;
        int i;
        for (i = 0; i < 48; i++)
        {
            loInfeasible -= delta;
            if (!feas(loInfeasible))
                break;
            delta *= 1.35f;
        }

        if (!feas(loInfeasible))
            minP = InspectBinarySearchBoundary(loInfeasible, desiredP, true, feas);
        else
            minP = loInfeasible;

        delta = Mathf.Max(0.02f, span * 0.05f);
        float hiInfeasible = desiredP;
        for (i = 0; i < 48; i++)
        {
            hiInfeasible += delta;
            if (!feas(hiInfeasible))
                break;
            delta *= 1.35f;
        }

        if (!feas(hiInfeasible))
            maxP = InspectBinarySearchBoundary(desiredP, hiInfeasible, false, feas);
        else
            maxP = hiInfeasible;

        if (minP > maxP)
        {
            float t = minP;
            minP = maxP;
            maxP = t;
        }
    }
}
