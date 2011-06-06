
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	public partial class PermissionsManager : System.Web.UI.UserControl {

		private string currentWiki = null;

		protected void Page_Load(object sender, EventArgs e) {
			currentWiki = Tools.DetectCurrentWiki();
		}

		/// <summary>
		/// Gets or sets the name of the resource of which to display the actions.
		/// </summary>
		public AclResources CurrentResourceType {
			get {
				return aclActionsSelector.CurrentResource;
			}
			set {
				aclActionsSelector.CurrentResource = value;
			}
		}

		/// <summary>
		/// Gets or sets the full (internal) name of the current resource.
		/// </summary>
		public string CurrentResourceName {
			get {
				return ViewState["CRN"] as string;
			}
			set {
				ViewState["CRN"] = value;
				PopulateSubjectsList();
			}
		}

		/// <summary>
		/// Gets or sets the current files provider (if CurrentResourceType is Directories).
		/// </summary>
		public string CurrentFilesProvider {
			get {
				return ViewState["CFP"] as string;
			}
			set {
				ViewState["CFP"] = value;
				PopulateSubjectsList();
			}
		}

		/// <summary>
		/// Gets the subjects for the current resource.
		/// </summary>
		/// <returns>The subjects.</returns>
		private SubjectInfo[] GetSubjects() {
			if(CurrentResourceType != AclResources.Namespaces && string.IsNullOrEmpty(CurrentResourceName) ||
				(CurrentResourceType == AclResources.Directories && string.IsNullOrEmpty(CurrentFilesProvider))) {
				return new SubjectInfo[0];
			}

			AuthReader authReader = new AuthReader(Collectors.CollectorsBox.GetSettingsProvider(currentWiki));
			switch(CurrentResourceType) {
				case AclResources.Namespaces:
					return authReader.RetrieveSubjectsForNamespace(Pages.FindNamespace(currentWiki, CurrentResourceName));
				case AclResources.Pages:
					return authReader.RetrieveSubjectsForPage(Pages.FindPage(currentWiki, CurrentResourceName));
				case AclResources.Directories:
					return authReader.RetrieveSubjectsForDirectory(
						Collectors.CollectorsBox.FilesProviderCollector.GetProvider(CurrentFilesProvider, currentWiki), CurrentResourceName);
				default:
					throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Populates the subjects list.
		/// </summary>
		private void PopulateSubjectsList() {
			SubjectInfo[] subjects = GetSubjects();

			// Sort: groups first, users second
			Array.Sort(subjects, delegate(SubjectInfo x, SubjectInfo y) {
				if(x.Type == y.Type) return x.Name.CompareTo(y.Name);
				else {
					if(x.Type == SubjectType.Group) return -1;
					else return 1;
				}
			});

			lstSubjects.Items.Clear();

			foreach(SubjectInfo subject in subjects) {
				bool isGroup = subject.Type == SubjectType.Group;
				lstSubjects.Items.Add(
					new ListItem((isGroup ? Properties.Messages.Group : Properties.Messages.User) +
						": " + subject.Name, (isGroup ? "G." : "U.") + subject.Name));
			}

			ClearPermissions();
		}

		protected void lstSubjects_SelectedIndexChanged(object sender, EventArgs e) {
			string selectedSubject = lstSubjects.SelectedValue;
			if(string.IsNullOrEmpty(selectedSubject)) ClearPermissions();
			else DisplaySubjectPermissions(selectedSubject.Substring(2), selectedSubject.StartsWith("G.") ? SubjectType.Group : SubjectType.User);
		}

		/// <summary>
		/// Displays the permissions for a subject in the actions matrix.
		/// </summary>
		/// <param name="subject">The subject.</param>
		/// <param name="type">The subject type.</param>
		private void DisplaySubjectPermissions(string subject, SubjectType type) {
			lblSelectedSubject.Text = subject;

			string[] grants = null;
			string[] denials = null;

			AuthReader authReader = new AuthReader(Collectors.CollectorsBox.GetSettingsProvider(currentWiki));
			switch(CurrentResourceType) {
				case AclResources.Namespaces:
					if(type == SubjectType.Group) {
						grants = authReader.RetrieveGrantsForNamespace(
							Users.FindUserGroup(currentWiki, subject),
							Pages.FindNamespace(currentWiki, CurrentResourceName));
						denials = authReader.RetrieveDenialsForNamespace(
							Users.FindUserGroup(currentWiki, subject),
							Pages.FindNamespace(currentWiki, CurrentResourceName));
					}
					else {
						grants = authReader.RetrieveGrantsForNamespace(
							Users.FindUser(currentWiki, subject),
							Pages.FindNamespace(currentWiki, CurrentResourceName));
						denials = authReader.RetrieveDenialsForNamespace(
							Users.FindUser(currentWiki, subject),
							Pages.FindNamespace(currentWiki, CurrentResourceName));
					}
					break;
				case AclResources.Pages:
					if(type == SubjectType.Group) {
						grants = authReader.RetrieveGrantsForPage(
							Users.FindUserGroup(currentWiki, subject),
							Pages.FindPage(currentWiki, CurrentResourceName));
						denials = authReader.RetrieveDenialsForPage(
							Users.FindUserGroup(currentWiki, subject),
							Pages.FindPage(currentWiki, CurrentResourceName));
					}
					else {
						grants = authReader.RetrieveGrantsForPage(
							Users.FindUser(currentWiki, subject),
							Pages.FindPage(currentWiki, CurrentResourceName));
						denials = authReader.RetrieveDenialsForPage(
							Users.FindUser(currentWiki, subject),
							Pages.FindPage(currentWiki, CurrentResourceName));
					}
					break;
				case AclResources.Directories:
					string directory = CurrentResourceName;
					IFilesStorageProviderV40 prov = Collectors.CollectorsBox.FilesProviderCollector.GetProvider(CurrentFilesProvider, currentWiki);
					if(type == SubjectType.Group) {
						grants = authReader.RetrieveGrantsForDirectory(
							Users.FindUserGroup(currentWiki, subject),
							prov, directory);
						denials = authReader.RetrieveDenialsForDirectory(
							Users.FindUserGroup(currentWiki, subject),
							prov, directory);
					}
					else {
						grants = authReader.RetrieveGrantsForDirectory(
							Users.FindUser(currentWiki, subject),
							prov, directory);
						denials = authReader.RetrieveDenialsForDirectory(
							Users.FindUser(currentWiki, subject),
							prov, directory);
					}
					break;
				default:
					throw new NotSupportedException();
			}

			aclActionsSelector.GrantedActions = grants;
			aclActionsSelector.DeniedActions = denials;

			btnSave.Enabled = true;
			btnRemove.Enabled = true;
		}

		/// <summary>
		/// Clears the actions matrix.
		/// </summary>
		private void ClearPermissions() {
			lblSelectedSubject.Text = "";

			aclActionsSelector.GrantedActions = new string[0];
			aclActionsSelector.DeniedActions = new string[0];

			btnSave.Enabled = false;
			btnRemove.Enabled = false;
		}

		/// <summary>
		/// Removes all the ACL entries for a subject.
		/// </summary>
		/// <param name="subject">The subject.</param>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
		private bool RemoveAllAclEntriesForNamespace(string subject, string nspace) {
			bool isGroup = lstSubjects.SelectedValue.StartsWith("G.");
			subject = subject.Substring(2);

			NamespaceInfo namespaceInfo = Pages.FindNamespace(currentWiki, nspace);

			AuthWriter authWriter = new AuthWriter(Collectors.CollectorsBox.GetSettingsProvider(currentWiki));

			if(isGroup) {
				return authWriter.RemoveEntriesForNamespace(
					Users.FindUserGroup(currentWiki, subject), namespaceInfo);
			}
			else {
				return authWriter.RemoveEntriesForNamespace(
					Users.FindUser(currentWiki, subject), namespaceInfo);
			}
		}

		/// <summary>
		/// Removes all the ACL entries for a subject.
		/// </summary>
		/// <param name="subject">The subject.</param>
		/// <param name="page">The page.</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
		private bool RemoveAllAclEntriesForPage(string subject, string page) {
			bool isGroup = lstSubjects.SelectedValue.StartsWith("G.");
			subject = subject.Substring(2);

			PageInfo currentPage = Pages.FindPage(currentWiki, page);

			AuthWriter authWriter = new AuthWriter(Collectors.CollectorsBox.GetSettingsProvider(currentWiki));

			if(isGroup) {
				return authWriter.RemoveEntriesForPage(
					Users.FindUserGroup(currentWiki, subject), currentPage);
			}
			else {
				return authWriter.RemoveEntriesForPage(
					Users.FindUser(currentWiki, subject), currentPage);
			}
		}

		/// <summary>
		/// Removes all the ACL entries for a subject.
		/// </summary>
		/// <param name="subject">The subject.</param>
		/// <param name="provider">The provider.</param>
		/// <param name="directory">The directory.</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
		private bool RemoveAllAclEntriesForDirectory(string subject, IFilesStorageProviderV40 provider, string directory) {
			bool isGroup = lstSubjects.SelectedValue.StartsWith("G.");
			subject = subject.Substring(2);

			AuthWriter authWriter = new AuthWriter(Collectors.CollectorsBox.GetSettingsProvider(currentWiki));

			if(isGroup) {
				return authWriter.RemoveEntriesForDirectory(
					Users.FindUserGroup(currentWiki, subject), provider, directory);
			}
			else {
				return authWriter.RemoveEntriesForDirectory(
					Users.FindUser(currentWiki, subject), provider, directory);
			}
		}

		/// <summary>
		/// Adds some ACL entries for a subject.
		/// </summary>
		/// <param name="subject">The subject.</param>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <param name="grants">The granted actions.</param>
		/// <param name="denials">The denied actions.</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
		private bool AddAclEntriesForNamespace(string subject, string nspace, string[] grants, string[] denials) {
			bool isGroup = subject.StartsWith("G.");
			subject = subject.Substring(2);

			NamespaceInfo namespaceInfo = Pages.FindNamespace(currentWiki, nspace);

			UserGroup group = null;
			UserInfo user = null;

			if(isGroup) group = Users.FindUserGroup(currentWiki, subject);
			else user = Users.FindUser(currentWiki, subject);

			AuthWriter authWriter = new AuthWriter(Collectors.CollectorsBox.GetSettingsProvider(currentWiki));

			foreach(string action in grants) {
				bool done = false;
				if(isGroup) {
					done = authWriter.SetPermissionForNamespace(AuthStatus.Grant,
						namespaceInfo, action, group);
				}
				else {
					done = authWriter.SetPermissionForNamespace(AuthStatus.Grant,
						namespaceInfo, action, user);
				}
				if(!done) return false;
			}

			foreach(string action in denials) {
				bool done = false;
				if(isGroup) {
					done = authWriter.SetPermissionForNamespace(AuthStatus.Deny,
						namespaceInfo, action, group);
				}
				else {
					done = authWriter.SetPermissionForNamespace(AuthStatus.Deny,
						namespaceInfo, action, user);
				}
				if(!done) return false;
			}

			return true;
		}

		/// <summary>
		/// Adds some ACL entries for a subject.
		/// </summary>
		/// <param name="subject">The subject.</param>
		/// <param name="page">The page.</param>
		/// <param name="grants">The granted actions.</param>
		/// <param name="denials">The denied actions.</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
		private bool AddAclEntriesForPage(string subject, string page, string[] grants, string[] denials) {
			bool isGroup = subject.StartsWith("G.");
			subject = subject.Substring(2);

			PageInfo currentPage = Pages.FindPage(currentWiki, page);

			UserGroup group = null;
			UserInfo user = null;

			if(isGroup) group = Users.FindUserGroup(currentWiki, subject);
			else user = Users.FindUser(currentWiki, subject);

			AuthWriter authWriter = new AuthWriter(Collectors.CollectorsBox.GetSettingsProvider(currentWiki));

			foreach(string action in grants) {
				bool done = false;
				if(isGroup) {
					done = authWriter.SetPermissionForPage(AuthStatus.Grant,
						currentPage, action, group);
				}
				else {
					done = authWriter.SetPermissionForPage(AuthStatus.Grant,
						currentPage, action, user);
				}
				if(!done) return false;
			}

			foreach(string action in denials) {
				bool done = false;
				if(isGroup) {
					done = authWriter.SetPermissionForPage(AuthStatus.Deny,
						currentPage, action, group);
				}
				else {
					done = authWriter.SetPermissionForPage(AuthStatus.Deny,
						currentPage, action, user);
				}
				if(!done) return false;
			}

			return true;
		}

		/// <summary>
		/// Adds some ACL entries for a subject.
		/// </summary>
		/// <param name="subject">The subject.</param>
		/// <param name="provider">The provider.</param>
		/// <param name="directory">The directory.</param>
		/// <param name="grants">The granted actions.</param>
		/// <param name="denials">The denies actions.</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
		private bool AddAclEntriesForDirectory(string subject, IFilesStorageProviderV40 provider, string directory, string[] grants, string[] denials) {
			bool isGroup = subject.StartsWith("G.");
			subject = subject.Substring(2);

			UserGroup group = null;
			UserInfo user = null;

			if(isGroup) group = Users.FindUserGroup(currentWiki, subject);
			else user = Users.FindUser(currentWiki, subject);

			AuthWriter authWriter = new AuthWriter(Collectors.CollectorsBox.GetSettingsProvider(currentWiki));

			foreach(string action in grants) {
				bool done = false;
				if(isGroup) {
					done = authWriter.SetPermissionForDirectory(AuthStatus.Grant,
						provider, directory, action, group);
				}
				else {
					done = authWriter.SetPermissionForDirectory(AuthStatus.Grant,
						provider, directory, action, user);
				}
				if(!done) return false;
			}

			foreach(string action in denials) {
				bool done = false;
				if(isGroup) {
					done = authWriter.SetPermissionForDirectory(AuthStatus.Deny,
						provider, directory, action, group);
				}
				else {
					done = authWriter.SetPermissionForDirectory(AuthStatus.Deny,
						provider, directory, action, user);
				}
				if(!done) return false;
			}

			return true;
		}

		protected void btnSave_Click(object sender, EventArgs e) {
			string subject = lstSubjects.SelectedValue;

			bool done = false;

			switch(CurrentResourceType) {
				case AclResources.Namespaces:
					// Remove old values, add new ones
					done = RemoveAllAclEntriesForNamespace(subject, CurrentResourceName);
					if(done) {
						done = AddAclEntriesForNamespace(subject, CurrentResourceName,
							aclActionsSelector.GrantedActions, aclActionsSelector.DeniedActions);
					}
					break;
				case AclResources.Pages:
					// Remove old values, add new ones
					done = RemoveAllAclEntriesForPage(subject, CurrentResourceName);
					if(done) {
						done = AddAclEntriesForPage(subject, CurrentResourceName,
							aclActionsSelector.GrantedActions, aclActionsSelector.DeniedActions);
					}
					break;
				case AclResources.Directories:
					// Remove old values, add new ones
					IFilesStorageProviderV40 prov = Collectors.CollectorsBox.FilesProviderCollector.GetProvider(CurrentFilesProvider, currentWiki);
					done = RemoveAllAclEntriesForDirectory(subject, prov, CurrentResourceName);
					if(done) {
						done = AddAclEntriesForDirectory(subject, prov, CurrentResourceName,
							aclActionsSelector.GrantedActions, aclActionsSelector.DeniedActions);
					}
					break;
				default:
					throw new NotSupportedException();
			}

			if(done) {
				PopulateSubjectsList();
			}
			else {
				lblSaveResult.CssClass = "resulterror";
				lblSaveResult.Text = Properties.Messages.CouldNotStorePermissions;
			}
		}

		protected void btnRemove_Click(object sender, EventArgs e) {
			string subject = lstSubjects.SelectedValue;

			bool done = false;

			switch(CurrentResourceType) {
				case AclResources.Namespaces:
					// Remove values
					done = RemoveAllAclEntriesForNamespace(subject, CurrentResourceName);
					break;
				case AclResources.Pages:
					// Remove values
					done = RemoveAllAclEntriesForPage(subject, CurrentResourceName);
					break;
				case AclResources.Directories:
					// Remove values
					done = RemoveAllAclEntriesForDirectory(subject,
						Collectors.CollectorsBox.FilesProviderCollector.GetProvider(CurrentFilesProvider, currentWiki),
						CurrentResourceName);
					break;
				default:
					throw new NotSupportedException();
			}

			if(done) {
				PopulateSubjectsList();
			}
			else {
				lblSaveResult.CssClass = "resulterror";
				lblSaveResult.Text = Properties.Messages.CouldNotStorePermissions;
			}
		}

		protected void btnSearch_Click(object sender, EventArgs e) {
			string subject = txtNewSubject.Text.Trim().ToLowerInvariant();

			SubjectInfo[] currentSubjects = GetSubjects();

			if(subject.Length > 0) {
				// Find all groups and users whose name starts with the specified string

				lstFoundSubjects.Items.Clear();

				foreach(UserGroup group in Users.GetUserGroups(currentWiki)) {
					if(group.Name.ToLowerInvariant().StartsWith(subject) &&
						!IsAlreadyPresent(group.Name, SubjectType.Group, currentSubjects)) {

						lstFoundSubjects.Items.Add(new ListItem(Properties.Messages.Group + ": " + group.Name, "G." + group.Name));
					}
				}

				foreach(UserInfo user in Users.GetUsers(currentWiki)) {
					if(user.Username.ToLowerInvariant().StartsWith(subject) &&
						!IsAlreadyPresent(user.Username, SubjectType.User, currentSubjects)) {

						lstFoundSubjects.Items.Add(new ListItem(Properties.Messages.User + ": " + user.Username, "U." + user.Username));
					}
				}

				btnAdd.Enabled = lstFoundSubjects.Items.Count > 0;
			}
		}

		/// <summary>
		/// Determines whether a subject is already in a list.
		/// </summary>
		/// <param name="subject">The subject to test.</param>
		/// <param name="type">The type of the subject.</param>
		/// <param name="allSubjects">The subject list.</param>
		/// <returns><c>true</c> if the subject is present in <b>allSubjects</b>, <c>false</c> otherwise.</returns>
		private bool IsAlreadyPresent(string subject, SubjectType type, SubjectInfo[] allSubjects) {
			foreach(SubjectInfo current in allSubjects) {
				if(current.Type == type && current.Name == subject) {
					return true;
				}
			}
			return false;
		}

		protected void btnAdd_Click(object sender, EventArgs e) {
			// Add the selected subject with full control deny, then select it in the main list

			string subject = lstFoundSubjects.SelectedValue.Substring(2);
			bool isGroup = lstFoundSubjects.SelectedValue.StartsWith("G.");

			bool done = false;

			AuthWriter authWriter = new AuthWriter(Collectors.CollectorsBox.GetSettingsProvider(currentWiki));

			switch(CurrentResourceType) {
				case AclResources.Namespaces:
					if(isGroup) {
						done = authWriter.SetPermissionForNamespace(AuthStatus.Deny,
							Pages.FindNamespace(currentWiki, CurrentResourceName), Actions.FullControl,
							Users.FindUserGroup(currentWiki, subject));
					}
					else {
						done = authWriter.SetPermissionForNamespace(AuthStatus.Deny,
							Pages.FindNamespace(currentWiki, CurrentResourceName), Actions.FullControl,
							Users.FindUser(currentWiki, subject));
					}
					break;
				case AclResources.Pages:
					if(isGroup) {
						done = authWriter.SetPermissionForPage(AuthStatus.Deny,
							Pages.FindPage(currentWiki, CurrentResourceName), Actions.FullControl,
							Users.FindUserGroup(currentWiki, subject));
					}
					else {
						done = authWriter.SetPermissionForPage(AuthStatus.Deny,
							Pages.FindPage(currentWiki, CurrentResourceName), Actions.FullControl,
							Users.FindUser(currentWiki, subject));
					}
					break;
				case AclResources.Directories:
					IFilesStorageProviderV40 prov = Collectors.CollectorsBox.FilesProviderCollector.GetProvider(CurrentFilesProvider, currentWiki);
					if(isGroup) {
						done = authWriter.SetPermissionForDirectory(AuthStatus.Deny,
							prov, CurrentResourceName, Actions.FullControl,
							Users.FindUserGroup(currentWiki, subject));
					}
					else {
						done = authWriter.SetPermissionForDirectory(AuthStatus.Deny,
							prov, CurrentResourceName, Actions.FullControl,
							Users.FindUser(currentWiki, subject));
					}
					break;
				default:
					throw new NotSupportedException();
			}

			if(done) {
				PopulateSubjectsList();

				// Select in main list and display permissions in actions matrix
				foreach(ListItem item in lstSubjects.Items) {
					if(item.Value == lstFoundSubjects.SelectedValue) {
						item.Selected = true;
						break;
					}
				}
				DisplaySubjectPermissions(subject, isGroup ? SubjectType.Group : SubjectType.User);

				txtNewSubject.Text = "";
				lstFoundSubjects.Items.Clear();
				btnAdd.Enabled = false;
			}
			else {
				lblAddResult.CssClass = "resulterror";
				lblAddResult.Text = Properties.Messages.CouldNotStorePermissions;
			}
		}

	}

}
