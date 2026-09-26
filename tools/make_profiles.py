#!/usr/bin/env python3
# Onshape MX Creative - drive Onshape from a Logitech MX Creative Console
# Copyright (c) 2026 Michal Fanta. MIT License, see LICENSE.

"""Generate the default .lp5 profiles shipped with the plugin.

A .lp5 is a zip holding ApplicationInfo.json and ProfileInfo.json. Action names
follow "$<plugin>___<action class>___<parameter>"; open

    loupedeck://plugin/Onshape/xliff

against a loaded plugin to list them.

Unlike the Blender profiles, these carry one layout per plugin mode. The plugin
switches mode when Onshape's toolbar changes, so the keypad shows sketch tools
while a sketch is open and Part Studio features otherwise.

Writes OnshapePlugin/src/package/profiles/DefaultProfile70.lp5 (MX Creative
Keypad, 9 keys per page) and DefaultProfile71.lp5 (Dialpad, 4 buttons + 2 dials).
"""

import json
import pathlib
import plistlib
import uuid
import zipfile
from datetime import datetime, timezone

PLUGIN = "Onshape"
APPLICATION = "@_onshape"
NS = "Loupedeck.Service"

ROOT = pathlib.Path(__file__).resolve().parent.parent
PACKAGE = ROOT / "OnshapePlugin" / "src" / "package"
OUT = PACKAGE / "profiles"
# Options+ copies this into the application's folder the first time the profile is
# created, and never refreshes it afterwards - so it has to be right up front.
APPLICATION_ICON = PACKAGE / "metadata" / "Icon256x256.png"
CHROME_APPS = pathlib.Path.home() / "Applications" / "Chrome Apps.localized"


def guid():
    return uuid.uuid4().hex.upper()


def action(cls, parameter=None):
    name = f"${PLUGIN}___Loupedeck.OnshapePlugin.{cls}"
    return f"{name}___{parameter}" if parameter else name


# Shorthands for the page tables below.
def feature(p): return ("FeatureCommand", p)
def modify(p): return ("ModifyCommand", p)
def pattern(p): return ("PatternCommand", p)
def draw(p): return ("SketchDrawCommand", p)
def sketch_modify(p): return ("SketchModifyCommand", p)
def constraint(p): return ("SketchConstraintCommand", p)
def view(p): return ("ViewCommand", p)
def edit(p): return ("EditCommand", p)
def assembly(p): return ("AssemblyCommand", p)


# Keypad pages per mode, nine keys each, read left to right and top to bottom.
# "Main" is the Part Studio; the plugin names it "main" and matches it either way.
KEYPAD_MODES = {
    "Main": [
        ("Create", [
            feature("newSketch"), feature("extrude"), feature("revolve"),
            feature("sweep"), feature("loft"), feature("cPlane"),
            modify("fillet"), modify("chamfer"), modify("shell"),
        ]),
        ("Modify", [
            feature("hole"), modify("draft"), pattern("mirror"),
            pattern("linearPattern"), pattern("circularPattern"), modify("booleanBodies"),
            modify("splitPart"), pattern("transform"), modify("moveFace"),
        ]),
        ("View", [
            view("front"), view("top"), view("iso"),
            view("fit"), view("normal"), view("section"),
            edit("undo"), edit("accept"), edit("cancel"),
        ]),
    ],
    "Sketch": [
        ("Draw", [
            draw("LINESEGMENT"), draw("RECTANGLE_TWO_CORNERS"), draw("CIRCLE_CENTER_RADIUS"),
            draw("ARC_START_END_RADIUS"), draw("ARC_TANGENT"), draw("RECTANGLE_CENTER"),
            draw("TOGGLE_CONSTRUCTION"), sketch_modify("DIMENSION"), edit("accept"),
        ]),
        ("Modify", [
            sketch_modify("USE"), sketch_modify("OFFSET"), sketch_modify("TRIM"),
            sketch_modify("FILLET"), sketch_modify("SKETCHMIRROR"), sketch_modify("EXTEND"),
            sketch_modify("SKETCHLPATTERN"), sketch_modify("SKETCHCPATTERN"), edit("undo"),
        ]),
        ("Constrain", [
            constraint("COINCIDENT"), constraint("HORIZONTAL"), constraint("VERTICAL"),
            constraint("PARALLEL"), constraint("PERPENDICULAR"), constraint("TANGENT"),
            constraint("EQUAL"), constraint("MIDPOINT"), constraint("MIRROR"),
        ]),
        ("More", [
            draw("SLOT"), draw("INSCRIBED_POLYGON"), draw("SPLINE"),
            draw("POINT"), draw("TEXT_RECTANGLE_TWO_CORNERS"), draw("CIRCLE_PERIMETER"),
            constraint("FIX"), constraint("CONCENTRIC"), view("normal"),
        ]),
    ],
    "Assembly": [
        ("Mate", [
            assembly("insertPartOrAssembly"), assembly("fastened"), assembly("revolute"),
            assembly("slider"), assembly("planar"), assembly("cylindrical"),
            feature("mateConnector"), assembly("TOGGLE_ASSEMBLY_SNAP_MODE"), assembly("TOGGLE_SHOW_MATES_ON_SELECTION_MODE"),
        ]),
        ("More", [
            assembly("pin_slot"), assembly("ball"), assembly("parallel"),
            assembly("geometryMate"), assembly("widthMate"), assembly("mateGroup"),
            assembly("replicate"), assembly("assemblyLinearPattern"), assembly("assemblyMirror"),
        ]),
        ("View", [
            view("front"), view("top"), view("iso"),
            view("fit"), view("transparent"), view("section"),
            edit("undo"), edit("accept"), edit("cancel"),
        ]),
    ],
}

