// Onshape MX Creative - drive Onshape from a Logitech MX Creative Console
// Copyright (c) 2026 Michal Fanta. MIT License, see LICENSE.
//
// Content script: reads Onshape's toolbar and presses its buttons.
//
// Every toolbar button carries a command-id attribute ("extrude",
// "CIRCLE_CENTER_RADIUS"), the running tool has the class "is-active" and an
// unavailable one "disabled". Tools that sit in a dropdown are only in the DOM
// while the dropdown is open, so pressing one means opening its dropdown first.
// Which tools the toolbar holds says what is open: the sketch toolbar has the
// line tool, the Part Studio toolbar has "new sketch".

(() => {
  const RETRY_MS = 1000;
  const SETTLE_MS = 120;

  let port = null;
  let lastState = "";
  let settleTimer = null;
  const wantedIcons = new Set();

  // Where each dropdown-only tool was last seen: command-id -> command-id of the
  // dropdown's face button. Saves opening every dropdown in turn next time.
  const dropdownOf = new Map();

  // ---------------------------------------------------------------- link

  function connectPort() {
    // After the extension is reloaded, this copy of the script is orphaned.
    if (!chrome.runtime?.id) {
      return;
    }

    try {
      port = chrome.runtime.connect({ name: "onshape" });
    } catch {
      setTimeout(connectPort, RETRY_MS);
      return;
    }

    port.onMessage.addListener(onMessage);
    port.onDisconnect.addListener(() => {
      port = null;
      lastState = "";
      setTimeout(connectPort, RETRY_MS);
    });

    publish();
  }

  function post(message) {
    try {
      port?.postMessage(message);
    } catch {
      // Disconnected mid-flight; onDisconnect reconnects.
    }
  }

  async function onMessage(message) {
    let result;
    try {
      switch (message.type) {
        case "run":
          result = await run(message.command);
          break;
        case "key":
          result = key(message);
          break;
        case "getIcons":
          for (const name of message.names ?? []) {
            wantedIcons.add(name);
          }
          sendIcons();
          return;
        case "discover":
          result = { ok: true, catalog: await discover() };
          break;
        default:
          result = { ok: false, error: `unknown request '${message.type}'` };
      }
    } catch (error) {
      result = { ok: false, error: String(error) };
    }

    if (message.id != null) {
      post({ type: "result", id: message.id, ...result });
    }

    // Pressing a tool changes the toolbar; report it without waiting for the observer.
    schedulePublish();
  }

  // ---------------------------------------------------------------- state

  const visible = (el) => {
    const r = el.getBoundingClientRect();
    return r.width > 0 && r.height > 0;
  };

  const toolbar = () => document.querySelector(".os-element-toolbar");

  const toolbarTools = () => {
    const bar = toolbar();
    return bar ? [...bar.querySelectorAll("[command-id]")].filter(visible) : [];
  };

  function contextOf(ids) {
    if (ids.has("LINESEGMENT")) {
      return "sketch";
    }
    if (ids.has("newSketch")) {
      return "partStudio";
    }
    if ([...ids].some((id) => /mate/i.test(id) && id !== "mateConnector")) {
      return "assembly";
    }
    return ids.size > 0 ? "other" : "none";
  }

  // A feature or sketch dialog: the panel with the green check and the red cross.
  function hasDialog() {
    return [...document.querySelectorAll(".os-feature-dialog, .feature-dialog, [class*='feature-dialog']")]
      .some(visible);
  }

  function readState() {
    const tools = toolbarTools();
    const ids = new Set(tools.map((t) => t.getAttribute("command-id")));
    const idsWhere = (cls) => tools.filter((t) => t.classList.contains(cls)).map((t) => t.getAttribute("command-id"));

    return {
      context: contextOf(ids),
      active: idsWhere("is-active"),
      disabled: idsWhere("disabled"),
      dialog: hasDialog(),
      focused: document.hasFocus() && document.visibilityState === "visible",
      toolbar: [...ids],
    };
  }

  function publish() {
    settleTimer = null;
    const state = readState();
    const text = JSON.stringify(state);
    if (text !== lastState) {
      lastState = text;
      post({ type: "state", state });
    }

    if (wantedIcons.size > 0) {
      sendIcons();
    }
  }

  function schedulePublish() {
    if (settleTimer == null) {
      settleTimer = setTimeout(publish, SETTLE_MS);
    }
  }

  // ---------------------------------------------------------------- icons

  // Onshape keeps its icons in one SVG sprite, a <symbol> per icon. Some parts of
  // the sprite arrive late, so icons not found yet are retried on later changes.
  function sendIcons() {
    const icons = {};
    for (const name of wantedIcons) {
      const symbol = document.getElementById(`svg-icon-${name}`);
      if (symbol) {
        icons[name] = symbol.outerHTML;
        wantedIcons.delete(name);
      }
    }

    if (Object.keys(icons).length > 0) {
      post({ type: "icons", icons });
    }
  }

  // ---------------------------------------------------------------- pressing

  const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

  // Onshape listens for pointer and mouse events, not just "click".
  function press(el) {
    const r = el.getBoundingClientRect();
    const init = {
      bubbles: true,
      cancelable: true,
      composed: true,
      clientX: r.x + r.width / 2,
      clientY: r.y + r.height / 2,
      button: 0,
    };
    for (const type of ["pointerover", "mouseover", "pointerdown", "mousedown", "pointerup", "mouseup", "click"]) {
      el.dispatchEvent(type.startsWith("pointer") ? new PointerEvent(type, init) : new MouseEvent(type, init));
    }
  }

  const faceOf = (arrow) => arrow.parentElement?.querySelector("[command-id]")?.getAttribute("command-id");

  const openDropdownItems = () =>
    [...document.querySelectorAll(".os-tool-dropdown-content [command-id]")].filter(visible);

  async function waitForDropdown() {
    for (let i = 0; i < 10; i++) {
      const items = openDropdownItems();
      if (items.length > 0) {
        return items;
      }
      await sleep(30);
    }
    return [];
  }

  // Opens dropdowns until one holds the tool; leaves that one open.
  async function findInDropdowns(id) {
    const bar = toolbar();
    if (!bar) {
      return {};
    }

    const known = dropdownOf.get(id);
    const arrows = [...bar.querySelectorAll(".dropdown-arrow")]
      .filter(visible)
      .sort((a, b) => (faceOf(b) === known) - (faceOf(a) === known));

    for (const arrow of arrows) {
      press(arrow);
      const items = await waitForDropdown();
      const face = faceOf(arrow);
      for (const item of items) {
        dropdownOf.set(item.getAttribute("command-id"), face);
      }

      const hit = items.find((item) => item.getAttribute("command-id") === id);
      if (hit) {
        return { el: hit, arrow };
      }

      press(arrow);
      await sleep(30);
    }

    return {};
  }

  async function run(id) {
    let el = toolbarTools().find((t) => t.getAttribute("command-id") === id);
    let arrow = null;

    if (!el) {
      ({ el, arrow } = await findInDropdowns(id));
    }

    if (!el) {
      return { ok: false, error: `'${id}' is not on this toolbar` };
    }

    if (el.classList.contains("disabled")) {
      if (arrow) {
        press(arrow);
      }
      return { ok: false, error: `'${id}' is disabled` };
    }

    press(el);
    return { ok: true };
  }

  // Replays a keyboard shortcut inside the page, spelled as a US keyboard would
  // spell it, whatever the system layout. keyCode and which are read-only on a
  // constructed KeyboardEvent, hence defineProperty.
  function key(spec) {
    const target = document.activeElement && document.activeElement !== document.body
      ? document.activeElement
      : document.body;

    const init = {
      key: spec.key,
      code: spec.code,
      shiftKey: !!spec.shift,
      ctrlKey: !!spec.ctrl,
      altKey: !!spec.alt,
      metaKey: !!spec.meta,
      bubbles: true,
      cancelable: true,
      composed: true,
    };

    const types = spec.key.length === 1 && !spec.ctrl && !spec.meta
      ? ["keydown", "keypress", "keyup"]
      : ["keydown", "keyup"];

    for (const type of types) {
      const event = new KeyboardEvent(type, init);
      const code = type === "keypress" ? spec.key.charCodeAt(0) : spec.keyCode;
      Object.defineProperty(event, "keyCode", { get: () => code });
      Object.defineProperty(event, "which", { get: () => code });
      if (type === "keypress") {
        Object.defineProperty(event, "charCode", { get: () => code });
      }
      target.dispatchEvent(event);
    }

    return { ok: true };
  }

  // Every tool the current toolbar offers, dropdowns included - for building the
  // plugin's catalog. Opens each dropdown in turn.
  async function discover() {
    const tools = [];
    const seen = new Set();
    const describe = (el, dropdown) => {
      const id = el.getAttribute("command-id");
      if (seen.has(id)) {
        return;
      }
      seen.add(id);
      tools.push({
        id,
        label: el.querySelector(".tool-label")?.textContent.trim() ?? "",
        title: el.getAttribute("data-bs-original-title") ?? "",
        icon: (el.querySelector("use")?.getAttribute("xlink:href") ?? "").replace(/^#svg-icon-/, ""),
        dropdown,
      });
    };

    toolbarTools().forEach((el) => describe(el, null));

    const bar = toolbar();
    for (const arrow of bar ? [...bar.querySelectorAll(".dropdown-arrow")].filter(visible) : []) {
      press(arrow);
      const face = faceOf(arrow);
      (await waitForDropdown()).forEach((el) => describe(el, face));
      press(arrow);
      await sleep(30);
    }

    return { context: readState().context, tools };
  }

  // ---------------------------------------------------------------- start

  new MutationObserver(schedulePublish).observe(document.body, {
    subtree: true,
    childList: true,
    attributes: true,
    attributeFilter: ["class"],
  });

  window.addEventListener("focus", () => {
    post({ type: "focus" });
    schedulePublish();
  });
  window.addEventListener("blur", schedulePublish);
  document.addEventListener("visibilitychange", schedulePublish);

  connectPort();
})();
