import { Routes } from "@angular/router";
import { ProductDetail } from "./product-detail/product-detail";
import { CatalogContent } from "./catalog-content/catalog-content";
import { MainLayout } from "../../layout/main-layout/main-layout";
import { ProductsList } from "./products-list/products-list";

export default [
    {
        path: '',
        component: MainLayout,
        // providers: [], // Introduce specific providers for each route and its child routes.
        children: [
            {
                path: '',
                component: CatalogContent
            },
            {
                path: 'products/:category',
                component: ProductsList
            },
            {
                path: 'detail/:id', 
                component: ProductDetail
            }
        ]
    },
   
] as Routes;