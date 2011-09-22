
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
using System.IO;

namespace ScrewTurn.Wiki {

	public partial class AdminImportExport : BasePage {

		private const string SettingsStorageProviderInterfaceName = "ScrewTurn.Wiki.PluginFramework.ISettingsStorageProviderV40";
		private const string PagesStorageProviderInterfaceName = "ScrewTurn.Wiki.PluginFramework.IPagesStorageProviderV40";
		private const string UsersStorageProviderInterfaceName = "ScrewTurn.Wiki.PluginFramework.IUsersStorageProviderV40";
		private const string FilesStorageProviderInterfaceName = "ScrewTurn.Wiki.PluginFramework.IFilesStorageProviderV40";
		private const string ThemesStorageProviderInterfaceName = "ScrewTurn.Wiki.PluginFramework.IThemesStorageProviderV40";

		string currentWiki;

		protected void Page_Load(object sender, EventArgs e) {
			currentWiki = DetectWiki();

			AdminMaster.RedirectToLoginIfNeeded();

			if(!AdminMaster.CanManageGlobalConfiguration(SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames(currentWiki))) UrlTools.Redirect("AccessDenied.aspx");

			if(!Page.IsPostBack) {
				LoadWikis();
				LoadDestinationStorageProviders();
			}
		}
		
		#region DataExportImport

		/// <summary>
		/// Loads all the wikis.
		/// </summary>
		private void LoadWikis() {
			List<PluginFramework.Wiki> wikis = Collectors.CollectorsBox.GlobalSettingsProvider.AllWikis().ToList();
			lstWiki.Items.Clear();
			lstWiki.Items.Add(new ListItem("- " + Properties.Messages.SelectWiki + " -", ""));
			for(int i = 0; i < wikis.Count; i++) {
				lstWiki.Items.Add(new ListItem(wikis[i].WikiName, wikis[i].WikiName));
			}
		}

		private void LoadDestinationStorageProviders() {
			// Create a list of available (not readonly) pages storage providers
			IPagesStorageProviderV40[] pagesStorageProviders = (from p in Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(lstWiki.SelectedValue)
																where !p.ReadOnly select p).ToArray();
			lstPagesStorageProviders.Items.Clear();
			foreach(IPagesStorageProviderV40 pagesStorageProvider in pagesStorageProviders) {
				ListItem lstItem = new ListItem(pagesStorageProvider.Information.Name, pagesStorageProvider.GetType().FullName);
				lstItem.Selected = false;
				lstPagesStorageProviders.Items.Add(lstItem);
			}
			lstPagesStorageProviders.Items.FindByValue(GlobalSettings.DefaultPagesProvider).Selected = true;

			// Create a list of available (not readonly) users storage providers
			IUsersStorageProviderV40[] usersStorageProviders = (from p in Collectors.CollectorsBox.UsersProviderCollector.GetAllProviders(lstWiki.SelectedValue)
																where IsUsersProviderFullWriteEnabled(p) select p).ToArray();
			lstUsersStorageProviders.Items.Clear();
			foreach(IUsersStorageProviderV40 usersStorageProvider in usersStorageProviders) {
				ListItem lstItem = new ListItem(usersStorageProvider.Information.Name, usersStorageProvider.GetType().FullName);
				lstItem.Selected = false;
				lstUsersStorageProviders.Items.Add(lstItem);
			}
			lstUsersStorageProviders.Items.FindByValue(GlobalSettings.DefaultUsersProvider).Selected = true;

			// Create a list of available (not readonly) files storage providers
			IFilesStorageProviderV40[] filesStorageProviders = (from p in Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(lstWiki.SelectedValue)
																where !p.ReadOnly select p).ToArray();
			lstFilesStorageProviders.Items.Clear();
			foreach(IFilesStorageProviderV40 filesStorageProvider in filesStorageProviders) {
				ListItem lstItem = new ListItem(filesStorageProvider.Information.Name, filesStorageProvider.GetType().FullName);
				lstItem.Selected = false;
				lstFilesStorageProviders.Items.Add(lstItem);
			}
			lstFilesStorageProviders.Items.FindByValue(GlobalSettings.DefaultFilesProvider).Selected = true;
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
				upGlobalSettings.Attributes.Add("value", "");
				Log.LogEntry("Import Global Settings completed succesfully.", EntryType.General, SessionFacade.CurrentUsername, null);
			}
			else {
				lblImportGlobalSettingsResult.CssClass = "resulterror";
				lblImportGlobalSettingsResult.Text = Properties.Messages.VoidOrInvalidFile;
			}
		}

