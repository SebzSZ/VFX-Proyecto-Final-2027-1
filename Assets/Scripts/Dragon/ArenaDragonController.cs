using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.VFX;
using FinalVFX.Boss;
using FinalVFX.Audio;

namespace FinalVFX.Dragon
{
    public sealed class ArenaDragonController : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private GuardianHealth boss;
        [SerializeField] private SplineAnimate highFlight;
        [SerializeField] private Animator animator;
        [SerializeField] private VisualEffect breath;
        [SerializeField] private Transform breathAim;
        [SerializeField] private Transform[] lowWaypoints;
        [SerializeField] private DragonLavaZone[] zones;

        [Header("Audio")]
        [SerializeField] private AudioClip _attackDecisionSound;
        [SerializeField] private AudioClip _fireLoopSound;
        private SoundEffectPool.Handle _fireSound;

        [Header("Vuelo")]
        [SerializeField] private float moveSpeed;
        [SerializeField] private Vector2 flapSeconds;
        [SerializeField] private Vector2 glideSeconds;

        [SerializeField, Min(0.1f)]
        private float acceleration = 8f;
        private float currentMoveSpeed;

        [Header("Ataques")]
        [SerializeField] private Vector2 attackInterval;
        [SerializeField] private float fireStart;
        [SerializeField] private float fireEnd;
        [SerializeField] private float travelSeconds;
        [SerializeField] private float attackTimeout;

        [Header("Trayectorias curvas")]
        [SerializeField, Range(0.15f, 0.6f)]
        private float curveStrength = 0.35f;

        [SerializeField, Min(1f)]
        private float minimumCurveHandle = 5f;

        [SerializeField, Min(2f)]
        private float maximumCurveHandle = 20f;

        private readonly List<Vector3> flightPositions = new List<Vector3>();
        private readonly List<Quaternion> flightRotations = new List<Quaternion>();
        private readonly List<float> flightDistances = new List<float>();

        private bool flightPathReady;
        private Vector3 flightDestination;
        private float flightDistance;
        private float flightLength;



        private enum State
        {
            HighFlight,
            Descending,
            Wandering,
            Approaching,
            Attacking
        }

        private static readonly int Flap = Animator.StringToHash("Base Layer.Flap");
        private static readonly int Glide = Animator.StringToHash("Base Layer.Glide");
        private static readonly int Breath = Animator.StringToHash("Base Layer.Breath");

        private readonly List<DragonLavaZone> candidates = new List<DragonLavaZone>();

        private State state;
        private DragonLavaZone target;
        private DragonLavaZone lastTarget;
        private int waypoint = -1;
        private float nextAttack;
        private float nextFlightAnimation;
        private float attackElapsed;
        private float emissionElapsed;
        private bool gliding;
        private bool fireStarted;
        private bool fireStopped;
        private bool zoneIgnited;
        private bool ready;

        private void Start()
        {
            bool valid =
                boss != null &&
                highFlight != null &&
                highFlight.transform == transform &&
                animator != null &&
                breath != null &&
                breathAim != null &&
                lowWaypoints != null &&
                lowWaypoints.Length > 0 &&
                zones != null &&
                zones.Length > 0 &&
                fireEnd > fireStart;

            if (valid)
            {
                foreach (Transform point in lowWaypoints)
                    valid &= point != null;

                foreach (DragonLavaZone zone in zones)
                    valid &= zone != null;

                valid &=
                    animator.HasState(0, Flap) &&
                    animator.HasState(0, Glide) &&
                    animator.HasState(0, Breath) &&
                    breath.HasFloat("BreathSpeed") &&
                    breath.HasFloat("BreathLifetime");
            }

            if (!valid)
            {
                enabled = false;
                return;
            }

            animator.applyRootMotion = false;

            breath.initialEventName = "Idle";
            breath.Reinit();
            breath.Stop();

            state = State.HighFlight;
            ready = true;

            animator.speed = 1f;
            highFlight.enabled = true;
            highFlight.Play();

            PlayFlight(false);
        }

        private void Update()
        {
            if (!ready || Time.deltaTime <= 0f)
                return;

            if (boss == null)
            {
                enabled = false;
                return;
            }

            if (!boss.gameObject.activeInHierarchy)
            {
                TickFlightAnimation();
                return;
            }

            if (boss.IsDead)
            {
                enabled = false;
                return;
            }

            if (state == State.HighFlight && boss.Phase >= 2)
            {
                highFlight.enabled = false;
                flightPathReady = false;
                currentMoveSpeed = moveSpeed;
                PickWaypoint();
                state = State.Descending;
                PlayFlight(false);
            }

            if (state != State.Attacking)
                TickFlightAnimation();

            switch (state)
            {
                case State.HighFlight:
                    break;

                case State.Descending:
                    if (MoveTo(lowWaypoints[waypoint].position))
                    {
                        state = State.Wandering;
                        ScheduleAttack();
                        PickWaypoint();
                    }
                    break;

                case State.Wandering:
                    if (Time.time >= nextAttack && SelectTarget())
                    {
                        SoundEffectPool.Instance.Play(_attackDecisionSound);
                        state = State.Approaching;
                        break;
                    }

                    if (MoveTo(lowWaypoints[waypoint].position))
                        PickWaypoint();

                    break;

                case State.Approaching:
                    {
                        if (target == null || !target.IsAvailable)
                        {
                            flightPathReady = false;
                            ResumeWandering();
                            break;
                        }

                        Vector3 attackForward =
                            target.ImpactPosition - target.AttackPosition;

                        attackForward.y = 0f;

                        if (attackForward.sqrMagnitude < 0.0001f)
                            attackForward = transform.forward;

                        if (MoveTo(target.AttackPosition, attackForward.normalized))
                            BeginAttack();

                        break;
                    }

                case State.Attacking:
                    TickAttack();
                    break;
            }
        }

