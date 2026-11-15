using System;
using System.Collections;
using FinalVFX.Combat;
using UnityEngine;

namespace FinalVFX.Core
{
    [DisallowMultipleComponent]
    public sealed class WeaponSelectionPresentation : MonoBehaviour
    {
        [Serializable]
        private sealed class SelectionEntry
        {
            public WeaponDisplay Display;
            public CanvasGroup NameGroup;
        }

        [Header("Exactly two entries: left, right")]
        [SerializeField] private SelectionEntry[] _entries;
        [SerializeField] private GameObject _selectionUI;
        [SerializeField, Min(0.01f)] private float _weaponNameFadeDuration = 0.2f;
        [SerializeField, Min(0.01f)] private float _weaponFadeDuration = 1f;
        [SerializeField, Min(0.01f)] private float _weaponAppearDuration = 1.5f;

        public Vector3 Midpoint => (FocusPoint(0).position + FocusPoint(1).position) * 0.5f;
        public Transform FocusPoint(int index) => _entries[index].Display.FocusPoint;
        public WeaponDefinition Definition(int index) => _entries[index].Display.Definition;

        public bool ValidateSetup()
        {
            if (_selectionUI == null || _entries == null || _entries.Length != 2)
            {
                Debug.LogError("WeaponSelectionPresentation necesita SelectionUI y dos entradas (izquierda, derecha).", this);
                return false;
            }
            for (int i = 0; i < _entries.Length; i++)
            {
                SelectionEntry entry = _entries[i];
                if (entry == null || entry.Display == null || entry.NameGroup == null ||
                    entry.Display.Definition == null || entry.Display.Definition.EquippedPrefab == null)
                {
                    Debug.LogError($"WeaponSelectionPresentation: revisa exhibicion, nombre, perfil y prefab de la entrada {i}.", this);
                    return false;
                }
            }
            return true;
        }

        public void Initialize()
        {
            foreach (SelectionEntry entry in _entries)
                entry.NameGroup.alpha = 0f;
            _selectionUI.SetActive(false);
        }

        public void BeginAppear()
        {
            foreach (SelectionEntry entry in _entries)
            {
                entry.Display.gameObject.SetActive(true);
                entry.Display.BeginAppear();
            }
        }

        public IEnumerator Appear()
        {
            yield return AnimateWeapons(true, _weaponAppearDuration);
            foreach (SelectionEntry entry in _entries)
                entry.Display.FinishAppear();
        }

        public void Select(int index)
        {
            for (int i = 0; i < _entries.Length; i++)
                _entries[i].Display.SetSelected(i == index);
        }

        public void ShowSelection() => _selectionUI.SetActive(true);

        // The flow drives this in LateUpdate, preserving the original frame order.
        public void TickNames(bool selecting, int selectedIndex)
        {
            if (!_selectionUI.activeInHierarchy)
                return;
            for (int i = 0; i < _entries.Length; i++)
            {
                float target = selecting && i == selectedIndex ? 1f : 0f;
                CanvasGroup label = _entries[i].NameGroup;
                label.alpha = Mathf.MoveTowards(label.alpha, target,
                    Time.deltaTime / Mathf.Max(0.01f, _weaponNameFadeDuration));
            }
        }

        public void BeginDisappear(int selectedIndex)
        {
            for (int i = 0; i < _entries.Length; i++)
                _entries[i].Display.BeginDisappear(i == selectedIndex);
        }

        public IEnumerator Disappear()
        {
            yield return AnimateWeapons(false, _weaponFadeDuration);
            // Preserve the label fade even when weapon disappearance is shorter.
            while (_entries[0].NameGroup.alpha > 0f || _entries[1].NameGroup.alpha > 0f)
                yield return null;
            _selectionUI.SetActive(false);
            foreach (SelectionEntry entry in _entries)
                entry.Display.gameObject.SetActive(false);
        }

        private IEnumerator AnimateWeapons(bool appearing, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration)));
                foreach (SelectionEntry entry in _entries)
                    entry.Display.SetVisibility(appearing ? progress : 1f - progress);
                yield return null;
            }
            foreach (SelectionEntry entry in _entries)
                entry.Display.SetVisibility(appearing ? 1f : 0f);
        }
    }
}