DIALPAD_BUTTONS = [edit("undo"), edit("redo"), edit("accept"), edit("cancel")]

DIALPAD_DIALS = [("OrbitAdjustment", None), ("ZoomAdjustment", None)]


def control(index, press=None, rotate=None):
    return {
        "$type": f"{NS}.Devices.Loupedeck7Devices.ProfileLayoutControl7, LoupedeckService",
        "controlId": index,
        "pressAction": press,
        "rotateAction": rotate,
    }


def page(display_name, controls):
    return {
        "$type": f"{NS}.Devices.Loupedeck7Devices.ProfileLayoutPage7, LoupedeckService",
        "name": guid(),
        "displayName": display_name,
        "description": None,
        "controls": controls,
    }


def layout_mode(device, mode, press_pages, rotate_pages):
    return {
        "$type": f"{NS}.Devices.Loupedeck7Devices.ProfileLayoutMode7, LoupedeckService",
        "deviceType": device,
        "modeName": mode,
        "parentModeName": None,
        "actions": None,
        "dynamicButtonPages": None,
        "dynamicEncoderPages": None,
        "workspaces": [{
            "$type": f"{NS}.Devices.Loupedeck7Devices.ProfileLayoutWorkspace7, LoupedeckService",
            "name": guid(),
            "displayName": "Workspace 1",
            "description": None,
            "pressPages": press_pages,
            "rotatePages": rotate_pages,
        }],
        "folderPages": [],
    }


def layout(device, modes):
    return {
        "$type": f"{NS}.Devices.Loupedeck7Devices.ProfileLayout7, LoupedeckService",
        "deviceType": device,
        "profileFlags": "None",
        "layoutModes": [layout_mode(device, mode, press, rotate) for mode, (press, rotate) in modes.items()],
        "folderPages": [],
    }


def onshape_bundle_id():
    """The bundle id Chrome gave the installed Onshape app on this machine.

    It is derived from the start URL, so it differs between installs; the
    plugin recognises the app by its URL either way. This only fills in the
    profile's informational processOrBundleName.
    """
    for app in sorted(CHROME_APPS.glob("*.app")) if CHROME_APPS.exists() else []:
        try:
            info = plistlib.loads((app / "Contents" / "Info.plist").read_bytes())
        except (OSError, plistlib.InvalidFileException):
            continue
        if "onshape.com" in info.get("CrAppModeShortcutURL", ""):
            return info["CFBundleIdentifier"]
    return "com.google.Chrome.app.onshape"


def profile(device, display_name, modes):
    name = guid()
    info = {
        "$type": f"{NS}.ApplicationProfile, LoupedeckService",
        "name": name,
        "profileFlags": "None",
        "displayName": display_name,
        "description": "",
        "deviceType": device,
        "applicationName": APPLICATION,
        "nativePluginName": PLUGIN,
        "hasNativePlugin": True,
        "additionalNativePluginNames": ["DefaultMac"],
        "lastModifiedTimeUtc": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
        "profileSettings": {
            "$type": "Loupedeck.DictionaryNoCase`1[[System.String, System.Private.CoreLib]], PluginApi"
        },
        "actionImages90": None,
        "actionImages60": None,
        "wheelImages": None,
        "actionColors": None,
        "layout": layout(device, modes),
        "macroCommands": [],
        "macroAdjustments": [],
        "profileCommands": [],
        "profileAdjustments": [],
        "conversionHistory": None,
        "profileActions": [],
    }
    application = {
        "$type": f"{NS}.SupportedApplicationInfo, LoupedeckService",
        "name": APPLICATION,
        "displayName": "Onshape",
        "description": None,
        "deviceType": device,
        "nativePluginName": PLUGIN,
        "hasNativePlugin": True,
        "processOrBundleName": onshape_bundle_id(),
        "modes": [{
            "$type": f"{NS}.ApplicationMode, LoupedeckService",
            "name": mode,
            "parentModeName": None,
            "displayName": "Part Studio" if mode == "Main" else mode,
        } for mode in modes],
        "defaultProfileName": name,
        "isEnabled": True,
    }
    return info, application


def write(path, info, application):
    path.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as zf:
        zf.writestr("ApplicationInfo.json", json.dumps(application, indent=2))
        zf.writestr("ProfileInfo.json", json.dumps(info, indent=2))
        zf.write(APPLICATION_ICON, "ApplicationIcon.png")
    print(f"wrote {path.name}")


def main():
    keypad = {
        mode: ([page(f"Page ({i + 1}) {title}",
                     [control(slot, press=action(cls, param)) for slot, (cls, param) in enumerate(items)])
                for i, (title, items) in enumerate(pages)], [])
        for mode, pages in KEYPAD_MODES.items()
    }
    write(OUT / "DefaultProfile70.lp5", *profile("Loupedeck70", "Onshape", keypad))

    # The Dialpad does the same in every mode: history, dialogs and the view.
    def dialpad():
        buttons = [page("Page (1)", [control(slot, press=action(cls, param))
                                     for slot, (cls, param) in enumerate(DIALPAD_BUTTONS)])]
        dials = [page("Dial Page", [control(slot, rotate=action(cls, param))
                                    for slot, (cls, param) in enumerate(DIALPAD_DIALS)])]
        return buttons, dials

    write(OUT / "DefaultProfile71.lp5",
          *profile("Loupedeck71", "Onshape", {mode: dialpad() for mode in KEYPAD_MODES}))


if __name__ == "__main__":
    main()
