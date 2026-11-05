import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import {
  DEFAULT_FILTER_CRITERIA,
  TutorFilterCriteria,
  TutorSearchItem
} from '../../../../core/models/tutor-search.models';
import { TutorService } from '../../../../core/services/tutor.service';
import { TutorCardComponent } from '../tutor-card/tutor-card.component';
import { TutorFilterBarComponent } from '../tutor-filter-bar/tutor-filter-bar.component';

export const SEARCH_PAGE_SIZE = 6;

@Component({
  selector: 'app-search-tutors',
  standalone: true,
  imports: [TutorFilterBarComponent, TutorCardComponent],
  templateUrl: './search-tutors.component.html',
  styleUrl: './search-tutors.component.css'
})
export class SearchTutorsComponent implements OnInit, OnDestroy {
  private readonly tutorService = inject(TutorService);
  private searchSubscription?: Subscription;

  readonly criteria = signal<TutorFilterCriteria>(DEFAULT_FILTER_CRITERIA);
  readonly page = signal(1);
  readonly tutors = signal<TutorSearchItem[]>([]);
  readonly totalCount = signal(0);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / SEARCH_PAGE_SIZE)));
  readonly skeletonItems = Array.from({ length: SEARCH_PAGE_SIZE }, (_, index) => index);

  ngOnInit(): void {
    this.loadTutors();
  }

  ngOnDestroy(): void {
    this.searchSubscription?.unsubscribe();
  }

  onCriteriaChange(criteria: TutorFilterCriteria): void {
    this.criteria.set(criteria);
    this.page.set(1);
    this.loadTutors();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.page()) {
      return;
    }
    this.page.set(page);
    this.loadTutors();
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
}
