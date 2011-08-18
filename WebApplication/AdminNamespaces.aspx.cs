﻿
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	public partial class AdminNamespaces : BasePage {

		private const string RootName = "&lt;root&gt;";
		private const string RootNameUnescaped = "-- root --";

		protected void Page_Load(object sender, EventArgs e) {
			AdminMaster.RedirectToLoginIfNeeded();

			if(!AdminMaster.CanManageNamespaces(SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames(DetectWiki()))) UrlTools.Redirect("AccessDenied.aspx");

			if(!Page.IsPostBack) {
				rptNamespaces.DataBind();
			}
		}

		protected void rptNamespaces_DataBinding(object sender, EventArgs e) {
			string currentWiki = DetectWiki();

			List<NamespaceInfo> namespaces = Pages.GetNamespaces(currentWiki);

			List<NamespaceRow> result = new List<NamespaceRow>(namespaces.Count);

			bool canSetPermissions = AdminMaster.CanManagePermissions(SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames(currentWiki));

			PageContent defaultPage = Pages.FindPage(currentWiki, Settings.GetDefaultPage(currentWiki));

			// Inject the root namespace as first entry, retrieving the default page in Settings
			result.Add(new NamespaceRow(new NamespaceInfo(RootName, defaultPage.Provider, defaultPage.FullName),
				Settings.GetTheme(currentWiki, null),
				Pages.GetPages(currentWiki, null).Count, Pages.GetCategories(currentWiki, null).Count,
				canSetPermissions, txtCurrentNamespace.Value == RootName));

			foreach(NamespaceInfo ns in namespaces) {
				result.Add(new NamespaceRow(ns, Settings.GetTheme(currentWiki, ns.Name),
					Pages.GetPages(currentWiki, ns).Count, Pages.GetCategories(currentWiki, ns).Count,
					canSetPermissions, txtCurrentNamespace.Value == ns.Name));
			}

			rptNamespaces.DataSource = result;
		}

		protected void rptNamespaces_ItemCommand(object sender, RepeaterCommandEventArgs e) {
			txtCurrentNamespace.Value = e.CommandArgument as string;

			string currentWiki = DetectWiki();

			NamespaceInfo nspace = txtCurrentNamespace.Value != RootName ?
				Pages.FindNamespace(currentWiki, txtCurrentNamespace.Value) : null;

			if(e.CommandName == "Select") {
				// rptNamespaces.DataBind(); Not needed because the list is hidden on select

				txtName.Enabled = false;
				txtName.Text = nspace != null ? nspace.Name : RootNameUnescaped;
				txtNewName.Text = "";
				cvName.Enabled = false;
				cvName2.Enabled = false;
				LoadDefaultPages();

				btnCreate.Visible = false;
				btnSave.Visible = true;
				btnDelete.Visible = true;
				// Cannot delete root namespace
				btnDelete.Enabled = nspace != null;
				// Cannot rename root namespace
				btnRename.Enabled = nspace != null;

				pnlList.Visible = false;
				pnlEditNamespace.Visible = true;

				lblResult.Text = "";
				lblResult.CssClass = "";

				string[] theme = Settings.GetTheme(currentWiki, nspace != null ? nspace.Name : null).Split(new char[] { '|' });

				providerThSelector.SelectedProvider = theme[0];
				providerThSelector.SelectedThemes = theme[1];
			}
			else if(e.CommandName == "Perms") {
				if(!AdminMaster.CanManagePermissions(SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames(currentWiki))) return;

				permissionsManager.CurrentResourceName = nspace != null ? nspace.Name : null;

				lblNamespaceName.Text = nspace != null ? nspace.Name : RootName;

				pnlList.Visible = false;
				pnlPermissions.Visible = true;

				lblResult.Text = "";
				lblResult.CssClass = "";
			}
		}

		protected void btnPublic_Click(object sender, EventArgs e) {
			string currentWiki = DetectWiki();

			NamespaceInfo nspace = Pages.FindNamespace(currentWiki, txtCurrentNamespace.Value);

			RemoveAllPermissions(nspace);

			AuthWriter authWriter = new AuthWriter(Collectors.CollectorsBox.GetSettingsProvider(currentWiki));

			// Set permissions
			authWriter.SetPermissionForNamespace(AuthStatus.Grant, nspace, Actions.FullControl,
				Users.FindUserGroup(currentWiki, Settings.GetAdministratorsGroup(currentWiki)));

			authWriter.SetPermissionForNamespace(AuthStatus.Grant, nspace, Actions.ForNamespaces.CreatePages,
				Users.FindUserGroup(currentWiki, Settings.GetUsersGroup(currentWiki)));
			authWriter.SetPermissionForNamespace(AuthStatus.Grant, nspace, Actions.ForNamespaces.ManageCategories,
				Users.FindUserGroup(currentWiki, Settings.GetUsersGroup(currentWiki)));
			authWriter.SetPermissionForNamespace(AuthStatus.Grant, nspace, Actions.ForNamespaces.PostDiscussion,
				Users.FindUserGroup(currentWiki, Settings.GetUsersGroup(currentWiki)));
			authWriter.SetPermissionForNamespace(AuthStatus.Grant, nspace, Actions.ForNamespaces.DownloadAttachments,
				Users.FindUserGroup(currentWiki, Settings.GetUsersGroup(currentWiki)));

			authWriter.SetPermissionForNamespace(AuthStatus.Grant, nspace, Actions.ForNamespaces.ModifyPages,
				Users.FindUserGroup(currentWiki, Settings.GetAnonymousGroup(currentWiki)));
			authWriter.SetPermissionForNamespace(AuthStatus.Grant, nspace, Actions.ForNamespaces.PostDiscussion,
				Users.FindUserGroup(currentWiki, Settings.GetAnonymousGroup(currentWiki)));
			authWriter.SetPermissionForNamespace(AuthStatus.Grant, nspace, Actions.ForNamespaces.DownloadAttachments,
				Users.FindUserGroup(currentWiki, Settings.GetAnonymousGroup(currentWiki)));

			RefreshPermissionsManager();
		}

		protected void btnNormal_Click(object sender, EventArgs e) {
			string currentWiki = DetectWiki();

			NamespaceInfo nspace = Pages.FindNamespace(currentWiki, txtCurrentNamespace.Value);

			RemoveAllPermissions(nspace);

			AuthWriter authWriter = new AuthWriter(Collectors.CollectorsBox.GetSettingsProvider(currentWiki));

			// Set permissions
			authWriter.SetPermissionForNamespace(AuthStatus.Grant, nspace, Actions.FullControl,
				Users.FindUserGroup(currentWiki, Settings.GetAdministratorsGroup(currentWiki)));

			authWriter.SetPermissionForNamespace(AuthStatus.Grant, nspace, Actions.ForNamespaces.CreatePages,
				Users.FindUserGroup(currentWiki, Settings.GetUsersGroup(currentWiki)));
			authWriter.SetPermissionForNamespace(AuthStatus.Grant, nspace, Actions.ForNamespaces.ManageCategories,
				Users.FindUserGroup(currentWiki, Settings.GetUsersGroup(currentWiki)));
			authWriter.SetPermissionForNamespace(AuthStatus.Grant, nspace, Actions.ForNamespaces.PostDiscussion,
				Users.FindUserGroup(currentWiki, Settings.GetUsersGroup(currentWiki)));
			authWriter.SetPermissionForNamespace(AuthStatus.Grant, nspace, Actions.ForNamespaces.DownloadAttachments,
				Users.FindUserGroup(currentWiki, Settings.GetUsersGroup(currentWiki)));

			authWriter.SetPermissionForNamespace(AuthStatus.Grant, nspace, Actions.ForNamespaces.ReadPages,
				Users.FindUserGroup(currentWiki, Settings.GetAnonymousGroup(currentWiki)));
			authWriter.SetPermissionForNamespace(AuthStatus.Grant, nspace, Actions.ForNamespaces.ReadDiscussion,
				Users.FindUserGroup(currentWiki, Settings.GetAnonymousGroup(currentWiki)));
			authWriter.SetPermissionForNamespace(AuthStatus.Grant, nspace, Actions.ForNamespaces.DownloadAttachments,
				Users.FindUserGroup(currentWiki, Settings.GetAnonymousGroup(currentWiki)));

			RefreshPermissionsManager();
		}

		protected void btnPrivate_Click(object sender, EventArgs e) {
			string currentWiki = DetectWiki();

			NamespaceInfo nspace = Pages.FindNamespace(currentWiki, txtCurrentNamespace.Value);

			RemoveAllPermissions(nspace);

			AuthWriter authWriter = new AuthWriter(Collectors.CollectorsBox.GetSettingsProvider(currentWiki));

			// Set permissions
			authWriter.SetPermissionForNamespace(AuthStatus.Grant, nspace, Actions.FullControl,
				Users.FindUserGroup(currentWiki, Settings.GetAdministratorsGroup(currentWiki)));

			authWriter.SetPermissionForNamespace(AuthStatus.Grant, nspace, Actions.ForNamespaces.CreatePages,
				Users.FindUserGroup(currentWiki, Settings.GetUsersGroup(currentWiki)));
			authWriter.SetPermissionForNamespace(AuthStatus.Grant, nspace, Actions.ForNamespaces.ManageCategories,
				Users.FindUserGroup(currentWiki, Settings.GetUsersGroup(currentWiki)));
			authWriter.SetPermissionForNamespace(AuthStatus.Grant, nspace, Actions.ForNamespaces.PostDiscussion,
				Users.FindUserGroup(currentWiki, Settings.GetUsersGroup(currentWiki)));
			authWriter.SetPermissionForNamespace(AuthStatus.Grant, nspace, Actions.ForNamespaces.DownloadAttachments,
				Users.FindUserGroup(currentWiki, Settings.GetUsersGroup(currentWiki)));

			RefreshPermissionsManager();
		}

		/// <summary>
		/// Refreshes the permissions manager.
		/// </summary>
		private void RefreshPermissionsManager() {
			string r = permissionsManager.CurrentResourceName;
			permissionsManager.CurrentResourceName = r;
		}

		/// <summary>
		/// Removes all the permissions for a namespace.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		private void RemoveAllPermissions(NamespaceInfo nspace) {
			string currentWiki = DetectWiki();

			AuthWriter authWriter = new AuthWriter(Collectors.CollectorsBox.GetSettingsProvider(currentWiki));
			authWriter.RemoveEntriesForNamespace(Users.FindUserGroup(currentWiki, Settings.GetAnonymousGroup(currentWiki)), nspace);
			authWriter.RemoveEntriesForNamespace(Users.FindUserGroup(currentWiki, Settings.GetUsersGroup(currentWiki)), nspace);
			authWriter.RemoveEntriesForNamespace(Users.FindUserGroup(currentWiki, Settings.GetAdministratorsGroup(currentWiki)), nspace);
		}

		protected void cvName_ServerValidate(object sender, ServerValidateEventArgs e) {
			e.IsValid = Pages.FindNamespace(DetectWiki(), txtName.Text) == null;
		}

		protected void cvName2_ServerValidate(object sender, ServerValidateEventArgs e) {
			e.IsValid = Pages.IsValidName(txtName.Text);
		}

		protected void btnCreate_Click(object sender, EventArgs e) {
			string currentWiki = DetectWiki();

			txtName.Text = txtName.Text.Trim();

			Page.Validate("namespace");
			if(!Page.IsValid) return;

			// Create new namespace and the default page (MainPage)
			bool done = Pages.CreateNamespace(currentWiki, txtName.Text,
				Collectors.CollectorsBox.PagesProviderCollector.GetProvider(providerSelector.SelectedProvider, currentWiki));

			if(done) {
				NamespaceInfo nspace = Pages.FindNamespace(currentWiki, txtName.Text);
				PageContent page = Pages.SetPageContent(currentWiki, nspace.Name, "MainPage", "Main Page", Log.SystemUsername,
														DateTime.Now, "", Defaults.MainPageContentForSubNamespace, new string[0], "", SaveMode.Normal);

				if(page != null) {
					done = Pages.SetNamespaceDefaultPage(currentWiki, nspace, page);

					if(done) {
						Settings.SetTheme(currentWiki, nspace.Name, providerThSelector.SelectedProvider + "|" + providerThSelector.SelectedThemes);

						if(done) {
							RefreshList();
							lblResult.CssClass = "resultok";
							lblResult.Text = Properties.Messages.NamespaceCreated;
							ReturnToList();
						}
						else {
							lblResult.CssClass = "resulterror";
							lblResult.Text = Properties.Messages.NamespaceCreatedCouldNotSetTheme;
						}
					}
					else {
						lblResult.CssClass = "resulterror";
						lblResult.Text = Properties.Messages.NamespaceCreatedCouldNotSetDefaultPage;
					}
				}
				else {
					lblResult.CssClass = "resulterror";
					lblResult.Text = Properties.Messages.NamespaceCreatedCouldNotStoreDefaultPageContent;
				}
			}
			else {
				lblResult.CssClass = "resulterror";
				lblResult.Text = Properties.Messages.CouldNotCreateNamespace;
			}
		}

		protected void btnSave_Click(object sender, EventArgs e) {
			// This can rarely occur
			if(string.IsNullOrEmpty(lstDefaultPage.SelectedValue)) return;
			
			string currentWiki = DetectWiki();

			NamespaceInfo nspace = txtCurrentNamespace.Value != RootName ?
				Pages.FindNamespace(currentWiki, txtCurrentNamespace.Value) : null;

			bool done = Pages.SetNamespaceDefaultPage(currentWiki, nspace, Pages.FindPage(currentWiki, lstDefaultPage.SelectedValue));

			if(done) {
				if(string.IsNullOrEmpty(providerThSelector.SelectedThemes)) done = false;
				
				if(done) {
					Settings.SetTheme(currentWiki, nspace != null ? nspace.Name : null, providerThSelector.SelectedProvider + "|" + providerThSelector.SelectedThemes);
					RefreshList();
					lblResult.CssClass = "resultok";
					lblResult.Text = Properties.Messages.NamespaceSaved;
					ReturnToList();
				}
				else {
					lblResult.CssClass = "resulterror";
					lblResult.Text = Properties.Messages.CouldNotSetNamespaceTheme;
				}
			}
			else {
				lblResult.CssClass = "resulterror";
				lblResult.Text = Properties.Messages.CouldNotSetNamespaceDefaultPage;
			}
		}

		protected void btnDelete_Click(object sender, EventArgs e) {
			pnlDelete.Visible = true;
		}

		protected void btnConfirmDeletion_Click(object sender, EventArgs e) {
			bool done = Pages.RemoveNamespace(DetectWiki(), Pages.FindNamespace(DetectWiki(), txtCurrentNamespace.Value));

			if(done) {
				RefreshList();
				lblResult.CssClass = "resultok";
				lblResult.Text = Properties.Messages.NamespaceDeleted;
				ReturnToList();
			}
			else {
				lblResult.CssClass = "resulterror";
				lblResult.Text = Properties.Messages.CouldNotDeleteNamespace;
			}
		}

		protected void btnCancel_Click(object sender, EventArgs e) {
			RefreshList();
			ReturnToList();
		}

		protected void btnBack_Click(object sender, EventArgs e) {
			RefreshList();
			ReturnToList();
		}

		protected void btnNewNamespace_Click(object sender, EventArgs e) {
			pnlList.Visible = false;
			pnlEditNamespace.Visible = true;
			lstDefaultPage.Enabled = false;
			lblDefaultPageInfo.Visible = true;
			btnRename.Enabled = false;

			lblResult.Text = "";
			lblResult.CssClass = "";
		}

		/// <summary>
		/// Refreshes the namespace list.
		/// </summary>
		private void RefreshList() {
			txtCurrentNamespace.Value = "";
			ResetEditor();
			rptNamespaces.DataBind();
		}

		/// <summary>
		/// Loads the available pages and selects the current one.
		/// </summary>
		private void LoadDefaultPages() {
			// Populate default page, if there is a namespace selected
			if(!string.IsNullOrEmpty(txtCurrentNamespace.Value)) {
				string currentWiki = DetectWiki();

				NamespaceInfo nspace = Pages.FindNamespace(
					currentWiki, txtCurrentNamespace.Value != RootName ? txtCurrentNamespace.Value : null);

				List<PageContent> pages = Pages.GetPages(currentWiki, nspace);

				string currentDefaultPage = nspace != null ? nspace.DefaultPageFullName : Settings.GetDefaultPage(currentWiki);

				lstDefaultPage.Items.Clear();
				foreach(PageContent page in pages) {
					ListItem item = new ListItem(NameTools.GetLocalName(page.FullName), page.FullName);
					if(page.FullName == currentDefaultPage) item.Selected = true;
					lstDefaultPage.Items.Add(item);
				}
			}
		}

		/// <summary>
		/// Resets the namespace editor.
		/// </summary>
		private void ResetEditor() {
			txtName.Text = "";
			txtName.Enabled = true;
			cvName.Enabled = true;
			cvName2.Enabled = true;
			providerThSelector.Enabled = true;
			providerThSelector.Reload();
			lstDefaultPage.Enabled = true;
			lstDefaultPage.Items.Clear();
			lblDefaultPageInfo.Visible = false;
			btnCreate.Visible = true;
			btnSave.Visible = false;
			btnDelete.Visible = false;
			btnDelete.Enabled = true;
			pnlDelete.Visible = false;
			btnRename.Enabled = true;
		}

		/// <summary>
		/// Returns to the group list.
		/// </summary>
		private void ReturnToList() {
			pnlEditNamespace.Visible = false;
			pnlPermissions.Visible = false;
			pnlList.Visible = true;
		}

		protected void cvNewName_ServerValidate(object sender, ServerValidateEventArgs e) {
			e.IsValid = Pages.FindNamespace(DetectWiki(), txtNewName.Text) == null;
		}

		protected void cvNewName2_ServerValidate(object sender, ServerValidateEventArgs e) {
			e.IsValid = Pages.IsValidName(txtNewName.Text);
		}

		protected void btnRename_Click(object sender, EventArgs e) {
			lblRenameResult.CssClass = "";
			lblRenameResult.Text = "";

			txtNewName.Text = txtNewName.Text.Trim();

			Page.Validate("rename");
			if(!Page.IsValid) return;

			string currentWiki = DetectWiki();

			NamespaceInfo nspace = Pages.FindNamespace(currentWiki, txtCurrentNamespace.Value);
			string theme = Settings.GetTheme(currentWiki, nspace.Name);

			if(Pages.RenameNamespace(currentWiki, nspace, txtNewName.Text)) {
				Settings.SetTheme(currentWiki, txtNewName.Text, theme);
				RefreshList();
				lblRenameResult.CssClass = "resultok";
				lblRenameResult.Text = Properties.Messages.NamespaceRenamed;
				ReturnToList();
			}
			else {
				lblRenameResult.CssClass = "resulterror";
				lblRenameResult.Text = Properties.Messages.CouldNotRenameNamespace;
			}
		}

	}

	/// <summary>
	/// Represents a Namespace for display purposes.
	/// </summary>
	public class NamespaceRow {

		private string name, defaultPage, theme, pageCount, categoryCount, provider, additionalClass;
		private bool canSetPermissions;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:NamespaceRow" /> class.
		/// </summary>
		/// <param name="nspace">The original namespace.</param>
		/// <param name="theme">The theme for the namespace.</param>
		/// <param name="pageCount">The page count.</param>
		/// <param name="categoryCount">The category count.</param>
		/// <param name="canSetPermissions">A value indicating whether the current user can set namespace permissions.</param>
		/// <param name="selected">A value indicating whether the namespace is selected.</param>
		public NamespaceRow(NamespaceInfo nspace, string theme, int pageCount, int categoryCount, bool canSetPermissions, bool selected) {
			name = nspace.Name;
			defaultPage = nspace.DefaultPageFullName;
			this.theme = theme;
			this.pageCount = pageCount.ToString();
			this.categoryCount = categoryCount.ToString();
			provider = nspace.Provider.Information.Name;
			this.canSetPermissions = canSetPermissions;
			additionalClass = selected ? " selected" : "";
		}

		/// <summary>
		/// Gets the name.
		/// </summary>
		public string Name {
			get { return name; }
		}

		/// <summary>
		/// Gets the default page.
		/// </summary>
		public string DefaultPage {
			get { return defaultPage; }
		}

		/// <summary>
		/// Gets the theme.
		/// </summary>
		public string Theme {
			get { return theme; }
		}

		/// <summary>
		/// Gets the page count.
		/// </summary>
		public string PageCount {
			get { return pageCount; }
		}

		/// <summary>
		/// Gets the category count.
		/// </summary>
		public string CategoryCount {
			get { return categoryCount; }
		}

		/// <summary>
		/// Gets the provider.
		/// </summary>
		public string Provider {
			get { return provider; }
		}

		/// <summary>
		/// Gets a value indicating whether the current user can set namespace permissions.
		/// </summary>
		public bool CanSetPermissions {
			get { return canSetPermissions; }
		}

		/// <summary>
		/// Gets the additional CSS class.
		/// </summary>
		public string AdditionalClass {
			get { return additionalClass; }
		}

	}

}
