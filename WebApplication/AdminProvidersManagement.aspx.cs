
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

			if(!AdminMaster.CanManageGlobalConfiguration(SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames(currentWiki))) UrlTools.Redirect("AccessDenied.aspx");

			if(!Page.IsPostBack) {
				LoadDlls();

				// Load providers and related data
				rptProviders.DataBind();
			}
		}
		
		#region DLLs

		protected void rptProviders_DataBinding(object sender, EventArgs e) {
			IFormatterProviderV40[] plugins = Collectors.CollectorsBox.FormatterProviderCollector.GetAllProviders(currentWiki);

			btnAutoUpdateProviders.Visible = true;
			btnAutoUpdateProviders.Enabled = plugins.Length > 0;

			List<ProviderRow> result = new List<ProviderRow>(plugins.Length);

			for(int i = 0; i < plugins.Length; i++) {
				result.Add(new ProviderRow(plugins[i].Information,
					plugins[i].GetType().FullName,
					GetUpdateStatus(plugins[i].Information),
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

			ProviderUpdater updater = new ProviderUpdater(GlobalSettings.Provider, Collectors.FileNames,
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

				LoadDlls();
				rptProviders.DataBind();
			}
		}

		#endregion

		#region DataExportImport

		protected void lstWiki_SelectedIndexChanged(object sender, EventArgs e) {
			btnExportSettings.Enabled = lstWiki.SelectedIndex > 0;
		}

		protected void btnExportSettings_Click(object sender, EventArgs e) {
			Log.LogEntry("Settings data export requested.", EntryType.General, SessionFacade.CurrentUsername, null);

			ISettingsStorageProviderV40 settingsProvider = Settings.GetProvider(lstWiki.SelectedValue);

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

			IGlobalSettingsStorageProviderV40 globalSettingsStorageProvider = GlobalSettings.Provider;

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
			ISettingsStorageProviderV40 settingsStorageProvider = Settings.GetProvider(selectedWiki);
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
			IGlobalSettingsStorageProviderV40 globalSettingsStorageProvider = GlobalSettings.Provider;
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
		private static bool IsUsersProviderFullWriteEnabled(IUsersStorageProviderV40 provider) {
			return
				!provider.UserAccountsReadOnly &&
				!provider.UserGroupsReadOnly &&
				!provider.GroupMembershipReadOnly &&
				!provider.UsersDataReadOnly;
		}

	}

}
