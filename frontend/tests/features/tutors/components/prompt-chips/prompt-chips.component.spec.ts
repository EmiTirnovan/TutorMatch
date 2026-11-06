import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  DEFAULT_PROMPT_CHIPS,
  PromptChipsComponent
} from '../../../../../src/app/features/tutors/components/prompt-chips/prompt-chips.component';

describe('PromptChipsComponent', () => {
  let fixture: ComponentFixture<PromptChipsComponent>;

  const element = (): HTMLElement => fixture.nativeElement as HTMLElement;
  const chips = (): HTMLButtonElement[] => Array.from(element().querySelectorAll<HTMLButtonElement>('.chip'));

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [PromptChipsComponent] }).compileComponents();
    fixture = TestBed.createComponent(PromptChipsComponent);
    fixture.detectChanges();
  });

  it('Given_DefaultPrompts_When_Rendering_Then_ShowsEverySuggestedChip', () => {
    expect(chips().map((chip) => chip.textContent?.trim())).toEqual([...DEFAULT_PROMPT_CHIPS]);
  });

  it('Given_DefaultPrompts_When_Rendering_Then_ContainsTheDocumentedSuggestions', () => {
    const labels = chips().map((chip) => chip.textContent?.trim());

    expect(labels).toContain('Pointeri în C');
    expect(labels).toContain('Algoritmi de sortare');
    expect(labels).toContain('Baze de date SQL');
  });

  it('Given_CustomPrompts_When_Rendering_Then_ShowsOnlyThosePrompts', () => {
    fixture.componentRef.setInput('prompts', ['Fizică', 'Chimie']);
    fixture.detectChanges();

    expect(chips().map((chip) => chip.textContent?.trim())).toEqual(['Fizică', 'Chimie']);
  });

  it('Given_Chip_When_Clicked_Then_EmitsItsPrompt', () => {
    const emitted: string[] = [];
    fixture.componentInstance.promptSelected.subscribe((prompt) => emitted.push(prompt));

    chips()[1].click();

    expect(emitted).toEqual(['Algoritmi de sortare']);
  });

  it('Given_EachChip_When_Clicked_Then_EmitsOnePromptPerClickInOrder', () => {
    const emitted: string[] = [];
    fixture.componentInstance.promptSelected.subscribe((prompt) => emitted.push(prompt));

    chips().forEach((chip) => chip.click());

    expect(emitted).toEqual([...DEFAULT_PROMPT_CHIPS]);
  });

  it('Given_DisabledChips_When_Rendering_Then_ButtonsAreDisabledAndDoNotEmit', () => {
    fixture.componentRef.setInput('disabled', true);
    fixture.detectChanges();
    const emitted: string[] = [];
    fixture.componentInstance.promptSelected.subscribe((prompt) => emitted.push(prompt));

    chips()[0].click();

    expect(chips().every((chip) => chip.disabled)).toBe(true);
    expect(emitted).toEqual([]);
  });

  it('Given_Chips_When_Rendering_Then_ExposesAccessibleList', () => {
    expect(element().querySelector('.chips')?.getAttribute('aria-label')).toBe('Căutări sugerate');
  });
});
