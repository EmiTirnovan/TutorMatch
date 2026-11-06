import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { SearchMode } from '../../../../core/models/semantic-search.models';
import {
  DEFAULT_FILTER_CRITERIA,
  TutorFilterCriteria,
  TutorSearchItem
} from '../../../../core/models/tutor-search.models';
import { SemanticSearchService } from '../../../../core/services/semantic-search.service';
import { TutorService } from '../../../../core/services/tutor.service';
import { EvaluationDashboardComponent } from '../evaluation-dashboard/evaluation-dashboard.component';
import { PromptChipsComponent } from '../prompt-chips/prompt-chips.component';
import { SearchModeBadgeComponent } from '../search-mode-badge/search-mode-badge.component';
import { SemanticSearchBarComponent } from '../semantic-search-bar/semantic-search-bar.component';
import { TutorCardComponent } from '../tutor-card/tutor-card.component';
import { TutorFilterBarComponent } from '../tutor-filter-bar/tutor-filter-bar.component';

export const SEARCH_PAGE_SIZE = 6;

@Component({
  selector: 'app-search-tutors',
  standalone: true,
  imports: [
    TutorFilterBarComponent,
    TutorCardComponent,
    SemanticSearchBarComponent,
    PromptChipsComponent,
    SearchModeBadgeComponent,
    EvaluationDashboardComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './search-tutors.component.html',
  styleUrl: './search-tutors.component.css'
})
export class SearchTutorsComponent implements OnInit, OnDestroy {
  private readonly tutorService = inject(TutorService);
  private readonly semanticSearchService = inject(SemanticSearchService);
  private searchSubscription?: Subscription;

  readonly criteria = signal<TutorFilterCriteria>(DEFAULT_FILTER_CRITERIA);
  readonly page = signal(1);
  readonly tutors = signal<TutorSearchItem[]>([]);
  readonly totalCount = signal(0);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  /** Text shown in the semantic search bar. */
  readonly semanticQuery = signal('');
  /** Query of the last semantic search, or null while the classic filtered list is shown. */
  readonly activeSemanticQuery = signal<string | null>(null);
  readonly searchMode = signal<SearchMode | null>(null);
  readonly showEvaluation = signal(false);

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / SEARCH_PAGE_SIZE)));
  readonly skeletonItems = Array.from({ length: SEARCH_PAGE_SIZE }, (_, index) => index);

  readonly topScore = computed(() => {
    const scores = this.tutors()
      .map((tutor) => tutor.similarityScore)
      .filter((score): score is number => typeof score === 'number');
    return scores.length > 0 ? Math.max(...scores) : null;
  });

  ngOnInit(): void {
    this.loadTutors();
  }

  ngOnDestroy(): void {
    this.searchSubscription?.unsubscribe();
  }

  onCriteriaChange(criteria: TutorFilterCriteria): void {
    this.criteria.set(criteria);
    this.page.set(1);
    this.resetSemanticState();
    this.loadTutors();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.page()) {
      return;
    }
    this.page.set(page);
    this.loadTutors();
  }

  onPromptSelected(prompt: string): void {
    this.semanticQuery.set(prompt);
    this.onSemanticSearch(prompt);
  }

  onSemanticSearch(query: string): void {
    const trimmed = query.trim();
    if (!trimmed) {
      return;
    }

    this.searchSubscription?.unsubscribe();
    this.activeSemanticQuery.set(trimmed);
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.searchSubscription = this.semanticSearchService.search(trimmed).subscribe({
      next: (result) => {
        this.tutors.set(result.tutors);
        this.totalCount.set(result.tutors.length);
        this.page.set(1);
        this.searchMode.set(result.mode);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('We could not load tutors. Please try again.');
        this.isLoading.set(false);
      }
    });
  }

  clearSemanticSearch(): void {
    this.resetSemanticState();
    this.page.set(1);
    this.loadTutors();
  }

  toggleEvaluation(): void {
    this.showEvaluation.update((visible) => !visible);
  }

  retry(): void {
    const query = this.activeSemanticQuery();
    if (query) {
      this.onSemanticSearch(query);
    } else {
      this.loadTutors();
    }
  }

  loadTutors(): void {
    this.searchSubscription?.unsubscribe();
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.searchSubscription = this.tutorService
      .searchTutors({ ...this.criteria(), page: this.page(), pageSize: SEARCH_PAGE_SIZE })
      .subscribe({
        next: (response) => {
          this.tutors.set(response.tutors);
          this.totalCount.set(response.totalCount);
          this.isLoading.set(false);
        },
        error: () => {
          this.errorMessage.set('We could not load tutors. Please try again.');
          this.isLoading.set(false);
        }
      });
  }

  private resetSemanticState(): void {
    this.semanticQuery.set('');
    this.activeSemanticQuery.set(null);
    this.searchMode.set(null);
  }
}
