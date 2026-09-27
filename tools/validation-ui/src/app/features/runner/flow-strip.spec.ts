import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { FlowNode } from '../../scenarios/scenario';
import { FlowStrip } from './flow-strip';

// Work item: TASK-089 (FEAT-020)
describe('FlowStrip', () => {
  it('renders one box per node with its state, decision marker, label, and detail', async () => {
    const nodes: FlowNode[] = [
      { id: 'a', label: 'Item', state: 'done', detail: '12 units' },
      { id: 'b', label: 'Tier', decision: true, state: 'fail', detail: 'expected 20%, got 10%' },
      { id: 'c', label: 'Read model', state: 'active', detail: '' },
      { id: 'd', label: 'Outbox', state: 'pending', detail: '' },
    ];
    TestBed.configureTestingModule({ imports: [FlowStrip] });
    const fixture = TestBed.createComponent(FlowStrip);
    fixture.componentRef.setInput('nodes', nodes);
    await fixture.whenStable();

    const boxes: HTMLElement[] = Array.from(fixture.nativeElement.querySelectorAll('.n'));
    expect(boxes.map((box) => box.dataset['state'])).toEqual(['done', 'fail', 'active', 'pending']);
    expect(boxes[1].classList.contains('dec')).toBe(true);
    expect(boxes[0].textContent).toContain('Item');
    expect(boxes[0].textContent).toContain('12 units');
    expect(fixture.nativeElement.querySelectorAll('.ar').length).toBe(3);
  });

  it('shows the node keys above the text and marks the ones seen in the trace', async () => {
    const nodes: FlowNode[] = [
      { id: 'a', label: 'Item', keys: ['SAL-CRT-01'], state: 'done', detail: '3 units asking 10%' },
      { id: 'b', label: 'Over 20?', decision: true, keys: ['SAL-CRT-15', 'SAL-CRT-16'], state: 'done', detail: 'no' },
    ];
    TestBed.configureTestingModule({ imports: [FlowStrip] });
    const fixture = TestBed.createComponent(FlowStrip);
    fixture.componentRef.setInput('nodes', nodes);
    fixture.componentRef.setInput('traced', new Set(['SAL-CRT-15']));
    await fixture.whenStable();

    const first: HTMLElement = fixture.nativeElement.querySelector('.n');
    expect(first.firstElementChild?.classList.contains('keys')).toBe(true);
    expect(first.textContent?.replace(/\s+/g, ' ').trim()).toBe('SAL-CRT-01 Item 3 units asking 10%');
    const keys: HTMLElement[] = Array.from(fixture.nativeElement.querySelectorAll('.key'));
    expect(keys.map((key) => [key.textContent, key.classList.contains('seen')])).toEqual([
      ['SAL-CRT-01', false], ['SAL-CRT-15', true], ['SAL-CRT-16', false],
    ]);
  });
});
