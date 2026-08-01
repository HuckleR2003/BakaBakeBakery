# Stability Audit

This audit covers the playable vertical slice: production, customers, days, crafting, milestones, persistence, UI, scene flow, and the Windows player journey.

## Runtime guardrails

- Every batch follows one explicit state machine. Input is accepted only while waiting for ingredients, oven loading, or counter placement; click spam during movement and baking is rejected.
- The manager calls the same action interface as the player. It cannot skip a station, overfill the counter, or start a batch that will not fit.
- Counter stock keeps one recipe identity until sold out, so another card cannot relabel existing products.
- A completed batch receives a protected display interval before a sale can consume it.
- Raw ingredients, carried dough, raw oven contents, baked oven contents, carried bakes, and counter servings are mutually exclusive state-driven displays.
- Customer actors retain queue order and leave with a visible purchase. Arrivals are capped at two; a sale requires both stock and a waiting customer.
- Invalid, infinite, negative, or abnormally large frame deltas are ignored or capped before reaching production and day timers.
- Day transitions are one-way: preparation, travel, ready, open, summary. Repeated input cannot skip or rewind a phase.
- Early close waits for the active batch to reach its resting station. A timed close cancels unfinished movement and clears the customer queue, so tomorrow never resumes yesterday's gesture.
- Open-day and market timers are checkpointed every few seconds. Reloaded values are clamped to their legal phase and duration.
- Crafting reserves only inventory that exists, accepts two to four ingredients, ignores order, and consumes stock only after an exact formula succeeds.
- The first tutorial basket is free. Later market trips cannot start outside preparation and require the exact basket price.
- Save values are versioned, clamped, and validated. Invalid or locked recipe selections fall back to Country Bread.
- Smoke-test and visual-capture runs never contaminate the player's save.
- Scene changes use one guarded asynchronous loader, so repeated clicks cannot enqueue duplicate loads.
- World clicks are raycast from the active camera and discarded while interactive UI is under the pointer.

## Verified boundaries

| Area | Verification |
|---|---|
| Manual production | Three deliberate actions, busy-state spam rejection, bake completion, counter placement, and first sale. |
| Physical production | Empty counter, raw ingredients, raw carry, oven contents, baked carry, stocked counter, and purchased parcel. |
| Manager | Unlock at ten Country Bread sales, automatic use of the same loop, and counter-capacity compliance. |
| Progression | Kaiser threshold, exact second-oven price, exact wooden-bakery threshold and price, repeat-purchase rejection. |
| Recipes | Nine unique products, discovery persistence, locked fallback, and no switching while stock remains. |
| Crafting | Order-independent matching, stock rejection, two-to-four-slot boundary, and no loss on a failed experiment. |
| Days | Three-second trip, exact morning cost, five-minute shift, negative opening profit, revenue, early close, and next morning. |
| Persistence | Sanitisation and round-trip restoration of purchases, selection, discoveries, pantry, tutorial, and day state. |
| UI contracts | Intro fragments, navigation, four crafting slots, pantry, market map, friend bubble, upgrades, and nine dynamic product slots. |
| Player journey | Studio ident, Main Menu, Main Bakery, and the first complete loaf-and-sale path in the built player. |

## Latest verified run — 2026-08-01

- EditMode tests: **32 passed, 0 failed, 0 skipped**.
- Windows x86_64 player: **Build Finished, Result: Success**.
- Runtime smoke expects `GAMEPLAY_SMOKE_READY first manual loaf completed and sold`; it also rejects missing ingredients, invisible oven contents, or a batch that never appears on the counter.
- The visual-foundation frame was regenerated. Existing runtime captures remain regression baselines; the current player journey is verified by the full smoke run.

## Remaining production boundaries

- The second oven currently expresses parallel capacity as a visible installation, faster rhythm, and larger counter. A true multi-oven scheduler remains a later production pass.
- Saves resume at a safe production station rather than halfway through an animation. This favours recovery over sub-second continuity.
- Offline earnings are not enabled; clock migration and tamper handling must ship with that feature.
- Characters and food use layered authored geometry, face and garment details, scoring, glaze, chips, seams, flour, smoke, and local shadows. Bespoke production meshes, animation clips, audio, and target-hardware budgets remain later work.
