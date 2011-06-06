﻿
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	public partial class AdminMaster : System.Web.UI.MasterPage {

		protected void Page_Load(object sender, EventArgs e) {
			string currentWiki = Tools.DetectCurrentWiki();
			StringBuilder sb = new StringBuilder(100);
			sb.Append("<script type=\"text/javascript\">\r\n<!--\r\n");
			sb.AppendFormat("\tvar ConfirmMessage = \"{0}\";\r\n", Properties.Messages.ConfirmOperation);
			sb.Append("// -->\r\n</script>");
			lblStrings.Text = sb.ToString();

			Page.Title = Properties.Messages.AdminTitle + " - " + Settings.GetWikiTitle(currentWiki);
			
			lblJS.Text = Tools.GetJavaScriptIncludes();

			SetupButtons();

			SetupButtonsVisibility(currentWiki);
		}

		/// <summary>
		/// Redirects to the login page if needed.
		/// </summary>
		public static void RedirectToLoginIfNeeded() {
			if(SessionFacade.LoginKey == null) {
				UrlTools.Redirect("Login.aspx?Redirect=" + Tools.UrlEncode(Tools.GetCurrentUrlFixed()));
			}
		}

		private readonly Dictionary<string, string> HyperlinkMap = new Dictionary<string, string>() {
			{ "admingroups", "lnkSelectGroups" },
			{ "adminusers", "lnkSelectAccounts" },
			{ "adminnamespaces", "lnkSelectNamespaces" },
			{ "adminpages", "lnkSelectPages" },
			{ "admincontent", "lnkSelectContent" },
			{ "adminconfig", "lnkSelectConfig" },
			{ "adminsnippets", "lnkSelectSnippets" },
			{ "admincategories", "lnkSelectCategories" },
			{ "adminhome", "lnkSelectAdminHome" },
			{ "adminnavpaths", "lnkSelectNavPaths" },
			{ "adminplugins", "lnkSelectPlugins" },
			{ "admintheme", "lnkSelectTheme" },
			{ "adminglobalhome", "lnkSelectAdminGlobalHome" },
			{ "adminglobalconfig", "lnkSelectGlobalConfig" },
			{ "adminprovidersmanagement", "lnkSelectProvidersManagement" },
			{ "adminlog", "lnkSelectLog" },
		};

		/// <summary>
		/// Sets up the buttons state.
		/// </summary>
		private void SetupButtons() {
			string selectedPage = System.IO.Path.GetFileNameWithoutExtension(Request.PhysicalPath).ToLowerInvariant();

			HyperLink hyperLink = (HyperLink)FindControl(HyperlinkMap[selectedPage]);

			lnkSelectGroups.CssClass = "tab";
			lnkSelectAccounts.CssClass = "tab";
			lnkSelectNamespaces.CssClass = "tab";
			lnkSelectPages.CssClass = "tab";
			lnkSelectContent.CssClass = "tab";
			lnkSelectConfig.CssClass = "tab";
			lnkSelectSnippets.CssClass = "tab";
			lnkSelectCategories.CssClass = "tab";
			lnkSelectAdminHome.CssClass = "tab";
			lnkSelectNavPaths.CssClass = "tab";
			lnkSelectPlugins.CssClass = "tab";
			lnkSelectTheme.CssClass = "tab";
			lnkSelectAdminGlobalHome.CssClass = "tab red";
			lnkSelectGlobalConfig.CssClass = "tab red";
			lnkSelectProvidersManagement.CssClass = "tab red";
			lnkSelectLog.CssClass = "tab red";

			hyperLink.CssClass = "tabselected";
			if(hyperLink.ID == "lnkSelectAdminGlobalHome" ||
			   hyperLink.ID == "lnkSelectGlobalConfig" ||
			   hyperLink.ID == "lnkSelectProvidersManagement" ||
			   hyperLink.ID == "lnkSelectLog") {
						hyperLink.CssClass += " red selected";
			}
		}

		/// <summary>
		/// Sets up the buttons visibility based on the current user's permissions.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		private void SetupButtonsVisibility(string currentWiki) {
			string currentUser = SessionFacade.GetCurrentUsername();
			string[] currentGroups = SessionFacade.GetCurrentGroupNames(currentWiki);

			// Categories (can manage categories in at least one NS)
			lnkSelectCategories.Visible = CanManageCategories(currentUser, currentGroups);

			// Configuration (can manage config)
			lnkSelectConfig.Visible = CanManageConfiguration(currentUser, currentGroups);

			// Global Configuration (can manage global config)
			lnkSelectGlobalConfig.Visible = CanManageConfiguration(currentUser, currentGroups);

			// Content (can manage config)
			lnkSelectContent.Visible = CanManageMetaFiles(currentUser, currentGroups);

			// Groups (can manage groups)
			lnkSelectGroups.Visible = CanManageGroups(currentUser, currentGroups);

			// Home (can manage config)
			lnkSelectAdminHome.Visible = CanManageConfiguration(currentUser, currentGroups);

			// Log (can manage config)
			lnkSelectLog.Visible = CanManageConfiguration(currentUser, currentGroups);

			// Namespaces (can manage namespaces)
			lnkSelectNamespaces.Visible = CanManageNamespaces(currentUser, currentGroups);

			// Nav. Paths (can manage pages in at least one NS)
			lnkSelectNavPaths.Visible = CanManagePages(currentUser, currentGroups);

			// Pages
			// Always displayed because checking every page can take too much time

			// Providers (can manage providers)
			lnkSelectPlugins.Visible = CanManageProviders(currentUser, currentGroups);

			// Snippets (can manage snippets)
			lnkSelectSnippets.Visible = CanManageSnippetsAndTemplates(currentUser, currentGroups);

			// Accounts (can manage user accounts)
			lnkSelectAccounts.Visible = CanManageUsers(currentUser, currentGroups);
		}

		/// <summary>
		/// Determines whether a user can manage the configuration.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <param name="groups">The groups.</param>
		/// <returns><c>true</c> if the user can manage the configuration, <c>false</c> otherwise.</returns>
		public static bool CanManageConfiguration(string username, string[] groups) {
			AuthChecker authChecker = new AuthChecker(Collectors.CollectorsBox.GetSettingsProvider(Tools.DetectCurrentWiki()));
			bool canManageConfiguration = authChecker.CheckActionForGlobals(Actions.ForGlobals.ManageConfiguration, username, groups);
			return canManageConfiguration;
		}

		/// <summary>
		/// Determines whether a user can manage the Meta-Files (Content).
		/// </summary>
		/// <param name="username">The username.</param>
		/// <param name="groups">The groups.</param>
		/// <returns><c>true</c> if the user can manage the Meta-Files (Content), <c>false</c> otherwise.</returns>
		public static bool CanManageMetaFiles(string username, string[] groups) {
			AuthChecker authChecker = new AuthChecker(Collectors.CollectorsBox.GetSettingsProvider(Tools.DetectCurrentWiki()));
			bool canManageMetaFiles = authChecker.CheckActionForGlobals(Actions.ForGlobals.ManageMetaFiles, username, groups);
			return canManageMetaFiles;
		}

		/// <summary>
		/// Determines whether a user can manage categories in at least one namespace.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <param name="groups">The groups.</param>
		/// <returns><c>true</c> if the user can manage categories in at least one namespace, <c>false</c> otherwise.</returns>
		public static bool CanManageCategories(string username, string[] groups) {
			AuthChecker authChecker = new AuthChecker(Collectors.CollectorsBox.GetSettingsProvider(Tools.DetectCurrentWiki()));
			if(authChecker.CheckActionForNamespace(null, Actions.ForNamespaces.ManageCategories, username, groups)) return true;

			foreach(NamespaceInfo ns in Pages.GetNamespaces(Tools.DetectCurrentWiki())) {
				if(authChecker.CheckActionForNamespace(ns, Actions.ForNamespaces.ManageCategories, username, groups)) return true;
			}

			return false;
		}

		/// <summary>
		/// Determines whether a user can manage user groups.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <param name="groups">The groups.</param>
		/// <returns><c>true</c> if the user can manage groups, <c>false</c> otherwise.</returns>
		public static bool CanManageGroups(string username, string[] groups) {
			AuthChecker authChecker = new AuthChecker(Collectors.CollectorsBox.GetSettingsProvider(Tools.DetectCurrentWiki()));
			bool canManageGroups = authChecker.CheckActionForGlobals(Actions.ForGlobals.ManageGroups, username, groups);
			return canManageGroups;
		}

		/// <summary>
		/// Determines whether a user can manage permissions.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <param name="groups">The groups.</param>
		/// <returns><c>true</c> if the user can manage permissions, <c>false</c> otherwise.</returns>
		public static bool CanManagePermissions(string username, string[] groups) {
			AuthChecker authChecker = new AuthChecker(Collectors.CollectorsBox.GetSettingsProvider(Tools.DetectCurrentWiki()));
			bool canManagePermissions = authChecker.CheckActionForGlobals(Actions.ForGlobals.ManagePermissions, username, groups);
			return canManagePermissions;
		}

		/// <summary>
		/// Determines whether a user can manage namespaces.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <param name="groups">The groups.</param>
		/// <returns><c>true</c> if the user can manage namespace, <c>false</c> otherwise.</returns>
		public static bool CanManageNamespaces(string username, string[] groups) {
			AuthChecker authChecker = new AuthChecker(Collectors.CollectorsBox.GetSettingsProvider(Tools.DetectCurrentWiki()));
			bool canManageNamespaces = authChecker.CheckActionForGlobals(Actions.ForGlobals.ManageNamespaces, username, groups);
			return canManageNamespaces;
		}

		/// <summary>
		/// Determines whether a user can manage pages in at least one namespace.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <param name="groups">The groups.</param>
		/// <returns><c>true</c> if the the user can manage pages in at least one namespace, <c>false</c> otherwise.</returns>
		public static bool CanManagePages(string username, string[] groups) {
			AuthChecker authChecker = new AuthChecker(Collectors.CollectorsBox.GetSettingsProvider(Tools.DetectCurrentWiki()));
			if(authChecker.CheckActionForNamespace(null, Actions.ForNamespaces.ManagePages, username, groups)) return true;

			foreach(NamespaceInfo ns in Pages.GetNamespaces(Tools.DetectCurrentWiki())) {
				if(authChecker.CheckActionForNamespace(ns, Actions.ForNamespaces.ManagePages, username, groups)) return true;
			}

			return false;
		}

		/// <summary>
		/// Determines whether a user can manage providers.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <param name="groups">The groups.</param>
		/// <returns><c>true</c> if the user can manage providers, <c>false</c> otherwise.</returns>
		public static bool CanManageProviders(string username, string[] groups) {
			AuthChecker authChecker = new AuthChecker(Collectors.CollectorsBox.GetSettingsProvider(Tools.DetectCurrentWiki()));
			bool canManageProviders = authChecker.CheckActionForGlobals(Actions.ForGlobals.ManageProviders, username, groups);
			return canManageProviders;
		}

		/// <summary>
		/// Determines whether a user can manage snippets and templates.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <param name="groups">The groups.</param>
		/// <returns><c>true</c> if the user can manage snippets and templates, <c>false</c> otherwise.</returns>
		public static bool CanManageSnippetsAndTemplates(string username, string[] groups) {
			AuthChecker authChecker = new AuthChecker(Collectors.CollectorsBox.GetSettingsProvider(Tools.DetectCurrentWiki()));
			bool canManageSnippets = authChecker.CheckActionForGlobals(Actions.ForGlobals.ManageSnippetsAndTemplates, username, groups);
			return canManageSnippets;
		}

		/// <summary>
		/// Determines whether a user can manager user accounts.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <param name="groups">The groups.</param>
		/// <returns><c>true</c> if the user can manage user accounts, <c>false</c> otherwise.</returns>
		public static bool CanManageUsers(string username, string[] groups) {
			AuthChecker authChecker = new AuthChecker(Collectors.CollectorsBox.GetSettingsProvider(Tools.DetectCurrentWiki()));
			bool canManageUsers = authChecker.CheckActionForGlobals(Actions.ForGlobals.ManageAccounts, username, groups);
			return canManageUsers;
		}

		/// <summary>
		/// Determines whether a user can approve/reject a draft of a page.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <param name="username">The username.</param>
		/// <param name="groups">The groups.</param>
		/// <returns><c>true</c> if the user can approve/reject a draft of the page, <c>false</c> otherwise.</returns>
		public static bool CanApproveDraft(PageInfo page, string username, string[] groups) {
			return Pages.CanApproveDraft(Tools.DetectCurrentWiki(), page, username, groups);
		}

	}

}
