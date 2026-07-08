export interface Product {
  id: string;
  name: string;
  brand: string;
  sku: string;
  variant: string;       // e.g. color, size, capacity
  imageUrl: string;
  rating: number;        // 0–5
  reviewCount: number;
  price: number;         // current price (COP)
  originalPrice: number; // pre-discount price (COP)
  stock: number;
  fastShipping: boolean;
}