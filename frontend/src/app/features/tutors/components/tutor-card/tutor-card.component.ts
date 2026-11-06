import { Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TutorSearchItem } from '../../../../core/models/tutor-search.models';

const MAX_STARS = 5;

@Component({
  selector: 'app-tutor-card',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './tutor-card.component.html',
  styleUrl: './tutor-card.component.css'
})
export class TutorCardComponent {
  readonly tutor = input.required<TutorSearchItem>();

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
}
