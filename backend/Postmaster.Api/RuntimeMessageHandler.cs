using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Postmaster.Api
{
    /// <summary>
    /// Implementation of IMessageHandler that supports runtime extensibility
    /// </summary>
    public class RuntimeMessageHandler : IMessageHandler
    {
        private readonly IMessageStore _messageStore;
        private readonly Dictionary<string, Func<Message, Task<Message>>> _processors = new Dictionary<string, Func<Message, Task<Message>>>();

        /// <summary>
        /// Initializes a new instance of the RuntimeMessageHandler class
        /// </summary>
        /// <param name="messageStore">The message store implementation</param>
        public RuntimeMessageHandler(IMessageStore messageStore)
        {
            _messageStore = messageStore ?? throw new ArgumentNullException(nameof(messageStore));
        }

        /// <inheritdoc />
        public async Task HandleIncomingMessageAsync(Message message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            // Route the message based on recipient type
            switch (message.RecipientType)
            {
                case RecipientType.User:
                    await _messageStore.RouteUserToUserMessageAsync(message.SenderId, message.RecipientId, message);
                    break;
                case RecipientType.Service:
                    await _messageStore.RouteUserToServiceMessageAsync(message.SenderId, message.RecipientId, message);
                    break;
                case RecipientType.Group:
                    await _messageStore.RouteUserToGroupMessageAsync(message.SenderId, message.RecipientId, message);
                    break;
                default:
                    throw new ArgumentException($"Unsupported recipient type: {message.RecipientType}");
            }
        }

        /// <inheritdoc />
        public async Task HandleOutgoingMessageAsync(Message message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            // Route the message based on sender type
            switch (message.SenderType)
            {
                case SenderType.Service:
                    if (message.RecipientType == RecipientType.User)
                    {
                        await _messageStore.RouteServiceToUserMessageAsync(message.SenderId, message.RecipientId, message);
                    }
                    else if (message.RecipientType == RecipientType.Group)
                    {
                        await _messageStore.RouteServiceToGroupMessageAsync(message.SenderId, message.RecipientId, message);
                    }
                    break;
                default:
                    throw new ArgumentException($"Unsupported sender type for outgoing message: {message.SenderType}");
            }
        }

        /// <inheritdoc />
        public async Task<Message> ProcessServiceMessageAsync(string serviceName, Message message)
        {
            if (string.IsNullOrEmpty(serviceName)) throw new ArgumentNullException(nameof(serviceName));
            if (message == null) throw new ArgumentNullException(nameof(message));

            // Check if there's a processor registered for this service
            if (_processors.TryGetValue(serviceName, out var processor))
            {
                // Process the message
                return await processor(message);
            }

            return null; // No processor found
        }

        /// <inheritdoc />
        public async Task BroadcastGroupMessageAsync(string groupName, Message message, IPermissionHandler permissionHandler)
        {
            if (string.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (permissionHandler == null) throw new ArgumentNullException(nameof(permissionHandler));

            await _messageStore.RouteGroupToMembersMessageAsync(groupName, message, permissionHandler);
        }

        /// <inheritdoc />
        public async Task DeliverUnreadMessagesAsync(string userId, IMessageStore messageStore)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            if (messageStore == null) throw new ArgumentNullException(nameof(messageStore));

            // Check if user has unread messages
            var hasUnread = await messageStore.UserHasUnreadMessagesAsync(userId);
            if (!hasUnread)
            {
                return;
            }

            // Get unread messages
            var messages = await messageStore.GetUserMessagesAsync(userId, true, unreadOnly: true);
            
            // Messages will be delivered by the caller (e.g., MessageHub)
        }

        /// <inheritdoc />
        public bool RegisterMessageProcessor(string serviceName, Func<Message, Task<Message>> processor)
        {
            if (string.IsNullOrEmpty(serviceName)) throw new ArgumentNullException(nameof(serviceName));
            if (processor == null) throw new ArgumentNullException(nameof(processor));

            // Add or update the processor
            _processors[serviceName] = processor;
            return true;
        }

        /// <inheritdoc />
        public bool UnregisterMessageProcessor(string serviceName)
        {
            if (string.IsNullOrEmpty(serviceName)) throw new ArgumentNullException(nameof(serviceName));

            return _processors.Remove(serviceName);
        }

        /// <inheritdoc />
        public IEnumerable<string> GetRegisteredServices()
        {
            return _processors.Keys;
        }
    }
}