        private void LateUpdate()
        {
            if (!ready || target == null ||
                (state != State.Approaching &&
                 state != State.Attacking))
                return;

            Vector3 direction =
                target.ImpactPosition - breathAim.position;

            if (direction.sqrMagnitude > 0.0001f)
                breathAim.rotation = Quaternion.LookRotation(direction);

            breath.SetFloat("BreathSpeed", direction.magnitude / travelSeconds);

            breath.SetFloat("BreathLifetime", travelSeconds);
        }

        private bool MoveTo(Vector3 destination, Vector3? arrivalForward = null)
        {
            bool stopAtDestination = arrivalForward.HasValue;

            if (!flightPathReady ||
                (destination - flightDestination).sqrMagnitude > 0.01f)
            {
                BuildFlightPath(destination, arrivalForward);
            }

            if (flightLength < 0.001f)
            {
                flightPathReady = false;
                return true;
            }

            float remaining = Mathf.Max(0f, flightLength - flightDistance);
            float wantedSpeed = moveSpeed;

            if (stopAtDestination)
            {
                wantedSpeed = Mathf.Min(
                    moveSpeed,
                    Mathf.Sqrt(2f * acceleration * remaining));
            }

            currentMoveSpeed = Mathf.MoveTowards(
                currentMoveSpeed,
                wantedSpeed,
                acceleration * Time.deltaTime);

            flightDistance = Mathf.Min(
                flightLength,
                flightDistance + currentMoveSpeed * Time.deltaTime);

            if (stopAtDestination &&
                flightLength - flightDistance <= 0.02f)
            {
                flightDistance = flightLength;
            }

            ApplyFlightPose(flightDistance);

            if (flightDistance < flightLength)
                return false;

            flightPathReady = false;

            if (stopAtDestination)
                currentMoveSpeed = 0f;

            return true;
        }

        private void BuildFlightPath(
            Vector3 destination,
            Vector3? arrivalForward)
        {
            flightPositions.Clear();
            flightRotations.Clear();
            flightDistances.Clear();

            flightDestination = destination;
            flightDistance = 0f;
            flightLength = 0f;

            Vector3 start = transform.position;
            Vector3 offset = destination - start;
            Vector3 initialDirection = transform.forward.normalized;

            Vector3 finalDirection = arrivalForward ?? offset;

            if (finalDirection.sqrMagnitude < 0.0001f)
                finalDirection = initialDirection;

            finalDirection.Normalize();

            flightPositions.Add(start);
            flightRotations.Add(transform.rotation);
            flightDistances.Add(0f);

            if (offset.sqrMagnitude < 0.0001f)
            {
                flightPathReady = true;
                return;
            }

            float facing = Vector3.Dot(
                initialDirection, offset.normalized);

            if (facing < -0.25f)
            {
                float side = Vector3.Dot(transform.right, offset) >= 0f
                    ? 1f
                    : -1f;

                float handle = GetCurveHandle(offset.magnitude);

                Vector3 middle =
                    start +
                    initialDirection * handle +
                    transform.right * side * handle * 2f;

                Vector3 middleDirection = destination - middle;

                if (middleDirection.sqrMagnitude < 0.0001f)
                    middleDirection = initialDirection;

                middleDirection.Normalize();

                AppendFlightCurve(
                    start, middle,
                    initialDirection, middleDirection);

                AppendFlightCurve(
                    middle, destination,
                    middleDirection, finalDirection);
            }
            else
            {
                AppendFlightCurve(
                    start, destination,
                    initialDirection, finalDirection);
            }

            flightPathReady = true;
        }

        private float GetCurveHandle(float distance)
        {
            return Mathf.Clamp(
                distance * curveStrength,
                minimumCurveHandle,
                Mathf.Max(minimumCurveHandle, maximumCurveHandle));
        }

