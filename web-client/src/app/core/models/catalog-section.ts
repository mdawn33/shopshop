import { Product } from "./product";

export interface CatalogSection {
  id: string;
  title: string;
  link: string;
  products: Product[];
}