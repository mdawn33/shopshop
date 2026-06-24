import { Injectable, signal } from "@angular/core";
import { CATEGORIES, NAV_LINKS } from "../../shared/data/sample-data";
import { Product } from "../models/product";


@Injectable({
  providedIn: 'root',
})
export class CatalogState {
  
  readonly wishlist = signal<ReadonlySet<string>>(new Set());
  readonly cart = signal<Record<string, number>>({});

  readonly navLinks = NAV_LINKS;
  readonly categories = CATEGORIES;

  addToCart(product: Product) {
    if(product) {
      this.cart.update((current) => ({
        ...current,
        [product.id]: (current[product.id] ?? 0) + 1
      }));
    }
  }

  toggleWishlist(product: Product) {
    this.wishlist.update((current) => {
      const newSet = new Set(current);
      newSet.has(product.id) ? newSet.delete(product.id) : newSet.add(product.id);
      return newSet;
    })
  }
}