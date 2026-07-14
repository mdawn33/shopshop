import { TestBed } from '@angular/core/testing';

import { ApiRequests } from './api-requests';

describe('ApiRequests', () => {
  let service: ApiRequests;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ApiRequests);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
