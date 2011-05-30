
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScrewTurn.Wiki.PluginFramework;
using System.Security.Cryptography;

namespace ScrewTurn.Wiki {

	public partial class AdminProvidersManagement : BasePage {

		string currentWiki;

		protected void Page_Load(object sender, EventArgs e) {
			currentWiki = DetectWiki();

			AdminMaster.RedirectToLoginIfNeeded();

			if(!AdminMaster.CanManageConfiguration(SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames(currentWiki))) UrlTools.Redirect("AccessDenied.aspx");

			if(!Page.IsPostBack) {
				LoadDlls();

				LoadSourceProviders();
			}
		}
		
		/// <summary>
		/// Performs all the actions that are needed after a provider status is changed.
		/// </summary>
		private void PerformPostProviderChangeActions() {
			Content.InvalidateAllPages();
		}

		#region DLLs

		/// <summary>
		/// Loads all the providers' DLLs.
		/// </summary>
		private void LoadDlls() {
			string[] files = GlobalSettings.Provider.ListPluginAssemblies();
			lstDlls.Items.Clear();
			lstDlls.Items.Add(new ListItem("- " + Properties.Messages.SelectAndDelete + " -", ""));
			for(int i = 0; i < files.Length; i++) {
				lstDlls.Items.Add(new ListItem(files[i], files[i]));
			}
		}

		protected void lstDlls_SelectedIndexChanged(object sender, EventArgs e) {
			btnDeleteDll.Enabled = lstDlls.SelectedIndex >= 0 && !string.IsNullOrEmpty(lstDlls.SelectedValue);
		}

		protected void btnDeleteDll_Click(object sender, EventArgs e) {
			if(GlobalSettings.Provider.DeletePluginAssembly(lstDlls.SelectedValue)) {
				LoadDlls();
				lstDlls_SelectedIndexChanged(sender, e);
				lblDllResult.CssClass = "resultok";
				lblDllResult.Text = Properties.Messages.DllDeleted;
			}
			else {
				lblDllResult.CssClass = "resulterror";
				lblDllResult.Text = Properties.Messages.CouldNotDeleteDll;
			}
		}

		protected void btnUpload_Click(object sender, EventArgs e) {
			string file = upDll.FileName;

			string ext = System.IO.Path.GetExtension(file);
			if(ext != null) ext = ext.ToLowerInvariant();
			if(ext != ".dll") {
				lblUploadResult.CssClass = "resulterror";
				lblUploadResult.Text = Properties.Messages.VoidOrInvalidFile;
				return;
			}

			Log.LogEntry("Provider DLL upload requested " + upDll.FileName, EntryType.General, SessionFacade.CurrentUsername, null);

			string[] asms = GlobalSettings.Provider.ListPluginAssemblies();
			if(Array.Find<string>(asms, delegate(string v) {
				if(v.Equals(file)) return true;
				else return false;
			}) != null) {
				// DLL already exists
				lblUploadResult.CssClass = "resulterror";
				lblUploadResult.Text = Properties.Messages.DllAlreadyExists;
				return;
			}
			else {
				GlobalSettings.Provider.StorePluginAssembly(file, upDll.FileBytes);

				int count = ProviderLoader.LoadFormatterProvidersFromAuto(file);

				lblUploadResult.CssClass = "resultok";
				lblUploadResult.Text = Properties.Messages.LoadedProviders.Replace("###", count.ToString());
				upDll.Attributes.Add("value", "");

				PerformPostProviderChangeActions();

				LoadDlls();
				LoadSourceProviders();
			}
		}

		#endregion

		#region Data Migration

