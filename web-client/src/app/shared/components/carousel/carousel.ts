import { Component, input } from '@angular/core';
import { PromoBanner } from '../../../core/models/promo-banner';

@Component({
  selector: 'app-carousel',
  imports: [],
  templateUrl: './carousel.html',
  styleUrl: './carousel.scss',
})
export class Carousel {

  readonly banners = input<PromoBanner[]>([]);
}
