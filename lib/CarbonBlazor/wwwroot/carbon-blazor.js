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

export function getStoredTheme() {
  return localStorage.getItem('cb-theme');
}

export function setStoredTheme(theme) {
  localStorage.setItem('cb-theme', theme);
}

const themeMap = {
  White: 'white',
  G10: 'g10',
  G90: 'g90',
  G100: 'g100',
  Teal: 'teal'
};

const defaultTheme = 'G100';

function updateThemeButtons(activeName) {
  document.querySelectorAll('[data-cb-theme-value]').forEach((button) => {
    const isActive = button.dataset.cbThemeValue === activeName;
    button.classList.toggle('cb-theme-toggle__btn--active', isActive);
    button.setAttribute('aria-pressed', isActive ? 'true' : 'false');
  });
}

function applyTheme(name) {
  const themeRoot = document.querySelector('.cb-theme');
  if (themeRoot && themeMap[name]) {
    themeRoot.dataset.theme = themeMap[name];
  }
  setStoredTheme(name);
  updateThemeButtons(name);
}

export function initAppShell() {
  const sideNav = document.getElementById('helix-side-nav');
  const content = document.getElementById('main-content');
  const menuBtn = document.querySelector('[data-cb-menu-toggle]');
  const themeRoot = document.querySelector('.cb-theme');
  const overlay = document.querySelector('[data-cb-nav-overlay]');
  const header = menuBtn?.closest('.cb-header');
  const themeButtons = document.querySelectorAll('[data-cb-theme-value]');

  const storedRaw = getStoredTheme();
  const stored = storedRaw && themeMap[storedRaw] ? storedRaw : defaultTheme;
  if (themeRoot && themeMap[stored]) {
    themeRoot.dataset.theme = themeMap[stored];
    updateThemeButtons(stored);
  }

  themeButtons.forEach((button) => {
    if (button.dataset.cbShellBound) {
      return;
    }

    button.dataset.cbShellBound = 'true';
    button.addEventListener('click', () => {
      applyTheme(button.dataset.cbThemeValue);
    });
  });

  if (menuBtn && !menuBtn.dataset.cbShellBound) {
    menuBtn.dataset.cbShellBound = 'true';
    menuBtn.addEventListener('click', () => {
      const isDesktop = window.matchMedia('(min-width: 1056px)').matches;
      if (isDesktop) {
        sideNav?.classList.toggle('cb-side-nav--collapsed');
        content?.classList.toggle('cb-content--side-nav-collapsed');
      } else {
        const open = sideNav?.classList.toggle('cb-side-nav--open') ?? false;
        menuBtn.setAttribute('aria-expanded', open ? 'true' : 'false');
        header?.classList.toggle('cb-header--nav-open', open);
        overlay?.classList.toggle('cb-side-nav__overlay--visible', open);
      }
    });
  }

  if (overlay && !overlay.dataset.cbShellBound) {
    overlay.dataset.cbShellBound = 'true';
    overlay.addEventListener('click', () => {
      sideNav?.classList.remove('cb-side-nav--open');
      menuBtn?.setAttribute('aria-expanded', 'false');
      header?.classList.remove('cb-header--nav-open');
      overlay.classList.remove('cb-side-nav__overlay--visible');
    });
  }
}

function getFocusable(root) {
  if (!root) return [];
  return [...root.querySelectorAll('a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])')]
    .filter((element) => !element.hasAttribute('disabled') && !element.getAttribute('aria-hidden'));
}
