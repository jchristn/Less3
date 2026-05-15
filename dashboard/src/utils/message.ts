import type { ArgsProps, JointContent, MessageInstance, MessageType } from 'antd/es/message/interface';
import type { Key } from 'react';

type MessageMethod = 'info' | 'success' | 'error' | 'warning' | 'loading';

type QueuedCall =
  | { method: MessageMethod; args: [JointContent, number | VoidFunction | undefined, VoidFunction | undefined] }
  | { method: 'open'; args: [ArgsProps] };

let messageInstance: MessageInstance | null = null;
const pendingCalls: QueuedCall[] = [];

const flushPendingCalls = () => {
  if (!messageInstance) {
    return;
  }

  while (pendingCalls.length > 0) {
    const nextCall = pendingCalls.shift();

    if (!nextCall) {
      continue;
    }

    if (nextCall.method === 'open') {
      messageInstance.open(...nextCall.args);
      continue;
    }

    messageInstance[nextCall.method](...nextCall.args);
  }
};

export const setMessageInstance = (instance: MessageInstance) => {
  messageInstance = instance;
  flushPendingCalls();
};

export const clearMessageInstance = () => {
  messageInstance = null;
};

const enqueueOrRun = (
  method: MessageMethod,
  content: JointContent,
  duration?: number | VoidFunction,
  onClose?: VoidFunction
): MessageType | undefined => {
  if (!messageInstance) {
    pendingCalls.push({ method, args: [content, duration, onClose] });
    return undefined;
  }

  return messageInstance[method](content, duration, onClose);
};

const open = (args: ArgsProps): MessageType | undefined => {
  if (!messageInstance) {
    pendingCalls.push({ method: 'open', args: [args] });
    return undefined;
  }

  return messageInstance.open(args);
};

export const message = {
  info: (content: JointContent, duration?: number | VoidFunction, onClose?: VoidFunction) =>
    enqueueOrRun('info', content, duration, onClose),
  success: (content: JointContent, duration?: number | VoidFunction, onClose?: VoidFunction) =>
    enqueueOrRun('success', content, duration, onClose),
  error: (content: JointContent, duration?: number | VoidFunction, onClose?: VoidFunction) =>
    enqueueOrRun('error', content, duration, onClose),
  warning: (content: JointContent, duration?: number | VoidFunction, onClose?: VoidFunction) =>
    enqueueOrRun('warning', content, duration, onClose),
  loading: (content: JointContent, duration?: number | VoidFunction, onClose?: VoidFunction) =>
    enqueueOrRun('loading', content, duration, onClose),
  open,
  destroy: (key?: Key) => {
    messageInstance?.destroy(key);
  },
};
