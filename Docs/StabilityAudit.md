# Stability Audit

This audit covers the pre-gameplay Visual Foundation build. It does not claim that the future production scheduler, customer queue, economy, or save migration is tested before those systems exist.

## Guardrails now in the project

- Scene changes go through one guarded asynchronous loader; repeated clicks cannot enqueue duplicate loads.
- The shipping scene order is explicit: `StudioIntro`, `MainMenu`, `MainBakery`.
- The intro uses unscaled time, accepts skip input, respects Reduce Motion, and has an eight-second escape path.
- Missing intro UI falls back to Main Menu instead of holding the player on a blank screen.
- Settings values are clamped, namespaced, persisted, and applied before the first scene.
- Missing mouse input or invalid screen dimensions centre the camera instead of throwing.
- Camera smoothing clamps invalid serialized timing values.
- Oven glow clamps amplitude and frequency, preventing negative or invalid light behaviour.
- Locked recipe cards ignore selection input.
- Escape closes open Settings and Bakery Ledger overlays.
- Recipe catalog lookup safely handles a missing list and null recipe entries.
- A command-line smoke journey visits all three shipping scenes and exits only after `MainBakery` is ready.

## Test matrix

| Area | Verification |
|---|---|
| Recipe data | Unique IDs, non-null assets, positive duration and revenue, six products. |
| Camera | Dead zone, no downward tilt, symmetric edges, top-corner response. |
| Settings | Volume clamping, Reduce Motion persistence contract, stable scene names. |
| UI contracts | Required buttons, toggles, animated elements, and six recipe cards exist. |
| Build configuration | Studio intro is build index 0; all shipping scenes are enabled. |
| Player journey | Headless smoke run logs Intro, Main Menu, and Main Bakery readiness. |

Final test counts and build results are recorded after each verified build rather than predicted in this document.

## Latest verified run — 2026-08-01

- EditMode tests: **15 passed, 0 failed, 0 skipped**.
- Windows x86_64 player: **Build Finished, Result: Success**.
- Runtime smoke journey: `StudioIntro`, `MainMenu`, and `MainBakery` each reported ready in order.
- Player log: no exceptions, missing references, failed scene loads, or assertion errors.
- Visual captures reviewed at `1280 x 720`: complete ident, broken-vial beat, Main Menu, and Settings panel.
- Source hygiene: no generated `Assets/Resources` test artifacts remained after the build.

## Known boundaries before the next slice

- The actual click-to-bake state machine is the next milestone and therefore cannot yet deadlock.
- Save versioning and offline progression are not implemented; they must receive migration and clock-tamper tests when introduced.
- Customer reservation and counter capacity rules are design contracts only until the gameplay scheduler exists.
- Visual Foundation props are deterministic graybox geometry, not final optimized meshes; a later performance budget will measure draw calls, batches, and memory on target hardware.
