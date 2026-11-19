using System.Collections.Generic;
using FinalVFX.Combat;
using UnityEngine;

namespace FinalVFX.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerHitVolumes : MonoBehaviour
    {
        [System.Serializable]
        private struct Box
        {
            public Vector3 center;
            public Vector3 size;
            public Vector3 euler;

            public Box(Vector3 center, Vector3 size)
            {
                this.center = center;
                this.size = size;
                euler = Vector3.zero;
            }
        }

        [SerializeField] private LayerMask _enemies;
        [SerializeField] private Box _basic = new Box(new Vector3(0, 1, 1.2f), new Vector3(1.6f, 1.8f, 2f));
        [SerializeField] private Box _swordSecondary = new Box(new Vector3(0, 1, 1.8f), new Vector3(3f, 2f, 3f));
        [Header("Secundario de guadaña")]
        [SerializeField] private float _centerHeight = 1f;
        [SerializeField, Min(0.01f)] private float _radius = 2.8f;
        [SerializeField, Min(0.01f)] private float _height = 2f;

        private readonly HashSet<HealthComponent> _touched = new HashSet<HealthComponent>();

        private PlayerUltimateEffects _ultimate;
        public event System.Action<HealthComponent, float> BasicDamaged;

        private void Awake() => _ultimate = GetComponent<PlayerUltimateEffects>();

        public void BeginStrike() => _touched.Clear();

        public void Strike(bool basic, WeaponDefinition.WeaponPoseType pose, float damage)
        {
            if (!isActiveAndEnabled)
                return;

            if (basic && _ultimate)
                damage *= _ultimate.BasicDamageMultiplier;
            Physics.SyncTransforms();
            if (!basic && pose == WeaponDefinition.WeaponPoseType.Scythe)
            {
                HitCircle(damage);
                return;
            }

            Box box = basic ? _basic : _swordSecondary;
            Vector3 center = transform.position + transform.rotation * box.center;
            Quaternion orientation = transform.rotation * Quaternion.Euler(box.euler);
            foreach (Collider target in Physics.OverlapBox(center, box.size * 0.5f,
                orientation, _enemies, QueryTriggerInteraction.Collide))
                Touch(target, damage, basic);
        }

        private void HitCircle(float damage)
        {
            Vector3 center = transform.position + Vector3.up * _centerHeight;
            Vector3 half = new Vector3(_radius, _height * 0.5f, _radius);
            foreach (Collider target in Physics.OverlapBox(center, half, Quaternion.identity,
                _enemies, QueryTriggerInteraction.Collide))
            {
                if (target is CapsuleCollider capsule && capsule.direction == 1 &&
                    Mathf.Abs(Vector3.Dot(capsule.transform.up, Vector3.up)) > 0.9999f)
                {
                    Bounds bounds = capsule.bounds;
                    float r = bounds.extents.x;
                    float midline = Mathf.Max(0f, bounds.extents.y - r);
                    float dy = Mathf.Max(0f, Mathf.Abs(bounds.center.y - center.y) - half.y - midline);
                    if (dy <= r)
                    {
                        float reach = _radius + Mathf.Sqrt(Mathf.Max(0f, r * r - dy * dy));
                        Vector3 delta = bounds.center - center;
                        if (delta.x * delta.x + delta.z * delta.z <= reach * reach)
                            Touch(target, damage);
                    }
                    continue;
                }

                Vector3 axis = center;
                axis.y = Mathf.Clamp(target.bounds.center.y, center.y - half.y, center.y + half.y);
                Vector3 offset = target.ClosestPoint(axis) - center;
                if (Mathf.Abs(offset.y) <= half.y &&
                    offset.x * offset.x + offset.z * offset.z <= _radius * _radius)
                    Touch(target, damage);
            }
        }

        private void Touch(Collider target, float damage, bool basic = false)
        {
            var health = target.GetComponentInParent<HealthComponent>();
            if (!health || !_touched.Add(health))
                return;
            float applied = health.ReceiveDamage(damage);
            if (basic && applied > 0f)
                BasicDamaged?.Invoke(health, applied);
        }

        private void OnDrawGizmosSelected()
        {
            DrawBox(_basic, Color.yellow);
            DrawBox(_swordSecondary, Color.red);
            Gizmos.color = Color.cyan;
            Vector3 mid = transform.position + Vector3.up * _centerHeight;
            Vector3 up = Vector3.up * (_height * 0.5f);
            Vector3 prev = Vector3.right * _radius;
            for (int i = 1; i <= 40; i++)
            {
                Vector3 point = Quaternion.AngleAxis(i * 9f, Vector3.up) * Vector3.right * _radius;
                Gizmos.DrawLine(mid + prev + up, mid + point + up);
                Gizmos.DrawLine(mid + prev - up, mid + point - up);
                if (i % 10 == 0)
                    Gizmos.DrawLine(mid + point - up, mid + point + up);
                prev = point;
            }
        }

        private void DrawBox(Box box, Color color)
        {
            Gizmos.color = color;
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position + transform.rotation * box.center,
                transform.rotation * Quaternion.Euler(box.euler), Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, box.size);
            Gizmos.matrix = previous;
        }
    }
}