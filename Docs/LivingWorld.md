# Living-World Rules

The bakery world must tell the truth about the simulation. A prop may not appear merely because it makes the counter look attractive, and a sale may not happen invisibly in the frame where a product arrives.

## Physical product journey

| Simulation phase | Visible world state |
|---|---|
| Waiting for dough | Recipe-specific flour, bowl, chilled ingredients, and raw batch wait on the preparation bench. The service counter is empty unless real inventory exists. |
| Gathering ingredients | Jules walks to the refrigerator, opens its hinged door, then crosses the whole truck to the preparation board. |
| Waiting for oven | Jules holds the selected raw product silhouette; the preparation ingredients have been consumed. |
| Loading | The oven door folds open and the raw batch transfers from Jules's hands to the oven. |
| Baking | A pale raw silhouette occupies the oven before changing into its browned product silhouette. The practical light wakes and steam rises from the chimney. |
| Ready | The baked product remains visible inside the oven until the player or manager collects it. |
| Serving | Jules carries the browned product to the service counter. There is no raw dough in his hands. |
| Stocked | Individual counter servings appear with a short placement animation and remain visible before sale eligibility. |
| Sold | One serving disappears; the front customer receives a paper parcel with a warm bake and walks out before the queue advances. |

## Motion language

- Locomotion uses continuous world-space movement; completing a task never teleports Jules back to idle.
- Legs and arms respond to actual movement rather than a permanently looping walk clip.
- Reaching, loading, watching the oven, carrying, and placing use different body tilts and arm gestures.
- Mrs. Rose and the neighbour have distinct entrances and exits. A customer already leaving temporarily owns the service space, preventing character overlap.
- Idle motion is deliberately small: breathing, the hanging service bell, oven light, and chimney steam support the work instead of competing with it.
- Hands are a rig, not a prop. Three knuckle-pivoted fingers and a thumb per hand open while reaching, close around whatever is being carried, and release as the load reaches the oven or the counter. The carried tray keeps the tilt the hands are actually holding and sags slightly while walking.

## The district around the work

The street is allowed a life the player never manages. None of it touches the simulation, and all of it is driven by name from `BakeryAmbientDistrict`:

- clouds drifting across the backdrop and birds gliding over the park with a flapping silhouette;
- leaves falling and tumbling around the park trees;
- bunting over the truck and laundry on a neighbour's line, both moving on their own breeze;
- a cat that has claimed the far bench, tail swinging;
- street lamps that carry light with a faint flicker, dim while the shutter is up and brightening once the shift closes;
- the key light cooling toward morning and warming into the closing summary, with the counter spot dropping while the bakery rests.

Every one of these respects the comfort setting: reduced motion scales them down instead of switching them off.

## Visual honesty checks

The Windows smoke journey verifies more than scene loading. It starts with an empty counter and visible mise en place, checks the raw carry, checks baked contents in the oven, checks a real serving on the counter, and only then accepts the first sale. Automated visual captures preserve the preparation, baking, ready, stocked, and purchased beats at `1600 × 900`.
