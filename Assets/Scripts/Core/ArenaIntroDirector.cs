using System;
using System.Collections;
using FinalVFX.Boss;
using FinalVFX.Combat;
using FinalVFX.Player;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FinalVFX.Core
{
    [DisallowMultipleComponent]
    public sealed class ArenaIntroDirector : MonoBehaviour
    {
        [Serializable]
        public sealed class Shot
        {
            public Transform pose;
            [Min(0.1f)] public float seconds = 2.5f;
            public Vector3 localTravel = new Vector3(0, 0, 0.5f);
            public bool lookAtBoss = true;
            [Range(15, 90)] public float fieldOfView = 45;
            public Vector2 focusOffset = Vector2.zero;
            public Vector2 aperture = new Vector2(4, 5.6f);
            [Range(1, 300)] public float focalLength = 50;
        }

        [Header("Existing scene objects")]
        [SerializeField] private PlayerController _player;
        [SerializeField] private PlayerEquipment _equipment;
        [SerializeField] private PlayerCameraController _playerCamera;
        [SerializeField] private CharacterController _body;
        [SerializeField] private InvulnerabilityComponent _playerProtection;
        [SerializeField] private BossController _boss;
        [SerializeField] private InvulnerabilityComponent _bossProtection;
        [SerializeField] private Transform _playerDestination;
        [SerializeField] private Transform _bossAim;
        [SerializeField] private GameObject _entranceRock;
        [SerializeField] private GameObject _gameplayHUD;
        [SerializeField] private GameObject _bossHUD;
        [SerializeField] private CanvasGroup _blackScreen;
        [Header("Cinemachine 3")]
        [SerializeField] private CinemachineBrain _brain;
        [SerializeField] private CinemachineCamera _introCamera;
        [SerializeField] private CinemachineCamera _gameplayCamera;
        [SerializeField] private CinemachineCamera _bossLockCamera;
        [SerializeField] private Shot[] _shots = { new Shot(), new Shot(), new Shot() };
        [Header("Independent fog and UI")]
        [SerializeField] private ArenaIntroFog _heavyFog;
        [SerializeField] private GameObject _skipHint;
        [SerializeField, Min(0.01f)] private float _fadeSeconds = 0.4f;
        [SerializeField, Min(0.01f)] private float _fogSeconds = 0.2f;
        [SerializeField, Min(1)] private float _cameraTimeout = 5;
        [SerializeField] private string _seenKey = "FinalVFX.SCN_Arena.IntroSeen.v1";

        private bool _armed, _running, _finished, _skipAllowed, _skip;
        private bool _ownsBlends, _cameraReady;
        private Renderer[] _renderers;
        private bool[] _renderingOff;
        private CinemachineBlendDefinition _savedBlend;
        private CinemachineBlenderSettings _savedCustomBlends;
        private GameObject _volumeObject;
        private VolumeProfile _profile;
        private DepthOfField _dof;
        private float _shotElapsed;
        private Shot _currentShot;
        private Vector3 _shotPosition;
        private Quaternion _shotRotation;

        public bool ValidateSetup()
        {
            UnityEngine.Object[] required = {
                _player, _equipment, _playerCamera, _body, _playerProtection,
                _boss, _bossProtection, _playerDestination, _bossAim, _entranceRock,
                _gameplayHUD, _bossHUD, _blackScreen, _brain, _introCamera,
                _gameplayCamera, _bossLockCamera, _heavyFog, _skipHint
            };
            foreach (var item in required)
                if (item == null) { Debug.LogError("ArenaIntroDirector: falta una referencia.", this); return false; }
            if (_shots == null || _shots.Length != 4 ||
                Array.Exists(_shots, s => s == null || s.pose == null || s.seconds <= 0) ||
                _introCamera == _gameplayCamera || _introCamera == _bossLockCamera ||
                string.IsNullOrWhiteSpace(_seenKey))
            { Debug.LogError("ArenaIntroDirector: revisa los tres planos, cámaras y Seen Key.", this); return false; }
            return true;
        }

        private void Awake()
        {
            if (_bossHUD) _bossHUD.SetActive(false);
            if (_entranceRock) _entranceRock.SetActive(false);
            if (_introCamera) _introCamera.gameObject.SetActive(false);
            if (_bossLockCamera) _bossLockCamera.gameObject.SetActive(false);
            if (_skipHint) _skipHint.SetActive(false);
        }

        // Called synchronously after WeaponSelectionFlow activates EncounterActors.
        public void Arm()
        {
            if (!ValidateSetup()) return;
            _boss.SetCombatEnabled(false);
            _bossProtection.SetWindow(this, Time.time, float.PositiveInfinity);
            _bossHUD.SetActive(false);
            _heavyFog.SetOpacity(1);
            _armed = true;
        }

        public void TryEnter(Collider other)
        {
            if (!_armed || _running || _finished || _player.IsSequenceLocked ||
                _equipment.EquippedWeapon == null ||
                other.GetComponentInParent<PlayerController>() != _player) return;
            _running = true;
            StartCoroutine(Play());
        }

        private void Update()
        {
            if (!_running || !_skipAllowed || !Application.isFocused)
                return;

            bool keyboardPressed =
                Keyboard.current != null &&
                Keyboard.current.anyKey.wasPressedThisFrame;

            bool gamepadPressed =
                Gamepad.current != null &&
                Gamepad.current.startButton.wasPressedThisFrame;

            if (keyboardPressed || gamepadPressed)
                _skip = true;
        }

        private void LateUpdate()
        {
            if (_currentShot == null) return;
            _shotElapsed += Time.unscaledDeltaTime;
            float eased = Mathf.SmoothStep(0, 1, Mathf.Clamp01(_shotElapsed / _currentShot.seconds));
            Transform cam = _introCamera.transform;
            cam.SetPositionAndRotation(
                _shotPosition + _shotRotation * (_currentShot.localTravel * eased), _shotRotation);
            Vector3 direction = _bossAim.position - cam.position;
            if (_currentShot.lookAtBoss && direction.sqrMagnitude > 0.0001f)
                cam.rotation = Quaternion.LookRotation(direction, Vector3.up);
            float distance = Vector3.Dot(direction, cam.forward);
            _dof.focusDistance.value = Mathf.Max(0.1f,
                distance + Mathf.Lerp(_currentShot.focusOffset.x, _currentShot.focusOffset.y, eased));
            _dof.aperture.value = Mathf.Lerp(_currentShot.aperture.x, _currentShot.aperture.y, eased);
        }

        private IEnumerator Play()
        {
            _skip = false;
            _skipAllowed = PlayerPrefs.GetInt(_seenKey, 0) == 1;
            _player.SetSequenceLocked(true);
            _player.SetGameplayEnabled(false);
            _playerProtection.SetWindow(this, Time.time, float.PositiveInfinity);
            _gameplayHUD.SetActive(false);
            yield return Fade(1);

            _body.enabled = false;
            _player.transform.SetPositionAndRotation(_playerDestination.position, _playerDestination.rotation);
            _body.enabled = true;
            _entranceRock.SetActive(true);
            Physics.SyncTransforms();
            HidePlayer();
            _playerCamera.ResetOrientation();
            _playerCamera.FollowTarget();
            _gameplayCamera.PreviousStateIsValid = false;

            float fogTime = 0;
            while (fogTime < _fogSeconds)
            {
                fogTime += Time.unscaledDeltaTime;
                _heavyFog.SetOpacity(1 - Mathf.Clamp01(fogTime / _fogSeconds));
                yield return null;
            }
            _heavyFog.SetOpacity(0);

            _savedBlend = _brain.DefaultBlend;
            _savedCustomBlends = _brain.CustomBlends;
            _ownsBlends = true;
            _brain.CustomBlends = null;
            _brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0);
            CreateFocusVolume();
            _introCamera.Priority = 1000;
            _introCamera.gameObject.SetActive(true);

            bool completed = true;
            foreach (Shot shot in _shots)
            {
                if (_skip) { completed = false; break; }
                PrepareShot(shot);
                yield return WaitForCamera(_introCamera);
                if (!_cameraReady) { completed = false; break; }
                _skipHint.SetActive(_skipAllowed);
                _currentShot = shot;
                yield return Fade(0);
                while (_shotElapsed < shot.seconds && !_skip) yield return null;
                _skipHint.SetActive(false);
                yield return Fade(1);
                _currentShot = null;
                if (_skip) { completed = false; break; }
            }

            _currentShot = null;
            _skipAllowed = false;
            _skipHint.SetActive(false);
            yield return Fade(1);
            _introCamera.gameObject.SetActive(false);
            DestroyFocusVolume();
            RestorePlayer();
            _playerCamera.ResetOrientation();
            _playerCamera.FollowTarget();
            _gameplayCamera.PreviousStateIsValid = false;
            yield return WaitForCamera(_gameplayCamera);
            RestoreBlends();
            if (!_cameraReady)
            {
                Debug.LogError("No se recuperó CM_PlayerCamera. Combate bloqueado. Revisa canales/prioridades y reinicia Play.", this);
                _running = false;
                yield break;
            }
            yield return Fade(0);
            if (completed)
            {
                PlayerPrefs.SetInt(_seenKey, 1);
                PlayerPrefs.Save();
            }
            _finished = true;
            _armed = false;
            _running = false;
            _gameplayHUD.SetActive(true);
            _bossHUD.SetActive(true);
            _bossLockCamera.gameObject.SetActive(true);
            _playerProtection.RemoveWindow(this);
            _bossProtection.RemoveWindow(this);
            _player.SetSequenceLocked(false);
            _player.SetGameplayEnabled(true);
            _boss.SetCombatEnabled(true);
        }

        private void PrepareShot(Shot shot)
        {
            _shotPosition = shot.pose.position;
            _shotRotation = shot.pose.rotation;
            _shotElapsed = 0;
            _introCamera.transform.SetPositionAndRotation(_shotPosition, _shotRotation);
            if (shot.lookAtBoss && (_bossAim.position - _shotPosition).sqrMagnitude > 0.0001f)
                _introCamera.transform.LookAt(_bossAim);
            var lens = _introCamera.Lens;
            lens.FieldOfView = shot.fieldOfView;
            _introCamera.Lens = lens;
            _introCamera.PreviousStateIsValid = false;
            _dof.focalLength.value = shot.focalLength;
            _dof.aperture.value = shot.aperture.x;
            _dof.focusDistance.value = Mathf.Max(0.1f,
                Vector3.Dot(_bossAim.position - _shotPosition, _introCamera.transform.forward) + shot.focusOffset.x);
        }

        private IEnumerator WaitForCamera(CinemachineCamera target)
        {
            _cameraReady = false;
            float elapsed = 0;
            yield return null;
            yield return new WaitForEndOfFrame();
            while (!ReferenceEquals(_brain.ActiveVirtualCamera, target) || _brain.IsBlending)
            {
                elapsed += Time.unscaledDeltaTime;
                if (elapsed >= _cameraTimeout)
                {
                    Debug.LogError("ArenaIntroDirector: no se activó " + target.name, this);
                    yield break;
                }
                yield return null;
            }
            _cameraReady = true;
        }

        private IEnumerator Fade(float target)
        {
            _blackScreen.blocksRaycasts = true;
            _blackScreen.interactable = false;
            float from = _blackScreen.alpha;
            if (Mathf.Approximately(from, target)) yield break;
            float elapsed = 0;
            while (elapsed < _fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                _blackScreen.alpha = Mathf.Lerp(from, target,
                    Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / _fadeSeconds)));
                yield return null;
            }
            _blackScreen.alpha = target;
            _blackScreen.blocksRaycasts = target > 0;
        }

        private void HidePlayer()
        {
            _renderers = _player.GetComponentsInChildren<Renderer>(true);
            _renderingOff = new bool[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderingOff[i] = _renderers[i].forceRenderingOff;
                _renderers[i].forceRenderingOff = true;
            }
        }

        private void RestorePlayer()
        {
            if (_renderers == null) return;
            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i]) _renderers[i].forceRenderingOff = _renderingOff[i];
            _renderers = null;
        }

        private void CreateFocusVolume()
        {
            _volumeObject = new GameObject("ArenaIntro_RuntimeDOF");
            _volumeObject.layer = gameObject.layer;
            var volume = _volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1000;
            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _dof = _profile.Add<DepthOfField>(true);
            _dof.mode.Override(DepthOfFieldMode.Bokeh);
            volume.sharedProfile = _profile;
        }

        private void DestroyFocusVolume()
        {
            if (_volumeObject) { _volumeObject.SetActive(false); Destroy(_volumeObject); }
            if (_dof) Destroy(_dof);
            if (_profile) Destroy(_profile);
            _volumeObject = null;
            _profile = null;
            _dof = null;
        }

        private void RestoreBlends()
        {
            if (!_ownsBlends || !_brain) return;
            _brain.DefaultBlend = _savedBlend;
            _brain.CustomBlends = _savedCustomBlends;
            _ownsBlends = false;
        }

        [ContextMenu("Forget intro seen (testing)")]
        private void ForgetSeen()
        {
            PlayerPrefs.DeleteKey(_seenKey);
            PlayerPrefs.Save();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            _currentShot = null;
            RestorePlayer();
            RestoreBlends();
            DestroyFocusVolume();
            if (_introCamera) _introCamera.gameObject.SetActive(false);
            if (_skipHint) _skipHint.SetActive(false);
            if (_playerProtection) _playerProtection.RemoveWindow(this);
            if (_bossProtection) _bossProtection.RemoveWindow(this);
            // Keep this director enabled until the scene reloads.
        }
    }
}
