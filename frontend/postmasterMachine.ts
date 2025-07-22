import { createMachine, assign } from 'xstate';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Message, SenderType, RecipientType } from './types';

// Define the context type for the machine
interface PostmasterContext {
  connection: HubConnection | null;
  connectionId: string | null;
  userId: string | null;
  token: string | null;
  messages: Message[];
  unreadCount: number;
  error: string | null;
  currentGroup: string | null;
}

// Define the events that can be sent to the machine
type PostmasterEvent =
  | { type: 'CONNECT'; token: string }
  | { type: 'DISCONNECT' }
  | { type: 'SEND_TO_USER'; recipientId: string; content: string }
  | { type: 'SEND_TO_SERVICE'; serviceName: string; content: string }
  | { type: 'SEND_TO_GROUP'; groupName: string; content: string }
  | { type: 'JOIN_GROUP'; groupName: string }
  | { type: 'LEAVE_GROUP'; groupName: string }
  | { type: 'MARK_AS_READ'; messageId: string }
  | { type: 'LOAD_MESSAGES'; isInbound: boolean; fromDate?: Date }
  | { type: 'LOAD_UNREAD_MESSAGES' }
  | { type: 'MESSAGE_RECEIVED'; message: Message }
  | { type: 'GROUP_MESSAGE_RECEIVED'; groupName: string; message: Message }
  | { type: 'SERVICE_RESPONSE'; data: Message };

