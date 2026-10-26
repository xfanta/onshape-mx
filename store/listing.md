# Chrome Web Store listing

What goes into the Developer Dashboard for *MX Console Bridge for Onshape*. Images and
the package are generated: `python3 tools/make_store_assets.py` and
`tools/make_dist.sh` (which writes `store/extension-webstore.zip`).

## Package

Upload `store/extension-webstore.zip` (manifest.json at the root of the zip).

## Store listing

**Description**

```
Drive Onshape from a Logitech MX Creative Console.

This extension is the link between Onshape in Chrome and the Onshape plugin for Logi Options+. With both installed, the console's keys press Onshape's own toolbar buttons — every tool, including the many Onshape has no keyboard shortcut for: Chamfer, Shell, Loft, patterns, surfaces, sheet metal.

The keypad follows what is on screen:
• open a sketch and it switches to sketch tools — line, rectangle, circle, dimension, constraints;
• open a feature and its keys become that dialog's options — direction, Solid / Surface / Thin, New / Add / Remove / Intersect, end type, Symmetric — with the Dialpad dial setting the depth or radius;
• open an Assembly and it offers the mates;
• the key of the running tool lights up.

Keys show Onshape's own icons, read from the page you are signed in to.

Requires the Onshape plugin for Logi Options+ (macOS) — installation and source code at github.com/xfanta/onshape-mx. Works best with Onshape installed as a Chrome app (Chrome menu › Cast, save and share › Install page as app), so the console switches to Onshape only when it is in front.

Everything stays on your computer: the extension talks only to the plugin on the same machine, and collects nothing.

Open source (MIT). Not affiliated with PTC, Onshape or Logitech.
```

**Category:** Productivity › Tools (or Workflow & Planning)
**Language:** English

**Graphic assets**

* Store icon: `store/icon-128.png` (96×96 artwork with a 16 px transparent margin)
* Screenshot: `store/screenshot-1.png` (1280×800)
* Small promo tile: `store/promo-small.png` (440×280)

**Additional fields**

* Homepage URL: `https://github.com/xfanta/onshape-mx`
* Support URL: `https://github.com/xfanta/onshape-mx/issues`

## Privacy practices

**Single purpose**

```
Connects Onshape in Chrome to the Onshape plugin for the Logitech MX Creative Console, so the console's keys can press Onshape's toolbar and dialog buttons and show which tool is active.
```

**Permission justification**

* `alarms`
  ```
  Retries the connection to the Logi Options+ plugin every 30 seconds while Onshape is open and the plugin is not yet running (for example before Logi Options+ has started).
  ```
* Host permission `https://*.onshape.com/*`
  ```
  The extension works only inside Onshape: it reads which buttons Onshape's toolbar and feature dialogs show and presses them when a console key is pressed. Company domains such as yourcompany.onshape.com are covered as well.
  ```
* Host permission `ws://127.0.0.1/*`
  ```
  The Onshape plugin for Logi Options+ runs on the same computer and listens on a local WebSocket (127.0.0.1:47810). The extension connects to it to receive key presses and report the toolbar state. Nothing is sent anywhere else.
  ```

**Remote code:** No, I am not using remote code.

**Data usage:** tick none of the data types — nothing is collected or transmitted off
the user's computer. Then tick all three certifications (not sold, not used for
unrelated purposes, not used for creditworthiness).

**Privacy policy URL:** `https://github.com/xfanta/onshape-mx/blob/main/PRIVACY.md`

## Distribution

* Visibility: Public
* Regions: all
* Pricing: free

## Note for reviewers (Test instructions tab, if asked)

```
The extension needs the companion Onshape plugin for Logi Options+ (macOS) and a Logitech MX Creative Console to do anything visible; without them it is idle and only retries a local connection. Source code: https://github.com/xfanta/onshape-mx (extension/ folder). It runs only on onshape.com pages and talks only to 127.0.0.1.
```
