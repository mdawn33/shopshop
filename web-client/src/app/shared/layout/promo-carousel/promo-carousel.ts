import { Component, input } from '@angular/core';
import { PromoBanner } from '../../../core/models/promo-banner';

@Component({
  selector: 'app-promo-carousel',
  imports: [],
  templateUrl: './promo-carousel.html',
  styleUrl: './promo-carousel.scss',
})
export class PromoCarousel {

  readonly banners = input<PromoBanner[]>([]);
}
