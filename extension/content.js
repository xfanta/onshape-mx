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
        case "dialog":
          result = dialog(message.action);
          break;
        case "param":
          result = await param(message);
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

  // A few buttons share a command-id - every assembly mate is "mate" - and only
  // their icon tells them apart, so a tool is named "mate#revolute" when needed.
  const iconOf = (el) => (el.querySelector("use")?.getAttribute("xlink:href") ?? "").replace(/^#svg-icon-/, "");

  const keysOf = (el) => {
    const id = el.getAttribute("command-id");
    const icon = iconOf(el);
    return icon ? [id, `${id}#${icon}`] : [id];
  };

  const matches = (el, key) => {
    const [id, icon] = key.split("#");
    return el.getAttribute("command-id") === id && (icon == null || iconOf(el) === icon);
  };

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
  const openDialog = () => [...document.querySelectorAll(".feature-dialog")].find(visible);

  const hasDialog = () => openDialog() != null;

  function readState() {
    const tools = toolbarTools();
    const ids = new Set(tools.map((t) => t.getAttribute("command-id")));
    const idsWhere = (cls) => tools.filter((t) => t.classList.contains(cls)).flatMap(keysOf);

    const context = contextOf(ids);
    return {
      context,
      active: idsWhere("is-active"),
      disabled: idsWhere("disabled"),
      dialog: hasDialog(),
      // A sketch is a dialog too, but it has a mode of its own.
      form: context === "sketch" ? null : readForm(),
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
        keysOf(item).forEach((k) => dropdownOf.set(k, face));
      }

      const hit = items.find((item) => matches(item, id));
      if (hit) {
        return { el: hit, arrow };
      }

      press(arrow);
      await sleep(30);
    }

    return {};
  }

  async function run(id) {
    let el = toolbarTools().find((t) => matches(t, id));
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

  const ESCAPE = { key: "Escape", code: "Escape", keyCode: 27 };

  // The dialog's own buttons rather than Enter and Escape: they work without the
  // page having keyboard focus, and OK stays refused while it is greyed out.
  function dialog(action) {
    const panel = openDialog();

    if (action === "ok") {
      const ok = panel?.querySelector(".ns-dialog-button-ok");
      if (!ok || !visible(ok)) {
        return { ok: false, error: "no dialog open" };
      }
      if (ok.classList.contains("disabled")) {
        return { ok: false, error: "the dialog cannot be accepted yet" };
      }
      press(ok);
      return { ok: true };
    }

    // Cancel steps back one level, like Escape: out of a running tool first. In
    // a sketch it never goes further - the sketch's red cross throws the whole
    // sketch away, which is not what a Cancel key should do.
    const toolRunning = toolbarTools().some((t) => t.classList.contains("is-active"));
    const inSketch = contextOf(new Set(toolbarTools().map((t) => t.getAttribute("command-id")))) === "sketch";
    const cancel = panel?.querySelector(".ns-dialog-button-cancel");
    if (!toolRunning && !inSketch && cancel && visible(cancel)) {
      press(cancel);
      return { ok: true };
    }

    return key(ESCAPE);
  }

  // ---------------------------------------------------------------- dialog parameters

  // A feature dialog is a list of parameters, each an .os-parameter-list-item
  // with data-parameter-id ("bodyType", "oppositeDirection", "depth") and a
  // child whose data-parameter-type says what it is. Four kinds are worth a key:
  //
  //   tabs    an enum shown as a row of options (Solid | Surface | Thin)
  //   select  an enum shown as a dropdown (Blind, Up to next, ...)
  //   flip    a boolean shown as an icon button (the direction arrow)
  //   check   a boolean shown as a checkbox (Symmetric, Draft, ...)
  //   number  a quantity (Depth: 25 mm) - for a dial rather than a key
  //
  // Selections, strings and the like are left to the mouse.

  const parameterItems = () => {
    const panel = openDialog();
    return panel ? [...panel.querySelectorAll(".os-parameter-list-item[data-parameter-id]")].filter(visible) : [];
  };

  const findParameter = (id) => parameterItems().find((item) => item.getAttribute("data-parameter-id") === id);

  const textOf = (el) => (el?.textContent ?? "").trim().replace(/\s+/g, " ");

  function describeParameter(item) {
    const id = item.getAttribute("data-parameter-id");
    const type = item.querySelector("[data-parameter-type]")?.getAttribute("data-parameter-type") ?? "";

    if (type === "os-enum-parameter") {
      const options = [...item.querySelectorAll("[data-enum-param-value]")];
      if (options.length > 0) {
        const active = options.find((o) => o.classList.contains("os-active"));
        return { id, kind: "tabs", label: textOf(active), value: active?.getAttribute("data-enum-param-value") ?? "" };
      }
      const current = item.querySelector(".os-select-match-text");
      if (current) {
        return { id, kind: "select", label: textOf(current), value: textOf(current) };
      }
      return null;
    }

    if (type === "os-boolean-parameter") {
      const button = item.querySelector(".os-param-icon-button");
      if (button) {
        return { id, kind: "flip", label: "", value: button.getAttribute("icon") ?? "", icon: button.getAttribute("icon") ?? "" };
      }
      const box = item.querySelector("input[type=checkbox]");
      if (box) {
        return { id, kind: "check", label: textOf(item.querySelector(".os-param-checkbox-label")), value: box.checked };
      }
      return null;
    }

    if (type === "os-quantity-parameter") {
      const input = item.querySelector("input.os-param-number");
      if (input) {
        return { id, kind: "number", label: textOf(item.querySelector(".os-param-label, label")), value: input.value };
      }
    }

    return null;
  }

  function readForm() {
    const panel = openDialog();
    if (!panel) {
      return null;
    }
    return {
      title: textOf(panel.querySelector(".ns-dialog-title")),
      params: parameterItems().map(describeParameter).filter((p) => p != null),
    };
  }

  const NUMBER = /^\s*(-?\d+(?:\.\d+)?)\s*([^\d\s].*)?$/;

  // Steps a quantity by `delta` detents. A detent is one unit of whatever the
  // field shows - 1 mm, 1 deg - or a tenth of an inch. Expressions ("#width * 2")
  // are left alone.
  //
  // The value is committed the way leaving the field commits it: input, change,
  // blur. Never Enter - in a feature dialog, Enter accepts the whole feature.
  function stepNumber(item, delta) {
    const input = item.querySelector("input.os-param-number");
    const match = input && NUMBER.exec(input.value);
    if (!match) {
      return { ok: false, error: "not a plain number" };
    }

    const unit = (match[2] ?? "").trim();
    const step = /^(in|")$/.test(unit) ? 0.1 : 1;
    // toFixed then Number: 0.1 + 0.2 shows as 0.3, not 0.30000000000000004.
    const next = Number((parseFloat(match[1]) + delta * step).toFixed(4));
    const text = unit ? `${next} ${unit}` : `${next}`;

    const setValue = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, "value").set;
    input.focus();
    setValue.call(input, text);
    input.dispatchEvent(new Event("input", { bubbles: true }));
    input.dispatchEvent(new Event("change", { bubbles: true }));
    input.blur();
    return { ok: true, value: text };
  }

  async function nextSelectOption(item) {
    const toggle = item.querySelector(".os-select-toggle");
    if (!toggle) {
      return { ok: false, error: "no dropdown" };
    }
    press(toggle);

    let rows = [];
    for (let i = 0; i < 10 && rows.length === 0; i++) {
      await sleep(30);
      rows = [...document.querySelectorAll(".os-select-choices-row")].filter(visible);
    }
    if (rows.length === 0) {
      return { ok: false, error: "dropdown did not open" };
    }

    const current = rows.findIndex((r) => r.classList.contains("active"));
    press(rows[(current + 1) % rows.length]);
    return { ok: true };
  }

  async function param({ id, op, delta }) {
    const item = findParameter(id);
    const info = item && describeParameter(item);
    if (!info) {
      return { ok: false, error: `no parameter '${id}' in the open dialog` };
    }

    switch (info.kind) {
      case "tabs": {
        const options = [...item.querySelectorAll("[data-enum-param-value]")].filter(visible);
        const current = options.findIndex((o) => o.classList.contains("os-active"));
        const step = op === "prev" ? -1 : 1;
        press(options[(current + step + options.length) % options.length]);
        return { ok: true };
      }
      case "select":
        return nextSelectOption(item);
      case "flip":
        press(item.querySelector(".os-param-icon-button"));
        return { ok: true };
      case "check":
        press(item.querySelector("label") ?? item.querySelector("input[type=checkbox]"));
        return { ok: true };
      case "number":
        return op === "step" ? stepNumber(item, delta ?? 0) : { ok: false, error: "numbers are stepped" };
    }
    return { ok: false, error: `cannot operate '${id}'` };
  }

  // Every tool the current toolbar offers, dropdowns included - for building the
  // plugin's catalog. Opens each dropdown in turn.
  async function discover() {
    const tools = [];
    const seen = new Set();
    const describe = (el, dropdown) => {
      const id = el.getAttribute("command-id");
      if (seen.has(`${id}#${iconOf(el)}`)) {
        return;
      }
      seen.add(`${id}#${iconOf(el)}`);
      tools.push({
        id,
        label: el.querySelector(".tool-label")?.textContent.trim() ?? "",
        title: el.getAttribute("data-bs-original-title") ?? "",
        icon: iconOf(el),
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
