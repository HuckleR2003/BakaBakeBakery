# Stability Audit

This audit covers the playable vertical slice: production, customers, days, crafting, milestones, persistence, UI, scene flow, and the Windows player journey.

## Runtime guardrails

- Decoration never takes a click. The speech bubbles, toasts and drag ghost are drawn over the whole screen and sit after the top row in document order, so with the default picking mode they silently swallowed every click on the day sign, the bakery book and the guide — a save could open on its first morning with no reachable way to the market. Those layers are now transparent to the pointer, and the smoke run pick-tests each top-row control against the element that actually receives the click.
- A saved bakery is escapable. The main menu offers a new game beside continue, behind a two-click confirmation, so no player is trapped in a stuck save.
- Entering the morning market is never gated on coins. The basket costs nothing, so an empty cash tin cannot end a run at the tutorial sign. A new bakery opens with a ten-coin float.
- Every discovery bit survives a save. The persisted book covers all eight test-kitchen recipes, and the product dock lists all fourteen products once unlocked.
- Every batch follows one explicit state machine. Input is accepted only while waiting for ingredients, oven loading, or counter placement; click spam during movement and baking is rejected.
- The manager calls the same action interface as the player. It cannot skip a station, overfill the counter, or start a batch that will not fit.
- Counter stock keeps one recipe identity until sold out, so another card cannot relabel existing products.
- A completed batch receives a protected display interval before a sale can consume it.
- Raw ingredients, carried dough, raw oven contents, baked oven contents, carried bakes, and counter servings are mutually exclusive state-driven displays.
- Customer actors retain queue order and leave with a visible purchase. Arrivals are capped at two; a sale requires both stock and a waiting customer.
- A departing customer blocks the service point only while physically beside it; a later park walk and eating loop cannot stall the next sale.
- Jules's carried recipe is attached to a shared two-hand anchor. The Windows smoke run rejects an incomplete hand rig or missing park pedestrian and traffic actors.
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
| Living diorama | Five configured ambient actors, deep park path, traffic loop, customer snack route, two-hand carry anchor, and oven reach. |
| Player journey | Studio ident, Main Menu, Main Bakery, and the first complete loaf-and-sale path in the built player. |
| Reachable controls | The day sign, bakery book, guide and baker action are pick-tested in the built player: the element under each control's centre must be that control. |
| Fifteen-minute session | Simulated quarter-hour soak: no production silence beyond 45 s, three trading days, manager inside the first shift, second oven and wooden bakery both affordable, no counter overflow, no negative balance, no NaN timer, no backwards day phase. |
| Hostile input | The same quarter-hour replayed with click spam, mid-batch recipe switching and NaN / infinite / negative frame deltas injected throughout. |

## Long-session soak

`BakerySoakTests` replays the controller's exact update order — day cycle, production, milestone spending — for 900 simulated seconds at 60 Hz, four times over. It is the regression net for "can somebody actually sit with this for a quarter of an hour", and it fails on a stall rather than on a crash.

The shipped player carries the same journey unattended: `BakaBakeBakery.exe -bakaSoakMinutes 15` runs the real build through market runs, shifts and day rollovers, logging a heartbeat each minute and failing on a stall, an overflowing counter or a negative balance. Automated runs never write to the player's save.

## Reproducing the run

```text
Unity.exe -batchmode -nographics -projectPath <project> -runTests -testPlatform EditMode -testResults results.xml
Unity.exe -batchmode -projectPath <project> -executeMethod BakaBakeBakery.Editor.VisualFoundationBuilder.BuildAll -quit
Unity.exe -batchmode -projectPath <project> -executeMethod BakaBakeBakery.Editor.PlayerBuilder.BuildWindows -quit
Builds\Windows\BakaBakeBakery.exe -bakaSmokeTest
```

## Latest verified run — 2026-08-01

- EditMode tests: **51 passed, 0 failed, 0 skipped**.
- Visual foundation: **rebuilt successfully**, including the articulated finger rig, the global post-processing volume and the living district.
- Windows x86_64 player: **Build Finished, Result: Succeeded** (108.1 MB).
- Runtime smoke: `BUILD_SMOKE_READY` for StudioIntro, MainMenu and MainBakery, plus `GAMEPLAY_SMOKE_READY first manual loaf completed and sold`, exit code 0. It rejects missing ingredients, an incomplete natural-hand rig — which now includes the three-knuckle fingers and thumb on both hands — missing park actors, invisible oven contents, or a batch that never appears on the counter.
- The previous audit claimed 39 green tests. That figure predated a change which clamped the saved coin balance to a minimum of thirty; the real baseline before this pass was **37 passed, 3 failed**. The clamp has been reverted and the tests are green again.
- The unattended fifteen-minute run on the built player has **not** been executed in this pass; the quarter-hour coverage above is the simulated soak.

## Remaining production boundaries

- The second oven currently expresses parallel capacity as a visible installation, faster rhythm, and larger counter. A true multi-oven scheduler remains a later production pass.
- Saves resume at a safe production station rather than halfway through an animation. This favours recovery over sub-second continuity.
- Offline earnings are not enabled; clock migration and tamper handling must ship with that feature.
- Characters and food use layered authored geometry, face and garment details, scoring, glaze, chips, seams, flour, smoke, and local shadows. Bespoke production meshes, animation clips, audio, and target-hardware budgets remain later work.
