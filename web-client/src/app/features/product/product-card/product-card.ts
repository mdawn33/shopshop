import { Component, output, input, ChangeDetectionStrategy } from '@angular/core';
import { Product } from '../../../core/models/product';
import { discountPercent, filledStars, formatPrice, stockLabel, stockLevel } from '../../../core/helpers/catalog-format-helpers';
import { RouterLink } from "@angular/router";

@Component({
  selector: 'app-product-card',
  imports: [RouterLink],
  templateUrl: './product-card.html',
  styleUrl: './product-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProductCard {

  readonly product = input.required<Product>();
  readonly wishlisted = input(false);
 
  readonly addToCart = output<Product>();
  readonly toggleWishlist = output<Product>();
 
  /** five-star helper for the @for loop in the template */
  protected readonly starSlots = [0, 1, 2, 3, 4];
 
  // Pure formatting helpers exposed for the template (see catalog-format.utils.ts).
  protected readonly discountPercent = discountPercent;
  protected readonly filledStars = filledStars;
  protected readonly stockLabel = stockLabel;
  protected readonly stockLevel = stockLevel;
  protected readonly formatPrice = formatPrice;
 
  onAddToCart(): void {
    this.addToCart.emit(this.product());
  }
 
  onToggleWishlist(): void {
    this.toggleWishlist.emit(this.product());
  }

}
