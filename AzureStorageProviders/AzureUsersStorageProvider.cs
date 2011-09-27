
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using Microsoft.WindowsAzure.StorageClient;

namespace ScrewTurn.Wiki.Plugins.AzureStorage {

	/// <summary>
	/// Implements a Users Storage Provider using Azure Table Storage.
	/// </summary>
	public class AzureUsersStorageProvider : IUsersStorageProviderV40 {

		private IHostV40 _host;
		private string _wiki;

		private TableServiceContext _context;

		#region IUsersStorageProviderV40 Members


		private Dictionary<string, UserEntity> _users;
		bool allUsersRetrieved = false;

		private UserEntity GetUserEntity(string wiki, string username) {
			if(_users == null) _users = new Dictionary<string, UserEntity>();

			if(!_users.ContainsKey(username)) {
				var userQuery = (from e in _context.CreateQuery<UserEntity>(UsersTable).AsTableServiceQuery()
								 where e.PartitionKey.Equals(wiki) && e.RowKey.Equals(username)
								 select e).AsTableServiceQuery();
				var entity = QueryHelper<UserEntity>.FirstOrDefault(userQuery);
				if(entity == null) return null;
				_users[username] = entity;
			}
			return _users[username];
		}

		private IList<UserEntity> GetUsersEntities(string wiki) {
			if(!(allUsersRetrieved && _users != null)) {
				allUsersRetrieved = true;
				var usersQuery = (from e in _context.CreateQuery<UserEntity>(UsersTable).AsTableServiceQuery()
								  where e.PartitionKey.Equals(wiki)
								  select e).AsTableServiceQuery();
				var entities = QueryHelper<UserEntity>.All(usersQuery);
				_users = new Dictionary<string, UserEntity>();
				foreach(UserEntity entity in entities) {
					_users[entity.RowKey] = entity;
				}
			}
			IList<UserEntity> usersEntities = new List<UserEntity>();
			foreach(var user in _users.Values) {
				usersEntities.Add(user);
			}
			return usersEntities;
		}

		/// <summary>
		/// Tests a Password for a User account.
		/// </summary>
		/// <param name="user">The User account.</param>
		/// <param name="password">The Password to test.</param>
		/// <returns>
		/// True if the Password is correct.
		/// </returns>
		/// <exception cref="ArgumentNullException">If <paramref name="user"/> or <paramref name="password"/> are <c>null</c>.</exception>
		public bool TestAccount(UserInfo user, string password) {
			if(user == null) throw new ArgumentNullException("user");
			if(password == null) throw new ArgumentNullException("password");

			return TryManualLogin(user.Username, password) != null;
		}

