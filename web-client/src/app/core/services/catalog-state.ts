import { Injectable, signal } from "@angular/core";
import { Product } from "../models/product";

// TODO check NGRX Signals Store to replace this service (state handler)


@Injectable({
  providedIn: 'root'
})
export class CatalogState {

  readonly cart = signal<Record<string, number>>({});
  readonly wishlist = signal<ReadonlySet<string>>(new Set());


  // Record is immutable
  // Uses implicit return, wraps the object in parenthesis ({}) to instantly return the object without using the return keyword
  // { ...record, key: value } => Adds a new record, or updates it if it already exists
  addToCart(product: Product) {
    if(product) {
      this.cart.update((current) => ({
        ...current,
        [product.id]: (current[product.id] ?? 0) + 1
      }));
    }
  }

  // uses explicit code block because it needs multiple lines of code, therefore it requires the return keyword 
  toggleWishlist(product: Product): void {

    if(product) {
      this.wishlist.update((current) => {
        const newSet = new Set(current);
        newSet.has(product.id) ? newSet.delete(product.id) : newSet.add(product.id);
        return newSet;
      });
    }
  }

}