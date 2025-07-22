# Postmaster - Real-time Messaging System

A real-time messaging system built with C# SignalR backend and React TypeScript frontend, featuring runtime extensibility and JWT authentication.

## Architecture

- **Backend**: ASP.NET Core 8.0 with SignalR for real-time communication
- **Frontend**: React 18 with TypeScript, XState for state management, and SignalR client
- **Authentication**: JWT-based authentication and authorization
- **Storage**: File-based message storage with routing capabilities

## Prerequisites

- .NET SDK 8.0 or later
- Node.js 18 or later
- npm or yarn

## Quick Start

### 1. Clone the Repository
```bash
git clone https://github.com/DevelApp-ai/Postmaster.git
cd Postmaster
```

### 2. Install Frontend Dependencies
```bash
npm run install:frontend
```

### 3. Build the Project
```bash
npm run build
```

### 4. Run Development Servers

#### Option A: Run Backend and Frontend Separately

**Terminal 1 - Backend:**
```bash
npm run dev:backend
```
The backend will start at `http://localhost:5000`

**Terminal 2 - Frontend:**
```bash
npm run dev:frontend
```
The frontend will start at `http://localhost:8080`

#### Option B: Using .NET CLI Directly

**Backend:**
```bash
cd backend/Postmaster.Api
dotnet run --urls="http://localhost:5000"
```

**Frontend:**
```bash
cd frontend
npm start
```

## Project Structure

```
Postmaster/
├── backend/                    # C# SignalR backend
│   └── Postmaster.Api/        # Main API project
│       ├── Program.cs         # Application entry point
│       ├── appsettings.json   # Configuration
│       ├── MessageHub.cs      # SignalR hub
│       ├── Message.cs         # Message model
│       ├── IMessageStore.cs   # Storage interface
│       ├── DiskMessageStore.cs # File-based storage
│       ├── IMessageHandler.cs # Message handling interface
│       ├── RuntimeMessageHandler.cs # Runtime extensible handler
│       ├── IPermissionHandler.cs # Permission interface
│       └── JwtPermissionHandler.cs # JWT-based permissions
├── frontend/                  # React TypeScript frontend
│   ├── src/
│   │   ├── index.tsx         # Application entry point
│   │   ├── index.html        # HTML template
│   │   └── css/main.css      # Styles
│   ├── PostmasterClient.tsx  # React component
│   ├── PostmasterService.ts  # Non-React service
│   ├── postmasterMachine.ts  # XState machine
│   ├── types.ts              # TypeScript definitions
│   ├── package.json          # Dependencies
│   ├── tsconfig.json         # TypeScript config
│   └── webpack.config.js     # Webpack config
├── docs/
│   └── README.md             # Detailed documentation
├── package.json              # Root package scripts
└── README.md                 # This file
```

## Development

### Backend Development

The backend is built with ASP.NET Core 8.0 and includes:

- **SignalR Hub** (`MessageHub.cs`): Handles real-time connections and message routing
- **Message Storage** (`DiskMessageStore.cs`): File-based storage with routing capabilities
- **JWT Authentication** (`JwtPermissionHandler.cs`): Token-based authentication
- **Runtime Extensibility** (`RuntimeMessageHandler.cs`): Pluggable message processors

#### Key Features:
- User-to-user messaging
- User-to-service messaging
- Group messaging with permissions
- Message persistence and retrieval
- JWT-based authentication
- Runtime message processor registration

### Frontend Development

The frontend is built with React 18 and TypeScript, featuring:

- **XState Integration**: State machine for connection and message management
- **SignalR Client**: Real-time communication with the backend
- **TypeScript**: Full type safety
- **Webpack**: Modern build system with development server

#### Key Features:
- Real-time message sending and receiving
- Connection state management
- Group messaging support
- Message history loading
- Responsive design

## Configuration

### Backend Configuration (`appsettings.json`)

```json
{
  "Jwt": {
    "Key": "your-secret-key-here",
    "Issuer": "PostmasterApi",
    "Audience": "PostmasterClient"
  },
  "ConnectionStrings": {
    "MessageStorePath": "./messages"
  }
}
```

### Frontend Configuration

The frontend automatically connects to the backend via the webpack dev server proxy configuration.

## API Endpoints

### SignalR Hub (`/hub`)

**Methods:**
- `SendToUser(recipientId, content)` - Send message to specific user
- `SendToService(serviceName, content)` - Send message to service
- `SendToGroup(groupName, content)` - Send message to group
- `JoinGroup(groupName)` - Join a group
- `LeaveGroup(groupName)` - Leave a group
- `LoadMessages(isInbound, fromDate?)` - Load message history
- `LoadUnreadMessages()` - Load unread messages
- `MarkAsRead(messageId)` - Mark message as read

**Events:**
- `MessageReceived` - New message received
- `GroupMessageReceived` - New group message received

## Message Routing

The system supports the following message routing patterns:

1. **User to User**: Direct messaging between users
2. **User to Service**: Messages from users to backend services
3. **User to Group**: Messages from users to groups
4. **Service to User**: Responses from services to users
5. **Service to Group**: Service announcements to groups
6. **Group to Members**: Group messages distributed to all members

## Authentication

The system uses JWT tokens for authentication. Tokens should be provided via:

1. **SignalR Connection**: As a query parameter `?access_token=<token>`
2. **HTTP Headers**: `Authorization: Bearer <token>`

## Storage

Messages are stored in a hierarchical folder structure:

```
messages/
├── user/{userId}/
│   ├── inbound/{date}/
│   └── outbound/{date}/
├── service/{serviceName}/
│   ├── inbound/{date}/
│   └── outbound/{date}/
└── group/{groupName}/
    ├── inbound/{date}/
    └── outbound/{date}/
```

## Runtime Extensibility

The system supports runtime registration of message processors:

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

## Troubleshooting

### Common Issues

1. **Backend fails to start**: Check that .NET SDK 8.0 is installed
2. **Frontend build errors**: Ensure Node.js 18+ and npm are installed
3. **SignalR connection fails**: Verify backend is running on port 5000
4. **CORS errors**: Check that frontend URL is in CORS policy

### Development Tips

1. **Hot Reload**: Both frontend and backend support hot reload during development
2. **Debugging**: Use browser dev tools for frontend, Visual Studio/VS Code for backend
3. **Logging**: Backend logs are available in the console, SignalR debug logging is enabled
4. **Message Storage**: Check the `./messages` directory for stored messages

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

