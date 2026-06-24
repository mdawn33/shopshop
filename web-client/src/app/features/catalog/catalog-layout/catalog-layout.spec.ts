import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CatalogLayout } from './catalog-layout';

describe('CatalogLayout', () => {
  let component: CatalogLayout;
  let fixture: ComponentFixture<CatalogLayout>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CatalogLayout],
    }).compileComponents();

    fixture = TestBed.createComponent(CatalogLayout);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
