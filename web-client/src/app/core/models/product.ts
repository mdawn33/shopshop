export interface Product {
  id: string;
  name: string;
  description: string;
  brand_author: string; // fix this
  sku: string;
  categoryId: string;
  variant: string;       // e.g. color, size, capacity
  imageUrl: string;
  rating: number;        // 0–5
  reviewCount: number;
  price: number;         // current price
  originalPrice: number; // pre-discount price
  stock: number;
  fastShipping: boolean;
}