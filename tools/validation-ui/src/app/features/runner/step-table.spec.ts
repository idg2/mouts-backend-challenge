import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { StepResult } from '../../scenarios/scenario';
import { StepTable } from './step-table';

// Work item: TASK-089 (FEAT-020)
describe('StepTable', () => {
  const step: StepResult = {
    label: 'Call', expected: '200', actual: '200', status: 'pass', keys: ['SAL-CRT'],
    exchanges: [{ method: 'GET', url: '/api/x', requestBody: null, status: 200, responseBody: {}, ms: 1 }],
  };

  it('shows a Key column with the step keys, marking the traced ones', async () => {
    TestBed.configureTestingModule({ imports: [StepTable] });
    const fixture = TestBed.createComponent(StepTable);
    fixture.componentRef.setInput('steps', [step]);
    fixture.componentRef.setInput('traced', new Set(['SAL-CRT-01']));
    await fixture.whenStable();

    const headers = Array.from(fixture.nativeElement.querySelectorAll('th')).map((th) => (th as HTMLElement).textContent);
    expect(headers).toEqual(['', 'Key', 'Step', 'Expected', 'Actual', '']);
    const key: HTMLElement = fixture.nativeElement.querySelector('tbody .key');
    expect(key.textContent).toBe('SAL-CRT');
    expect(key.classList.contains('seen')).toBe(true);
  });

  it('collapses expanded rows when a new run clears the steps', async () => {
    TestBed.configureTestingModule({ imports: [StepTable] });
    const fixture = TestBed.createComponent(StepTable);
    fixture.componentRef.setInput('steps', [step]);
    await fixture.whenStable();
    fixture.nativeElement.querySelector('button.link').click();
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelectorAll('tr.exchange').length).toBe(1);

    fixture.componentRef.setInput('steps', []);
    await fixture.whenStable();
    fixture.componentRef.setInput('steps', [step]);
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelectorAll('tr.exchange').length).toBe(0);
  });
});
