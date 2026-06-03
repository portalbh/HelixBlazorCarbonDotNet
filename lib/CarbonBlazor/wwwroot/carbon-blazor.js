export function focusFirst(selector) {
  const root = document.querySelector(selector);
  const first = getFocusable(root)[0];
  if (first) first.focus();
}

export function focusById(id) {
  const element = document.getElementById(id);
  if (element) element.focus();
}

export function setBodyScrollLock(locked) {
  document.body.classList.toggle('cb-scroll-lock', locked);
}

export function trapFocus(rootId, event) {
  if (event.key !== 'Tab') return;
  const root = document.getElementById(rootId);
  const focusable = getFocusable(root);
  if (!focusable.length) return;

  const first = focusable[0];
  const last = focusable[focusable.length - 1];
  if (event.shiftKey && document.activeElement === first) {
    event.preventDefault();
    last.focus();
  } else if (!event.shiftKey && document.activeElement === last) {
    event.preventDefault();
    first.focus();
  }
}

export function clickOutside(rootId, dotNetRef, methodName) {
  const root = document.getElementById(rootId);
  if (!root) return;

  const listen = () => {
    document.addEventListener('click', handler, { once: true, capture: true });
  };

  const handler = (event) => {
    if (root.contains(event.target)) {
      listen();
      return;
    }

    dotNetRef.invokeMethodAsync(methodName).catch(() => {});
  };

  window.setTimeout(listen, 0);
}

export function rove(rootId, nextIndex) {
  const root = document.getElementById(rootId);
  if (!root) return;
  const items = [...root.querySelectorAll('[data-roving-item]')];
  if (!items.length) return;
  items.forEach((item, index) => item.tabIndex = index === nextIndex ? 0 : -1);
  items[nextIndex]?.focus();
}

export function moveTreeFocus(rootId, delta) {
  const root = document.getElementById(rootId);
  if (!root) return;

  const items = [...root.querySelectorAll('[data-roving-item]')]
    .filter((item) => item.offsetParent !== null && !item.disabled);
  if (!items.length) return;

  const currentIndex = Math.max(0, items.indexOf(document.activeElement));
  const nextIndex = Math.min(items.length - 1, Math.max(0, currentIndex + delta));
  items.forEach((item, index) => item.tabIndex = index === nextIndex ? 0 : -1);
  items[nextIndex]?.focus();
}

export function matchesMedia(query) {
  return window.matchMedia(query).matches;
}

function getFocusable(root) {
  if (!root) return [];
  return [...root.querySelectorAll('a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])')]
    .filter((element) => !element.hasAttribute('disabled') && !element.getAttribute('aria-hidden'));
}
