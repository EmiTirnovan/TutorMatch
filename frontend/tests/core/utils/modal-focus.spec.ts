import { keepTabFocusInside } from '../../../src/app/core/utils/modal-focus';

describe('keepTabFocusInside', () => {
  let container: HTMLElement;
  let first: HTMLButtonElement;
  let last: HTMLButtonElement;

  const tab = (shiftKey = false): KeyboardEvent =>
    new KeyboardEvent('keydown', { key: 'Tab', shiftKey, cancelable: true });

  beforeEach(() => {
    container = document.createElement('div');
    container.tabIndex = -1;
    container.innerHTML = '<button>First</button><button disabled>Skipped</button><button>Last</button>';
    document.body.appendChild(container);
    [first, , last] = Array.from(container.querySelectorAll('button'));
  });

  afterEach(() => container.remove());

  it('Given_FocusOnLast_When_PressingTab_Then_MovesToFirst', () => {
    last.focus();
    const event = tab();

    keepTabFocusInside(event, container);

    expect(event.defaultPrevented).toBe(true);
    expect(document.activeElement).toBe(first);
  });

  it('Given_FocusOnFirst_When_PressingShiftTab_Then_MovesToLast', () => {
    first.focus();
    const event = tab(true);

    keepTabFocusInside(event, container);

    expect(event.defaultPrevented).toBe(true);
    expect(document.activeElement).toBe(last);
  });

  it('Given_FocusOnContainer_When_PressingShiftTab_Then_MovesToLast', () => {
    container.focus();

    keepTabFocusInside(tab(true), container);

    expect(document.activeElement).toBe(last);
  });

  it('Given_FocusInTheMiddle_When_PressingTab_Then_LetsBrowserMoveFocus', () => {
    first.focus();
    const event = tab();

    keepTabFocusInside(event, container);

    expect(event.defaultPrevented).toBe(false);
  });

  it('Given_OtherKey_When_Pressed_Then_IsIgnored', () => {
    last.focus();
    const event = new KeyboardEvent('keydown', { key: 'Enter', cancelable: true });

    keepTabFocusInside(event, container);

    expect(event.defaultPrevented).toBe(false);
  });
});
