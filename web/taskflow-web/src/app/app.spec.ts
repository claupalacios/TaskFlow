import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);

    expect(fixture.componentInstance).toBeTruthy();
  });

  it('shows the app name in the toolbar', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const toolbar = (fixture.nativeElement as HTMLElement).querySelector('mat-toolbar');

    expect(toolbar?.textContent).toContain('TaskFlow');
  });

  it('has a main region that hosts the routed pages', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const main = (fixture.nativeElement as HTMLElement).querySelector('main');

    expect(main?.querySelector('router-outlet')).not.toBeNull();
  });
});