        private void AppendFlightCurve(
            Vector3 start,
            Vector3 end,
            Vector3 startDirection,
            Vector3 endDirection)
        {
            float handle = GetCurveHandle(
                Vector3.Distance(start, end));

            Vector3 controlA = start + startDirection * handle;
            Vector3 controlB = end - endDirection * handle;

            const int samples = 64;

            for (int i = 1; i <= samples; i++)
            {
                float t = i / (float)samples;
                float u = 1f - t;

                Vector3 position =
                    u * u * u * start +
                    3f * u * u * t * controlA +
                    3f * u * t * t * controlB +
                    t * t * t * end;

                Vector3 tangent =
                    3f * u * u * (controlA - start) +
                    6f * u * t * (controlB - controlA) +
                    3f * t * t * (end - controlB);

                Vector3 previous =
                    flightPositions[flightPositions.Count - 1];

                float segmentLength = Vector3.Distance(previous, position);

                if (segmentLength < 0.00001f)
                    continue;

                Quaternion rotation =
                    flightRotations[flightRotations.Count - 1];

                if (tangent.sqrMagnitude > 0.0001f)
                    rotation = Quaternion.LookRotation(tangent, Vector3.up);

                flightLength += segmentLength;

                flightPositions.Add(position);
                flightRotations.Add(rotation);
                flightDistances.Add(flightLength);
            }
        }

        private void ApplyFlightPose(float distance)
        {
            int last = flightPositions.Count - 1;

            if (distance >= flightLength)
            {
                transform.SetPositionAndRotation(
                    flightPositions[last],
                    flightRotations[last]);

                return;
            }

            for (int i = 1; i < flightDistances.Count; i++)
            {
                if (flightDistances[i] < distance)
                    continue;

                float t = Mathf.InverseLerp(
                    flightDistances[i - 1],
                    flightDistances[i],
                    distance);

                transform.SetPositionAndRotation(
                    Vector3.Lerp(
                        flightPositions[i - 1],
                        flightPositions[i],
                        t),
                    Quaternion.Slerp(
                        flightRotations[i - 1],
                        flightRotations[i],
                        t));

                return;
            }
        }

        private void PickWaypoint()
        {
            if (lowWaypoints.Length == 1)
            {
                waypoint = 0;
                return;
            }

            int next;
            do
            {
                next = Random.Range(0, lowWaypoints.Length);
            }
            while (next == waypoint);

            waypoint = next;
        }

        private bool SelectTarget()
        {
            candidates.Clear();

            foreach (DragonLavaZone zone in zones)
            {
                if (zone != null && zone.IsAvailable)
                    candidates.Add(zone);
            }

            if (candidates.Count > 1)
                candidates.Remove(lastTarget);

            if (candidates.Count == 0)
            {
                nextAttack = Time.time + 1f;
                return false;
            }

            target = candidates[Random.Range(0, candidates.Count)];
            return true;
        }

        private void BeginAttack()
        {
            state = State.Attacking;
            attackElapsed = 0f;
            emissionElapsed = 0f;
            fireStarted = false;
            fireStopped = false;
            zoneIgnited = false;

            animator.CrossFadeInFixedTime(Breath, 0.15f, 0, 0f);
        }

        private void TickAttack()
        {
            attackElapsed += Time.deltaTime;

            if (target == null || !target.isActiveAndEnabled ||
                attackElapsed >= attackTimeout)
            {
                ResumeWandering();
                return;
            }

            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);

            if (info.fullPathHash != Breath || animator.IsInTransition(0))
                return;

            float progress = info.normalizedTime;

            if (!fireStarted &&
                progress >= fireStart && progress < fireEnd)
            {
                fireStarted = true;
                breath.Play();
                _fireSound = SoundEffectPool.Instance.Play(_fireLoopSound, loop: true);
            }

            if (fireStarted && !zoneIgnited)
            {
                emissionElapsed += Time.deltaTime;

                if (emissionElapsed >= travelSeconds)
                {
                    zoneIgnited = true;
                    target.Ignite();
                }
            }

            if (fireStarted && !fireStopped && progress >= fireEnd)
            {
                fireStopped = true;
                _fireSound.Stop();
                breath.Stop();
            }

            if (progress >= 1f)
            {
                lastTarget = target;
                ResumeWandering();
            }
        }

        private void ResumeWandering()
        {
            _fireSound.Stop();
            flightPathReady = false;
            breath.Stop();
            target = null;
            state = State.Wandering;
            PickWaypoint();
            ScheduleAttack();
            PlayFlight(false);
        }

        private void ScheduleAttack()
        {
            nextAttack = Time.time + RandomDuration(attackInterval);
        }

        private void TickFlightAnimation()
        {
            if (Time.time >= nextFlightAnimation)
                PlayFlight(!gliding);
        }

        private void PlayFlight(bool glide)
        {
            gliding = glide;

            animator.CrossFadeInFixedTime(
                glide ? Glide : Flap, 0.25f, 0, 0f);

            nextFlightAnimation = Time.time +
                RandomDuration(glide ? glideSeconds : flapSeconds);
        }

        private static float RandomDuration(Vector2 range)
        {
            float min = Mathf.Max(0.1f, Mathf.Min(range.x, range.y));
            float max = Mathf.Max(min, Mathf.Max(range.x, range.y));
            return Random.Range(min, max);
        }

        private void OnDisable()
        {
            _fireSound.Stop();
            if (breath != null)
                breath.Stop();

            if (highFlight != null)
                highFlight.enabled = false;

            if (zones != null)
            {
                foreach (DragonLavaZone zone in zones)
                {
                    if (zone != null)
                        zone.ResetZone();
                }
            }

            if (ready && animator != null)
                animator.speed = 0f;
        }
    }
}