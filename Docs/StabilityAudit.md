# Stability Audit

This audit covers the playable food-truck vertical slice, including the production state machine, customers, milestones, local progress, runtime interface, scene flow, and Windows player journey.

## Runtime guardrails

- Every loaf follows one explicit state machine. Input is accepted only while waiting for dough, oven loading, or counter placement; click spam during movement and baking is rejected.
- The manager calls the same action interface as the player. It cannot skip a station, overfill the counter, or start a batch that will not fit.
- Counter stock keeps one recipe identity until sold out, so selecting another card cannot relabel existing products.
- Customer arrivals are capped at two. Sales require both stock and a waiting customer, and each sale consumes exactly one of each.
- Invalid, infinite, negative, or abnormally large frame deltas are ignored or capped before they reach production timers.
- Save values are versioned, clamped, and validated. Missing, malformed, unavailable, or locked recipe selections safely fall back to Country Bread.
- Progress is saved after meaningful events and when leaving the scene. Smoke-test runs never contaminate the player's save.
- Scene changes go through one guarded asynchronous loader, so repeated clicks cannot enqueue duplicate loads.
- The intro uses unscaled time, accepts skip input, respects Reduce Motion, and has a hard escape path if an expected UI element is missing.
- Missing mouse, touch, camera, station, product, or UI references fail softly and leave a diagnostic rather than trapping the production loop.
- World clicks are raycast from the active camera and are discarded when an interactive UI control is under the pointer.
- Escape closes open Settings and Bakery Book overlays before any broader navigation is considered.

## Verified boundaries

| Area | Verification |
|---|---|
| Manual production | Three deliberate actions, busy-state spam rejection, bake completion, counter placement, and first sale. |
| Manager | Unlock at ten Country Bread sales, automatic use of the same loop, and counter-capacity compliance. |
| Progression | Kaiser threshold, exact second-oven price, exact wooden-bakery threshold and price, repeat-purchase rejection. |
| Recipes | Six unique products; locked selection fallback; no switching while fresh stock remains. |
| Persistence | Corrupt-value sanitisation and round-trip restoration of valid purchases and selected recipe. |
| Timing | NaN, infinity, negative values, and oversized frame stalls cannot corrupt state. |
| Input | Mouse/touch world hit, Space action, number-key recipe selection, B ledger, Escape close. |
| UI contracts | Required controls, twelve intro fragments, scan/shockwave elements, bubbles, warmth, upgrades, and six cards exist. |
| Player journey | Studio ident, Main Menu, Main Bakery, and the first complete loaf-and-sale path run in the built player. |

## Latest verified run — 2026-08-01

- EditMode tests: **23 passed, 0 failed, 0 skipped**.
- Windows x86_64 player: **Build Finished, Result: Success**.
- Runtime gameplay smoke: `GAMEPLAY_SMOKE_READY first manual loaf completed and sold.`
- Player log: no exceptions, failed scene loads, compiler warnings, or unsupported UI selectors.
- Runtime captures reviewed at `1600 × 900`: full ident, vial break, diagonal wipe, Main Menu, Settings, idle food truck, baker movement, oven phase, conversation bubble, and first sale.

## Remaining production boundaries

- The second oven currently expresses parallel capacity as a visible installed oven, 40% faster baking rhythm, and a larger counter. A true multi-oven scheduler is reserved for the next production-system pass.
- Saves resume at a safe station boundary rather than halfway through an animation or bake. This intentionally favours recoverability over sub-second continuity.
- Offline earnings are not enabled yet; clock migration and tamper handling must ship with that feature, not before it.
- Current characters and props are authored procedural low-poly geometry. Mesh optimisation, animation clips, audio, and target-hardware performance budgets remain later production work.
