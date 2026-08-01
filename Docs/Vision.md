# Game Vision

## The promise

**Baka Bake Bakery is a calm idle clicker where a tiny bakery becomes a stage.** The player begins by directing one baker action by action, then gradually turns that routine into a charming, observable machine.

The emotional arc is small but clear:

1. *I made this loaf.*
2. *I taught this bakery to run.*
3. *I can see everything I changed.*

## Design pillars

### A living miniature

The bakery is presented as a compact 2.5D/3D diorama. The camera keeps the production chain readable while subtle cursor-driven movement reveals depth around the set. The scene should feel composed, but never frozen.

### Work worth watching

Automation does not remove the game; it changes the player's role. Walking, loading, baking, finishing, stocking, and selling remain physically visible after the manager takes control.

### Tactile progression

An upgrade changes the set whenever possible. A purchased oven occupies real space. A new product appears on trays and in customer bubbles. The bakery upgrade replaces the truck with a larger wooden structure and an illuminated sign.

### Gentle management

The player makes understandable choices about recipes, capacity, and timing. Early play avoids punishment, aggressive monetisation, and opaque optimisation.

### Human-scale charm

Customers speak in short, warm lines. Characters have small imperfections and pauses. The tone is sincere with a quiet streak of silliness, never sugary or loud.

## First-session arc

The target first session is roughly 12–18 minutes:

- **0–2 min:** learn the four manual bread actions;
- **2–4 min:** produce loaf ten and unlock the manager;
- **4–8 min:** watch automation, build stock, and reach 30 bread sales;
- **8–12 min:** unlock Kaiser Rolls and consider the second oven;
- **12–18 min:** unlock Croissants and approach the wooden bakery upgrade.

The Cinnamon Swirl follows shortly after the building transformation and proves the expanded production chain.

## Core loop

```text
Choose recipe
  -> prepare dough
  -> load oven
  -> wait and observe
  -> collect or finish product
  -> stock counter
  -> fulfil customer order
  -> earn coins
  -> make a visible upgrade
```

## First playable slice

- One food-truck diorama and one wooden-bakery upgrade state.
- One baker and one manager.
- Two ovens and one finishing counter.
- Four products with distinct silhouettes and production roles.
- Small customer queue with readable speech bubbles.
- Manual tutorial, automation, upgrades, save data, and offline progress.
- Mouse and keyboard input at 16:9 and 16:10 desktop resolutions.

## Explicit non-goals for the slice

- Multiple districts or a world map.
- Prestige/reset systems.
- Advertising, in-app purchases, or live-service hooks.
- Character customisation.
- Complex ingredient supply chains.
- Multiplayer, accounts, cloud saves, or a backend.
- Mobile UI and touch camera controls.

These are not rejected forever; they are excluded until the core bakery is delightful on its own.

## Success criteria

The slice succeeds when a new viewer can understand the production chain without explanation, identify every product at gameplay scale, and feel an immediate visual difference after each milestone.
