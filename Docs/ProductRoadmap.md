# Product Roadmap

The initial progression contains six products. Each one changes timing, yield, presentation, or the station chain so that an unlock creates a new decision.

Numbers below are balance targets, not code constants. They will live in recipe data assets.

## Recipe set

| Product | Unlock | Bake | Yield | Sale price | Production role |
|---|---:|---:|---:|---:|---|
| Country Bread | Start | 4.0 s | 1 | 6 | Manual tutorial and baseline |
| Basic Kaiser Roll | 30 bread sold | 6.0 s | 3 | 3 each | First batch recipe |
| Butter Croissant | 45 total sales | 8.0 s | 2 | 8 each | Slow, high-value oven use |
| Cinnamon Swirl | 75 total sales and bakery level 2 | 7.0 s + 2.0 s finish | 3 | 7 each | First multi-station recipe |
| Finezja | 100 total sales and bakery level 2 | 2.0 s prep + 9.0 s bake + 3.0 s finish | 2 | 11 each | First two-cream finishing pattern |
| Cinnamon Monocle | 125 total sales and bakery level 2 | 1.0 s prep + 8.0 s bake + 1.0 s cinnamon finish | 3 | 9 each | Crisp laminated spiral and faster premium finish |

## Milestones

| Condition | Reward | Presentation |
|---|---|---|
| First interaction | Manual action prompt | Small world-space paper label |
| 10 Country Breads produced | Manager | Stamped manager card and visible arrival |
| 30 Country Breads sold | Kaiser Roll + oven slot | Recipe reveal and covered oven bay opens |
| 45 total sales | Butter Croissant | Golden recipe card and customer request preview |
| 60 total sales | Wooden bakery purchase | Building card with before/after silhouette |
| 75 total sales in bakery level 2 | Cinnamon Swirl + finishing bench | New station is installed in-world |
| 100 total sales in bakery level 2 | Finezja | Vanilla and strawberry cream piping appears at the bench |
| 125 total sales in bakery level 2 | Cinnamon Monocle | A new laminated tray and cinnamon-dusting motion appear |

## Manual bread tutorial

The first ten breads require four successful commands:

1. Fetch dough from the refrigerator.
2. Load the oven.
3. Collect the baked loaf after the timer completes.
4. Place the loaf on the counter.

Clicks made while the baker is moving are ignored with a small visual acknowledgement. The prompt always identifies the next valid action.

## Automation scheduler

Once unlocked, the manager advances the same command interface used by the player. Priority order:

1. Remove finished products from ovens.
2. Complete required finishing steps.
3. Stock the customer counter.
4. Load empty ovens according to their assigned recipes.
5. Wait when visible storage is full.

This keeps manual and automatic production behaviourally identical.

## Counter inventory

- Food truck capacity: 8 items.
- Wooden bakery capacity: 16 items.
- Products remain visibly grouped by type.
- Production pauses at capacity.
- Customers reserve available stock when joining the purchase step, preventing double sales.

## Economy targets

- Second oven target price: 120 coins.
- Wooden bakery target price: 220 coins.
- Finishing bench is included with the Cinnamon Swirl milestone in the slice.
- No ingredient costs during the first session.
- No customer penalty before the manager unlock.

The first balance pass should make the second oven affordable close to its unlock and the wooden bakery affordable shortly after 60 sales without mandatory waiting.

## Data requirements

Every recipe needs:

- stable identifier;
- display name and icon/render reference;
- unlock condition;
- preparation, bake, and finish durations;
- batch yield and sale value;
- required stations;
- inventory footprint;
- baked prop and tray presentation;
- customer dialogue pool;
- audio and effect cues.
