import { DOCUMENT } from '@angular/common';
import { DestroyRef, inject } from '@angular/core';

const FOCUSABLE =
  'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

// A role="dialog" div does not make the page inert like <dialog> does, so Tab must be kept inside by hand.
export function keepTabFocusInside(event: KeyboardEvent, container: HTMLElement): void {
  if (event.key !== 'Tab') {
    return;
  }

  const focusable = Array.from(container.querySelectorAll<HTMLElement>(FOCUSABLE));
  const first = focusable[0];
  const last = focusable[focusable.length - 1];
  const active = container.ownerDocument.activeElement;

  if (!first) {
    event.preventDefault();
  } else if (event.shiftKey && (active === first || active === container)) {
    event.preventDefault();
    last.focus();
  } else if (!event.shiftKey && active === last) {
    event.preventDefault();
    first.focus();
  }
}

/** Call from a field initializer: returns focus to the element that opened the modal once it is destroyed. */
export function restoreFocusOnDestroy(): void {
  const opener = inject(DOCUMENT).activeElement as HTMLElement | null;
  inject(DestroyRef).onDestroy(() => opener?.focus());
}