// Create the XState machine
export const postmasterMachine = createMachine<PostmasterContext, PostmasterEvent>(
  {
    id: 'postmaster',
    initial: 'disconnected',
    context: {
      connection: null,
      connectionId: null,
      userId: null,
      token: null,
      messages: [],
      unreadCount: 0,
      error: null,
      currentGroup: null,
    },
    states: {
      disconnected: {
        on: {
          CONNECT: {
            target: 'connecting',
            actions: assign({
              token: (_, event) => event.token,
            }),
          },
        },
      },
      connecting: {
        invoke: {
          src: 'connectToHub',
          onDone: {
            target: 'connected',
            actions: assign({
              connection: (_, event) => event.data.connection,
              connectionId: (_, event) => event.data.connectionId,
              userId: (_, event) => event.data.userId,
            }),
          },
          onError: {
            target: 'disconnected',
            actions: assign({
              error: (_, event) => event.data.message,
            }),
          },
        },
      },
      connected: {
        initial: 'idle',
        states: {
          idle: {
            on: {
              LOAD_MESSAGES: {
                target: 'loadingMessages',
                actions: assign({
                  messages: (_, __) => [], // Clear messages when loading new ones
                }),
              },
              LOAD_UNREAD_MESSAGES: {
                target: 'loadingUnreadMessages',
              },
              SEND_TO_USER: {
                target: 'sendingToUser',
              },
              SEND_TO_SERVICE: {
                target: 'sendingToService',
              },
              SEND_TO_GROUP: {
                target: 'sendingToGroup',
              },
              JOIN_GROUP: {
                target: 'joiningGroup',
              },
              LEAVE_GROUP: {
                target: 'leavingGroup',
              },
              MARK_AS_READ: {
                target: 'markingAsRead',
              },
              MESSAGE_RECEIVED: {
                actions: [
                  'addMessageToList',
                  'incrementUnreadCount',
                ],
              },
              GROUP_MESSAGE_RECEIVED: {
                actions: [
                  'addGroupMessageToList',
                  'incrementUnreadCount',
                ],
              },
            },
          },
          loadingMessages: {
            invoke: {
              src: 'loadMessages',
              onDone: {
                target: 'idle',
                actions: assign({
                  messages: (_, event) => event.data,
                }),
              },
              onError: {
                target: 'idle',
                actions: assign({
                  error: (_, event) => event.data.message,
                }),
              },
            },
          },
          loadingUnreadMessages: {
            invoke: {
              src: 'loadUnreadMessages',
              onDone: {
                target: 'idle',
                actions: assign({
                  messages: (_, event) => event.data,
                  unreadCount: (_, event) => event.data.length,
                }),
              },
              onError: {
                target: 'idle',
                actions: assign({
                  error: (_, event) => event.data.message,
                }),
              },
            },
          },
          sendingToUser: {
            invoke: {
              src: 'sendToUser',
              onDone: {
                target: 'idle',
              },
              onError: {
                target: 'idle',
                actions: assign({
                  error: (_, event) => event.data.message,
                }),
              },
            },
          },
          sendingToService: {
            invoke: {
              src: 'sendToService',
              onDone: {
                target: 'idle',
                actions: 'handleServiceResponse',
              },
              onError: {
                target: 'idle',
                actions: assign({
                  error: (_, event) => event.data.message,
                }),
              },
            },
          },
          sendingToGroup: {
            invoke: {
              src: 'sendToGroup',
              onDone: {
                target: 'idle',
              },
              onError: {
                target: 'idle',
                actions: assign({
                  error: (_, event) => event.data.message,
                }),
              },
            },
          },
          joiningGroup: {
            invoke: {
              src: 'joinGroup',
              onDone: {
                target: 'idle',
                actions: assign({
                  currentGroup: (_, event) => event.data.groupName,
                }),
              },
              onError: {
                target: 'idle',
                actions: assign({
                  error: (_, event) => event.data.message,
                }),
              },
            },
          },
          leavingGroup: {
            invoke: {
              src: 'leaveGroup',
              onDone: {
                target: 'idle',
                actions: assign({
                  currentGroup: (_, __) => null,
                }),
              },
              onError: {
                target: 'idle',
                actions: assign({
                  error: (_, event) => event.data.message,
                }),
              },
            },
          },
          markingAsRead: {
            invoke: {
              src: 'markAsRead',
              onDone: {
                target: 'idle',
                actions: [
                  'updateMessageReadStatus',
                  'decrementUnreadCount',
                ],
              },
              onError: {
                target: 'idle',
                actions: assign({
                  error: (_, event) => event.data.message,
                }),
              },
            },
          },
        },
        on: {
          DISCONNECT: {
            target: 'disconnecting',
          },
        },
      },
      disconnecting: {
        invoke: {
          src: 'disconnectFromHub',
          onDone: {
            target: 'disconnected',
            actions: assign({
              connection: (_, __) => null,
              connectionId: (_, __) => null,
              userId: (_, __) => null,
              token: (_, __) => null,
              messages: (_, __) => [],
              unreadCount: (_, __) => 0,
              currentGroup: (_, __) => null,
            }),
          },
          onError: {
            target: 'disconnected',
            actions: assign({
              error: (_, event) => event.data.message,
              connection: (_, __) => null,
              connectionId: (_, __) => null,
              userId: (_, __) => null,
              token: (_, __) => null,
            }),
          },
        },
      },
    },
  },
  {
    services: {
      connectToHub: (context, event) => async (callback) => {
        try {
          if (event.type !== 'CONNECT') return;
          
          // Build the connection
          const connection = new HubConnectionBuilder()
            .withUrl('/hub', { accessTokenFactory: () => event.token })
            .configureLogging(LogLevel.Information)
            .withAutomaticReconnect()
            .build();

          // Set up message handlers
          connection.on('ReceiveMessage', (message: Message) => {
            callback({ type: 'MESSAGE_RECEIVED', message });
          });

          connection.on('ReceiveGroupMessage', (groupName: string, message: Message) => {
            callback({ type: 'GROUP_MESSAGE_RECEIVED', groupName, message });
          });

          // Start the connection
          await connection.start();
          
          // Get the connection ID and user ID
          const connectionId = connection.connectionId;
          const userId = connection.invoke('GetUserId'); // Assuming there's a method to get the user ID
          
          return { connection, connectionId, userId };
        } catch (error) {
          throw error;
        }
      },
      disconnectFromHub: async (context) => {
        if (context.connection) {
          await context.connection.stop();
        }
        return true;
      },
      loadMessages: async (context, event) => {
        if (event.type !== 'LOAD_MESSAGES' || !context.connection) return [];
        
        try {
          return await context.connection.invoke('GetUserMessages', event.isInbound, event.fromDate);
        } catch (error) {
          throw error;
        }
      },
      loadUnreadMessages: async (context) => {
        if (!context.connection) return [];
        
        try {
          return await context.connection.invoke('GetUnreadMessages');
        } catch (error) {
          throw error;
        }
      },
      sendToUser: async (context, event) => {
        if (event.type !== 'SEND_TO_USER' || !context.connection) return;
        
        try {
          await context.connection.invoke('SendToUser', event.recipientId, event.content);
          return true;
        } catch (error) {
          throw error;
        }
      },
      sendToService: async (context, event) => {
        if (event.type !== 'SEND_TO_SERVICE' || !context.connection) return;
        
        try {
          const response = await context.connection.invoke('SendToService', event.serviceName, event.content);
          return response;
        } catch (error) {
          throw error;
        }
      },
      sendToGroup: async (context, event) => {
        if (event.type !== 'SEND_TO_GROUP' || !context.connection) return;
        
        try {
          await context.connection.invoke('SendToGroup', event.groupName, event.content);
          return true;
        } catch (error) {
          throw error;
        }
      },
      joinGroup: async (context, event) => {
        if (event.type !== 'JOIN_GROUP' || !context.connection) return;
        
        try {
          // This would typically be handled by the server when the user is added to a group
          // Here we're just returning the group name for state management
          return { groupName: event.groupName };
        } catch (error) {
          throw error;
        }
      },
      leaveGroup: async (context, event) => {
        if (event.type !== 'LEAVE_GROUP' || !context.connection) return;
        
        try {
          // This would typically be handled by the server when the user is removed from a group
          return true;
        } catch (error) {
          throw error;
        }
      },
      markAsRead: async (context, event) => {
        if (event.type !== 'MARK_AS_READ' || !context.connection) return;
        
        try {
          await context.connection.invoke('MarkAsRead', event.messageId);
          return { messageId: event.messageId };
        } catch (error) {
          throw error;
        }
      },
    },
    actions: {
      addMessageToList: assign({
        messages: (context, event) => {
          if (event.type !== 'MESSAGE_RECEIVED') return context.messages;
          return [...context.messages, event.message];
        },
      }),
      addGroupMessageToList: assign({
        messages: (context, event) => {
          if (event.type !== 'GROUP_MESSAGE_RECEIVED') return context.messages;
          return [...context.messages, event.message];
        },
      }),
      incrementUnreadCount: assign({
        unreadCount: (context) => context.unreadCount + 1,
      }),
      decrementUnreadCount: assign({
        unreadCount: (context) => Math.max(0, context.unreadCount - 1),
      }),
      updateMessageReadStatus: assign({
        messages: (context, event) => {
          if (event.type !== 'MARK_AS_READ') return context.messages;
          return context.messages.map(message => 
            message.id === event.messageId 
              ? { ...message, isRead: true } 
              : message
          );
        },
      }),
      handleServiceResponse: assign({
        messages: (context, event: any) => {
          if (!event.data) return context.messages;
          return [...context.messages, event.data];
        },
      }),
    },
  }
);
