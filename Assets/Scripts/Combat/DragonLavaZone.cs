using UnityEngine;
using FinalVFX.Combat;

namespace FinalVFX.Dragon
{
    [DisallowMultipleComponent]
    public sealed class DragonLavaZone : MonoBehaviour
    {
        [Header("Contenedor existente: Zone1, Zone2, etc.")]
        [SerializeField] private GameObject zoneRoot;

        [Header("Puntos del ataque")]
        [SerializeField] private Transform impactPoint;
        [SerializeField] private Transform attackPosition;

        [Header("Shader")]
        [SerializeField] private string amountProperty = "_LavaAmount";

        [Header("Duraciones")]
        [SerializeField, Min(0.05f)] private float appearSeconds = 2f;
        [SerializeField, Min(0.05f)] private float activeSeconds = 6f;
        [SerializeField, Min(0.05f)] private float disappearSeconds = 2f;

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
            if (zoneRoot == null ||
                impactPoint == null ||
                attackPosition == null)
            {
                Fail("Asigna Zone Root, Impact Point y Attack Position.");
                return;
            }

            // El controlador debe permanecer fuera de la zona apagable.
            if (transform == zoneRoot.transform ||
                transform.IsChildOf(zoneRoot.transform))
            {
                Fail("El controlador debe estar fuera de Zone Root.");
                return;
            }

            // Incluye hijos aunque el contenedor esté desactivado.
            surfaces = zoneRoot.GetComponentsInChildren<Renderer>(true);
            damageVolumes =
                zoneRoot.GetComponentsInChildren<FloorDamage>(true);

            zoneRoot.SetActive(false);
            SetDamage(false);

            amountId = Shader.PropertyToID(amountProperty);
            block = new MaterialPropertyBlock();

            if (surfaces.Length == 0 || damageVolumes.Length == 0)
            {
                Fail("La zona necesita superficies y componentes FloorDamage.");
                return;
            }

            // Todas las superficies de esta zona deben usar el shader preparado.
            foreach (Renderer surface in surfaces)
            {
                Material[] materials = surface.sharedMaterials;

                if (materials.Length == 0)
                {
                    Fail($"{surface.name} no tiene materiales.");
                    return;
                }

                foreach (Material material in materials)
                {
                    if (material == null || !material.HasProperty(amountId))
                    {
                        Fail(
                            $"{surface.name}: su material necesita " +
                            $"la propiedad {amountProperty}.");
                        return;
                    }
                }
            }

            ready = true;
            ResetZone();
        }

        public bool Ignite()
        {
            if (!IsAvailable)
                return false;

            // Preparar antes de activar evita un destello de lava completa.
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

            // Si otro sistema apaga el contenedor, cancelar este ciclo.
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
                        float t = Mathf.Clamp01(
                            elapsed / Mathf.Max(0.05f, appearSeconds));

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
                        float t = Mathf.Clamp01(
                            elapsed / Mathf.Max(0.05f, disappearSeconds));

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

        private void Fail(string message)
        {
            Debug.LogError($"{name}: {message}", this);
            enabled = false;
        }

        private void OnDisable()
        {
            ResetZone();
        }
    }
}