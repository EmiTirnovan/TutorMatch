import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, input, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { EVALUATION_CASES } from '../../../../core/data/evaluation-cases';
import {
  EvaluationCase,
  EvaluationCaseResult,
  EvaluationCaseStatus,
  EvaluationState
} from '../../../../core/models/semantic-search.models';
import { SemanticEvaluationService, summarizeEvaluation } from '../../../../core/services/semantic-evaluation.service';

const STATUS_LABELS: Record<EvaluationCaseStatus, string> = {
  hit: 'Top-3 hit',
  miss: 'Miss',
  fallback: 'Keyword fallback',
  error: 'Error'
};

@Component({
  selector: 'app-evaluation-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './evaluation-dashboard.component.html',
  styleUrl: './evaluation-dashboard.component.css'
})
export class EvaluationDashboardComponent {
  private readonly evaluationService = inject(SemanticEvaluationService);
  private readonly destroyRef = inject(DestroyRef);
  private runSubscription?: Subscription;

  readonly cases = input<readonly EvaluationCase[]>(EVALUATION_CASES);

  readonly state = signal<EvaluationState>('idle');
  readonly results = signal<readonly EvaluationCaseResult[]>([]);

  readonly summary = computed(() => summarizeEvaluation(this.results()));
  readonly isLoading = computed(() => this.state() === 'loading');
  readonly progress = computed(() => `${this.results().length} / ${this.cases().length}`);

  constructor() {
    this.destroyRef.onDestroy(() => this.runSubscription?.unsubscribe());
  }

  run(): void {
    if (this.isLoading()) {
      return;
    }
    this.runSubscription?.unsubscribe();
    this.results.set([]);
    this.state.set('loading');

    this.runSubscription = this.evaluationService.evaluate(this.cases()).subscribe({
      next: (result) => this.results.update((current) => [...current, result]),
      error: () => this.state.set('error'),
      complete: () => {
        const allFailed = this.results().length === 0 || this.results().every((r) => r.status === 'error');
        this.state.set(allFailed ? 'error' : 'success');
      }
    });
  }

  statusLabel(status: EvaluationCaseStatus): string {
    return STATUS_LABELS[status];
  }

  formatScore(score: number | null): string {
    return score === null ? '—' : `${Math.round(score * 100)}%`;
  }
}
