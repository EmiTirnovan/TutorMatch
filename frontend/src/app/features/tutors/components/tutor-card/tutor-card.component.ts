import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TutorSearchItem } from '../../../../core/models/tutor-search.models';

const MAX_STARS = 5;

@Component({
  selector: 'app-tutor-card',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './tutor-card.component.html',
  styleUrl: './tutor-card.component.css'
})
export class TutorCardComponent {
  readonly tutor = input.required<TutorSearchItem>();
  /** Highlights the card as the closest semantic match. */
  readonly bestMatch = input(false);

  readonly initials = computed(() =>
    this.tutor()
      .name.split(' ')
      .map((part) => part.charAt(0).toUpperCase())
      .slice(0, 2)
      .join('')
  );

  readonly hasRatings = computed(() => this.tutor().ratingCount > 0);

  readonly formattedRating = computed(() => this.tutor().averageRating.toFixed(1));

  readonly stars = computed(() => {
    const filled = Math.round(this.tutor().averageRating);
    return Array.from({ length: MAX_STARS }, (_, index) => index < filled);
  });

  /** Semantic match in percent, or null when the tutor was not ranked semantically. */
  readonly matchPercent = computed(() => {
    const score = this.tutor().similarityScore;
    return score === null || score === undefined ? null : Math.round(Math.min(1, Math.max(0, score)) * 100);
  });
}
