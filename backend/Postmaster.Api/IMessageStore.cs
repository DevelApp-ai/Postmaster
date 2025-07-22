using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Postmaster.Api
{
    /// <summary>
    /// Interface for message storage and retrieval operations
    /// </summary>
    public interface IMessageStore
    {
        /// <summary>
        /// Stores a message in the specified direction (inbound/outbound) for a user
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="message">The message content</param>
        /// <param name="isInbound">True if message is inbound to user, false if outbound</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task StoreUserMessageAsync(string userId, Message message, bool isInbound);

        /// <summary>
        /// Stores a message in the specified direction (inbound/outbound) for a service
        /// </summary>
        /// <param name="serviceName">The service name</param>
        /// <param name="message">The message content</param>
        /// <param name="isInbound">True if message is inbound to service, false if outbound</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task StoreServiceMessageAsync(string serviceName, Message message, bool isInbound);

        /// <summary>
        /// Stores a message in the specified direction (inbound/outbound) for a group
        /// </summary>
        /// <param name="groupName">The group name</param>
        /// <param name="message">The message content</param>
        /// <param name="isInbound">True if message is inbound to group, false if outbound</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task StoreGroupMessageAsync(string groupName, Message message, bool isInbound);

        /// <summary>
        /// Retrieves messages for a user in the specified direction
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="isInbound">True to get inbound messages, false for outbound</param>
        /// <param name="fromDate">Optional date filter to get messages from</param>
        /// <param name="unreadOnly">True to get only unread messages</param>
        /// <returns>Collection of messages</returns>
        Task<IEnumerable<Message>> GetUserMessagesAsync(string userId, bool isInbound, DateTime? fromDate = null, bool unreadOnly = false);

        /// <summary>
        /// Retrieves messages for a service in the specified direction
        /// </summary>
        /// <param name="serviceName">The service name</param>
        /// <param name="isInbound">True to get inbound messages, false for outbound</param>
        /// <param name="fromDate">Optional date filter to get messages from</param>
        /// <returns>Collection of messages</returns>
        Task<IEnumerable<Message>> GetServiceMessagesAsync(string serviceName, bool isInbound, DateTime? fromDate = null);

        /// <summary>
        /// Retrieves messages for a group in the specified direction
        /// </summary>
        /// <param name="groupName">The group name</param>
        /// <param name="isInbound">True to get inbound messages, false for outbound</param>
        /// <param name="fromDate">Optional date filter to get messages from</param>
        /// <returns>Collection of messages</returns>
        Task<IEnumerable<Message>> GetGroupMessagesAsync(string groupName, bool isInbound, DateTime? fromDate = null);

        /// <summary>
        /// Marks a message as read
        /// </summary>
        /// <param name="messageId">The message identifier</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task MarkMessageAsReadAsync(string messageId);

        /// <summary>
        /// Checks if a user has unread messages
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <returns>True if user has unread messages, otherwise false</returns>
        Task<bool> UserHasUnreadMessagesAsync(string userId);

        /// <summary>
        /// Routes a message from a user to another user
        /// </summary>
        /// <param name="senderId">The sender user identifier</param>
        /// <param name="recipientId">The recipient user identifier</param>
        /// <param name="message">The message to route</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task RouteUserToUserMessageAsync(string senderId, string recipientId, Message message);

        /// <summary>
        /// Routes a message from a user to a service
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="serviceName">The service name</param>
        /// <param name="message">The message to route</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task RouteUserToServiceMessageAsync(string userId, string serviceName, Message message);

        /// <summary>
        /// Routes a message from a service to a user
        /// </summary>
        /// <param name="serviceName">The service name</param>
        /// <param name="userId">The user identifier</param>
        /// <param name="message">The message to route</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task RouteServiceToUserMessageAsync(string serviceName, string userId, Message message);

        /// <summary>
        /// Routes a message from a user to a group
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="groupName">The group name</param>
        /// <param name="message">The message to route</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task RouteUserToGroupMessageAsync(string userId, string groupName, Message message);

        /// <summary>
        /// Routes a message from a service to a group
        /// </summary>
        /// <param name="serviceName">The service name</param>
        /// <param name="groupName">The group name</param>
        /// <param name="message">The message to route</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task RouteServiceToGroupMessageAsync(string serviceName, string groupName, Message message);

        /// <summary>
        /// Routes messages from a group to its members
        /// </summary>
        /// <param name="groupName">The group name</param>
        /// <param name="message">The message to route</param>
        /// <param name="permissionHandler">The permission handler to check group membership</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task RouteGroupToMembersMessageAsync(string groupName, Message message, IPermissionHandler permissionHandler);
    }
}
