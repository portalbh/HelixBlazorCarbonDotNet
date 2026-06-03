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

const themeCycleOrder = ['White', 'G10', 'G90', 'G100'];

function updateThemeCycleButton(activeName) {
  const cycleButton = document.querySelector('[data-cb-theme-cycle]');
  document.querySelectorAll('[data-cb-theme-icon]').forEach((icon) => {
    const isActive = icon.dataset.cbThemeIcon === activeName;
    icon.classList.toggle('cb-theme-toggle__icon--active', isActive);
  });

  if (cycleButton) {
    const label = themeLabels[activeName] ?? activeName;
    cycleButton.setAttribute('aria-label', `Theme: ${label}. Click to switch.`);
    cycleButton.setAttribute('title', `Theme: ${label}`);
  }
}

const themeLabels = {
  White: 'White',
  G10: 'Gray 10',
  G90: 'Gray 90',
  G100: 'Gray 100'
};

function getNextTheme(name) {
  const index = themeCycleOrder.indexOf(name);
  const nextIndex = index >= 0 ? (index + 1) % themeCycleOrder.length : 0;
  return themeCycleOrder[nextIndex];
}

function applyTheme(name) {
  const themeRoot = document.querySelector('.cb-theme');
  if (themeRoot && themeMap[name]) {
    themeRoot.dataset.theme = themeMap[name];
    document.documentElement.style.backgroundColor = getThemeBackground(name);
    document.body.style.backgroundColor = getThemeBackground(name);
  }
  setStoredTheme(name);
  updateThemeCycleButton(name);
}

function getThemeBackground(name) {
  switch (name) {
    case 'White': return '#ffffff';
    case 'G10': return '#f4f4f4';
    case 'G90': return '#262626';
    case 'G100': return '#161616';
    default: return '#161616';
  }
}

export function initAppShell() {
  const sideNav = document.getElementById('helix-side-nav');
  const content = document.getElementById('main-content');
  const menuBtn = document.querySelector('[data-cb-menu-toggle]');
  const themeRoot = document.querySelector('.cb-theme');
  const overlay = document.querySelector('[data-cb-nav-overlay]');
  const header = menuBtn?.closest('.cb-header');
  const themeCycleButton = document.querySelector('[data-cb-theme-cycle]');

  const storedRaw = getStoredTheme();
  const stored = storedRaw && themeMap[storedRaw] ? storedRaw : defaultTheme;
  if (themeRoot && themeMap[stored]) {
    themeRoot.dataset.theme = themeMap[stored];
    document.documentElement.style.backgroundColor = getThemeBackground(stored);
    document.body.style.backgroundColor = getThemeBackground(stored);
    updateThemeCycleButton(stored);
  }

  if (themeCycleButton && !themeCycleButton.dataset.cbShellBound) {
    themeCycleButton.dataset.cbShellBound = 'true';
    themeCycleButton.addEventListener('click', () => {
      const current = getStoredTheme() && themeMap[getStoredTheme()] ? getStoredTheme() : defaultTheme;
      applyTheme(getNextTheme(current));
    });
  }

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
