import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CatalogContent } from './catalog-content';

describe('CatalogContent', () => {
  let component: CatalogContent;
  let fixture: ComponentFixture<CatalogContent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CatalogContent],
    }).compileComponents();

    fixture = TestBed.createComponent(CatalogContent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
