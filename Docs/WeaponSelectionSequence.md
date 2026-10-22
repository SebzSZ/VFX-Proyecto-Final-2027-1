# Weapon selection sequence

Behavior reference captured before extraction on 2026-10-20 from the working
SCN_Dev_Combat scene, including its existing local changes.

## Existing behavior to preserve

1. Start with a black screen, selection camera priority 100 and gameplay camera
   priority 10. Hide selection UI, gameplay HUD and encounter actors.
2. Lock the player sequence, disable gameplay and grant sequence invulnerability.
   Teleport to StartPos with CharacterController temporarily disabled. Reset the
   camera rig, invalidate both Cinemachine camera histories and hide the body.
3. Activate both weapon displays at zero visibility. Aim between their focus
   points and wait for the selection camera to become active without a blend.
4. Fade from black over 1.5 seconds, then materialize both weapons over 2.5
   seconds. Select the left weapon and aim toward it over 0.5 seconds.
5. Show selection UI and enable selection input. Require neutral navigation and
   released confirmation before accepting input, including after focus loss or
   pause. Navigation thresholds remain 0.25 / 0.6. Mouse selection uses 30 pixels
   of accumulated horizontal travel, resetting on direction change or a gap
   longer than 0.2 seconds. Camera tracking precedes navigation each frame.
6. Fade the selected name over 0.2 seconds in LateUpdate. Camera tracking uses
   sharpness 12. Keyboard, mouse and gamepad bindings remain unchanged.
7. Confirm by equipping immediately and hiding the equipped weapon. Disable
   selection input. Disappear both displays over 2.5 seconds using their existing
   selected/unselected material behavior; finish the name fade before hiding UI
   and displays. Wait another 0.3 seconds.
8. Set gameplay camera priority to 200. Reveal body and equipped weapon once the
   output camera is at least 0.7 units from the head, or when the blend finishes.
   Camera waits retain the existing 15-second timeout.
9. Remove sequence invulnerability, show HUD, release the sequence lock, enable
   gameplay, then activate encounter actors.
10. On death, stop the player's actions. The sequence locks the player, hides HUD
    and encounter actors, fades to black over 0.8 seconds, restores timeScale to
    1 and reloads the same scene. Weapon selection starts again.

Presentation timing uses unscaled time as before. The extraction intentionally
does not change the existing failure/re-enable policy or pause behavior.

## Ownership after extraction

- WeaponSelectionFlow: the only global sequence state, player placement and
  locks, equipment choice, encounter activation, screen fade and scene reload.
- WeaponSelectionInput: private input action instances, navigation, confirmation,
  focus/rearm and mouse accumulation. Ticked by the flow to preserve frame order.
- WeaponSelectionPresentation: two paired display/name entries, weapon effects
  and label fades. WeaponDisplay keeps its existing material implementation.
- WeaponSelectionCamera: camera poses, aim, priorities, blend completion and
  body visibility. Requests equipped-weapon visibility through an event.
- PlayerController: handles HealthComponent.Died locally, then publishes Died.
  It does not reload the scene. The sequence subscribes to this player event.

The three extracted components live on the existing WeaponSelectionFlow object.
Their serialized values are migrated from the original flow component. The
disabled EncounterRestart component and its script are removed.

## Verification cases

- Left / sword: enter gameplay with 120 health, 80 stamina and equipped sword.
- Right / scythe: enter gameplay with 80 health, 120 stamina and equipped scythe.
- For each choice: verify name, disappearance, camera blend, body/weapon reveal,
  HUD, player control and encounter activation.
- Take lethal damage: player actions stop, death fade runs once, scene reloads
  once and returns to left/default selection with both displays available.
- Check that initial held confirmation and focus loss do not bypass input rearm.

These are acceptance cases, not a claim that every case has been run.
