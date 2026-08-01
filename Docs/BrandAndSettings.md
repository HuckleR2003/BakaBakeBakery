# Brand and Settings Decisions

## Identity hierarchy

The game uses a three-level signature rather than repeating one oversized logo everywhere:

1. **HCK Labs** — studio name and the primary ident.
2. **Software & Games** — studio discipline, used only as a restrained descriptor.
3. **Marcin 'HCK' Firmuga** — creator credit, visible in the Main Menu signature and Settings panel.

The animated ident deliberately does not borrow Unity branding or a stock laboratory mark. Its test vial, white serum, break pattern, and sour-cherry diagonal wipe are built from native UI geometry and remain resolution-independent. A near-black laboratory field, restrained grid, scan line, experiment labels, and soft cherry reaction glow give the mark its own quiet technical atmosphere.

## Startup sequence

| Time | Beat |
|---:|---|
| 0.18–0.88 s | The two-line-height laboratory vial reveals from left to right. |
| 0.68–1.48 s | `HCK Labs` and `Software & Games` reveal toward the right. |
| 1.48–3.58 s | The complete signature holds for just over two seconds. |
| 3.58–4.42 s | The vial breaks; twelve fragments scatter, a reaction ring expands, and white serum spills. |
| 4.42–5.28 s | A sour-cherry backslash edge pulls a cream wipe into the Main Menu. |

Any key or primary click advances to the wipe. With Reduce Motion enabled, the explosion is replaced by a simple fade. A hard failsafe attempts to leave the intro after eight seconds.

## Player settings

- Windowed `1600 x 900` default with a resizable window.
- Borderless fullscreen toggle.
- Master volume from `0–100%`, defaulting to `80%`.
- Reduce Motion disables bakery camera sway and simplifies the studio ident.
- Preferences persist through namespaced `PlayerPrefs` keys.
- The native Unity splash is disabled; Unity 6 makes the Made with Unity splash optional, including on Personal.
- Player logging remains enabled for test diagnostics.
- The application identifier is `com.hcklabs.bakabakebakery`.

## Research basis

- Unity 6 splash settings: <https://docs.unity3d.com/6000.0/Documentation/Manual/class-PlayerSettingsSplashScreen.html>
- Unity 6 scene-list ordering: <https://docs.unity3d.com/6000.0/Documentation/Manual/build-profile-scene-list.html>
- Unity Personal splash policy: <https://unity.com/products/pricing-updates>
- Xbox Accessibility Guideline 102 — contrast: <https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/102>
- Xbox Accessibility Guideline 117 — motion: <https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/117>

The default palette places functional paper text against cocoa or evening-blue fields. Important standard-sized menu text targets at least `4.5:1` contrast; large text targets at least `3:1`. Locked states use both a written requirement and changed styling, never colour alone.
