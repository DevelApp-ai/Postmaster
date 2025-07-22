using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Postmaster.Api
{
    /// <summary>
    /// Interface for handling messages in the Postmaster system
    /// </summary>
    public interface IMessageHandler
    {
        /// <summary>
        /// Handles an incoming message from a client
        /// </summary>
        /// <param name="message">The message to handle</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task HandleIncomingMessageAsync(Message message);

        /// <summary>
        /// Handles an outgoing message to a client
        /// </summary>
        /// <param name="message">The message to handle</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task HandleOutgoingMessageAsync(Message message);

        /// <summary>
        /// Processes a message for a specific service
        /// </summary>
        /// <param name="serviceName">The service name</param>
        /// <param name="message">The message to process</param>
        /// <returns>Task representing the asynchronous operation with optional response message</returns>
        Task<Message> ProcessServiceMessageAsync(string serviceName, Message message);

        /// <summary>
        /// Broadcasts a message to all members of a group
        /// </summary>
        /// <param name="groupName">The group name</param>
        /// <param name="message">The message to broadcast</param>
        /// <param name="permissionHandler">The permission handler to check group membership</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task BroadcastGroupMessageAsync(string groupName, Message message, IPermissionHandler permissionHandler);

        /// <summary>
        /// Delivers unread messages to a user
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="messageStore">The message store to retrieve messages from</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task DeliverUnreadMessagesAsync(string userId, IMessageStore messageStore);

        /// <summary>
        /// Registers a runtime message processor for a specific service
        /// </summary>
        /// <param name="serviceName">The service name</param>
        /// <param name="processor">The message processor function</param>
        /// <returns>True if registration was successful, otherwise false</returns>
        bool RegisterMessageProcessor(string serviceName, Func<Message, Task<Message>> processor);

        /// <summary>
        /// Unregisters a runtime message processor for a specific service
        /// </summary>
        /// <param name="serviceName">The service name</param>
        /// <returns>True if unregistration was successful, otherwise false</returns>
        bool UnregisterMessageProcessor(string serviceName);

        /// <summary>
        /// Gets all registered service names
        /// </summary>
        /// <returns>Collection of registered service names</returns>
        IEnumerable<string> GetRegisteredServices();
    }
}
