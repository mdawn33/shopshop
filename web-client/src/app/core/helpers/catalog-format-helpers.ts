import { Product } from "../models/product";


/** Rounded number of filled stars to render for a 0–5 rating. */
export function filledStars(rating: number): number {
  return Math.round(rating);
}

/** Discount percentage vs. the original price, 0 when there is no discount. */
export function discountPercent(book: Pick<Product, 'price' | 'originalPrice'>): number {
  if (book.originalPrice <= book.price) return 0;
  return Math.round((1 - book.price / book.originalPrice) * 100);
}

/** Human-readable stock label, matching the source site's wording. */
export function stockLabel(stock: number): string {
  if (stock <= 0) return 'Agotado';
  if (stock >= 100) return 'Quedan 100+ unidades';
  return `Quedan ${stock} unidades`;
}

/** Coarse stock level used to color-code the stock label. */
export function stockLevel(stock: number): 'low' | 'mid' | 'high' {
  if (stock <= 5) return 'low';
  if (stock < 100) return 'mid';
  return 'high';
}

/** Formats a COP amount, e.g. 18000 -> "$ 18.000". */
//TODO Modify this to allow any currency
export function formatPrice(value: number): string {
  return new Intl.NumberFormat('es-CO', {
    style: 'currency',
    currency: 'COP',
    maximumFractionDigits: 0,
  }).format(value);
}