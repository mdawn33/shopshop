import { Component, input, output } from '@angular/core';
import { Product } from '../../../core/models/product';
import { ProductCard } from '../product-card/product-card';

@Component({
  selector: 'app-product-rail',
  imports: [ProductCard],
  templateUrl: './product-rail.html',
  styleUrl: './product-rail.scss',
})
export class ProductRail {

  readonly sectionId = input<string>('');
  readonly title = input.required<string>();
  readonly link = input('#');
  readonly products = input<Product[]>([]);
  readonly wishlistedIds = input<ReadonlySet<string>>(new Set());
 
  readonly addToCart = output<Product>();
  readonly toggleWishlist = output<Product>();
 
  scrollByAmount(track: HTMLElement, direction: number): void {
    track.scrollBy({ left: direction * track.clientWidth * 0.85, behavior: 'smooth' });
  }
}
