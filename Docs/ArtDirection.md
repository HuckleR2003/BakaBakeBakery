# Art Direction

## Visual thesis: bakery theatre diorama

The open side of the food truck acts like a theatre proscenium. Stations form a readable left-to-right rhythm, the baker crosses that stage, and customers briefly occupy the foreground. The world is fully 3D, but composition takes priority over free camera movement.

The target is **softly sculpted, hand-finished 3D**: clean silhouettes, broad planes, restrained texture, rounded wear, and food detailed one step above the environment.

## Shape language

- Environment: softened rectangles, broad arches, sturdy proportions.
- Characters: compact bodies, slightly oversized hands and heads, clear limb poses.
- Food: bold silhouette, exaggerated scoring and layers, approximately 10–15% oversized at gameplay scale.
- Interface: paper cards, enamel labels, stitched tabs, wood or painted metal supports.
- Corners: consistent soft radius; avoid unrelated capsule shapes.

Objects should look designed for this bakery, not assembled from unrelated asset packs.

## Camera and composition

- Perspective camera with a restrained field of view.
- Three-quarter view into the open side of the truck.
- Production stations remain visible in the default frame.
- The lower screen edge is reserved for recipe cards.
- Cursor response uses a central dead zone and a maximum yaw of roughly two degrees.
- Left/right edges reveal side depth; the top edge gently reveals the roof and rear set dressing.
- Camera movement can be reduced or disabled in accessibility settings.

The back and sides of the set must be art-directed because the camera reveals them during edge movement.

## Palette

| Role | Colour | Use |
|---|---|---|
| Flour cream | `#F4E5C6` | paper, trim, flour, UI field |
| Bread crust | `#C8753D` | products, warm wood accents |
| Cocoa | `#382824` | text, deep wood, outlines by value |
| Sage | `#71816B` | truck panels, cloth, calm secondary areas |
| Sour cherry | `#A84D46` | calls to action, apron detail, milestone accent |
| Oven glow | `#FFB45D` | emissive heat, rewards, focused highlights |
| Evening blue | `#526777` | distant environment and shadow separation |

The palette is a hierarchy, not a requirement to use every colour on every object.

## Materials

- Painted metal has broad, soft highlights and limited edge wear.
- Wood shows large grain direction without noisy photographic texture.
- Cloth uses simple folds and a matte response.
- Bread uses warmer subsurface-like colour variation, crisp scoring, and small toasted gradients.
- Glass is used sparingly; reflections must not obscure products.
- Flour, steam, crumbs, and glaze are accent effects rather than constant screen noise.

## Lighting

- Warm oven and interior practical lights are the focal source.
- A cooler environmental fill separates the truck silhouette.
- Shadows are soft but grounded under feet, trays, and equipment.
- The default visual target is late afternoon moving toward early evening.
- The wooden bakery milestone introduces the neon sign as a new warm focal point.

Bloom must never reduce text or product readability.

## Food presentation

Each product owns a distinct read:

- Country Bread: round mass, deep cross score, dark lower crust.
- Kaiser Roll: small five-segment crown, displayed in a cluster.
- Butter Croissant: wide crescent, visible laminated ridges, glossy tips.
- Cinnamon Swirl: top-facing spiral, pale glaze ribbon, compact height.
- Finezja: low soft base with alternating white vanilla and strawberry cream ribbons.
- Cinnamon Monocle: tight top-facing laminated spiral, crisp edge, dark cinnamon coil, and light sugar dust.

Products use authored tray arrangements. Numeric inventory can exceed the visible pieces, but the visible set changes at deliberate thresholds.

## Character motion

Animation follows three beats:

1. anticipation;
2. readable main action;
3. short settle.

The baker should not snap between tasks. Pickups align hands to consistent prop anchors. The manager's automation is shown through calm clipboard checks, pointing, or a small approval gesture rather than literal floating clicks.

## Interface grammar

### Recipe rail

Recipe cards sit in a wooden rail along the bottom. A selected card rises slightly, sharpens its product render, and gains a warm edge light. Locked cards show an identifiable silhouette and milestone, not a generic padlock wall.

### Upgrade ledger

Upgrades open in a compact bakery ledger from the right. Cloth tabs divide `Recipes`, `Staff`, and `Bakery`. Page changes are brief and physical; they do not cover the working baker for long.

### World prompts

Speech bubbles and action prompts use paper-label shapes with cocoa text. Customer product icons carry more information than sentences. Milestone cards resemble newly stamped recipes.

### Typography shortlist

- Display candidate: Fraunces.
- UI candidates: Atkinson Hyperlegible or Nunito Sans.
- Final choice requires Polish glyph coverage, TextMeshPro atlas tests, and legibility at target scale.

## Quality gates

An asset is not production-ready until it passes:

- silhouette recognition at 25% screen scale;
- grayscale hierarchy check;
- material and palette consistency check;
- gameplay-camera check, not only a close-up render;
- clean interaction anchor and collider check;
- licence/source record when externally sourced.

Every visual milestone ends with one 1920×1080 hero frame. If the frame is not convincing, the language is refined before more assets are produced.

## Avoid

- generic purple/blue mobile-game gradients;
- unrelated icon styles;
- excessive bloom, particles, or bounce;
- tiny decorative text;
- photoreal textures on simplified geometry;
- perfect, sterile surfaces everywhere;
- final assets copied directly from generated concept imagery.
