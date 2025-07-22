using AspNetPostmaster.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AspNetPostmaster.Hubs
{
    /// <summary>
    /// SignalR hub for handling real-time messaging
    /// </summary>
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class MessageHub : Hub
    {
        private readonly IMessageStore _messageStore;
        private readonly IMessageHandler _messageHandler;
        private readonly IPermissionHandler _permissionHandler;
        private static readonly ConcurrentDictionary<string, string> _userConnectionMap = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the MessageHub class
        /// </summary>
        /// <param name="messageStore">The message store implementation</param>
        /// <param name="messageHandler">The message handler implementation</param>
        /// <param name="permissionHandler">The permission handler implementation</param>
        public MessageHub(
            IMessageStore messageStore,
            IMessageHandler messageHandler,
            IPermissionHandler permissionHandler)
        {
            _messageStore = messageStore ?? throw new ArgumentNullException(nameof(messageStore));
            _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
            _permissionHandler = permissionHandler ?? throw new ArgumentNullException(nameof(permissionHandler));
        }

        /// <summary>
        /// Handles client connection
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User.Identity.Name;
            var connectionId = Context.ConnectionId;

            // Map the user to their connection
            _userConnectionMap.AddOrUpdate(userId, connectionId, (_, _) => connectionId);

            // Add user to their groups
            var userGroups = await _permissionHandler.GetUserGroupsAsync(userId);
            foreach (var group in userGroups)
            {
                await Groups.AddToGroupAsync(connectionId, group);
            }

            // Deliver any unread messages
            await _messageHandler.DeliverUnreadMessagesAsync(userId, _messageStore);

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Handles client disconnection
        /// </summary>
        /// <param name="exception">The exception that caused the disconnection, if any</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.User.Identity.Name;
            
            // Remove the user from the connection map
            _userConnectionMap.TryRemove(userId, out _);

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Sends a message to a specific user
        /// </summary>
        /// <param name="recipientId">The recipient user identifier</param>
        /// <param name="content">The message content</param>
        /// <returns>Task representing the asynchronous operation</returns>
        [Authorize]
        public async Task SendToUser(string recipientId, string content)
        {
            var senderId = Context.User.Identity.Name;
            
            // Create the message
            var message = new Message
            {
                SenderId = senderId,
                SenderType = SenderType.User,
                RecipientId = recipientId,
                RecipientType = RecipientType.User,
                Content = content
            };

            // Route the message
            await _messageStore.RouteUserToUserMessageAsync(senderId, recipientId, message);

            // If the recipient is connected, send the message directly
            if (_userConnectionMap.TryGetValue(recipientId, out var connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveMessage", message);
            }
        }

        /// <summary>
        /// Sends a message to a service
        /// </summary>
        /// <param name="serviceName">The service name</param>
        /// <param name="content">The message content</param>
        /// <returns>Task representing the asynchronous operation with optional response message</returns>
        [Authorize]
        public async Task<Message> SendToService(string serviceName, string content)
        {
            var senderId = Context.User.Identity.Name;
            
            // Create the message
            var message = new Message
            {
                SenderId = senderId,
                SenderType = SenderType.User,
                RecipientId = serviceName,
                RecipientType = RecipientType.Service,
                Content = content
            };

            // Route the message
            await _messageStore.RouteUserToServiceMessageAsync(senderId, serviceName, message);

            // Process the message with the service handler
            var response = await _messageHandler.ProcessServiceMessageAsync(serviceName, message);
            
            // If there's a response, route it back to the user
            if (response != null)
            {
                await _messageStore.RouteServiceToUserMessageAsync(serviceName, senderId, response);
                
                // Send the response directly to the user
                await Clients.Caller.SendAsync("ReceiveMessage", response);
            }

            return response;
        }

        /// <summary>
        /// Sends a message to a group
        /// </summary>
        /// <param name="groupName">The group name</param>
        /// <param name="content">The message content</param>
        /// <returns>Task representing the asynchronous operation</returns>
        [Authorize]
        public async Task SendToGroup(string groupName, string content)
        {
            var senderId = Context.User.Identity.Name;
            
            // Check if user is a member of the group
            var isMember = await _permissionHandler.IsUserInGroupAsync(senderId, groupName);
            if (!isMember)
            {
                throw new HubException("You are not a member of this group");
            }
            
            // Create the message
            var message = new Message
            {
                SenderId = senderId,
                SenderType = SenderType.User,
                RecipientId = groupName,
                RecipientType = RecipientType.Group,
                Content = content
            };

            // Route the message
            await _messageStore.RouteUserToGroupMessageAsync(senderId, groupName, message);

            // Broadcast to the group
            await _messageHandler.BroadcastGroupMessageAsync(groupName, message, _permissionHandler);
            
            // Send to all connected clients in the group
            await Clients.Group(groupName).SendAsync("ReceiveGroupMessage", groupName, message);
        }

        /// <summary>
        /// Marks a message as read
        /// </summary>
        /// <param name="messageId">The message identifier</param>
        /// <returns>Task representing the asynchronous operation</returns>
        [Authorize]
        public async Task MarkAsRead(string messageId)
        {
            await _messageStore.MarkMessageAsReadAsync(messageId);
        }

        /// <summary>
        /// Gets unread messages for the current user
        /// </summary>
        /// <returns>Collection of unread messages</returns>
        [Authorize]
        public async Task<IEnumerable<Message>> GetUnreadMessages()
        {
            var userId = Context.User.Identity.Name;
            return await _messageStore.GetUserMessagesAsync(userId, true, unreadOnly: true);
        }

        /// <summary>
        /// Gets messages for the current user
        /// </summary>
        /// <param name="isInbound">True to get inbound messages, false for outbound</param>
        /// <param name="fromDate">Optional date filter to get messages from</param>
        /// <returns>Collection of messages</returns>
        [Authorize]
        public async Task<IEnumerable<Message>> GetUserMessages(bool isInbound, DateTime? fromDate = null)
        {
            var userId = Context.User.Identity.Name;
            return await _messageStore.GetUserMessagesAsync(userId, isInbound, fromDate);
        }
    }
}
