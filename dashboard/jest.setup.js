import "@testing-library/jest-dom";
import { webcrypto } from "node:crypto";

if (!globalThis.crypto?.subtle) {
  Object.defineProperty(globalThis, "crypto", {
    configurable: true,
    value: webcrypto,
  });
}

class MessagePortMock {
  constructor() {
    this.onmessage = null;
    this._target = null;
    this._listeners = new Set();
  }

  postMessage(message) {
    const event = { data: message };
    queueMicrotask(() => {
      if (this._target?.onmessage) {
        this._target.onmessage(event);
      }

      if (this._target?._listeners) {
        this._target._listeners.forEach((listener) => listener(event));
      }
    });
  }

  addEventListener(type, listener) {
    if (type === "message") {
      this._listeners.add(listener);
    }
  }

  removeEventListener(type, listener) {
    if (type === "message") {
      this._listeners.delete(listener);
    }
  }

  start() {}

  close() {
    this._listeners.clear();
    this.onmessage = null;
    this._target = null;
  }
}

class MessageChannelMock {
  constructor() {
    this.port1 = new MessagePortMock();
    this.port2 = new MessagePortMock();
    this.port1._target = this.port2;
    this.port2._target = this.port1;
  }
}

class ResizeObserverMock {
  observe() {}

  unobserve() {}

  disconnect() {}
}

if (!globalThis.MessageChannel) {
  Object.defineProperty(globalThis, "MessageChannel", {
    configurable: true,
    value: MessageChannelMock,
  });
}

if (!globalThis.MessagePort) {
  Object.defineProperty(globalThis, "MessagePort", {
    configurable: true,
    value: MessagePortMock,
  });
}

if (!globalThis.ResizeObserver) {
  Object.defineProperty(globalThis, "ResizeObserver", {
    configurable: true,
    writable: true,
    value: ResizeObserverMock,
  });
}

if (typeof window !== "undefined") {
  if (!window.MessageChannel) {
    Object.defineProperty(window, "MessageChannel", {
      configurable: true,
      value: MessageChannelMock,
    });
  }

  if (!window.MessagePort) {
    Object.defineProperty(window, "MessagePort", {
      configurable: true,
      value: MessagePortMock,
    });
  }

  if (!window.ResizeObserver) {
    Object.defineProperty(window, "ResizeObserver", {
      configurable: true,
      writable: true,
      value: ResizeObserverMock,
    });
  }

  const originalGetComputedStyle = window.getComputedStyle.bind(window);
  window.getComputedStyle = (element) => originalGetComputedStyle(element);
}

jest.mock("#/utils/message", () => ({
  message: {
    success: jest.fn(),
    error: jest.fn(),
    warning: jest.fn(),
    info: jest.fn(),
    loading: jest.fn(),
    open: jest.fn(),
    destroy: jest.fn(),
  },
  setMessageInstance: jest.fn(),
  clearMessageInstance: jest.fn(),
}));

// Mock next/navigation
jest.mock("next/navigation", () => ({
  useRouter() {
    return {
      push: jest.fn(),
      replace: jest.fn(),
      prefetch: jest.fn(),
    };
  },
  usePathname() {
    return "";
  },
}));

window.matchMedia =
  window.matchMedia ||
  function () {
    return {
      matches: false,
      addListener: () => {},
      removeListener: () => {},
    };
  };

class BroadcastChannelMock {
  constructor(channelName) {
    this.name = channelName;
    this.onmessage = null;
  }

  postMessage(message) {
    if (this.onmessage) {
      this.onmessage({ data: message });
    }
  }

  close() {
    // No-op
  }
}

global.BroadcastChannel = BroadcastChannelMock;

global.TransformStream = class {
  constructor() {
    this.readable = {};
    this.writable = {};
  }
};

jest.mock("react-password-checklist", () => function PasswordChecklistMock() {
  return <div>PasswordChecklistMock</div>;
});

jest.mock("jsoneditor-react", () => ({
  JsonEditor: function JsonEditorMock({ value, onChange }) {
    return (
      <input
        data-testid="json-editor-textarea"
        value={value}
        onChange={(e) => onChange(e.target.value)}
      />
    );
  },
}));

// Handle unhandled promise rejections for form validation errors
if (typeof window !== 'undefined') {
  window.addEventListener('unhandledrejection', (event) => {
    // Suppress Ant Design form validation errors (they have errorFields)
    if (event.reason?.errorFields || (event.reason?.name && Array.isArray(event.reason.name))) {
      event.preventDefault();
    }
  });
}