		/// <summary>
		/// Gets the complete list of Users.
		/// </summary>
		/// <returns>All the Users, sorted by username.</returns>
		public UserInfo[] GetUsers() {
			try {
				IList<UserEntity> usersEntities = GetUsersEntities(_wiki);

				List<UserInfo> users = new List<UserInfo>(usersEntities != null ? usersEntities.Count : 0);
				foreach(UserEntity userEntity in usersEntities) {
					users.Add(new UserInfo(userEntity.RowKey, userEntity.DisplayName, userEntity.Email, userEntity.Active, new DateTime(userEntity.CreationDateTime.Ticks, DateTimeKind.Utc), this) {
						Groups = userEntity.Groups != null ? userEntity.Groups.Split(new char[] {'|'}, StringSplitOptions.RemoveEmptyEntries) : new string[0]
					});
				}
				return users.ToArray();
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Adds a new User.
		/// </summary>
		/// <param name="username">The Username.</param>
		/// <param name="displayName">The display name (can be <c>null</c>).</param>
		/// <param name="password">The Password.</param>
		/// <param name="email">The Email address.</param>
		/// <param name="active">A value indicating whether the account is active.</param>
		/// <param name="dateTime">The Account creation Date/Time.</param>
		/// <returns>The correct <see cref="T:UserInfo"/> object or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="username"/>, <paramref name="password"/> or <paramref name="email"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="username"/>, <paramref name="password"/> or <paramref name="email"/> are empty.</exception>
		public UserInfo AddUser(string username, string displayName, string password, string email, bool active, DateTime dateTime) {
			if(username == null) throw new ArgumentNullException("username");
			if(username.Length == 0) throw new ArgumentException("Username cannot be empty", "username");
			if(password == null) throw new ArgumentNullException("password");
			if(password.Length == 0) throw new ArgumentException("Password cannot be empty", "password");
			if(email == null) throw new ArgumentNullException("email");
			if(email.Length == 0) throw new ArgumentException("Email cannot be empty", "email");

			try {
				// If a user with the given username altready exists return null
				if(GetUserEntity(_wiki, username) != null) return null;

				// If a user with the given email already exists return null
				if(GetUserByEmail(email) != null) return null;

				UserEntity userEntity = new UserEntity() {
					PartitionKey = _wiki,
					RowKey = username,
					DisplayName = displayName,
					Password = Hash.Compute(password),
					Email = email,
					Active = active,
					CreationDateTime = dateTime
				};
				_context.AddObject(UsersTable, userEntity);
				_context.SaveChangesStandard();

				_users = null;

				return new UserInfo(username, displayName, email, active, dateTime, this);
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Modifies a User.
		/// </summary>
		/// <param name="user">The Username of the user to modify.</param>
		/// <param name="newDisplayName">The new display name (can be <c>null</c>).</param>
		/// <param name="newPassword">The new Password (<c>null</c> or blank to keep the current password).</param>
		/// <param name="newEmail">The new Email address.</param>
		/// <param name="newActive">A value indicating whether the account is active.</param>
		/// <returns>The correct <see cref="T:UserInfo"/> object or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="user"/> or <paramref name="newEmail"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="newEmail"/> is empty.</exception>
		public UserInfo ModifyUser(UserInfo user, string newDisplayName, string newPassword, string newEmail, bool newActive) {
			if(user == null) throw new ArgumentNullException("user");
			if(newEmail == null) throw new ArgumentNullException("newEmail");
			if(newEmail.Length == 0) throw new ArgumentException("New Email cannot be empty", "newEmail");

			try {
				UserEntity oldUserEntity = GetUserEntity(_wiki, user.Username);
				// If the user does not exists return null
				if(oldUserEntity == null) return null;

				oldUserEntity.DisplayName = newDisplayName;
				oldUserEntity.Password = string.IsNullOrEmpty(newPassword) ? oldUserEntity.Password : Hash.Compute(newPassword);
				oldUserEntity.Email = newEmail;
				oldUserEntity.Active = newActive;

				_context.UpdateObject(oldUserEntity);
				_context.SaveChangesStandard();

				_users = null;

				return new UserInfo(user.Username, newDisplayName, newEmail, newActive, user.DateTime, this) {
					Groups = user.Groups
				};
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Removes a User.
		/// </summary>
		/// <param name="user">The User to remove.</param>
		/// <returns>True if the User has been removed successfully.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="user"/> is <c>null</c>.</exception>
		public bool RemoveUser(UserInfo user) {
			if(user == null) throw new ArgumentNullException("user");

			try {
				UserEntity userEntity = GetUserEntity(_wiki, user.Username);
				// If the user does not exists return false;
				if(userEntity == null) return false;

				IList<UserDataEntity> userDataEntities = GetUserDataEntities(_wiki, user.Username);
				foreach(UserDataEntity userDataEntity in userDataEntities) {
					_context.DeleteObject(userDataEntity);
				}
				_context.SaveChangesStandard();

				_context.DeleteObject(userEntity);
				_context.SaveChangesStandard();

				_users = null;

				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		private Dictionary<string, UserGroupEntity> _userGroups;
		bool allGroupsRetrieved = false;

		private UserGroupEntity GetUserGroupEntity(string wiki, string groupName) {
			if(_userGroups == null) _userGroups = new Dictionary<string, UserGroupEntity>();

			if(!_userGroups.ContainsKey(groupName)) {
				var userGroupQuery = (from e in _context.CreateQuery<UserGroupEntity>(UserGroupsTable).AsTableServiceQuery()
									  where e.PartitionKey.Equals(wiki) && e.RowKey.Equals(groupName)
									  select e).AsTableServiceQuery();
				var entity = QueryHelper<UserGroupEntity>.FirstOrDefault(userGroupQuery);
				if(entity == null) return null;
				_userGroups[groupName] = entity;
			}
			return _userGroups[groupName];
		}

		private IList<UserGroupEntity> GetUserGroupsEntities(string wiki) {
			if(!(allGroupsRetrieved && _userGroups != null)) {
				allGroupsRetrieved = true;
				var userGroupsQuery = (from e in _context.CreateQuery<UserGroupEntity>(UserGroupsTable).AsTableServiceQuery()
									   where e.PartitionKey.Equals(wiki)
									   select e).AsTableServiceQuery();
				var entities = QueryHelper<UserGroupEntity>.All(userGroupsQuery);

				_userGroups = new Dictionary<string, UserGroupEntity>();
				foreach(UserGroupEntity entity in entities) {
					_userGroups[entity.RowKey] = entity;
				}
			}
			IList<UserGroupEntity> userGroups = new List<UserGroupEntity>();
			foreach(var group in _userGroups.Values) {
				userGroups.Add(group);
			}
			return userGroups;
		}

		/// <summary>
		/// Gets all the user groups.
		/// </summary>
		/// <returns>All the groups, sorted by name.</returns>
		public UserGroup[] GetUserGroups() {
			try {
				IList<UserGroupEntity> userGroupsEntities = GetUserGroupsEntities(_wiki);

				List<UserGroup> userGroups = new List<UserGroup>(userGroupsEntities != null ? userGroupsEntities.Count : 0);
				UserInfo[] allUsers = GetUsers();
				foreach(UserGroupEntity userGroupEntity in userGroupsEntities) {
					userGroups.Add(new UserGroup(userGroupEntity.RowKey, userGroupEntity.Description, this) {
						Users = (from u in allUsers
								 where u.Groups.ToList().Contains(userGroupEntity.RowKey)
								 select u.Username).ToArray()
					});
				}
				return userGroups.ToArray();
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Adds a new user group.
		/// </summary>
		/// <param name="name">The name of the group.</param>
		/// <param name="description">The description of the group.</param>
		/// <returns>The correct <see cref="T:UserGroup"/> object or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> or <paramref name="description"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty.</exception>
		public UserGroup AddUserGroup(string name, string description) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");
			if(description == null) throw new ArgumentNullException("description");

			try {
				// If a group with the given name already exists return null
				if(GetUserGroupEntity(_wiki, name) != null) return null;

				UserGroupEntity userGroupEntity = new UserGroupEntity() {
					PartitionKey = _wiki,
					RowKey = name,
					Description = description
				};

				_context.AddObject(UserGroupsTable, userGroupEntity);
				_context.SaveChangesStandard();

				_userGroups = null;

				return new UserGroup(name, description, this);
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Modifies a user group.
		/// </summary>
		/// <param name="group">The group to modify.</param>
		/// <param name="description">The new description of the group.</param>
		/// <returns>The correct <see cref="T:UserGroup"/> object or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="group"/> or <paramref name="description"/> are <c>null</c>.</exception>
		public UserGroup ModifyUserGroup(UserGroup group, string description) {
			if(group == null) throw new ArgumentNullException("group");
			if(description == null) throw new ArgumentNullException("description");

			try {
				UserGroupEntity userGroupEntity = GetUserGroupEntity(_wiki, group.Name);
				// If the userGroup does not exists return null
				if(userGroupEntity == null) return null;

				userGroupEntity.Description = description;

				_context.UpdateObject(userGroupEntity);
				_context.SaveChangesStandard();

				_userGroups = null;

				return new UserGroup(group.Name, description, this) {
					Users = (from u in GetUsers()
							 where u.Groups.ToList().Contains(userGroupEntity.RowKey)
							 select u.Username).ToArray()
				};
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Removes a user group.
		/// </summary>
		/// <param name="group">The group to remove.</param>
		/// <returns><c>true</c> if the group is removed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="group"/> is <c>null</c>.</exception>
		public bool RemoveUserGroup(UserGroup group) {
			if(group == null) throw new ArgumentNullException("group");

			try {
				UserGroupEntity userGroupEntity = GetUserGroupEntity(_wiki, group.Name);
				// If the group does not exists return false
				if(userGroupEntity == null) return false;

				_context.DeleteObject(userGroupEntity);
				_context.SaveChangesStandard();

				_userGroups = null;

				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Sets the group memberships of a user account.
		/// </summary>
		/// <param name="user">The user account.</param>
		/// <param name="groups">The groups the user account is member of.</param>
		/// <returns>The correct <see cref="T:UserGroup"/> object or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="user"/> or <paramref name="groups"/> are <c>null</c>.</exception>
		public UserInfo SetUserMembership(UserInfo user, string[] groups) {
			if(user == null) throw new ArgumentNullException("user");
			if(groups == null) throw new ArgumentNullException("groups");

			try {
				UserEntity userEntity = GetUserEntity(_wiki, user.Username);
				// If the user does not exists return null
				if(userEntity == null) return null;

				// If one of the groups does not exists return null
				foreach(string group in groups) {
					if(GetUserGroupEntity(_wiki, group) == null) return null;
				}

				userEntity.Groups = string.Join("|", groups);
				_context.UpdateObject(userEntity);
				_context.SaveChangesStandard();

				_users = null;

				return new UserInfo(user.Username, user.DisplayName, user.Email, user.Active, user.DateTime, this) {
					Groups = groups
				};
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Tries to login a user directly through the provider.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <param name="password">The password.</param>
		/// <returns>The correct UserInfo object, or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="username"/> or <paramref name="password"/> are <c>null</c>.</exception>
		public UserInfo TryManualLogin(string username, string password) {
			if(username == null) throw new ArgumentNullException("username");
			if(password == null) throw new ArgumentNullException("password");

			// Shortcut
			if(username.Length == 0) return null;
			if(password.Length == 0) return null;

			try {
				UserEntity userEntity = GetUserEntity(_wiki, username);
				// If the user does not exists return null
				if(userEntity == null) return null;

				// If the user is NOT active return null
				if(!userEntity.Active) return null;

				if(userEntity.Password == Hash.Compute(password)) return new UserInfo(userEntity.RowKey, userEntity.DisplayName, userEntity.Email, userEntity.Active, new DateTime(userEntity.CreationDateTime.Ticks, DateTimeKind.Utc), this) {
					Groups = userEntity.Groups != null ? userEntity.Groups.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries) : new string[0]
				};

				// If the password is not correct return null
				return null;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Tries to login a user directly through the provider using
		/// the current HttpContext and without username/password.
		/// </summary>
		/// <param name="context">The current HttpContext.</param>
		/// <returns>The correct UserInfo object, or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="context"/> is <c>null</c>.</exception>
		public UserInfo TryAutoLogin(System.Web.HttpContext context) {
			if(context == null) throw new ArgumentNullException("context");

			return null;
		}

		/// <summary>
		/// Gets a user account.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <returns>The <see cref="T:UserInfo"/>, or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="username"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="username"/> is empty.</exception>
		public UserInfo GetUser(string username) {
			if(username == null) throw new ArgumentNullException("username");
			if(username.Length == 0) throw new ArgumentException("Username cannot be empty", "username");

			try {
				UserEntity userEntity = GetUserEntity(_wiki, username);
				// If the user does not exists return null
				if(userEntity == null) return null;

				return new UserInfo(userEntity.RowKey, userEntity.DisplayName, userEntity.Email, userEntity.Active, new DateTime(userEntity.CreationDateTime.Ticks, DateTimeKind.Utc), this) {
					Groups = userEntity.Groups != null ? userEntity.Groups.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries) : new string[0]
				};
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Gets a user account.
		/// </summary>
		/// <param name="email">The email address.</param>
		/// <returns>The first user found with the specified email address, or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="email"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="email"/> is empty.</exception>
		public UserInfo GetUserByEmail(string email) {
			if(email == null) throw new ArgumentNullException("email");
			if(email.Length == 0) throw new ArgumentException("Email cannot be empty", "email");

			try {
				var userQuery = (from e in _context.CreateQuery<UserEntity>(UsersTable).AsTableServiceQuery()
								 where e.PartitionKey.Equals(_wiki) && e.Email.Equals(email)
								 select e).AsTableServiceQuery();
				UserEntity userEntity = QueryHelper<UserEntity>.FirstOrDefault(userQuery);

				// if the user does not exists return null
				if(userEntity == null) return null;

				return new UserInfo(userEntity.RowKey, userEntity.DisplayName, userEntity.Email, userEntity.Active, new DateTime(userEntity.CreationDateTime.Ticks, DateTimeKind.Utc), this) {
					Groups = userEntity.Groups != null ? userEntity.Groups.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries) : new string[0]
				};
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Notifies the provider that a user has logged in through the authentication cookie.
		/// </summary>
		/// <param name="user">The user who has logged in.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="user"/> is <c>null</c>.</exception>
		public void NotifyCookieLogin(UserInfo user) {
			if(user == null) throw new ArgumentNullException("user");
			// Nothing to do
		}

		/// <summary>
		/// Notifies the provider that a user has logged out.
		/// </summary>
		/// <param name="user">The user who has logged out.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="user"/> is <c>null</c>.</exception>
		public void NotifyLogout(UserInfo user) {
			if(user == null) throw new ArgumentNullException("user");
			// Nothing to do
		}

		private UserDataEntity GetUserDataEntity(string _wiki, string username, string key) {
			var userDataQuery = (from e in _context.CreateQuery<UserDataEntity>(UserDataTable).AsTableServiceQuery()
								  where e.PartitionKey.Equals(_wiki + "|" + username) && e.RowKey.Equals(key)
								  select e).AsTableServiceQuery();
			return QueryHelper<UserDataEntity>.FirstOrDefault(userDataQuery);
		}

		private IList<UserDataEntity> GetUserDataEntities(string wiki, string username) {
			var userGroupsQuery = (from e in _context.CreateQuery<UserDataEntity>(UserDataTable).AsTableServiceQuery()
								   where e.PartitionKey.Equals(wiki + "|" + username)
								   select e).AsTableServiceQuery();
			return QueryHelper<UserDataEntity>.All(userGroupsQuery);
		}

		/// <summary>
		/// Stores a user data element, overwriting the previous one if present.
		/// </summary>
		/// <param name="user">The user the data belongs to.</param>
		/// <param name="key">The key of the data element (case insensitive).</param>
		/// <param name="value">The value of the data element, <c>null</c> for deleting the data.</param>
		/// <returns><c>true</c> if the data element is stored, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="user"/> or <paramref name="key"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="key"/> is empty.</exception>
		public bool StoreUserData(UserInfo user, string key, string value) {
			if(user == null) throw new ArgumentNullException("user");
			if(key == null) throw new ArgumentNullException("key");
			if(key.Length == 0) throw new ArgumentException("Key cannot be empty", "key");

			try {
				// If the user does not exists return false
				if(GetUserEntity(_wiki, user.Username) == null) return false;

				UserDataEntity userDataEntity = GetUserDataEntity(_wiki, user.Username, key);
				if(userDataEntity == null) {
					userDataEntity = new UserDataEntity() {
						PartitionKey = _wiki + "|" + user.Username,
						RowKey = key,
						Value = value
					};
					_context.AddObject(UserDataTable, userDataEntity);
				}
				else {
					userDataEntity.Value = value;
					_context.UpdateObject(userDataEntity);
				}
				_context.SaveChangesStandard();

				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Gets a user data element, if any.
		/// </summary>
		/// <param name="user">The user the data belongs to.</param>
		/// <param name="key">The key of the data element.</param>
		/// <returns>The value of the data element, or <c>null</c> if the element is not found.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="user"/> or <paramref name="key"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="key"/> is empty.</exception>
		public string RetrieveUserData(UserInfo user, string key) {
			if(user == null) throw new ArgumentNullException("user");
			if(key == null) throw new ArgumentNullException("key");
			if(key.Length == 0) throw new ArgumentException("Key cannot be empty", "key");

			try {
				UserDataEntity userDataEntity = GetUserDataEntity(_wiki, user.Username, key);
				// If the userData with the given key does not exists return null
				if(userDataEntity == null) return null;

				return userDataEntity.Value;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Retrieves all the user data elements for a user.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <returns>The user data elements (key->value).</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="user"/> is <c>null</c>.</exception>
		public IDictionary<string, string> RetrieveAllUserData(UserInfo user) {
			if(user == null) throw new ArgumentNullException("user");

			try {
				IList<UserDataEntity> userDataEntities = GetUserDataEntities(_wiki, user.Username);

				Dictionary<string, string> userData = new Dictionary<string, string>();
				foreach(UserDataEntity userDataEntity in userDataEntities) {
					userData.Add(userDataEntity.RowKey, userDataEntity.Value);
				}
				return userData;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Gets all the users that have the specified element in their data.
		/// </summary>
		/// <param name="key">The key of the data.</param>
		/// <returns>The users and the data.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="key"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="key"/> is empty.</exception>
		public IDictionary<UserInfo, string> GetUsersWithData(string key) {
			if(key == null) throw new ArgumentNullException("key");
			if(key.Length == 0) throw new ArgumentException("Key cannot be empty", "key");

			try {
				Dictionary<UserInfo, string> usersWithData = new Dictionary<UserInfo, string>();
				UserInfo[] allUsers = GetUsers();
				foreach(UserInfo user in allUsers) {
					string value = RetrieveUserData(user, key);
					if(value != null) {
						usersWithData.Add(user, value);
					}
				}

				return usersWithData;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Gets a value indicating whether user accounts are read-only.
		/// </summary>
		public bool UserAccountsReadOnly {
			get { return false; }
		}

		/// <summary>
		/// Gets a value indicating whether user groups are read-only. If so, the provider
		/// should support default user groups as defined in the wiki configuration.
		/// </summary>
		public bool UserGroupsReadOnly {
			get { return false; }
		}

		/// <summary>
		/// Gets a value indicating whether group membership is read-only (if <see cref="UserAccountsReadOnly"/>
		/// is <c>false</c>, then this property must be <c>false</c>). If this property is <c>true</c>, the provider
		/// should return membership data compatible with default user groups.
		/// </summary>
		public bool GroupMembershipReadOnly {
			get { return false; }
		}

		/// <summary>
		/// Gets a value indicating whether users' data is read-only.
		/// </summary>
		public bool UsersDataReadOnly {
			get { return false; }
		}

		#endregion

		#region IProviderV40 Members


		/// <summary>
		/// The users table name.
		/// </summary>
		public static readonly string UsersTable = "Users";

		/// <summary>
		/// The user groups table name.
		/// </summary>
		public static readonly string UserGroupsTable = "UserGroups";

		/// <summary>
		/// The user data table name.
		/// </summary>
		public static readonly string UserDataTable = "UserData";

		/// <summary>
		/// Gets the wiki that has been used to initialize the current instance of the provider.
		/// </summary>
		public string CurrentWiki {
			get { return _wiki; }
		}

		/// <summary>
		/// Initializes the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <param name="wiki">The wiki.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="host"/> or <paramref name="config"/> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <paramref name="config"/> is not valid or is incorrect.</exception>
		public void Init(IHostV40 host, string config, string wiki) {
			if(host == null) throw new ArgumentNullException("host");
			if(config == null) throw new ArgumentNullException("config");

			if(config == "") config = Config.GetConnectionString();

			_host = host;
			_wiki = string.IsNullOrEmpty(wiki) ? "root" : wiki.ToLowerInvariant();
			
			_context = TableStorage.GetContext(config);
		}

		/// <summary>
		/// Sets up the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="host"/> or <paramref name="config"/> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <paramref name="config"/> is not valid or is incorrect.</exception>
		public void SetUp(IHostV40 host, string config) {
			if(host == null) throw new ArgumentNullException("host");
			if(config == null) throw new ArgumentNullException("config");

			if(config == "") config = Config.GetConnectionString();

			TableStorage.CreateTable(config, UsersTable);
			TableStorage.CreateTable(config, UserGroupsTable);
			TableStorage.CreateTable(config, UserDataTable);
		}

		/// <summary>
		/// Gets the Information about the Provider.
		/// </summary>
		public ComponentInformation Information {
			get { return new ComponentInformation("Azure Table Storage Users Storage Provider", "Threeplicate Srl", "4.0.1.71", "http://www.screwturn.eu", null); }
		}

		/// <summary>
		/// Gets a brief summary of the configuration string format, in HTML. Returns <c>null</c> if no configuration is needed.
		/// </summary>
		public string ConfigHelpHtml {
			get { return ""; }
		}

		#endregion

		#region IDisposable Members

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose() {
			// Nothing todo
		}

		#endregion
	}

	internal class UserEntity : TableServiceEntity {
		// PartitionKey = wiki
		// RowKey = username

		public string DisplayName { get; set; }
		public string Password { get; set; }
		public string Email { get; set; }
		public bool Active { get; set; }
		public DateTime CreationDateTime { get; set; }
		public string Groups { get; set; }    // GroupsNames separated by '|' (pipe)
	}

	internal class UserGroupEntity : TableServiceEntity {
		// PartitionKey = wiki
		// RowKey = groupName

		public string Description { get; set; }
	}

	internal class UserDataEntity : TableServiceEntity {
		// PartitionKey = wiki|username
		// RowKey = key

		public string Value { get; set; }
	}
}
