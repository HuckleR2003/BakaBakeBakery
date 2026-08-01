# Baka Bake Bakery

**A tiny bakery automation game staged inside a living 3D diorama.**

Start with a food truck, bake the first loaves by hand, train a manager, expand the menu, and grow into a glowing wooden bakery. The game is designed around a simple pleasure: every upgrade should be visible in the world, not only in a number.

## Current status

The project now contains a **playable vertical slice**. The first ten Country Bread sales are made through a deliberate three-step rhythm: fetch the dough, load the oven, and move the finished loaf to the counter. Mila then takes over the same safe command loop as manager.

The current build includes:

- customers, a two-person queue, visible counter inventory, sales, and local progress;
- Country Bread, Kaiser Rolls, Butter Croissants, Cinnamon Swirls, Finezja, and Cinnamon Monocles;
- manager, second-oven, and wooden-bakery milestones;
- a state-driven physical production story: ingredients, raw batch, oven contents, carried bake, counter stock, and customer parcel;
- a neighbourhood Warmth meter and temporary double-income Golden Minutes;
- walking customers, conversational bubbles for Jules, Mrs. Rose, and a returning neighbour;
- the animated black HCK Labs ident, Main Menu, comfort settings, and responsive bakery HUD.

## Project setup

- Unity `6000.4.11f1`
- Universal Render Pipeline `17.4.0`
- Input System `1.19.0`
- Primary target: Windows desktop
- Secondary target: WebGL

Open the repository root in Unity Hub, load `Assets/_BakaBakeBakery/Scenes/StudioIntro.unity`, and press Play. The shipping flow is `StudioIntro -> MainMenu -> MainBakery`.

## Controls

- Click Jules in the truck or press `Space` to perform the highlighted bakery action.
- Press `1`–`6` or click a recipe card to select an unlocked product.
- Press `B` to open the Bakery Book and buy available upgrades.
- Press `Escape` to close the open Bakery Book or Settings panel.
- Move the pointer toward the screen edges for the restrained diorama camera lean.

## Direction

- [Game vision](Docs/Vision.md)
- [Art direction](Docs/ArtDirection.md)
- [Product roadmap](Docs/ProductRoadmap.md)
- [Reference study](Docs/References.md)
- [Brand and settings decisions](Docs/BrandAndSettings.md)
- [Stability audit](Docs/StabilityAudit.md)
- [Living-world rules](Docs/LivingWorld.md)
- [Concept frame notes](Docs/Concepts/README.md)

## Repository layout

```text
Assets/_BakaBakeBakery/
  Art/          Authored game art and visual development
  Data/         ScriptableObject game data
  Editor/       Project setup and authoring tools
  Prefabs/      Runtime prefabs
  Scenes/       Shipping scenes
  Scripts/      Runtime source grouped by responsibility
  Tests/        Edit Mode and Play Mode tests
Docs/           Public design documentation
```

## Working principles

- The world is the primary interface.
- Products are identified by silhouette before labels.
- Automation must remain enjoyable to watch.
- Reference images guide decisions; they are never shipped as substitutes for authored assets.
- Changes stay small, reviewable, and reproducible in Unity.