		protected void lstWiki_SelectedIndexChanged(object sender, EventArgs e) {
			if(lstWiki.SelectedIndex > 0) {
				btnExportAll.Enabled = true;
				txtBackupFileURL.Enabled = true;
				btnImportBackup.Enabled = true;
			}
			else {
				btnExportAll.Enabled = false;
				txtBackupFileURL.Enabled = false;
				btnImportBackup.Enabled = false;
			}
		}

		protected void btnExportAll_Click(object sender, EventArgs e) {
			Log.LogEntry("Data export requested.", EntryType.General, SessionFacade.GetCurrentUsername(), lstWiki.SelectedValue);

			string tempDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());
			Directory.CreateDirectory(tempDir);
			string zipFileName = Path.Combine(tempDir, "Backup.zip");

			bool backupFileSucceded = BackupRestore.BackupRestore.BackupAll(zipFileName, lstWiki.SelectedValue, GlobalSettings.Provider.ListPluginAssemblies(),
				Collectors.CollectorsBox.GetSettingsProvider(lstWiki.SelectedValue),
				(from p in Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(lstWiki.SelectedValue)
					 where !p.ReadOnly select p).ToArray(),
				(from p in Collectors.CollectorsBox.UsersProviderCollector.GetAllProviders(lstWiki.SelectedValue)
					 where IsUsersProviderFullWriteEnabled(p) select p).ToArray(),
				(from p in Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(lstWiki.SelectedValue)
					 where !p.ReadOnly select p).ToArray());

			FileInfo file = new FileInfo(zipFileName);
			Response.Clear();
			Response.AddHeader("content-type", GetMimeType(zipFileName));
			Response.AddHeader("content-disposition", "attachment;filename=\"Backup-" + lstWiki.SelectedValue + ".zip\"");
			Response.AddHeader("content-length", file.Length.ToString());

			Response.TransmitFile(zipFileName);
			Response.Flush();

			Directory.Delete(tempDir, true);
			Log.LogEntry("Data export completed.", EntryType.General, SessionFacade.GetCurrentUsername(), lstWiki.SelectedValue);
		}

		private string GetMimeType(string ext) {
			string mime = "";
			if(MimeTypes.Types.TryGetValue(ext, out mime)) return mime;
			else return "application/octet-stream";
		}

		protected void btnImportBackup_Click(object sender, EventArgs e) {
			if(!string.IsNullOrEmpty(txtBackupFileURL.Text)) {
				Log.LogEntry("Data Import requested.", EntryType.General, SessionFacade.GetCurrentUsername(), lstWiki.SelectedValue);

				BackupRestore.BackupRestore.RestoreAll(txtBackupFileURL.Text, Collectors.CollectorsBox.GlobalSettingsProvider, Collectors.CollectorsBox.GetSettingsProvider(lstWiki.SelectedValue), Collectors.CollectorsBox.PagesProviderCollector.GetProvider(lstPagesStorageProviders.SelectedValue, lstWiki.SelectedValue),
					Collectors.CollectorsBox.UsersProviderCollector.GetProvider(lstUsersStorageProviders.SelectedValue, lstWiki.SelectedValue), Collectors.CollectorsBox.FilesProviderCollector.GetProvider(lstFilesStorageProviders.SelectedValue, lstWiki.SelectedValue),
					settingName => GlobalSettings.AllSettingsNames.Any(s => StringComparer.OrdinalIgnoreCase.Compare(s, settingName) == 0));

				Log.LogEntry("Data Import completed.", EntryType.General, SessionFacade.GetCurrentUsername(), lstWiki.SelectedValue);
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

	
	/// <summary>
	/// Represents a storage provider.
	/// </summary>
	class StorageProviderRow {

		private string _provider, _providerType, _providerInterface;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:StorageProviderRow" /> class.
		/// </summary>
		/// <param name="provider">The original provider.</param>
		public StorageProviderRow(IProviderV40 provider, Type providerInterface) {
			_provider = provider.Information.Name;
			_providerType = provider.GetType().FullName;
			_providerInterface = providerInterface.FullName;
		}

		/// <summary>
		/// Gets the provider.
		/// </summary>
		public string Provider {
			get { return _provider; }
		}

		/// <summary>
		/// Gets the provider type name.
		/// </summary>
		public string ProviderType {
			get { return _providerType; }
		}

		/// <summary>
		/// Gets the provider interface name.
		/// </summary>
		public string ProviderInterface {
			get { return _providerInterface; }
		}

	}

}
