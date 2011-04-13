
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.AclEngine;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Utility class for reading permissions and authorizations.
	/// </summary>
	public class AuthReader {

		/// <summary>
		/// Gets the settings storage provider.
		/// </summary>
		private ISettingsStorageProviderV30 _settingsProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthReader"/> class.
		/// </summary>
		/// <param name="settingsProvider">The settings provider.</param>
		public AuthReader(ISettingsStorageProviderV30 settingsProvider) {
			_settingsProvider = settingsProvider;
		}

		/// <summary>
		/// Gets all the actions for global resources that are granted to a group.
		/// </summary>
		/// <param name="group">The user group.</param>
		/// <returns>The granted actions.</returns>
		public string[] RetrieveGrantsForGlobals(UserGroup group) {
			if(group == null) throw new ArgumentNullException("group");

			return RetrieveGrantsForGlobals(AuthTools.PrepareGroup(group.Name));
		}

		/// <summary>
		/// Gets all the actions for global resources that are granted to a user.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <returns>The granted actions.</returns>
		public string[] RetrieveGrantsForGlobals(UserInfo user) {
			if(user == null) throw new ArgumentNullException("user");

			return RetrieveGrantsForGlobals(AuthTools.PrepareUsername(user.Username));
		}

		/// <summary>
		/// Gets all the actions for global resources that are granted to a subject.
		/// </summary>
		/// <param name="subject">The subject.</param>
		/// <returns>The granted actions.</returns>
		private string[] RetrieveGrantsForGlobals(string subject) {
			AclEntry[] entries = _settingsProvider.AclManager.RetrieveEntriesForSubject(subject);

			List<string> result = new List<string>(entries.Length);
			foreach(AclEntry entry in entries) {
				if(entry.Value == Value.Grant && entry.Resource == Actions.ForGlobals.ResourceMasterPrefix) {
					result.Add(entry.Action);
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// Gets all the actions for global resources that are denied to a group.
		/// </summary>
		/// <param name="group">The user group.</param>
		/// <returns>The denied actions.</returns>
		public string[] RetrieveDenialsForGlobals(UserGroup group) {
			if(group == null) throw new ArgumentNullException("group");

			return RetrieveDenialsForGlobals(AuthTools.PrepareGroup(group.Name));
		}

		/// <summary>
		/// Gets all the actions for global resources that are denied to a user.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <returns>The denied actions.</returns>
		public string[] RetrieveDenialsForGlobals(UserInfo user) {
			if(user == null) throw new ArgumentNullException("user");

			return RetrieveDenialsForGlobals(AuthTools.PrepareUsername(user.Username));
		}

		/// <summary>
		/// Gets all the actions for global resources that are denied to a subject.
		/// </summary>
		/// <param name="subject">The subject.</param>
		/// <returns>The denied actions.</returns>
		private string[] RetrieveDenialsForGlobals(string subject) {
			AclEntry[] entries = _settingsProvider.AclManager.RetrieveEntriesForSubject(subject);

			List<string> result = new List<string>(entries.Length);
			foreach(AclEntry entry in entries) {
				if(entry.Value == Value.Deny && entry.Resource == Actions.ForGlobals.ResourceMasterPrefix) {
					result.Add(entry.Action);
				}
			}
			return result.ToArray();
		}

		/// <summary>
		/// Retrieves the subjects that have ACL entries set for a namespace.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The subjects.</returns>
		public SubjectInfo[] RetrieveSubjectsForNamespace(NamespaceInfo nspace) {
			string resourceName = Actions.ForNamespaces.ResourceMasterPrefix;
			if(nspace != null) resourceName += nspace.Name;

			AclEntry[] entries = _settingsProvider.AclManager.RetrieveEntriesForResource(resourceName);

			List<SubjectInfo> result = new List<SubjectInfo>(entries.Length);

			for(int i = 0; i < entries.Length; i++) {
				SubjectType type = AuthTools.IsGroup(entries[i].Subject) ? SubjectType.Group : SubjectType.User;

				// Remove the subject qualifier ('U.' or 'G.')
				string name = entries[i].Subject.Substring(2);

				if(result.Find(delegate(SubjectInfo x) { return x.Name == name && x.Type == type; }) == null) {
					result.Add(new SubjectInfo(name, type));
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// Gets all the actions for a namespace that are granted to a group.
		/// </summary>
		/// <param name="group">The user group.</param>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The granted actions.</returns>
		public string[] RetrieveGrantsForNamespace(UserGroup group, NamespaceInfo nspace) {
			if(group == null) throw new ArgumentNullException("group");

			return RetrieveGrantsForNamespace(AuthTools.PrepareGroup(group.Name), nspace);
		}

		/// <summary>
		/// Gets all the actions for a namespace that are granted to a user.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The granted actions.</returns>
		public string[] RetrieveGrantsForNamespace(UserInfo user, NamespaceInfo nspace) {
			if(user == null) throw new ArgumentNullException("user");

			return RetrieveGrantsForNamespace(AuthTools.PrepareUsername(user.Username), nspace);
		}

		/// <summary>
		/// Gets all the actions for a namespace that are granted to a subject.
		/// </summary>
		/// <param name="subject">The subject.</param>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The granted actions.</returns>
		private string[] RetrieveGrantsForNamespace(string subject, NamespaceInfo nspace) {
			string resourceName = Actions.ForNamespaces.ResourceMasterPrefix;
			if(nspace != null) resourceName += nspace.Name;

			AclEntry[] entries = _settingsProvider.AclManager.RetrieveEntriesForSubject(subject);

			List<string> result = new List<string>(entries.Length);

			foreach(AclEntry entry in entries) {
				if(entry.Value == Value.Grant && entry.Resource == resourceName) {
					result.Add(entry.Action);
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// Gets all the actions for a namespace that are denied to a group.
		/// </summary>
		/// <param name="group">The user group.</param>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The denied actions.</returns>
		public string[] RetrieveDenialsForNamespace(UserGroup group, NamespaceInfo nspace) {
			if(group == null) throw new ArgumentNullException("group");

			return RetrieveDenialsForNamespace(AuthTools.PrepareGroup(group.Name), nspace);
		}

		/// <summary>
		/// Gets all the actions for a namespace that are denied to a user.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The denied actions.</returns>
		public string[] RetrieveDenialsForNamespace(UserInfo user, NamespaceInfo nspace) {
			if(user == null) throw new ArgumentNullException("user");

			return RetrieveDenialsForNamespace(AuthTools.PrepareUsername(user.Username), nspace);
		}

		/// <summary>
		/// Gets all the actions for a namespace that are denied to a subject.
		/// </summary>
		/// <param name="subject">The subject.</param>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The denied actions.</returns>
		private string[] RetrieveDenialsForNamespace(string subject, NamespaceInfo nspace) {
			string resourceName = Actions.ForNamespaces.ResourceMasterPrefix;
			if(nspace != null) resourceName += nspace.Name;

			AclEntry[] entries = _settingsProvider.AclManager.RetrieveEntriesForSubject(subject);

			List<string> result = new List<string>(entries.Length);

			foreach(AclEntry entry in entries) {
				if(entry.Value == Value.Deny && entry.Resource == resourceName) {
					result.Add(entry.Action);
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// Retrieves the subjects that have ACL entries set for a page.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <returns>The subjects.</returns>
		public SubjectInfo[] RetrieveSubjectsForPage(PageInfo page) {
			if(page == null) throw new ArgumentNullException("page");

			AclEntry[] entries = _settingsProvider.AclManager.RetrieveEntriesForResource(Actions.ForPages.ResourceMasterPrefix + page.FullName);

			List<SubjectInfo> result = new List<SubjectInfo>(entries.Length);

			for(int i = 0; i < entries.Length; i++) {
				SubjectType type = AuthTools.IsGroup(entries[i].Subject) ? SubjectType.Group : SubjectType.User;

				// Remove the subject qualifier ('U.' or 'G.')
				string name = entries[i].Subject.Substring(2);

				if(result.Find(delegate(SubjectInfo x) { return x.Name == name && x.Type == type; }) == null) {
					result.Add(new SubjectInfo(name, type));
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// Gets all the actions for a page that are granted to a group.
		/// </summary>
		/// <param name="group">The user group.</param>
		/// <param name="page">The page.</param>
		/// <returns>The granted actions.</returns>
		public string[] RetrieveGrantsForPage(UserGroup group, PageInfo page) {
			if(group == null) throw new ArgumentNullException("group");

			return RetrieveGrantsForPage(AuthTools.PrepareGroup(group.Name), page);
		}

		/// <summary>
		/// Gets all the actions for a page that are granted to a user.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <param name="page">The page.</param>
		/// <returns>The granted actions.</returns>
		public string[] RetrieveGrantsForPage(UserInfo user, PageInfo page) {
			if(user == null) throw new ArgumentNullException("user");

			return RetrieveGrantsForPage(AuthTools.PrepareUsername(user.Username), page);
		}

		/// <summary>
		/// Gets all the actions for a page that are granted to a subject.
		/// </summary>
		/// <param name="subject">The subject.</param>
		/// <param name="page">The page.</param>
		/// <returns>The granted actions.</returns>
		private string[] RetrieveGrantsForPage(string subject, PageInfo page) {
			if(page == null) throw new ArgumentNullException("page");

			string resourceName = Actions.ForPages.ResourceMasterPrefix + page.FullName;

			AclEntry[] entries = _settingsProvider.AclManager.RetrieveEntriesForSubject(subject);

			List<string> result = new List<string>(entries.Length);

			foreach(AclEntry entry in entries) {
				if(entry.Value == Value.Grant && entry.Resource == resourceName) {
					result.Add(entry.Action);
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// Gets all the actions for a page that are denied to a group.
		/// </summary>
		/// <param name="group">The user group.</param>
		/// <param name="page">The page.</param>
		/// <returns>The granted actions.</returns>
		public string[] RetrieveDenialsForPage(UserGroup group, PageInfo page) {
			if(group == null) throw new ArgumentNullException("group");

			return RetrieveDenialsForPage(AuthTools.PrepareGroup(group.Name), page);
		}

		/// <summary>
		/// Gets all the actions for a page that are denied to a user.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <param name="page">The page.</param>
		/// <returns>The granted actions.</returns>
		public string[] RetrieveDenialsForPage(UserInfo user, PageInfo page) {
			if(user == null) throw new ArgumentNullException("user");

			return RetrieveDenialsForPage(AuthTools.PrepareUsername(user.Username), page);
		}

		/// <summary>
		/// Gets all the actions for a page that are denied to a subject.
		/// </summary>
		/// <param name="subject">The subject.</param>
		/// <param name="page">The page.</param>
		/// <returns>The granted actions.</returns>
		private string[] RetrieveDenialsForPage(string subject, PageInfo page) {
			if(page == null) throw new ArgumentNullException("page");

			string resourceName = Actions.ForPages.ResourceMasterPrefix + page.FullName;

			AclEntry[] entries = _settingsProvider.AclManager.RetrieveEntriesForSubject(subject);

			List<string> result = new List<string>(entries.Length);

			foreach(AclEntry entry in entries) {
				if(entry.Value == Value.Deny && entry.Resource == resourceName) {
					result.Add(entry.Action);
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// Retrieves the subjects that have ACL entries set for a directory.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <param name="directory">The directory.</param>
		/// <returns>The subjects.</returns>
		public SubjectInfo[] RetrieveSubjectsForDirectory(IFilesStorageProviderV30 provider, string directory) {
			if(provider == null) throw new ArgumentNullException("provider");
			if(directory == null) throw new ArgumentNullException("directory");
			if(directory.Length == 0) throw new ArgumentException("Directory cannot be empty", "directory");

			AclEntry[] entries = _settingsProvider.AclManager.RetrieveEntriesForResource(Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(provider, directory));

			List<SubjectInfo> result = new List<SubjectInfo>(entries.Length);

			for(int i = 0; i < entries.Length; i++) {
				SubjectType type = AuthTools.IsGroup(entries[i].Subject) ? SubjectType.Group : SubjectType.User;

				// Remove the subject qualifier ('U.' or 'G.')
				string name = entries[i].Subject.Substring(2);

				if(result.Find(delegate(SubjectInfo x) { return x.Name == name && x.Type == type; }) == null) {
					result.Add(new SubjectInfo(name, type));
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// Gets all the actions for a directory that are granted to a group.
		/// </summary>
		/// <param name="group">The user group.</param>
		/// <param name="provider">The provider.</param>
		/// <param name="directory">The directory.</param>
		/// <returns>The granted actions.</returns>
		public string[] RetrieveGrantsForDirectory(UserGroup group, IFilesStorageProviderV30 provider, string directory) {
			if(group == null) throw new ArgumentNullException("group");

			return RetrieveGrantsForDirectory(AuthTools.PrepareGroup(group.Name), provider, directory);
		}

		/// <summary>
		/// Gets all the actions for a directory that are granted to a user.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <param name="provider">The provider.</param>
		/// <param name="directory">The directory.</param>
		/// <returns>The granted actions.</returns>
		public string[] RetrieveGrantsForDirectory(UserInfo user, IFilesStorageProviderV30 provider, string directory) {
			if(user == null) throw new ArgumentNullException("user");

			return RetrieveGrantsForDirectory(AuthTools.PrepareUsername(user.Username), provider, directory);
		}

		/// <summary>
		/// Gets all the actions for a directory that are granted to a subject.
		/// </summary>
		/// <param name="subject">The subject.</param>
		/// <param name="provider">The provider.</param>
		/// <param name="directory">The directory.</param>
		/// <returns>The granted actions.</returns>
		private string[] RetrieveGrantsForDirectory(string subject, IFilesStorageProviderV30 provider, string directory) {
			if(provider == null) throw new ArgumentNullException("provider");
			if(directory == null) throw new ArgumentNullException("directory");
			if(directory.Length == 0) throw new ArgumentException("Directory cannot be empty", "directory");

			string resourceName = Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(provider, directory);

			AclEntry[] entries = _settingsProvider.AclManager.RetrieveEntriesForSubject(subject);

			List<string> result = new List<string>(entries.Length);

			foreach(AclEntry entry in entries) {
				if(entry.Value == Value.Grant && entry.Resource == resourceName) {
					result.Add(entry.Action);
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// Gets all the actions for a directory that are denied to a group.
		/// </summary>
		/// <param name="group">The user group.</param>
		/// <param name="provider">The provider.</param>
		/// <param name="directory">The directory.</param>
		/// <returns>The denied actions.</returns>
		public string[] RetrieveDenialsForDirectory(UserGroup group, IFilesStorageProviderV30 provider, string directory) {
			if(group == null) throw new ArgumentNullException("group");

			return RetrieveDenialsForDirectory(AuthTools.PrepareGroup(group.Name), provider, directory);
		}

		/// <summary>
		/// Gets all the actions for a directory that are denied to a user.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <param name="provider">The provider.</param>
		/// <param name="directory">The directory.</param>
		/// <returns>The denied actions.</returns>
		public string[] RetrieveDenialsForDirectory(UserInfo user, IFilesStorageProviderV30 provider, string directory) {
			if(user == null) throw new ArgumentNullException("user");

			return RetrieveDenialsForDirectory(AuthTools.PrepareUsername(user.Username), provider, directory);
		}

		/// <summary>
		/// Gets all the actions for a directory that are denied to a subject.
		/// </summary>
		/// <param name="subject">The subject.</param>
		/// <param name="provider">The provider.</param>
		/// <param name="directory">The directory.</param>
		/// <returns>The denied actions.</returns>
		private string[] RetrieveDenialsForDirectory(string subject, IFilesStorageProviderV30 provider, string directory) {
			if(provider == null) throw new ArgumentNullException("provider");
			if(directory == null) throw new ArgumentNullException("directory");
			if(directory.Length == 0) throw new ArgumentException("Directory cannot be empty", "directory");

			string resourceName = Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(provider, directory);

			AclEntry[] entries = _settingsProvider.AclManager.RetrieveEntriesForSubject(subject);

			List<string> result = new List<string>(entries.Length);

			foreach(AclEntry entry in entries) {
				if(entry.Value == Value.Deny && entry.Resource == resourceName) {
					result.Add(entry.Action);
				}
			}

			return result.ToArray();
		}

	}

}
