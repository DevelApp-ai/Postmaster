using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AspNetPostmaster.Interface
{
    /// <summary>
    /// Interface for handling permissions and group membership in the Postmaster system
    /// </summary>
    public interface IPermissionHandler
    {
        /// <summary>
        /// Checks if a user is a member of a specific group
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="groupName">The group name</param>
        /// <returns>True if user is a member of the group, otherwise false</returns>
        Task<bool> IsUserInGroupAsync(string userId, string groupName);

        /// <summary>
        /// Gets all groups that a user is a member of
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <returns>Collection of group names</returns>
        Task<IEnumerable<string>> GetUserGroupsAsync(string userId);

        /// <summary>
        /// Gets all members of a specific group
        /// </summary>
        /// <param name="groupName">The group name</param>
        /// <returns>Collection of user identifiers</returns>
        Task<IEnumerable<string>> GetGroupMembersAsync(string groupName);

        /// <summary>
        /// Adds a user to a group
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="groupName">The group name</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task AddUserToGroupAsync(string userId, string groupName);

        /// <summary>
        /// Removes a user from a group
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="groupName">The group name</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task RemoveUserFromGroupAsync(string userId, string groupName);

        /// <summary>
        /// Creates a new group
        /// </summary>
        /// <param name="groupName">The group name</param>
        /// <param name="creatorId">The user identifier of the creator</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task CreateGroupAsync(string groupName, string creatorId);

        /// <summary>
        /// Deletes an existing group
        /// </summary>
        /// <param name="groupName">The group name</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task DeleteGroupAsync(string groupName);

        /// <summary>
        /// Validates a user's authentication token
        /// </summary>
        /// <param name="token">The authentication token</param>
        /// <returns>User identifier if token is valid, otherwise null</returns>
        Task<string> ValidateTokenAsync(string token);

        /// <summary>
        /// Checks if a user has permission to access a specific resource
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="resourceName">The resource name</param>
        /// <param name="permissionType">The type of permission required</param>
        /// <returns>True if user has the required permission, otherwise false</returns>
        Task<bool> HasPermissionAsync(string userId, string resourceName, PermissionType permissionType);
    }

    /// <summary>
    /// Defines the types of permissions that can be checked
    /// </summary>
    public enum PermissionType
    {
        Read,
        Write,
        Execute,
        Admin
    }
}
