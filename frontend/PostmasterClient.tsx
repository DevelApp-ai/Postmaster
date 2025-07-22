import React, { useEffect, useState } from 'react';
import { useMachine } from '@xstate/react';
import { postmasterMachine } from './postmasterMachine';
import { Message, PostmasterConfig } from './types';

interface PostmasterProps {
  config: PostmasterConfig;
}

export const PostmasterClient: React.FC<PostmasterProps> = ({ config }) => {
  const [state, send] = useMachine(postmasterMachine);
  const [newMessage, setNewMessage] = useState('');
  const [recipient, setRecipient] = useState('');
  const [recipientType, setRecipientType] = useState<'user' | 'service' | 'group'>('user');

  // Connect to the hub when the component mounts
  useEffect(() => {
    const connectToHub = async () => {
      const token = await config.tokenProvider();
      send({ type: 'CONNECT', token });
    };

    connectToHub();

    // Disconnect when the component unmounts
    return () => {
      send({ type: 'DISCONNECT' });
    };
  }, [config, send]);

  // Load unread messages when connected
  useEffect(() => {
    if (state.matches('connected.idle')) {
      send({ type: 'LOAD_UNREAD_MESSAGES' });
    }
  }, [state.value, send]);

  // Notify on connection status changes
  useEffect(() => {
    if (state.matches('connected') && config.onConnected) {
      config.onConnected();
    }
    if (state.matches('disconnected') && config.onDisconnected) {
      config.onDisconnected();
    }
    if (state.context.error && config.onError) {
      config.onError(new Error(state.context.error));
    }
  }, [state.value, state.context.error, config]);

  // Notify on new messages
  useEffect(() => {
    if (config.onMessageReceived && state.context.messages.length > 0) {
      const latestMessage = state.context.messages[state.context.messages.length - 1];
      config.onMessageReceived(latestMessage);
    }
  }, [state.context.messages, config]);

  const handleSendMessage = () => {
    if (!newMessage || !recipient) return;

    switch (recipientType) {
      case 'user':
        send({ type: 'SEND_TO_USER', recipientId: recipient, content: newMessage });
        break;
      case 'service':
        send({ type: 'SEND_TO_SERVICE', serviceName: recipient, content: newMessage });
        break;
      case 'group':
        send({ type: 'SEND_TO_GROUP', groupName: recipient, content: newMessage });
        break;
    }

    setNewMessage('');
  };

  const handleMarkAsRead = (messageId: string) => {
    send({ type: 'MARK_AS_READ', messageId });
  };

  const handleLoadMessages = (isInbound: boolean) => {
    send({ type: 'LOAD_MESSAGES', isInbound });
  };

  return (
    <div className="postmaster-client">
      <div className="connection-status">
        Status: {state.value.toString()}
        {state.context.error && <div className="error">Error: {state.context.error}</div>}
      </div>

      <div className="message-composer">
        <select 
          value={recipientType} 
          onChange={(e) => setRecipientType(e.target.value as 'user' | 'service' | 'group')}
        >
          <option value="user">User</option>
          <option value="service">Service</option>
          <option value="group">Group</option>
        </select>
        <input
          type="text"
          placeholder="Recipient ID"
          value={recipient}
          onChange={(e) => setRecipient(e.target.value)}
        />
        <textarea
          placeholder="Type your message..."
          value={newMessage}
          onChange={(e) => setNewMessage(e.target.value)}
        />
        <button onClick={handleSendMessage} disabled={!state.matches('connected')}>
          Send
        </button>
      </div>

      <div className="message-controls">
        <button onClick={() => handleLoadMessages(true)} disabled={!state.matches('connected')}>
          Load Inbound
        </button>
        <button onClick={() => handleLoadMessages(false)} disabled={!state.matches('connected')}>
          Load Outbound
        </button>
        <button onClick={() => send({ type: 'LOAD_UNREAD_MESSAGES' })} disabled={!state.matches('connected')}>
          Load Unread ({state.context.unreadCount})
        </button>
      </div>

      <div className="message-list">
        <h3>Messages</h3>
        {state.context.messages.length === 0 ? (
          <p>No messages</p>
        ) : (
          <ul>
            {state.context.messages.map((message: Message) => (
              <li key={message.id} className={message.isRead ? 'read' : 'unread'}>
                <div className="message-header">
                  <span className="sender">{message.senderId}</span>
                  <span className="timestamp">{new Date(message.timestamp).toLocaleString()}</span>
                </div>
                <div className="message-content">{message.content}</div>
                {!message.isRead && (
                  <button onClick={() => handleMarkAsRead(message.id)}>Mark as Read</button>
                )}
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
};
