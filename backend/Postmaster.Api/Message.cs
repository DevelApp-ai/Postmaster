using System;

namespace Postmaster.Api
{
    /// <summary>
    /// Represents a message in the Postmaster system
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Unique identifier for the message
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Content of the message
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Sender identifier (user, service, or group)
        /// </summary>
        public string SenderId { get; set; }

        /// <summary>
        /// Type of sender (user, service, or group)
        /// </summary>
        public SenderType SenderType { get; set; }

        /// <summary>
        /// Recipient identifier (user, service, or group)
        /// </summary>
        public string RecipientId { get; set; }

        /// <summary>
        /// Type of recipient (user, service, or group)
        /// </summary>
        public RecipientType RecipientType { get; set; }

        /// <summary>
        /// Timestamp when the message was created
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Indicates whether the message has been read by the recipient
        /// </summary>
        public bool IsRead { get; set; } = false;

        /// <summary>
        /// Optional metadata for the message
        /// </summary>
        public string Metadata { get; set; }
    }

    /// <summary>
    /// Defines the type of message sender
    /// </summary>
    public enum SenderType
    {
        User,
        Service,
        Group
    }

    /// <summary>
    /// Defines the type of message recipient
    /// </summary>
    public enum RecipientType
    {
        User,
        Service,
        Group
    }
}
