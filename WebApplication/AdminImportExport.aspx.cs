
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

		//protected void lstDestinationWiki_SelectedIndexChanged(object sender, EventArgs e) {
		//    btnImportSettings.Enabled = lstDestinationWiki.SelectedIndex > 0;
		//}

		//protected void btnImportSettings_Click(object sender, EventArgs e) {
		//    string file = upSettings.FileName;

		//    string ext = System.IO.Path.GetExtension(file);
		//    if(ext != null) ext = ext.ToLowerInvariant();
		//    if(ext != ".json") {
		//        lblImportSettingsResult.CssClass = "resulterror";
		//        lblImportSettingsResult.Text = Properties.Messages.VoidOrInvalidFile;
		//        return;
		//    }

		//    string selectedWiki = lstDestinationWiki.SelectedValue;

		//    Log.LogEntry("Import Settings requested for wiki: " + selectedWiki, EntryType.General, SessionFacade.CurrentUsername, null);
		//    ISettingsStorageProviderV40 settingsStorageProvider = Settings.GetProvider(selectedWiki);
		//    bool result = BackupRestore.BackupRestore.RestoreSettingsStorageProvider(upSettings.FileBytes, settingsStorageProvider);

		//    if(result) {
		//        lblImportSettingsResult.CssClass = "resultok";
		//        lblImportSettingsResult.Text = Properties.Messages.ImportedSettings;
		//        upSettings.Attributes.Add("value", "");
		//        Log.LogEntry("Import Settings for wiki " + selectedWiki + " completed succesfully.", EntryType.General, SessionFacade.CurrentUsername, null);
		//    }
		//    else {
		//        lblImportSettingsResult.CssClass = "resulterror";
		//        lblImportSettingsResult.Text = Properties.Messages.VoidOrInvalidFile;
		//    }
		//}

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
		//    if(lstWiki.SelectedIndex > 0) {
		//        rptStorageProviders.DataBind();
		//        rptStorageProviders.Visible = true;
		//    }
		//    else {
		//        rptStorageProviders.Visible = false;
		//    }
		}

		//protected void rptStorageProviders_DataBinding(object sender, EventArgs e) {
		//    List<StorageProviderRow> result = new List<StorageProviderRow>();

		//    // Settings
		//    result.Add(new StorageProviderRow(Collectors.CollectorsBox.GetSettingsProvider(lstWiki.SelectedValue), typeof(ISettingsStorageProviderV40)));

		//    // Pages
		//    foreach(IPagesStorageProviderV40 prov in Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(lstWiki.SelectedValue)) {
		//        result.Add(new StorageProviderRow(prov, typeof(IPagesStorageProviderV40)));
		//    }

		//    // Users
		//    foreach(IUsersStorageProviderV40 prov in Collectors.CollectorsBox.UsersProviderCollector.GetAllProviders(lstWiki.SelectedValue)) {
		//        result.Add(new StorageProviderRow(prov, typeof(IUsersStorageProviderV40)));
		//    }

		//    // Files
		//    foreach(IFilesStorageProviderV40 prov in Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(lstWiki.SelectedValue)) {
		//        result.Add(new StorageProviderRow(prov, typeof(IFilesStorageProviderV40)));
		//    }

		//    // Themes
		//    foreach(IThemesStorageProviderV40 prov in Collectors.CollectorsBox.ThemesProviderCollector.GetAllProviders(lstWiki.SelectedValue)) {
		//        result.Add(new StorageProviderRow(prov, typeof(IThemesStorageProviderV40)));
		//    }

		//    rptStorageProviders.DataSource = result;
		//}

		protected void btnExportAll_Click(object sender, EventArgs e) {
			Log.LogEntry("Data export requested.", EntryType.General, SessionFacade.GetCurrentUsername(), lstWiki.SelectedValue);

			byte[] backupFile = BackupRestore.BackupRestore.BackupAll(lstWiki.SelectedValue, GlobalSettings.Provider.ListPluginAssemblies(),
				Collectors.CollectorsBox.GetSettingsProvider(lstWiki.SelectedValue),
				(from p in Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(lstWiki.SelectedValue)
					 where !p.ReadOnly select p).ToArray(),
				(from p in Collectors.CollectorsBox.UsersProviderCollector.GetAllProviders(lstWiki.SelectedValue)
					 where IsUsersProviderFullWriteEnabled(p) select p).ToArray(),
				(from p in Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(lstWiki.SelectedValue)
					 where !p.ReadOnly select p).ToArray());

			Response.Clear();
			Response.AddHeader("content-type", "application/zip");
			Response.AddHeader("content-disposition", "attachment;filename=\"Backup-" + lstWiki.SelectedValue + ".zip\"");
			Response.AddHeader("content-length", backupFile.Length.ToString());

			Response.OutputStream.Write(backupFile, 0, backupFile.Length);
		}

		protected void btnImportBackup_Click(object sender, EventArgs e) {
			Log.LogEntry("Data Import requested.", EntryType.General, SessionFacade.GetCurrentUsername(), lstWiki.SelectedValue);

			BackupRestore.BackupRestore.RestoreAll(upBackup.FileBytes, Collectors.CollectorsBox.GetSettingsProvider(lstWiki.SelectedValue), Collectors.CollectorsBox.PagesProviderCollector.GetProvider(GlobalSettings.DefaultPagesProvider, lstWiki.SelectedValue),
				Collectors.CollectorsBox.UsersProviderCollector.GetProvider(GlobalSettings.DefaultUsersProvider, lstWiki.SelectedValue), Collectors.CollectorsBox.FilesProviderCollector.GetProvider(GlobalSettings.DefaultFilesProvider, lstWiki.SelectedValue));
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
