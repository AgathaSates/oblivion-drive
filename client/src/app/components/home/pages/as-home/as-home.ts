import { Component } from '@angular/core';
import { AsCarousel } from '../../sections/as-carousel/as-carousel';
import { AsPlanHighlights } from '../../sections/as-plan-highlights/as-plan-highlights';
import { AsAdvantages } from '../../sections/as-advantages/as-advantages';
import { AsReviews } from '../../sections/as-reviews/as-reviews';

@Component({
  selector: 'as-home',
  imports: [AsCarousel, AsPlanHighlights, AsAdvantages, AsReviews],
  templateUrl: './as-home.html',
})
export class AsHome {}
