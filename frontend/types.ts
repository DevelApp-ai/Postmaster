export interface Message {
  id: string;
  content: string;
  senderId: string;
  senderType: SenderType;
  recipientId: string;
  recipientType: RecipientType;
  timestamp: string;
  isRead: boolean;
  metadata?: string;
}

export enum SenderType {
  User = 0,
  Service = 1,
  Group = 2
}

export enum RecipientType {
  User = 0,
  Service = 1,
  Group = 2
}

export interface PostmasterConfig {
  hubUrl: string;
  tokenProvider: () => string | Promise<string>;
  onError?: (error: Error) => void;
  onConnected?: () => void;
  onDisconnected?: () => void;
  onMessageReceived?: (message: Message) => void;
}
