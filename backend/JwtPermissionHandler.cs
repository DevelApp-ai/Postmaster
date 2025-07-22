using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using AspNetPostmaster.Interface;

namespace JwtPermissionHandler
{
    /// <summary>
    /// Implementation of IPermissionHandler using JWT tokens for authentication and authorization
    /// </summary>
    public class JwtPermissionHandler : IPermissionHandler
    {
        private readonly string _secretKey;
        private readonly Dictionary<string, List<string>> _groupMemberships = new Dictionary<string, List<string>>();
        private readonly Dictionary<string, Dictionary<string, List<PermissionType>>> _userPermissions = new Dictionary<string, Dictionary<string, List<PermissionType>>>();

        /// <summary>
        /// Initializes a new instance of the JwtPermissionHandler class
        /// </summary>
        /// <param name="secretKey">Secret key for JWT token validation</param>
        public JwtPermissionHandler(string secretKey)
        {
            _secretKey = secretKey ?? throw new ArgumentNullException(nameof(secretKey));
        }

        /// <inheritdoc />
        public Task<bool> IsUserInGroupAsync(string userId, string groupName)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            if (string.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));

            if (_groupMemberships.TryGetValue(groupName, out var members))
            {
                return Task.FromResult(members.Contains(userId));
            }

            return Task.FromResult(false);
        }

        /// <inheritdoc />
        public Task<IEnumerable<string>> GetUserGroupsAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));

            var groups = new List<string>();

            foreach (var group in _groupMemberships)
            {
                if (group.Value.Contains(userId))
                {
                    groups.Add(group.Key);
                }
            }

            return Task.FromResult<IEnumerable<string>>(groups);
        }

        /// <inheritdoc />
        public Task<IEnumerable<string>> GetGroupMembersAsync(string groupName)
        {
            if (string.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));

            if (_groupMemberships.TryGetValue(groupName, out var members))
            {
                return Task.FromResult<IEnumerable<string>>(members);
            }

            return Task.FromResult<IEnumerable<string>>(new List<string>());
        }

        /// <inheritdoc />
        public Task AddUserToGroupAsync(string userId, string groupName)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            if (string.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));

            if (!_groupMemberships.TryGetValue(groupName, out var members))
            {
                members = new List<string>();
                _groupMemberships[groupName] = members;
            }

            if (!members.Contains(userId))
            {
                members.Add(userId);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task RemoveUserFromGroupAsync(string userId, string groupName)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            if (string.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));

            if (_groupMemberships.TryGetValue(groupName, out var members))
            {
                members.Remove(userId);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task CreateGroupAsync(string groupName, string creatorId)
        {
            if (string.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));
            if (string.IsNullOrEmpty(creatorId)) throw new ArgumentNullException(nameof(creatorId));

            if (!_groupMemberships.ContainsKey(groupName))
            {
                _groupMemberships[groupName] = new List<string> { creatorId };
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task DeleteGroupAsync(string groupName)
        {
            if (string.IsNullOrEmpty(groupName)) throw new ArgumentNullException(nameof(groupName));

            _groupMemberships.Remove(groupName);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<string> ValidateTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token)) throw new ArgumentNullException(nameof(token));

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_secretKey);
                
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var userId = jwtToken.Claims.First(x => x.Type == "nameid").Value;

                return Task.FromResult(userId);
            }
            catch
            {
                return Task.FromResult<string>(null);
            }
        }

        /// <inheritdoc />
        public Task<bool> HasPermissionAsync(string userId, string resourceName, PermissionType permissionType)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            if (string.IsNullOrEmpty(resourceName)) throw new ArgumentNullException(nameof(resourceName));

            if (_userPermissions.TryGetValue(userId, out var resources) &&
                resources.TryGetValue(resourceName, out var permissions))
            {
                return Task.FromResult(permissions.Contains(permissionType));
            }

            return Task.FromResult(false);
        }

        /// <summary>
        /// Grants a permission to a user for a specific resource
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="resourceName">The resource name</param>
        /// <param name="permissionType">The type of permission to grant</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task GrantPermissionAsync(string userId, string resourceName, PermissionType permissionType)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            if (string.IsNullOrEmpty(resourceName)) throw new ArgumentNullException(nameof(resourceName));

            if (!_userPermissions.TryGetValue(userId, out var resources))
            {
                resources = new Dictionary<string, List<PermissionType>>();
                _userPermissions[userId] = resources;
            }

            if (!resources.TryGetValue(resourceName, out var permissions))
            {
                permissions = new List<PermissionType>();
                resources[resourceName] = permissions;
            }

            if (!permissions.Contains(permissionType))
            {
                permissions.Add(permissionType);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Revokes a permission from a user for a specific resource
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="resourceName">The resource name</param>
        /// <param name="permissionType">The type of permission to revoke</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task RevokePermissionAsync(string userId, string resourceName, PermissionType permissionType)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            if (string.IsNullOrEmpty(resourceName)) throw new ArgumentNullException(nameof(resourceName));

            if (_userPermissions.TryGetValue(userId, out var resources) &&
                resources.TryGetValue(resourceName, out var permissions))
            {
                permissions.Remove(permissionType);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Generates a JWT token for a user
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="expireMinutes">Token expiration time in minutes</param>
        /// <returns>JWT token string</returns>
        public string GenerateToken(string userId, int expireMinutes = 60)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }),
                Expires = DateTime.UtcNow.AddMinutes(expireMinutes),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
