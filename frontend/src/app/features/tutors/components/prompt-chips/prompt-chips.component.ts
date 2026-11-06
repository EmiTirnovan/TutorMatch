import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

export const DEFAULT_PROMPT_CHIPS: readonly string[] = [
  'Pointeri în C',
  'Algoritmi de sortare',
  'Baze de date SQL',
  'Programare orientată pe obiecte',
  'Analiză matematică',
  'Pregătire IELTS'
];

@Component({
  selector: 'app-prompt-chips',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './prompt-chips.component.html',
  styleUrl: './prompt-chips.component.css'
})
export class PromptChipsComponent {
  readonly prompts = input<readonly string[]>(DEFAULT_PROMPT_CHIPS);
  readonly disabled = input(false);

  readonly promptSelected = output<string>();

  select(prompt: string): void {
    this.promptSelected.emit(prompt);
  }
}
