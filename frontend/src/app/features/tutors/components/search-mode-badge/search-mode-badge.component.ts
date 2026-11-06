import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { SearchMode } from '../../../../core/models/semantic-search.models';
import { KEYWORD_FALLBACK_MESSAGE } from '../../../../core/services/semantic-search.service';

@Component({
  selector: 'app-search-mode-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './search-mode-badge.component.html',
  styleUrl: './search-mode-badge.component.css'
})
export class SearchModeBadgeComponent {
  readonly mode = input.required<SearchMode>();
  /** Best similarity (0..1) among the results; only displayed in semantic mode. */
  readonly topScore = input<number | null>(null);

  readonly isSemantic = computed(() => this.mode() === 'semantic');
  readonly label = computed(() => (this.isSemantic() ? 'Semantic Search' : 'Keyword Fallback'));
  readonly fallbackMessage = KEYWORD_FALLBACK_MESSAGE;

  readonly scorePercent = computed(() => {
    const score = this.topScore();
    return score === null ? null : Math.round(Math.min(1, Math.max(0, score)) * 100);
  });
}
