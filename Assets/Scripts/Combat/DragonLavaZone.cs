using UnityEngine;
using FinalVFX.Combat;

namespace FinalVFX.Dragon
{
    public sealed class DragonLavaZone : MonoBehaviour
    {
        [Header("Contenedor")]
        [SerializeField] private GameObject zoneRoot;

        [Header("Puntos del ataque")]
        [SerializeField] private Transform impactPoint;
        [SerializeField] private Transform attackPosition;

        [Header("Shader")]
        [SerializeField] private string amountProperty;

        [Header("Duraciones")]
        [SerializeField] private float appearSeconds;
        [SerializeField] private float activeSeconds;
        [SerializeField] private float disappearSeconds;

        private enum State
        {
            Hidden,
            Appearing,
            Active,
            Disappearing
        }

        private Renderer[] surfaces;
        private FloorDamage[] damageVolumes;
        private MaterialPropertyBlock block;

        private State state;
        private float elapsed;
        private int amountId;
        private bool ready;

        public Vector3 ImpactPosition => impactPoint.position;
        public Vector3 AttackPosition => attackPosition.position;

        public bool IsAvailable =>
            ready &&
            isActiveAndEnabled &&
            zoneRoot != null &&
            state == State.Hidden;

        private void Awake()
        {
            surfaces = zoneRoot.GetComponentsInChildren<Renderer>(true);
            damageVolumes = zoneRoot.GetComponentsInChildren<FloorDamage>(true);

            zoneRoot.SetActive(false);
            SetDamage(false);

            amountId = Shader.PropertyToID(amountProperty);
            block = new MaterialPropertyBlock();

            foreach (Renderer surface in surfaces)
            {
                Material[] materials = surface.sharedMaterials;
            }

            ready = true;
            ResetZone();
        }

        public bool Ignite()
        {
            if (!IsAvailable)
                return false;

            SetDamage(false);
            SetAmount(0f);

            state = State.Appearing;
            elapsed = 0f;

            zoneRoot.SetActive(true);
            return true;
        }

        private void Update()
        {
            if (!ready || Time.deltaTime <= 0f)
                return;

            if (state == State.Hidden)
                return;

            if (zoneRoot == null || !zoneRoot.activeInHierarchy)
            {
                ResetZone();
                return;
            }

            elapsed += Time.deltaTime;

            switch (state)
            {
                case State.Appearing:
                    {
                        float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, appearSeconds));

                        SetAmount(Mathf.SmoothStep(0f, 1f, t));

                        if (t >= 1f)
                        {
                            state = State.Active;
                            elapsed = 0f;
                            SetDamage(true);
                        }

                        break;
                    }

                case State.Active:
                    if (elapsed >= activeSeconds)
                    {
                        SetDamage(false);
                        state = State.Disappearing;
                        elapsed = 0f;
                    }
                    break;

                case State.Disappearing:
                    {
                        float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, disappearSeconds));

                        SetAmount(1f - Mathf.SmoothStep(0f, 1f, t));

                        if (t >= 1f)
                            ResetZone();

                        break;
                    }
            }
        }

        private void SetAmount(float amount)
        {
            if (surfaces == null || block == null)
                return;

            foreach (Renderer surface in surfaces)
            {
                if (surface == null)
                    continue;

                surface.GetPropertyBlock(block);
                block.SetFloat(amountId, amount);
                surface.SetPropertyBlock(block);
            }
        }

        private void SetDamage(bool active)
        {
            if (damageVolumes == null)
                return;

            foreach (FloorDamage damage in damageVolumes)
            {
                if (damage != null)
                    damage.enabled = active;
            }
        }

        public void ResetZone()
        {
            state = State.Hidden;
            elapsed = 0f;

            SetDamage(false);
            SetAmount(0f);

            if (zoneRoot != null &&
                transform != zoneRoot.transform &&
                !transform.IsChildOf(zoneRoot.transform))
            {
                zoneRoot.SetActive(false);
            }
        }

        private void OnDisable()
        {
            ResetZone();
        }
    }
}