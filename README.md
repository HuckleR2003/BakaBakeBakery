# Baka Bake Bakery

**A tiny bakery automation game staged inside a living 3D diorama.**

Start with a food truck, bake the first loaves by hand, train a manager, expand the menu, and grow into a glowing wooden bakery. Every upgrade should be visible in the world, not only in a number.

## Current status

The project contains a playable `0.7.0` vertical slice. The first ten Country Bread sales use a deliberate physical rhythm: pantry, preparation board, oven, counter, then customer parcel. Mila arrives on the first morning with a clickable, event-driven guide and later automates the same guarded command loop.

The current build includes:

- customers, a two-person queue, visible counter inventory, sales, and local progress;
- nine playable products: six neighbourhood milestones plus Chocolate Muffins, Village Jam Turnovers, and Chocolate Pillows discovered in the test kitchen;
- manager, second-oven, and wooden-bakery milestones;
- a five-minute day structure: morning market run, opening sign, live profit board, early close, shutter animation, and next-morning reset;
- Mila's four-slot discovery workbench with draggable flour, milk, puff pastry, jam, and chocolate;
- a focused product journey that shows only available bakes and one meaningful locked target at a time;
- a state-driven physical production story: ingredients, raw batch, oven contents, carried bake, counter stock, and customer parcel;
- Jules's articulated shoulder–elbow–forearm–hand rig, including a two-handed tray grip, oven-handle reach, loading gesture, and visible carried recipe;
- a neighbourhood Warmth meter and temporary double-income Golden Minutes;
- walking customers and translucent conversations for Jules, Mrs. Rose, a returning neighbour, and Mila;
- a park path running into the diorama, sideways benches, swaying trees, looping walkers, post-purchase snacks, a distant road with traffic, lit house windows and chimney smoke;
- a three-second illustrated road to market, food-truck bicycle, bakery delivery car, flour motes, oven steam, and a closing service shutter;
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
- Click an available card in Home to select a product.
- Use the wooden day sign to visit the market, open the shift, close early, or begin the next morning.
- Open Craft, then drag two to four pantry ingredients into the equation. Clicking an ingredient is an accessible shortcut to the next empty slot.
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
- [Days and recipe discovery](Docs/DaysAndDiscovery.md)
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
