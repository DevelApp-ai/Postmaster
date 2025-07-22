using AspNetPostmaster.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DiskMessageStore
{
    /// <summary>
    /// Implementation of IMessageStore that stores messages on disk
    /// </summary>
    public class DiskMessageStore : IMessageStore
    {
        private readonly string _basePath;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the DiskMessageStore class
        /// </summary>
        /// <param name="basePath">Base path for message storage</param>
        public DiskMessageStore(string basePath)
        {
            _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
            
            // Create base directories if they don't exist
            Directory.CreateDirectory(Path.Combine(_basePath, "service"));
            Directory.CreateDirectory(Path.Combine(_basePath, "group"));
            Directory.CreateDirectory(Path.Combine(_basePath, "user"));
            
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <inheritdoc />
        public async Task StoreUserMessageAsync(string userId, Message message, bool isInbound)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            if (message == null) throw new ArgumentNullException(nameof(message));

            string direction = isInbound ? "inbound" : "outbound";
            string datePath = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string directoryPath = Path.Combine(_basePath, "user", userId, direction, datePath);
            
            await StoreMessageAsync(directoryPath, message);
        }

        /// <inheritdoc />
        public async Task StoreServiceMessageAsync(string serviceName, Message message, bool isInbound)
        {
            if (string.IsNullOrEmpty(serviceName)) throw new ArgumentNullException(nameof(serviceName));
            if (message == null) throw new ArgumentNullException(nameof(message));

            string direction = isInbound ? "inbound" : "outbound";
            string datePath = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string directoryPath = Path.Combine(_basePath, "service", serviceName, direction, datePath);
            
            await StoreMessageAsync(directoryPath, message);
        }

        /// <inheritdoc />
        public async Task StoreGroupMessageAsync(string groupName, Message message, bool isInbound)
        {
            if (string.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));
            if (message == null) throw new ArgumentNullException(nameof(message));

            string direction = isInbound ? "inbound" : "outbound";
            string datePath = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string directoryPath = Path.Combine(_basePath, "group", groupName, direction, datePath);
            
            await StoreMessageAsync(directoryPath, message);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Message>> GetUserMessagesAsync(string userId, bool isInbound, DateTime? fromDate = null, bool unreadOnly = false)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));

            string direction = isInbound ? "inbound" : "outbound";
            string basePath = Path.Combine(_basePath, "user", userId, direction);
            
            return await GetMessagesAsync(basePath, fromDate, unreadOnly);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Message>> GetServiceMessagesAsync(string serviceName, bool isInbound, DateTime? fromDate = null)
        {
            if (string.IsNullOrEmpty(serviceName)) throw new ArgumentNullException(nameof(serviceName));

            string direction = isInbound ? "inbound" : "outbound";
            string basePath = Path.Combine(_basePath, "service", serviceName, direction);
            
            return await GetMessagesAsync(basePath, fromDate);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Message>> GetGroupMessagesAsync(string groupName, bool isInbound, DateTime? fromDate = null)
        {
            if (string.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));

            string direction = isInbound ? "inbound" : "outbound";
            string basePath = Path.Combine(_basePath, "group", groupName, direction);
            
            return await GetMessagesAsync(basePath, fromDate);
        }

        /// <inheritdoc />
        public async Task MarkMessageAsReadAsync(string messageId)
        {
            if (string.IsNullOrEmpty(messageId)) throw new ArgumentNullException(nameof(messageId));

            // Search for the message in all directories
            var messagePath = await FindMessagePathAsync(messageId);
            if (messagePath == null)
            {
                throw new FileNotFoundException($"Message with ID {messageId} not found");
            }

            // Read the message
            var message = await ReadMessageAsync(messagePath);
            
            // Mark as read and save
            message.IsRead = true;
            await File.WriteAllTextAsync(messagePath, JsonSerializer.Serialize(message, _jsonOptions));
        }

        /// <inheritdoc />
        public async Task<bool> UserHasUnreadMessagesAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));

            var messages = await GetUserMessagesAsync(userId, true, unreadOnly: true);
            return messages.Any();
        }

        /// <inheritdoc />
        public async Task RouteUserToServiceMessageAsync(string userId, string serviceName, Message message)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            if (string.IsNullOrEmpty(serviceName)) throw new ArgumentNullException(nameof(serviceName));
            if (message == null) throw new ArgumentNullException(nameof(message));

            // Store in user's outbound
            await StoreUserMessageAsync(userId, message, false);
            
            // Store in service's inbound
            await StoreServiceMessageAsync(serviceName, message, true);
        }

        /// <inheritdoc />
        public async Task RouteServiceToUserMessageAsync(string serviceName, string userId, Message message)
        {
            if (string.IsNullOrEmpty(serviceName)) throw new ArgumentNullException(nameof(serviceName));
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            if (message == null) throw new ArgumentNullException(nameof(message));

            // Store in service's outbound
            await StoreServiceMessageAsync(serviceName, message, false);
            
            // Store in user's inbound
            await StoreUserMessageAsync(userId, message, true);
        }

        /// <inheritdoc />
        public async Task RouteUserToGroupMessageAsync(string userId, string groupName, Message message)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            if (string.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));
            if (message == null) throw new ArgumentNullException(nameof(message));

            // Store in user's outbound
            await StoreUserMessageAsync(userId, message, false);
            
            // Store in group's inbound
            await StoreGroupMessageAsync(groupName, message, true);
        }

        /// <inheritdoc />
        public async Task RouteServiceToGroupMessageAsync(string serviceName, string groupName, Message message)
        {
            if (string.IsNullOrEmpty(serviceName)) throw new ArgumentNullException(nameof(serviceName));
            if (string.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));
            if (message == null) throw new ArgumentNullException(nameof(message));

            // Store in service's outbound
            await StoreServiceMessageAsync(serviceName, message, false);
            
            // Store in group's inbound
            await StoreGroupMessageAsync(groupName, message, true);
        }

        /// <inheritdoc />
        public async Task RouteGroupToMembersMessageAsync(string groupName, Message message, IPermissionHandler permissionHandler)
        {
            if (string.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (permissionHandler == null) throw new ArgumentNullException(nameof(permissionHandler));

            // Get all members of the group
            var members = await permissionHandler.GetGroupMembersAsync(groupName);
            
            // Store in group's outbound
            await StoreGroupMessageAsync(groupName, message, false);
            
            // Store in each member's inbound
            foreach (var userId in members)
            {
                await StoreUserMessageAsync(userId, message, true);
            }
        }

        #region Private Helper Methods

        private async Task StoreMessageAsync(string directoryPath, Message message)
        {
            Directory.CreateDirectory(directoryPath);
            
            string filePath = Path.Combine(directoryPath, $"{message.Id}.json");
            string json = JsonSerializer.Serialize(message, _jsonOptions);
            
            await File.WriteAllTextAsync(filePath, json);
        }

        private async Task<IEnumerable<Message>> GetMessagesAsync(string basePath, DateTime? fromDate = null, bool unreadOnly = false)
        {
            var messages = new List<Message>();
            
            if (!Directory.Exists(basePath))
            {
                return messages;
            }

            // Get all date directories
            var dateDirs = Directory.GetDirectories(basePath);
            
            foreach (var dateDir in dateDirs)
            {
                // Skip directories before fromDate if specified
                if (fromDate.HasValue)
                {
                    var dirName = Path.GetFileName(dateDir);
                    if (DateTime.TryParse(dirName, out var dirDate) && dirDate < fromDate.Value.Date)
                    {
                        continue;
                    }
                }
                
                // Get all message files in the directory
                var messageFiles = Directory.GetFiles(dateDir, "*.json");
                
                foreach (var messageFile in messageFiles)
                {
                    var message = await ReadMessageAsync(messageFile);
                    
                    // Skip read messages if unreadOnly is true
                    if (unreadOnly && message.IsRead)
                    {
                        continue;
                    }
                    
                    messages.Add(message);
                }
            }
            
            return messages.OrderBy(m => m.Timestamp);
        }

        private async Task<Message> ReadMessageAsync(string filePath)
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<Message>(json, _jsonOptions);
        }

        private async Task<string> FindMessagePathAsync(string messageId)
        {
            // Search in all directories
            var rootDirs = new[] { "user", "service", "group" };
            
            foreach (var rootDir in rootDirs)
            {
                var rootPath = Path.Combine(_basePath, rootDir);
                if (!Directory.Exists(rootPath))
                {
                    continue;
                }
                
                var entityDirs = Directory.GetDirectories(rootPath);
                
                foreach (var entityDir in entityDirs)
                {
                    var directionDirs = new[] { "inbound", "outbound" };
                    
                    foreach (var direction in directionDirs)
                    {
                        var directionPath = Path.Combine(entityDir, direction);
                        if (!Directory.Exists(directionPath))
                        {
                            continue;
                        }
                        
                        var dateDirs = Directory.GetDirectories(directionPath);
                        
                        foreach (var dateDir in dateDirs)
                        {
                            var messagePath = Path.Combine(dateDir, $"{messageId}.json");
                            if (File.Exists(messagePath))
                            {
                                return messagePath;
                            }
                        }
                    }
                }
            }
            
            return null;
        }

        #endregion
    }
}
