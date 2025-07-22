# Postmaster Implementation

This is a complete implementation of the Postmaster messaging system, a runtime extendable C# website using SignalR for real-time messaging. The implementation includes both backend and frontend components that can be integrated into your development environment.

## Architecture Overview

The Postmaster system consists of the following components:

1. **Core Interfaces**: Defines the contract for message handling, storage, and permissions
2. **DiskMessageStore**: Implements file-based message storage with routing capabilities
3. **MessageHub**: SignalR hub for real-time messaging with routing and authentication
4. **TypeScript Frontend**: Modern TypeScript implementation with XState for state management

## Backend Components

The backend is implemented in C# and includes:

- **IMessageStore**: Interface for message storage and retrieval operations
- **IMessageHandler**: Interface for handling messages and runtime extensibility
- **IPermissionHandler**: Interface for handling permissions and group membership
- **Message**: Class representing a message in the system
- **DiskMessageStore**: Implementation of IMessageStore using file system storage
- **MessageHub**: SignalR hub for real-time messaging

## Frontend Components

The frontend is implemented in TypeScript with React and XState:

- **PostmasterClient**: React component for messaging UI
- **PostmasterService**: Non-React service for messaging operations
- **postmasterMachine**: XState machine for state management
- **types.ts**: TypeScript type definitions

## Integration Instructions

### Backend Integration

1. Update your project to .NET 7.0 or newer
2. Add the backend files to your project
3. Register the services in your Program.cs:

```csharp
// Add services to the container
builder.Services.AddSignalR();
builder.Services.AddSingleton<IMessageStore, DiskMessageStore.DiskMessageStore>(sp => 
    new DiskMessageStore.DiskMessageStore("/path/to/message/storage"));
builder.Services.AddSingleton<IMessageHandler, YourMessageHandlerImplementation>();
builder.Services.AddSingleton<IPermissionHandler, YourPermissionHandlerImplementation>();

// Configure SignalR
app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<MessageHub>("/hub");
});
```

4. Implement the IMessageHandler and IPermissionHandler interfaces for your specific needs

### Frontend Integration

1. Install the required dependencies:

```bash
npm install @microsoft/signalr xstate @xstate/react react react-dom
```

2. Add the frontend files to your project
3. Use the PostmasterClient component in your React application:

```tsx
import { PostmasterClient } from './PostmasterClient';

function App() {
  return (
    <div className="App">
      <PostmasterClient 
        config={{
          hubUrl: '/hub',
          tokenProvider: () => 'your-auth-token',
          onError: (error) => console.error(error),
          onConnected: () => console.log('Connected'),
          onDisconnected: () => console.log('Disconnected'),
          onMessageReceived: (message) => console.log('Message received', message)
        }}
      />
    </div>
  );
}
```

4. Or use the PostmasterService for non-React applications:

```ts
import { PostmasterService } from './PostmasterService';

const postmaster = new PostmasterService({
  hubUrl: '/hub',
  tokenProvider: () => 'your-auth-token',
  onError: (error) => console.error(error),
  onConnected: () => console.log('Connected'),
  onDisconnected: () => console.log('Disconnected'),
  onMessageReceived: (message) => console.log('Message received', message)
});

// Start the service
await postmaster.start();

// Send a message
await postmaster.sendToUser('user123', 'Hello, world!');
```

## Runtime Extensibility

The Postmaster system supports runtime extensibility through the IMessageHandler interface. You can register and unregister message processors at runtime:

```csharp
// Register a message processor
messageHandler.RegisterMessageProcessor("myService", async (message) => {
    // Process the message
    return new Message { 
        Content = "Response to " + message.Content,
        SenderId = "myService",
        SenderType = SenderType.Service,
        RecipientId = message.SenderId,
        RecipientType = RecipientType.User
    };
});

// Unregister a message processor
messageHandler.UnregisterMessageProcessor("myService");
```

## Message Routing

The system supports the following message routing patterns:

1. User to User
2. User to Service
3. User to Group
4. Service to User
5. Service to Group
6. Group to Members

Messages are stored in both the sender's outbound and the recipient's inbound folders, making it easy to track and retrieve messages.

## Authentication and Authorization

The MessageHub uses JWT authentication to secure the SignalR connection. You need to implement the IPermissionHandler interface to handle group membership and permissions.

## Folder Structure

The message store uses the following folder structure:

```
/service/{service name}/inbound/{date}/
/service/{service name}/outbound/{date}/
/group/{group name}/inbound/{date}/
/group/{group name}/outbound/{date}/
/user/{user name}/inbound/{date}/
/user/{user name}/outbound/{date}/
```

Each message is stored as a JSON file with the message ID as the filename.
