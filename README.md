# Baka Bake Bakery

**A tiny bakery automation game staged inside a living 3D diorama.**

Start with a food truck, bake the first loaves by hand, train a manager, expand the menu, and grow into a glowing wooden bakery. The game is designed around a simple pleasure: every upgrade should be visible in the world, not only in a number.

## Current status

The project is in its **Visual Foundation** phase. The current build includes the HCK Labs studio ident, Main Menu, persistent comfort settings, six recipe definitions, camera language, interface grammar, and a representative food-truck scene.

The first playable slice will contain:

- a four-action manual bread tutorial;
- automated production after the tenth loaf;
- customers, counter inventory, and visible sales;
- Country Bread, Kaiser Rolls, Butter Croissants, Cinnamon Swirls, Finezja, and Cinnamon Monocles;
- a second oven and a finishing station;
- the food-truck-to-bakery transformation;
- local save data and capped offline progress.

## Project setup

- Unity `6000.4.11f1`
- Universal Render Pipeline `17.4.0`
- Input System `1.19.0`
- Primary target: Windows desktop
- Secondary target: WebGL

Open the repository root in Unity Hub, load `Assets/_BakaBakeBakery/Scenes/StudioIntro.unity`, and press Play. The shipping flow is `StudioIntro -> MainMenu -> MainBakery`.

## Direction

- [Game vision](Docs/Vision.md)
- [Art direction](Docs/ArtDirection.md)
- [Product roadmap](Docs/ProductRoadmap.md)
- [Reference study](Docs/References.md)
- [Brand and settings decisions](Docs/BrandAndSettings.md)
- [Stability audit](Docs/StabilityAudit.md)
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
