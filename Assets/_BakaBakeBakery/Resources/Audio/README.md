# Bakery audio

Every clip here is synthesised by `Tools/generate_audio.py` in this repository. Nothing is
sampled, downloaded or licensed from a third party, which keeps the project's rule that only
owner-supplied or clearly licensed production assets ship.

To change or regenerate the set:

```bash
python Tools/generate_audio.py
```

The generator is seeded, so a rerun reproduces the same clips byte for byte.

| Clip | Where it plays |
|---|---|
| `ui_tap` | any button in the HUD or the menu |
| `knead` | Jules starting a batch, and an ingredient landing in the equation |
| `oven_door` | the oven being loaded |
| `bake_ready` | a batch reaching the counter |
| `shop_bell` | a neighbour arriving |
| `coin` | a sale |
| `discovery` | a recipe, a milestone, a purchase, a golden minute |
| `day_bell` | the shutter opening and closing, and the wooden bakery |
| `room_tone` | looping under everything, quieter while the bakery rests |
| `music_home` | the music box loop |

## Direction

Domestic, not cinematic. Wooden taps rather than clicks, a small brass bell rather than a chime,
a music box rather than a score. Everything is low-passed so no edge is brittle, one-shots carry a
small pitch wobble so repetition never turns mechanical, and the two looping beds are cross-faded
at the seam so they do not tick.
