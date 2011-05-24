
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

	public partial class AdminGlobalConfig : BasePage {

		string currentWiki;

		protected void Page_Load(object sender, EventArgs e) {
			currentWiki = DetectWiki();

			AdminMaster.RedirectToLoginIfNeeded();

			if(!AdminMaster.CanManageConfiguration(SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames(currentWiki))) UrlTools.Redirect("AccessDenied.aspx");

			if(!Page.IsPostBack) {
				// Setup validation regular expressions
				revContactEmail.ValidationExpression = GlobalSettings.EmailRegex;
				revSenderEmail.ValidationExpression = GlobalSettings.EmailRegex;
				revSmtpServer.ValidationExpression = GlobalSettings.SmtpServerRegex;

				// Load current values
				LoadGeneralConfig(currentWiki);
				LoadSecurityConfig(currentWiki);
				LoadAdvancedConfig();

				LoadDlls();

				LoadSourceProviders();
			}
		}

		/// <summary>
		/// Loads the general configuration.
		/// </summary>
		/// <param name="currentWiki">The wiki.</param>
		private void LoadGeneralConfig(string currentWiki) {
			txtContactEmail.Text = GlobalSettings.ContactEmail;
			txtSenderEmail.Text = GlobalSettings.SenderEmail;
			txtErrorsEmails.Text = string.Join(", ", GlobalSettings.ErrorsEmails);
			txtSmtpServer.Text = GlobalSettings.SmtpServer;
			int port = GlobalSettings.SmtpPort;
			txtSmtpPort.Text = port != -1 ? port.ToString() : "";
			txtUsername.Text = GlobalSettings.SmtpUsername;
			txtPassword.Attributes.Add("value", GlobalSettings.SmtpPassword);
			chkEnableSslForSmtp.Checked = GlobalSettings.SmtpSsl;
		}
		
		/// <summary>
		/// Loads the security configuration.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		private void LoadSecurityConfig(string currentWiki) {
			txtPasswordRegEx.Text = GlobalSettings.PasswordRegex;
			txtUsernameRegEx.Text = GlobalSettings.UsernameRegex;

			txtMaxFileSize.Text = GlobalSettings.MaxFileSize.ToString();
			txtMaxLogSize.Text = GlobalSettings.MaxLogSize.ToString();
			switch(GlobalSettings.LoggingLevel) {
				case LoggingLevel.DisableLog:
					rdoDisableLog.Checked = true;
					break;
				case LoggingLevel.ErrorsOnly:
					rdoErrorsOnly.Checked = true;
					break;
				case LoggingLevel.WarningsAndErrors:
					rdoWarningsAndErrors.Checked = true;
					break;
				case LoggingLevel.AllMessages:
					rdoAllMessages.Checked = true;
					break;
			}
		}

		/// <summary>
		/// Loads the advanced configuration.
		/// </summary>
		private void LoadAdvancedConfig() {
			chkEnableAutomaticUpdateChecks.Checked = !GlobalSettings.DisableAutomaticVersionCheck;
			chkEnableViewStateCompression.Checked = GlobalSettings.EnableViewStateCompression;
			chkEnableHttpCompression.Checked = GlobalSettings.EnableHttpCompression;
		}

		protected void cvUsername_ServerValidate(object sender, ServerValidateEventArgs e) {
			e.IsValid = txtUsername.Text.Length == 0 ||
				(txtUsername.Text.Length > 0 && txtPassword.Text.Length > 0);
		}

		protected void cvPassword_ServerValidate(object sender, ServerValidateEventArgs e) {
			e.IsValid = txtPassword.Text.Length == 0 ||
				(txtUsername.Text.Length > 0 && txtPassword.Text.Length > 0);
		}

		/// <summary>
		/// Gets the errors emails, properly trimmed.
		/// </summary>
		/// <returns>The emails.</returns>
		private string[] GetErrorsEmails() {
			string[] emails = txtErrorsEmails.Text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			for(int i = 0; i < emails.Length; i++) {
				emails[i] = emails[i].Trim();
			}

			return emails;
		}

		protected void cvErrorsEmails_ServerValidate(object sender, ServerValidateEventArgs e) {
			string[] emails = GetErrorsEmails();

			Regex regex = new Regex(GlobalSettings.EmailRegex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

			foreach(string email in emails) {
				if(!regex.Match(email).Success) {
					e.IsValid = false;
					return;
				}
			}

			e.IsValid = true;
		}

		protected void cvUsernameRegEx_ServerValidate(object sender, ServerValidateEventArgs e) {
			try {
				var r = new Regex(txtUsernameRegEx.Text);
				r.IsMatch("Test String to validate Regular Expression");
				e.IsValid = true;
			}
			catch {
				e.IsValid = false;
			}
		}

		protected void cvPasswordRegEx_ServerValidate(object sender, ServerValidateEventArgs e) {
			try {
				var r = new Regex(txtPasswordRegEx.Text);
				r.IsMatch("Test String to validate Regular Expression");
				e.IsValid = true;
			} 
			catch {
				e.IsValid = false;
			}
		}

		protected void btnSave_Click(object sender, EventArgs e) {
			lblResult.CssClass = "";
			lblResult.Text = "";

			Page.Validate();

			if(!Page.IsValid) return;

			Log.LogEntry("Wiki Configuration change requested", EntryType.General, SessionFacade.CurrentUsername);

			string currentWiki = DetectWiki();

			// Save general configuration
			GlobalSettings.ContactEmail = txtContactEmail.Text;
			GlobalSettings.SenderEmail = txtSenderEmail.Text;
			GlobalSettings.ErrorsEmails = GetErrorsEmails();
			GlobalSettings.SmtpServer = txtSmtpServer.Text;
			
			txtSmtpPort.Text = txtSmtpPort.Text.Trim();
			if(txtSmtpPort.Text.Length > 0) GlobalSettings.SmtpPort = int.Parse(txtSmtpPort.Text);
			else GlobalSettings.SmtpPort = -1;
			if(txtUsername.Text.Length > 0) {
				GlobalSettings.SmtpUsername = txtUsername.Text;
				GlobalSettings.SmtpPassword = txtPassword.Text;
			}
			else {
				GlobalSettings.SmtpUsername = "";
				GlobalSettings.SmtpPassword = "";
			}
			GlobalSettings.SmtpSsl = chkEnableSslForSmtp.Checked;

			// Save security configuration
			GlobalSettings.UsernameRegex = txtUsernameRegEx.Text;
			GlobalSettings.PasswordRegex = txtPasswordRegEx.Text;

			GlobalSettings.MaxFileSize = int.Parse(txtMaxFileSize.Text);
			LoggingLevel level = LoggingLevel.AllMessages;
			if(rdoAllMessages.Checked) level = LoggingLevel.AllMessages;
			else if(rdoWarningsAndErrors.Checked) level = LoggingLevel.WarningsAndErrors;
			else if(rdoErrorsOnly.Checked) level = LoggingLevel.ErrorsOnly;
			else level = LoggingLevel.DisableLog;
			GlobalSettings.LoggingLevel = level;
			GlobalSettings.MaxLogSize = int.Parse(txtMaxLogSize.Text);

			// Save advanced configuration
			GlobalSettings.DisableAutomaticVersionCheck = !chkEnableAutomaticUpdateChecks.Checked;
			GlobalSettings.EnableViewStateCompression = chkEnableViewStateCompression.Checked;
			GlobalSettings.EnableHttpCompression = chkEnableHttpCompression.Checked;

			Content.InvalidateAllPages();

			lblResult.CssClass = "resultok";
			lblResult.Text = Properties.Messages.ConfigSaved;
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

			Log.LogEntry("Provider DLL upload requested " + upDll.FileName, EntryType.General, SessionFacade.CurrentUsername);

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
			IPagesStorageProviderV30 from = Collectors.CollectorsBox.PagesProviderCollector.GetProvider(lstPagesSource.SelectedValue, currentWiki);
			IPagesStorageProviderV30 to = Collectors.CollectorsBox.PagesProviderCollector.GetProvider(lstPagesDestination.SelectedValue, currentWiki);

			Log.LogEntry("Pages data migration requested from " + from.Information.Name + " to " + to.Information.Name, EntryType.General, SessionFacade.CurrentUsername);

			//DataMigrator.MigratePagesStorageProviderData(from, to);

			lblMigratePagesResult.CssClass = "resultok";
			lblMigratePagesResult.Text = Properties.Messages.DataMigrated;
		}

		protected void btnMigrateUsers_Click(object sender, EventArgs e) {
			IUsersStorageProviderV30 from = Collectors.CollectorsBox.UsersProviderCollector.GetProvider(lstUsersSource.SelectedValue, currentWiki);
			IUsersStorageProviderV30 to = Collectors.CollectorsBox.UsersProviderCollector.GetProvider(lstUsersDestination.SelectedValue, currentWiki);

			Log.LogEntry("Users data migration requested from " + from.Information.Name + " to " + to.Information.Name, EntryType.General, SessionFacade.CurrentUsername);

			//DataMigrator.MigrateUsersStorageProviderData(from, to, true);

			lblMigrateUsersResult.CssClass = "resultok";
			lblMigrateUsersResult.Text = Properties.Messages.DataMigrated;
		}

		protected void btnMigrateFiles_Click(object sender, EventArgs e) {
			IFilesStorageProviderV30 from = Collectors.CollectorsBox.FilesProviderCollector.GetProvider(lstFilesSource.SelectedValue, currentWiki);
			IFilesStorageProviderV30 to = Collectors.CollectorsBox.FilesProviderCollector.GetProvider(lstFilesDestination.SelectedValue, currentWiki);

			Log.LogEntry("Files data migration requested from " + from.Information.Name + " to " + to.Information.Name, EntryType.General, SessionFacade.CurrentUsername);

			//DataMigrator.MigrateFilesStorageProviderData(from, to, Settings.Provider);

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

			Log.LogEntry("Settings data copy requested to " + to.Information.Name, EntryType.General, SessionFacade.CurrentUsername);

			try {
				to.Init(Host.Instance, txtSettingsDestinationConfig.Text, currentWiki);
			}
			catch(InvalidConfigurationException ex) {
				Log.LogEntry("Provider rejected configuration: " + ex.ToString(), EntryType.Error, Log.SystemUsername);
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
