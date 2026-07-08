import { Component, input, output, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Product } from '../../../core/models/product';
import { discountPercent, filledStars, formatPrice, stockLabel, stockLevel } from '../../../core/helpers/catalog-format-helpers';

@Component({
  selector: 'app-product-card',
  imports: [RouterLink],
  templateUrl: './product-card.html',
  styleUrl: './product-card.scss',
})
export class ProductCard {
  
  readonly product = input.required<Product>();
  readonly wishlisted = input(false);

  readonly addToCart = output<Product>();
  readonly toggleWishlist = output<Product>();
  
  protected readonly starSlots = [0,1,2,3,4];

  // Formatting helpers
  protected readonly filledStars = filledStars;
  protected readonly stockLevel = stockLevel; 
  protected readonly stockLabel = stockLabel;
  protected readonly discountPercent = discountPercent;
  protected readonly formatPrice = formatPrice;

  onAddToCart(): void {
    this.addToCart.emit(this.product());
  }

  onToggleWishlist(): void {
    this.toggleWishlist.emit(this.product());
  }
}
