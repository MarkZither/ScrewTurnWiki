using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki.AclEngine;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Utility class for checking permissions and authorizations.
	/// </summary>
	/// <remarks>All the methods in this class implement a security bypass for the <i>admin</i> user.</remarks>
	public static class AuthChecker {

		/// <summary>
		/// Gets the settings storage provider.
		/// </summary>
		private static ISettingsStorageProviderV30 SettingsProvider {
			get { return Collectors.SettingsProvider; }
		}

		/// <summary>
		/// Checks whether an action is allowed for the global resources.
		/// </summary>
		/// <param name="action">The action the user is attempting to perform.</param>
		/// <param name="currentUser">The current user.</param>
		/// <param name="groups">The groups the user is member of.</param>
		/// <returns><c>true</c> if the action is allowed.</returns>
		public static bool CheckActionForGlobals(string action, string currentUser, string[] groups) {
			if(action == null) throw new ArgumentNullException("action");
			if(action.Length == 0) throw new ArgumentException("Action cannot be empty", "action");
			if(!AuthTools.IsValidAction(action, Actions.ForGlobals.All)) throw new ArgumentException("Invalid action", "action");

			if(currentUser == null) throw new ArgumentNullException("currentUser");
			if(currentUser.Length == 0) throw new ArgumentException("Current User cannot be empty", "currentUser");

			if(groups == null) throw new ArgumentNullException("groups");

			if(currentUser == "admin") return true;

			AclEntry[] entries = SettingsProvider.AclManager.RetrieveEntriesForResource(Actions.ForGlobals.ResourceMasterPrefix);
			Authorization auth = AclEvaluator.AuthorizeAction(Actions.ForGlobals.ResourceMasterPrefix, action,
				AuthTools.PrepareUsername(currentUser), AuthTools.PrepareGroups(groups), entries);

			return auth == Authorization.Granted;
		}

		/// <summary>
		/// Checks whether an action is allowed for a namespace.
		/// </summary>
		/// <param name="nspace">The current namespace (<c>null</c> for the root).</param>
		/// <param name="action">The action the user is attempting to perform.</param>
		/// <param name="currentUser">The current user.</param>
		/// <param name="groups">The groups the user is member of.</param>
		/// <returns><c>true</c> if the action is allowed, <c>false</c> otherwise.</returns>
		public static bool CheckActionForNamespace(NamespaceInfo nspace, string action, string currentUser, string[] groups) {
			if(action == null) throw new ArgumentNullException("action");
			if(action.Length == 0) throw new ArgumentException("Action cannot be empty", "action");
			if(!AuthTools.IsValidAction(action, Actions.ForNamespaces.All)) throw new ArgumentException("Invalid action", "action");

			if(currentUser == null) throw new ArgumentNullException("currentUser");
			if(currentUser.Length == 0) throw new ArgumentException("Current User cannot be empty", "currentUser");

			if(groups == null) throw new ArgumentNullException("groups");

			if(currentUser == "admin") return true;

			string namespaceName = nspace != null ? nspace.Name : "";

			AclEntry[] entries = SettingsProvider.AclManager.RetrieveEntriesForResource(
				Actions.ForNamespaces.ResourceMasterPrefix + namespaceName);

			Authorization auth = AclEvaluator.AuthorizeAction(Actions.ForNamespaces.ResourceMasterPrefix + namespaceName,
				action, AuthTools.PrepareUsername(currentUser), AuthTools.PrepareGroups(groups), entries);

			if(auth != Authorization.Unknown) return auth == Authorization.Granted;

			// Try local escalators
			string[] localEscalators = null;
			if(Actions.ForNamespaces.LocalEscalators.TryGetValue(action, out localEscalators)) {
				foreach(string localAction in localEscalators) {
					bool authorized = CheckActionForNamespace(nspace, localAction, currentUser, groups);
					if(authorized) return true;
				}
			}

			// Try root escalation
			if(nspace != null) {
				bool authorized = CheckActionForNamespace(null, action, currentUser, groups);
				if(authorized) return true;
			}

			// Try global escalators
			string[] globalEscalators = null;
			if(Actions.ForNamespaces.GlobalEscalators.TryGetValue(action, out globalEscalators)) {
				foreach(string globalAction in globalEscalators) {
					bool authorized = CheckActionForGlobals(globalAction, currentUser, groups);
					if(authorized) return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Checks whether an action is allowed for a page.
		/// </summary>
		/// <param name="page">The current page.</param>
		/// <param name="action">The action the user is attempting to perform.</param>
		/// <param name="currentUser">The current user.</param>
		/// <param name="groups">The groups the user is member of.</param>
		/// <returns><c>true</c> if the action is allowed, <c>false</c> otherwise.</returns>
		public static bool CheckActionForPage(PageInfo page, string action, string currentUser, string[] groups) {
			if(page == null) throw new ArgumentNullException("page");

			if(action == null) throw new ArgumentNullException("action");
			if(action.Length == 0) throw new ArgumentException("Action cannot be empty", "action");
			if(!AuthTools.IsValidAction(action, Actions.ForPages.All)) throw new ArgumentException("Invalid action", "action");

			if(currentUser == null) throw new ArgumentNullException("currentUser");
			if(currentUser.Length == 0) throw new ArgumentException("Current User cannot be empty", "currentUser");

			if(groups == null) throw new ArgumentNullException("groups");

			if(currentUser == "admin") return true;

			AclEntry[] entries = SettingsProvider.AclManager.RetrieveEntriesForResource(Actions.ForPages.ResourceMasterPrefix + page.FullName);
			Authorization auth = AclEvaluator.AuthorizeAction(Actions.ForPages.ResourceMasterPrefix + page.FullName, action,
				AuthTools.PrepareUsername(currentUser), AuthTools.PrepareGroups(groups), entries);

			if(auth != Authorization.Unknown) return auth == Authorization.Granted;

			// Try local escalators
			string[] localEscalators = null;
			if(Actions.ForPages.LocalEscalators.TryGetValue(action, out localEscalators)) {
				foreach(string localAction in localEscalators) {
					bool authorized = CheckActionForPage(page, localAction, currentUser, groups);
					if(authorized) return true;
				}
			}

			// Try namespace escalators
			string[] namespaceEscalators = null;
			string nsName = NameTools.GetNamespace(page.FullName);
			NamespaceInfo ns = string.IsNullOrEmpty(nsName) ? null : new NamespaceInfo(nsName, null, null);
			if(Actions.ForPages.NamespaceEscalators.TryGetValue(action, out namespaceEscalators)) {
				foreach(string namespaceAction in namespaceEscalators) {
					bool authorized = CheckActionForNamespace(ns, namespaceAction, currentUser, groups);
					if(authorized) return true;
				}
			}

			// Try global escalators
			string[] globalEscalators = null;
			if(Actions.ForPages.GlobalEscalators.TryGetValue(action, out globalEscalators)) {
				foreach(string globalAction in globalEscalators) {
					bool authorized = CheckActionForGlobals(globalAction, currentUser, groups);
					if(authorized) return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Checks whether an action is allowed for a directory.
		/// </summary>
		/// <param name="provider">The provider that manages the directory.</param>
		/// <param name="directory">The full path of the directory.</param>
		/// <param name="action">The action the user is attempting to perform.</param>
		/// <param name="currentUser">The current user.</param>
		/// <param name="groups">The groups the user is member of.</param>
		/// <returns><c>true</c> if the action is allowed, <c>false</c> otherwise.</returns>
		public static bool CheckActionForDirectory(IFilesStorageProviderV30 provider, string directory, string action, string currentUser, string[] groups) {
			if(provider == null) throw new ArgumentNullException("provider");

			if(directory == null) throw new ArgumentNullException("directory");
			if(directory.Length == 0) throw new ArgumentException("Directory cannot be empty", "directory");

			if(action == null) throw new ArgumentNullException("action");
			if(action.Length == 0) throw new ArgumentException("Action cannot be empty", "action");
			if(!AuthTools.IsValidAction(action, Actions.ForDirectories.All)) throw new ArgumentException("Invalid action", "action");

			if(currentUser == null) throw new ArgumentNullException("currentUser");
			if(currentUser.Length == 0) throw new ArgumentException("Current User cannot be empty", "currentUser");

			if(groups == null) throw new ArgumentNullException("groups");

			if(currentUser == "admin") return true;

			string resourceName = Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(provider, directory);

			AclEntry[] entries = SettingsProvider.AclManager.RetrieveEntriesForResource(resourceName);

			Authorization auth = AclEvaluator.AuthorizeAction(resourceName, action,
				AuthTools.PrepareUsername(currentUser), AuthTools.PrepareGroups(groups), entries);

			if(auth != Authorization.Unknown) return auth == Authorization.Granted;

			// Try local escalators
			string[] localEscalators = null;
			if(Actions.ForDirectories.LocalEscalators.TryGetValue(action, out localEscalators)) {
				foreach(string localAction in localEscalators) {
					bool authorized = CheckActionForDirectory(provider, directory, localAction, currentUser, groups);
					if(authorized) return true;
				}
			}

			// Try directory escalation (extract parent directory and check its permissions)
			// Path manipulation keeps the format used by the caller (leading and trailing slashes are preserved if appropriate)
			string trimmedDirectory = directory.Trim('/');
			if(trimmedDirectory.Length > 0) {
				int slashIndex = trimmedDirectory.LastIndexOf('/');
				string parentDir = "";
				if(slashIndex > 0) {
					// Navigate one level up, using the same slash format as the current one
					parentDir = (directory.StartsWith("/") ? "/" : "") +
						trimmedDirectory.Substring(0, slashIndex) + (directory.EndsWith("/") ? "/" : "");
				}
				else {
					// This is the root
					parentDir = directory.StartsWith("/") ? "/" : "";
				}
				bool authorized = CheckActionForDirectory(provider, parentDir, action, currentUser, groups);
				if(authorized) return true;
			}

			// Try global escalators
			string[] globalEscalators = null;
			if(Actions.ForDirectories.GlobalEscalators.TryGetValue(action, out globalEscalators)) {
				foreach(string globalAction in globalEscalators) {
					bool authorized = CheckActionForGlobals(globalAction, currentUser, groups);
					if(authorized) return true;
				}
			}

			return false;
		}

	}

}