		/// <summary>
		/// Loads source providers for data migration.
		/// </summary>
		private void LoadSourceProviders() {
			lstPagesSource.Items.Clear();
			lstPagesSource.Items.Add(new ListItem("", ""));
			foreach(IPagesStorageProviderV30 prov in Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(currentWiki)) {
				if(!prov.ReadOnly) {
					lstPagesSource.Items.Add(new ListItem(prov.Information.Name, prov.GetType().ToString()));
				}
			}

			lstUsersSource.Items.Clear();
			lstUsersSource.Items.Add(new ListItem("", ""));
			foreach(IUsersStorageProviderV30 prov in Collectors.CollectorsBox.UsersProviderCollector.GetAllProviders(currentWiki)) {
				if(IsUsersProviderFullWriteEnabled(prov)) {
					lstUsersSource.Items.Add(new ListItem(prov.Information.Name, prov.GetType().ToString()));
				}
			}

			lstFilesSource.Items.Clear();
			lstFilesSource.Items.Add(new ListItem("", ""));
			foreach(IFilesStorageProviderV30 prov in Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(currentWiki)) {
				if(!prov.ReadOnly) {
					lstFilesSource.Items.Add(new ListItem(prov.Information.Name, prov.GetType().ToString()));
				}
			}

			lblSettingsSource.Text = Settings.GetProvider(currentWiki).Information.Name;
			lstSettingsDestination.Items.Clear();
			lstSettingsDestination.Items.Add(new ListItem("", ""));
			if(Settings.GetProvider(currentWiki).GetType().FullName != typeof(SettingsStorageProvider).FullName) {
				lstSettingsDestination.Items.Add(new ListItem(SettingsStorageProvider.ProviderName, typeof(SettingsStorageProvider).FullName));
			}
			//foreach(ISettingsStorageProviderV30 prov in ProviderLoader..LoadAllSettingsStorageProviders(GlobalSettings.Provider)) {
			//    if(prov.GetType().FullName != Settings.Provider.GetType().FullName) {
			//        lstSettingsDestination.Items.Add(new ListItem(prov.Information.Name, prov.GetType().FullName));
			//    }
			//}
		}

		protected void lstPagesSource_SelectedIndexChanged(object sender, EventArgs e) {
			lstPagesDestination.Items.Clear();
			if(lstPagesSource.SelectedValue != "") {
				foreach(IPagesStorageProviderV30 prov in Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(currentWiki)) {
					if(!prov.ReadOnly && lstPagesSource.SelectedValue != prov.GetType().ToString()) {
						lstPagesDestination.Items.Add(new ListItem(prov.Information.Name, prov.GetType().ToString()));
					}
				}
			}
			btnMigratePages.Enabled = lstPagesDestination.Items.Count > 0;
		}

		protected void lstUsersSource_SelectedIndexChanged(object sender, EventArgs e) {
			lstUsersDestination.Items.Clear();
			if(lstUsersSource.SelectedValue != "") {
				foreach(IUsersStorageProviderV30 prov in Collectors.CollectorsBox.UsersProviderCollector.GetAllProviders(currentWiki)) {
					if(IsUsersProviderFullWriteEnabled(prov) && lstUsersSource.SelectedValue != prov.GetType().ToString()) {
						lstUsersDestination.Items.Add(new ListItem(prov.Information.Name, prov.GetType().ToString()));
					}
				}
			}
			btnMigrateUsers.Enabled = lstUsersDestination.Items.Count > 0;
		}

		protected void lstFilesSource_SelectedIndexChanged(object sender, EventArgs e) {
			lstFilesDestination.Items.Clear();
			if(lstFilesSource.SelectedValue != "") {
				foreach(IFilesStorageProviderV30 prov in Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(currentWiki)) {
					if(!prov.ReadOnly && lstFilesSource.SelectedValue != prov.GetType().ToString()) {
						lstFilesDestination.Items.Add(new ListItem(prov.Information.Name, prov.GetType().ToString()));
					}
				}
			}
			btnMigrateFiles.Enabled = lstFilesDestination.Items.Count > 0;
		}

		protected void lstSettingsDestination_SelectedIndexChanged(object sender, EventArgs e) {
			btnCopySettings.Enabled = lstSettingsDestination.SelectedValue != "";
		}

		protected void btnMigratePages_Click(object sender, EventArgs e) {
			Log.LogEntry("Pages data migration requested from " + lstPagesSource.SelectedValue + " to " + lstPagesDestination.SelectedValue, EntryType.General, SessionFacade.CurrentUsername, null);

			foreach(PluginFramework.Wiki wiki in GlobalSettings.Provider.AllWikis()) {
				IPagesStorageProviderV30 from = Collectors.CollectorsBox.PagesProviderCollector.GetProvider(lstPagesSource.SelectedValue, wiki.WikiName);
				IPagesStorageProviderV30 to = Collectors.CollectorsBox.PagesProviderCollector.GetProvider(lstPagesDestination.SelectedValue, wiki.WikiName);

				Log.LogEntry("Pages data migration started for wiki: " + wiki.WikiName, EntryType.General, SessionFacade.CurrentUsername, null);	

				DataMigrator.MigratePagesStorageProviderData(from, to);
			}
			lblMigratePagesResult.CssClass = "resultok";
			lblMigratePagesResult.Text = Properties.Messages.DataMigrated;
		}

