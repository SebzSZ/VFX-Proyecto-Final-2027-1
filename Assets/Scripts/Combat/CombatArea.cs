using UnityEngine;

namespace FinalVFX.Combat
{
    public static class CombatArea
    {
        public static Collider[] QueryCylinder(Vector3 center, float radius, float height, LayerMask layers)
        {
            return Physics.OverlapBox(center, new Vector3(radius, height * 0.5f, radius),
                Quaternion.identity, layers, QueryTriggerInteraction.Collide);
        }

        public static bool IntersectsCylinder(Collider target, Vector3 center, float radius, float height)
        {
            float halfHeight = height * 0.5f;
            if (target is CapsuleCollider capsule && capsule.direction == 1 &&
                Mathf.Abs(Vector3.Dot(capsule.transform.up, Vector3.up)) > 0.9999f)
            {
                Bounds bounds = capsule.bounds;
                float r = bounds.extents.x;
                float midline = Mathf.Max(0f, bounds.extents.y - r);
                float dy = Mathf.Max(0f, Mathf.Abs(bounds.center.y - center.y) - halfHeight - midline);
                if (dy > r)
                    return false;
                float reach = radius + Mathf.Sqrt(Mathf.Max(0f, r * r - dy * dy));
                Vector3 delta = bounds.center - center;
                return delta.x * delta.x + delta.z * delta.z <= reach * reach;
            }

            Vector3 axis = center;
            axis.y = Mathf.Clamp(target.bounds.center.y, center.y - halfHeight, center.y + halfHeight);
            Vector3 offset = target.ClosestPoint(axis) - center;
            return Mathf.Abs(offset.y) <= halfHeight &&
                offset.x * offset.x + offset.z * offset.z <= radius * radius;
        }
    }
}
