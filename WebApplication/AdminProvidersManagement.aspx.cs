
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

				// Load providers and related data
				rptProviders.DataBind();
			}
		}
		
		/// <summary>
		/// Performs all the actions that are needed after a provider status is changed.
		/// </summary>
		private void PerformPostProviderChangeActions() {
			Content.InvalidateAllPages();
		}

		#region DLLs

		protected void rdo_CheckedChanged(object sender, EventArgs e) {
			ResetEditor();
			rptProviders.DataBind();
		}

		/// <summary>
		/// Resets the editor.
		/// </summary>
		private void ResetEditor() {
			btnAutoUpdateProviders.Visible = true;
		}

		protected void rptProviders_DataBinding(object sender, EventArgs e) {
			List<IProviderV30> providers = new List<IProviderV30>(5);

			int enabledCount = 0;

			if(rdoFormatter.Checked) {
				IFormatterProviderV30[] formatterProviders = Collectors.CollectorsBox.FormatterProviderCollector.GetAllProviders(currentWiki);
				enabledCount = formatterProviders.Length;
				providers.AddRange(formatterProviders);
			}
			else {
				IGlobalSettingsStorageProviderV30 globalSettingsStorageProvider = Collectors.CollectorsBox.GlobalSettingsProvider;
				providers.Add(globalSettingsStorageProvider);
				ISettingsStorageProviderV30 settingsStorageProviders = Collectors.CollectorsBox.GetSettingsProvider(currentWiki);
				providers.Add(settingsStorageProviders);
				IPagesStorageProviderV30[] pagesStorageProviders = Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(currentWiki);
				providers.AddRange(pagesStorageProviders);
				IFilesStorageProviderV30[] filesStorageProviders = Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(currentWiki);
				providers.AddRange(filesStorageProviders);
				IUsersStorageProviderV30[] usersStorageProviders = Collectors.CollectorsBox.UsersProviderCollector.GetAllProviders(currentWiki);
				providers.AddRange(usersStorageProviders);
			}

			List<ProviderRow> result = new List<ProviderRow>(providers.Count);

			for(int i = 0; i < providers.Count; i++) {
				IProviderV30 prov = providers[i];
				result.Add(new ProviderRow(prov.Information,
					prov.GetType().FullName,
					GetUpdateStatus(prov.Information),
					false,
					false));
			}

			rptProviders.DataSource = result;
		}

		/// <summary>
		/// Gets the update status of a provider.
		/// </summary>
		/// <param name="info">The component information.</param>
		/// <returns>The update status.</returns>
		private string GetUpdateStatus(ComponentInformation info) {
			if(!GlobalSettings.DisableAutomaticVersionCheck) {
				if(string.IsNullOrEmpty(info.UpdateUrl)) return "n/a";
				else {
					string newVersion = null;
					string newAssemblyUrl = null;
					UpdateStatus status = Tools.GetUpdateStatus(info.UpdateUrl, info.Version, out newVersion, out newAssemblyUrl);

					if(status == UpdateStatus.Error) {
						return "<span class=\"resulterror\">" + Properties.Messages.Error + "</span>";
					}
					else if(status == UpdateStatus.NewVersionFound) {
						return "<span class=\"resulterror\">" + Properties.Messages.NewVersion + " <b>" + newVersion + "</b>" +
							(string.IsNullOrEmpty(newAssemblyUrl) ? "" : " (" + Properties.Messages.AutoUpdateAvailable + ")") + "</span>";
					}
					else if(status == UpdateStatus.UpToDate) {
						return "<span class=\"resultok\">" + Properties.Messages.UpToDate + "</span>";
					}
					else throw new NotSupportedException();
				}
			}
			else return "n/a";
		}

		protected void btnAutoUpdateProviders_Click(object sender, EventArgs e) {
			lblAutoUpdateResult.CssClass = "";
			lblAutoUpdateResult.Text = "";

			Log.LogEntry("Providers auto-update requested", EntryType.General, SessionFacade.CurrentUsername, currentWiki);

			ProviderUpdater updater = new ProviderUpdater(GlobalSettings.Provider,
				Collectors.FileNames,
				Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(currentWiki),
				Collectors.CollectorsBox.UsersProviderCollector.GetAllProviders(currentWiki),
				Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(currentWiki),
				Collectors.CollectorsBox.FormatterProviderCollector.GetAllProviders(currentWiki));

			int count = updater.UpdateAll();

			lblAutoUpdateResult.CssClass = "resultok";
			if(count > 0) lblAutoUpdateResult.Text = Properties.Messages.ProvidersUpdated;
			else lblAutoUpdateResult.Text = Properties.Messages.NoProvidersToUpdate;

			rptProviders.DataBind();
		}

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
			lstWiki.Items.Clear();
			lstWiki.Items.Add(new ListItem(Properties.Messages.SelectWiki, ""));
			lstDestinationWiki.Items.Clear();
			lstDestinationWiki.Items.Add(new ListItem(Properties.Messages.SelectWiki, ""));
			foreach(PluginFramework.Wiki wiki in GlobalSettings.Provider.AllWikis()) {
				lstWiki.Items.Add(wiki.WikiName);
				lstDestinationWiki.Items.Add(wiki.WikiName);
			}
			lblGlobalSettingsSource.Text = GlobalSettings.Provider.Information.Name;
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

		#endregion

		#region DataExportImport

		protected void lstWiki_SelectedIndexChanged(object sender, EventArgs e) {
			btnExportSettings.Enabled = lstWiki.SelectedIndex > 0;
		}

		protected void btnExportSettings_Click(object sender, EventArgs e) {
			Log.LogEntry("Settings data export requested.", EntryType.General, SessionFacade.CurrentUsername, null);

			ISettingsStorageProviderV30 settingsProvider = Settings.GetProvider(lstWiki.SelectedValue);

			// Find namespaces
			List<string> namespaces = new List<string>(5);
			foreach(NamespaceInfo ns in Pages.GetNamespaces(currentWiki)) {
				namespaces.Add(ns.Name);
			}

			byte[] backupFile = BackupRestore.BackupRestore.BackupSettingsStorageProvider(settingsProvider, namespaces.ToArray(), GlobalSettings.Provider.ListPluginAssemblies());

			Response.Clear();
			Response.AddHeader("content-type", "application/json");
			Response.AddHeader("content-disposition", "attachment;filename=\"SettingsBackup-" + lstWiki.SelectedValue + ".json\"");
			Response.AddHeader("content-length", backupFile.Length.ToString());

			Response.OutputStream.Write(backupFile, 0, backupFile.Length);
		}

		protected void btnExportGlobalSettings_Click(object sender, EventArgs e) {
			Log.LogEntry("Global Settings data export requested.", EntryType.General, SessionFacade.CurrentUsername, null);

			IGlobalSettingsStorageProviderV30 globalSettingsStorageProvider = GlobalSettings.Provider;

			byte[] backupFile = BackupRestore.BackupRestore.BackupGlobalSettingsStorageProvider(globalSettingsStorageProvider);

			Response.Clear();
			Response.AddHeader("content-type", "application/zip");
			Response.AddHeader("content-disposition", "attachment;filename=\"GlobalSettingsBackup.zip\"");
			Response.AddHeader("content-length", backupFile.Length.ToString());

			Response.OutputStream.Write(backupFile, 0, backupFile.Length);
		}

		protected void lstDestinationWiki_SelectedIndexChanged(object sender, EventArgs e) {
			btnImportSettings.Enabled = lstDestinationWiki.SelectedIndex > 0;
		}

		protected void btnImportSettings_Click(object sender, EventArgs e) {
			string file = upSettings.FileName;

			string ext = System.IO.Path.GetExtension(file);
			if(ext != null) ext = ext.ToLowerInvariant();
			if(ext != ".json") {
				lblImportSettingsResult.CssClass = "resulterror";
				lblImportSettingsResult.Text = Properties.Messages.VoidOrInvalidFile;
				return;
			}

			string selectedWiki = lstDestinationWiki.SelectedValue;

			Log.LogEntry("Import Settings requested for wiki: " + selectedWiki, EntryType.General, SessionFacade.CurrentUsername, null);
			ISettingsStorageProviderV30 settingsStorageProvider = Settings.GetProvider(selectedWiki);
			bool result = BackupRestore.BackupRestore.RestoreSettingsStorageProvider(upSettings.FileBytes, settingsStorageProvider);

			if(result) {
				lblImportSettingsResult.CssClass = "resultok";
				lblImportSettingsResult.Text = Properties.Messages.ImportedSettings;
				upSettings.Attributes.Add("value", "");
				Log.LogEntry("Import Settings for wiki " + selectedWiki + " completed succesfully.", EntryType.General, SessionFacade.CurrentUsername, null);
			}
			else {
				lblImportSettingsResult.CssClass = "resulterror";
				lblImportSettingsResult.Text = Properties.Messages.VoidOrInvalidFile;
			}
		}

		protected void btnImportGlobalSettings_Click(object sender, EventArgs e) {
			string file = upGlobalSettings.FileName;

			string ext = System.IO.Path.GetExtension(file);
			if(ext != null) ext = ext.ToLowerInvariant();
			if(ext != ".zip") {
				lblImportGlobalSettingsResult.CssClass = "resulterror";
				lblImportGlobalSettingsResult.Text = Properties.Messages.VoidOrInvalidFile;
				return;
			}

			Log.LogEntry("Import Global Settings requested.", EntryType.General, SessionFacade.CurrentUsername, null);
			IGlobalSettingsStorageProviderV30 globalSettingsStorageProvider = GlobalSettings.Provider;
			bool result = BackupRestore.BackupRestore.RestoreGlobalSettingsStorageProvider(upGlobalSettings.FileBytes, globalSettingsStorageProvider);

			if(result) {
				lblImportGlobalSettingsResult.CssClass = "resultok";
				lblImportGlobalSettingsResult.Text = Properties.Messages.ImportedSettings;
				upSettings.Attributes.Add("value", "");
				Log.LogEntry("Import Global Settings completed succesfully.", EntryType.General, SessionFacade.CurrentUsername, null);
			}
			else {
				lblImportGlobalSettingsResult.CssClass = "resulterror";
				lblImportGlobalSettingsResult.Text = Properties.Messages.VoidOrInvalidFile;
			}
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