		protected void btnMigrateUsers_Click(object sender, EventArgs e) {
			Log.LogEntry("Users data migration requested from " + lstUsersSource.SelectedValue + " to " + lstUsersDestination.SelectedValue, EntryType.General, SessionFacade.CurrentUsername, null);

			foreach(PluginFramework.Wiki wiki in GlobalSettings.Provider.AllWikis()) {
				IUsersStorageProviderV30 from = Collectors.CollectorsBox.UsersProviderCollector.GetProvider(lstUsersSource.SelectedValue, wiki.WikiName);
				IUsersStorageProviderV30 to = Collectors.CollectorsBox.UsersProviderCollector.GetProvider(lstUsersDestination.SelectedValue, wiki.WikiName);

				Log.LogEntry("Users data migration started for wiki: " + wiki.WikiName, EntryType.General, SessionFacade.CurrentUsername, null);

				DataMigrator.MigrateUsersStorageProviderData(wiki.WikiName, from, to, true);
			}

			lblMigrateUsersResult.CssClass = "resultok";
			lblMigrateUsersResult.Text = Properties.Messages.DataMigrated;
		}

		protected void btnMigrateFiles_Click(object sender, EventArgs e) {
			Log.LogEntry("Files data migration requested from " + lstFilesSource.SelectedValue + " to " + lstFilesDestination.SelectedValue, EntryType.General, SessionFacade.CurrentUsername, null);

			foreach(PluginFramework.Wiki wiki in GlobalSettings.Provider.AllWikis()) {
				IFilesStorageProviderV30 from = Collectors.CollectorsBox.FilesProviderCollector.GetProvider(lstFilesSource.SelectedValue, wiki.WikiName);
				IFilesStorageProviderV30 to = Collectors.CollectorsBox.FilesProviderCollector.GetProvider(lstFilesDestination.SelectedValue, wiki.WikiName);

				Log.LogEntry("Files data migration started for wiki: " + wiki.WikiName, EntryType.General, SessionFacade.CurrentUsername, null);

				DataMigrator.MigrateFilesStorageProviderData(from, to, Settings.GetProvider(wiki.WikiName));
			}

			lblMigrateFilesResult.CssClass = "resultok";
			lblMigrateFilesResult.Text = Properties.Messages.DataMigrated;
		}

		protected void btnCopySettings_Click(object sender, EventArgs e) {
			ISettingsStorageProviderV30 to = null;

			//ISettingsStorageProviderV30[] allProviders = ProviderLoader.LoadAllSettingsStorageProviders(Settings.Provider);
			//foreach(ISettingsStorageProviderV30 prov in allProviders) {
			//    if(prov.GetType().ToString() == lstSettingsDestination.SelectedValue) {
			//        to = prov;
			//        break;
			//    }
			//}

			Log.LogEntry("Settings data copy requested to " + to.Information.Name, EntryType.General, SessionFacade.CurrentUsername, null);

			try {
				to.Init(Host.Instance, txtSettingsDestinationConfig.Text, currentWiki);
			}
			catch(InvalidConfigurationException ex) {
				Log.LogEntry("Provider rejected configuration: " + ex.ToString(), EntryType.Error, Log.SystemUsername, null);
				lblCopySettingsResult.CssClass = "resulterror";
				lblCopySettingsResult.Text = Properties.Messages.ProviderRejectedConfiguration;
				return;
			}

			// Find namespaces
			List<string> namespaces = new List<string>(5);
			foreach(NamespaceInfo ns in Pages.GetNamespaces(currentWiki)) {
				namespaces.Add(ns.Name);
			}

			//DataMigrator.CopySettingsStorageProviderData(Settings.Provider, to, namespaces.ToArray(), Collectors.GetAllProviders());

			lblCopySettingsResult.CssClass = "resultok";
			lblCopySettingsResult.Text = Properties.Messages.DataCopied;
		}

		#endregion

		/// <summary>
		/// Detects whether a users storage provider fully supports writing to all managed data.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <returns><c>true</c> if the provider fully supports writing all managed data, <c>false</c> otherwise.</returns>
		private static bool IsUsersProviderFullWriteEnabled(IUsersStorageProviderV30 provider) {
			return
				!provider.UserAccountsReadOnly &&
				!provider.UserGroupsReadOnly &&
				!provider.GroupMembershipReadOnly &&
				!provider.UsersDataReadOnly;
		}

	}

}
