# Releasing on itch.io

Everything needed to put a playable build on an itch.io page, and the reasons behind each choice.

## What ships

`Baka Bake Bakery/Package for itch.io` builds the Windows player and writes a single archive to `Builds/Release/baka-bake-bakery-windows-<version>.zip`. The same run is available headless:

```text
Unity.exe -batchmode -projectPath <project> -executeMethod BakaBakeBakery.Editor.PlayerBuilder.PackageForItch -quit
```

The archive root holds the executable, its `_Data` folder, `UnityPlayer.dll`, the DirectStorage libraries and `.itch.toml`. Unity's output only runs when the executable and its data folder stay together, so the zip must not add a wrapping folder — the itch app looks for the executable at the top level. Burst debug symbols and `.pdb` files are left out; they are development artefacts and add tens of megabytes to a download.

## The app manifest

`.itch.toml` sits at the root of the archive and tells the itch desktop app what to launch, so players get a Play button rather than a folder of files:

```toml
[[actions]]
name = "play"
path = "BakaBakeBakery.exe"
```

A second action links to the issue tracker. Without a manifest the app has to guess the entry point, which is unreliable once a build contains more than one executable — and Unity ships `UnityCrashHandler64.exe` alongside the game, which is exactly the case that confuses it. That file is excluded from the archive for the same reason.

## Page setup

| Field | Value |
|---|---|
| Kind of project | Downloadable |
| Upload | the release zip, tagged **Windows** |
| Pricing | free, or "no payments" while testing |
| Genre / tags | simulation, idle, cosy, bakery, singleplayer |
| Screenshots | the frames in `Docs/Captures`, at 1600 × 900 |
| Cover image | 630 × 500 is the size itch renders in browse pages |

Mark the upload as **Windows** on the file itself; without that flag the itch app will not offer to install it.

## Publishing updates with butler

butler is itch's own command line uploader. It pushes a folder or a zip to a named channel and only transfers what changed, which makes patch updates small:

```bash
butler push Builds/Release/baka-bake-bakery-windows-0.8.0.zip huckler2003/baka-bake-bakery:windows --userversion 0.8.0
butler status huckler2003/baka-bake-bakery
```

The channel name carries the platform, so `windows` is enough for this project. `--userversion` should match `PlayerSettings.bundleVersion`, which `BuildConfigurationTests` pins.

## A browser build

itch.io gives browser games far more plays than downloads. `Baka Bake Bakery/Package for itch.io (Browser)` builds it and writes `Builds/Release/baka-bake-bakery-browser-<version>.zip`, applying the two settings that decide whether a Unity web build works on itch at all:

- **Compression Format:** Gzip. Brotli compresses better but needs server headers itch does not let you set.
- **Decompression Fallback:** enabled. This embeds a JavaScript decompressor in the build so the browser can unpack the data even when the host serves no `Content-Encoding` header. Without it the page loads to a blank canvas. Enabling it is what gives the payload files their `.unityweb` extension.

Exceptions are limited to explicitly thrown ones, which keeps the guarded save loading working without paying for full stack traces.

The zip has `index.html` at its root. On the itch page the upload must be marked **This file will be played in the browser**, with the viewport set to 1600 × 900 and fullscreen allowed, matching the desktop framing.

### Verified

The archive was served from a plain static server with no `Content-Encoding` header — the same condition itch.io provides — and the game booted: `index.html`, the loader, and the three `.unityweb` payloads all returned `200`, the client-side fallback unpacked them, a 1600 × 900 WebGL2 canvas came up, the loading bar cleared, the Unity engine and Input System initialised and the audio context resumed. No loader or template errors.

### Still open for the browser build

- Rendering was not visually confirmed. The verification browser did not composite frames, so the load path is proven but the picture is not.
- Software rendering reported three internal URP shaders as unsupported: `Hidden/CoreSRP/CoreCopy`, `Hidden/Universal Render Pipeline/StencilDitherMaskSeed` and `Hidden/Universal/HDRDebugView`. These usually resolve on real GPUs, but they must be checked on a normal machine before the page promises a browser build.
- Frame cost has not been measured. The post-processing volume, the two street-lamp point lights and the soft shadows are all more expensive on the web than on desktop.

## Before pressing publish

- [ ] `EditMode` tests green.
- [ ] Visual foundation rebuilt, so the scene matches the source.
- [ ] Windows player built and the runtime smoke run exits `0`.
- [ ] A real fifteen minute session, either by hand or `BakaBakeBakery.exe -bakaSoakMinutes 15`.
- [ ] A fresh machine check: no save present, **START BAKING** reaches the first sale.
- [ ] A returning player check: save present, **CONTINUE BAKING** resumes and **NEW BAKERY** clears it.
- [ ] `PlayerSettings.bundleVersion` bumped and matching the butler `--userversion`.

## Sources

- [App manifests · The itch.io app book](https://itch.io/docs/itch/integrating/manifest.html)
- [Manifest actions · The itch.io app book](https://docs.itch.ovh/itch/master/integrating/manifest-actions.html)
- [Windows builds · The itch.io app book](https://itch.io/docs/itch/integrating/platforms/windows.html)
- [Packaging Your Unity Game for itch.io](https://itch.io/t/3260941/packaging-your-unity-game-for-itchio-a-step-by-step-guide)
- [How to upload your game or project to itch.io with butler](https://itch.io/t/2872417/how-to-upload-your-game-or-project-to-itchio-with-butler)
- [Unity Manual — Deploy a Web application](https://docs.unity3d.com/Manual/webgl-deploying.html)
