using System.Collections.Generic;
using UnityEngine;

namespace FinalVFX.Boss
{
    public sealed class BossFogBarrier : MonoBehaviour
    {
        private static readonly List<BossFogBarrier> Active = new List<BossFogBarrier>();
        public float Radius { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Active.Clear();
        private void OnEnable() { if (!Active.Contains(this)) Active.Add(this); }
        private void OnDisable() => Active.Remove(this);
        public void Configure(float radius, float height)
        {
            Radius = radius;
            transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
        }

        public static Vector3 Constrain(CharacterController body, Vector3 motion)
        {
            foreach (var barrier in Active)
            {
                if (!barrier || barrier.Radius <= 0f) continue;
                Vector3 offset = body.transform.TransformPoint(body.center) - barrier.transform.position;
                Vector2 p = new Vector2(offset.x, offset.z);
                Vector2 d = new Vector2(motion.x, motion.z);
                Vector3 scale = body.transform.lossyScale;
                float r = barrier.Radius + body.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)) + 0.04f;
                float length = p.magnitude;
                if (length < r)
                {
                    Vector2 normal = length > 0.001f ? p / length : Vector2.up;
                    float inward = Vector2.Dot(d, normal);
                    if (inward < 0f) d -= normal * inward;
                }
                else if (d.sqrMagnitude > 0.000001f)
                {
                    float b = Vector2.Dot(p, d);
                    float c = p.sqrMagnitude - r * r;
                    float disc = b * b - d.sqrMagnitude * c;
                    if (b < 0f && disc >= 0f)
                    {
                        float t = (-b - Mathf.Sqrt(disc)) / d.sqrMagnitude;
                        if (t >= 0f && t <= 1f)
                        {
                            Vector2 contact = p + d * Mathf.Max(0f, t - 0.001f);
                            Vector2 n = contact.normalized;
                            Vector2 remainder = d * (1f - t);
                            remainder -= n * Mathf.Min(0f, Vector2.Dot(remainder, n));
                            d = contact - p + remainder;
                        }
                    }
                }
                motion.x = d.x;
                motion.z = d.y;
            }
            return motion;
        }
    }
}
