import '@testing-library/jest-dom';
import { afterEach, vi } from 'vitest';
import { cleanup } from '@testing-library/react';

// Mock localStorage
const localStorageMock = (() => {
  let store: Record<string, string> = {};
  return {
    getItem: vi.fn((key: string) => (key in store ? store[key] : null)),
    setItem: vi.fn((key: string, value: string) => {
      store[key] = value.toString();
    }),
    removeItem: vi.fn((key: string) => {
      delete store[key];
    }),
    clear: vi.fn(() => {
      store = {};
    }),
    get length() { return Object.keys(store).length; },
    key: vi.fn((index: number) => Object.keys(store)[index] || null),
  };
})();

Object.defineProperty(window, 'localStorage', {
  value: localStorageMock,
});

// Mock IntersectionObserver
class IntersectionObserverMock {
  root: Document | Element | null = null;
  rootMargin = "";
  thresholds: number[] = [];

  private callback: IntersectionObserverCallback;
  private observedElements: Set<Element> = new Set();

  constructor(callback: IntersectionObserverCallback, options?: IntersectionObserverInit) {
    this.callback = callback;
    if (options) {
      this.root = options.root ?? null;
      this.rootMargin = options.rootMargin ?? "";
      this.thresholds = Array.isArray(options.threshold)
        ? options.threshold
        : [options.threshold ?? 0];
    }
  }

  disconnect = vi.fn(() => {
    this.observedElements.clear();
  });

  observe = vi.fn((element: Element) => {
    this.observedElements.add(element);
    // IntersectionObserver callbacks are usually async
    queueMicrotask(() => {
      if (!this.observedElements.has(element)) return;

      this.callback(
        [{
          target: element,
          isIntersecting: true,
          intersectionRatio: 1,
          boundingClientRect: element.getBoundingClientRect(),
          intersectionRect: element.getBoundingClientRect(),
          rootBounds: null,
          time: Date.now(),
        } as unknown as IntersectionObserverEntry],
        this as unknown as IntersectionObserver
      );
    });
  });

  takeRecords = vi.fn(() => []);

  unobserve = vi.fn((element: Element) => {
    this.observedElements.delete(element);
  });
}

Object.defineProperty(window, 'IntersectionObserver', {
  writable: true,
  configurable: true,
  value: IntersectionObserverMock,
});

// Runs a cleanup after each test case (e.g. clearing jsdom)
afterEach(() => {
  cleanup();
});
