
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

	public partial class AdminConfig : BasePage {

		protected void Page_Load(object sender, EventArgs e) {
			string currentWiki = DetectWiki();

			AdminMaster.RedirectToLoginIfNeeded();

			if(!AdminMaster.CanManageConfiguration(SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames(currentWiki))) UrlTools.Redirect("AccessDenied.aspx");

			StringBuilder sb = new StringBuilder(200);
			sb.Append("<script type=\"text/javascript\">\r\n<!--\r\n");
			sb.AppendFormat("\tvar __DateTimeFormatTextBox = '{0}';\r\n", txtDateTimeFormat.ClientID);
			sb.Append("// -->\r\n</script>");
			lblStrings.Text = sb.ToString();

			if(!Page.IsPostBack) {
				// Setup validation regular expressions
				revMainUrl.ValidationExpression = GlobalSettings.MainUrlRegex;
				revWikiTitle.ValidationExpression = GlobalSettings.WikiTitleRegex;
				revContactEmail.ValidationExpression = GlobalSettings.EmailRegex;
				revSenderEmail.ValidationExpression = GlobalSettings.EmailRegex;
				revSmtpServer.ValidationExpression = GlobalSettings.SmtpServerRegex;

				// Load current values
				LoadGeneralConfig(currentWiki);
				LoadContentConfig(currentWiki);
				LoadSecurityConfig(currentWiki);
				LoadAdvancedConfig();
			}
		}

		/// <summary>
		/// Loads the general configuration.
		/// </summary>
		/// <param name="currentWiki">The wiki.</param>
		private void LoadGeneralConfig(string currentWiki) {
			txtWikiTitle.Text = Settings.GetWikiTitle(currentWiki);
			txtMainUrl.Text = GlobalSettings.MainUrl;
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
		/// Populates the main pages list selecting the current one.
		/// </summary>
		/// <param name="current">The current page.</param>
		private void PopulateMainPages(string current) {
			current = current.ToLowerInvariant();

			List<PageInfo> pages = Pages.GetPages(DetectWiki(), null);
			lstMainPage.Items.Clear();
			foreach(PageInfo page in pages) {
				lstMainPage.Items.Add(new ListItem(page.FullName, page.FullName));
				if(page.FullName.ToLowerInvariant() == current) {
					lstMainPage.SelectedIndex = -1;
					lstMainPage.Items[lstMainPage.Items.Count - 1].Selected = true;
				}
			}
		}

		/// <summary>
		/// Populates the languages list selecting the current one.
		/// </summary>
		/// <param name="current">The current language.</param>
		private void PopulateLanguages(string current) {
			current = current.ToLowerInvariant();

			string[] langs = Tools.AvailableCultures;
			lstDefaultLanguage.Items.Clear();
			foreach(string lang in langs) {
				string[] fields = lang.Split('|');
				lstDefaultLanguage.Items.Add(new ListItem(fields[1], fields[0]));
				if(fields[0].ToLowerInvariant() == current) lstDefaultLanguage.Items[lstDefaultLanguage.Items.Count - 1].Selected = true;
			}
		}

		/// <summary>
		/// Populates the time zones list selecting the current one.
		/// </summary>
		/// <param name="current">The current time zone.</param>
		private void PopulateTimeZones(string current) {
			for(int i = 0; i < lstDefaultTimeZone.Items.Count; i++) {
				if(lstDefaultTimeZone.Items[i].Value == current) lstDefaultTimeZone.Items[i].Selected = true;
				else lstDefaultTimeZone.Items[i].Selected = false;
			}
		}

		/// <summary>
		/// Populates the date/time format templates list.
		/// </summary>
		private void PopulateDateTimeFormats() {
			StringBuilder sb = new StringBuilder(500);
			DateTime test = DateTime.Now;
			sb.Append(@"<option value=""ddd', 'dd' 'MMM' 'yyyy' 'HH':'mm"">" + test.ToString("ddd, dd MMM yyyy HH':'mm") + "</option>");
			sb.Append(@"<option value=""dddd', 'dd' 'MMMM' 'yyyy' 'HH':'mm"">" + test.ToString("dddd, dd MMMM yyyy HH':'mm") + "</option>");
			sb.Append(@"<option value=""yyyy'/'MM'/'dd' 'HH':'mm"">" + test.ToString("yyyy'/'MM'/'dd' 'HH':'mm") + "</option>");
			sb.Append(@"<option value=""MM'/'dd'/'yyyy' 'HH':'mm"">" + test.ToString("MM'/'dd'/'yyyy' 'HH':'mm") + "</option>");
			sb.Append(@"<option value=""dd'/'MM'/'yyyy' 'HH':'mm"">" + test.ToString("dd'/'MM'/'yyyy' 'HH':'mm") + "</option>");

			sb.Append(@"<option value=""ddd', 'dd' 'MMM' 'yyyy' 'hh':'mm' 'tt"">" + test.ToString("ddd, dd MMM yyyy hh':'mm' 'tt") + "</option>");
			sb.Append(@"<option value=""dddd', 'dd' 'MMMM' 'yyyy' 'hh':'mm' 'tt"">" + test.ToString("dddd, dd MMMM yyyy hh':'mm' 'tt") + "</option>");
			sb.Append(@"<option value=""yyyy'/'MM'/'dd' 'hh':'mm' 'tt"">" + test.ToString("yyyy'/'MM'/'dd' 'hh':'mm' 'tt") + "</option>");
			sb.Append(@"<option value=""MM'/'dd'/'yyyy' 'hh':'mm' 'tt"">" + test.ToString("MM'/'dd'/'yyyy' 'hh':'mm' 'tt") + "</option>");
			sb.Append(@"<option value=""dd'/'MM'/'yyyy' 'hh':'mm' 'tt"">" + test.ToString("dd'/'MM'/'yyyy' 'hh':'mm' 'tt") + "</option>");

			lblDateTimeFormatTemplates.Text = sb.ToString();
		}

		/// <summary>
		/// Loads the content configuration.
		/// </summary>
		/// <param name="currentWiki">The wiki.</param>
		private void LoadContentConfig(string currentWiki) {
			PopulateMainPages(Settings.GetDefaultPage(currentWiki));
			txtDateTimeFormat.Text = Settings.GetDateTimeFormat(currentWiki);
			PopulateDateTimeFormats();
			PopulateLanguages(Settings.GetDefaultLanguage(currentWiki));
			PopulateTimeZones(Settings.GetDefaultLanguage(currentWiki).ToString());
			txtMaxRecentChangesToDisplay.Text = Settings.GetMaxRecentChangesToDisplay(currentWiki).ToString();

			lstRssFeedsMode.SelectedIndex = -1;
			switch(Settings.GetRssFeedsMode(currentWiki)) {
				case RssFeedsMode.FullText:
					lstRssFeedsMode.SelectedIndex = 0;
					break;
				case RssFeedsMode.Summary:
					lstRssFeedsMode.SelectedIndex = 1;
					break;
				case RssFeedsMode.Disabled:
					lstRssFeedsMode.SelectedIndex = 2;
					break;
			}

			chkEnableDoubleClickEditing.Checked = Settings.GetEnableDoubleClickEditing(currentWiki);
			chkEnableSectionEditing.Checked = Settings.GetEnableSectionEditing(currentWiki);
			chkEnableSectionAnchors.Checked = Settings.GetEnableSectionAnchors(currentWiki);
			chkEnablePageToolbar.Checked = Settings.GetEnablePageToolbar(currentWiki);
			chkEnableViewPageCode.Checked = Settings.GetEnableViewPageCodeFeature(currentWiki);
			chkEnablePageInfoDiv.Checked = Settings.GetEnablePageInfoDiv(currentWiki);
			chkEnableBreadcrumbsTrail.Checked = !Settings.GetDisableBreadcrumbsTrail(currentWiki);
			chkAutoGeneratePageNames.Checked = Settings.GetAutoGeneratePageNames(currentWiki);
			chkProcessSingleLineBreaks.Checked = Settings.GetProcessSingleLineBreaks(currentWiki);
			chkUseVisualEditorAsDefault.Checked = Settings.GetUseVisualEditorAsDefault(currentWiki);
			if(Settings.GetKeptBackupNumber(currentWiki) == -1) txtKeptBackupNumber.Text = "";
			else txtKeptBackupNumber.Text = Settings.GetKeptBackupNumber(currentWiki).ToString();
			chkDisplayGravatars.Checked = Settings.GetDisplayGravatars(currentWiki);
			txtListSize.Text = Settings.GetListSize(currentWiki).ToString();
		}

		/// <summary>
		/// Populates the activation mode list selecting the current one.
		/// </summary>
		/// <param name="current">The current account activation mode.</param>
		private void PopulateAccountActivationMode(AccountActivationMode current) {
			if(current == AccountActivationMode.Email) lstAccountActivationMode.SelectedIndex = 0;
			else if(current == AccountActivationMode.Administrator) lstAccountActivationMode.SelectedIndex = 1;
			else lstAccountActivationMode.SelectedIndex = 2;
		}

		/// <summary>
		/// Populates the default groups lists, selecting the current ones.
		/// </summary>
		/// <param name="users">The current default users group.</param>
		/// <param name="admins">The current default administrators group.</param>
		/// <param name="anonymous">The current default anonymous users group.</param>
		private void PopulateDefaultGroups(string users, string admins, string anonymous) {
			users = users.ToLowerInvariant();
			admins = admins.ToLowerInvariant();
			anonymous = anonymous.ToLowerInvariant();

			lstDefaultUsersGroup.Items.Clear();
			lstDefaultAdministratorsGroup.Items.Clear();
			lstDefaultAnonymousGroup.Items.Clear();
			foreach(UserGroup group in Users.GetUserGroups(DetectWiki())) {
				string lowerName = group.Name.ToLowerInvariant();

				lstDefaultUsersGroup.Items.Add(new ListItem(group.Name, group.Name));
				if(lowerName == users) {
					lstDefaultUsersGroup.SelectedIndex = -1;
					lstDefaultUsersGroup.Items[lstDefaultUsersGroup.Items.Count - 1].Selected = true;
				}

				lstDefaultAdministratorsGroup.Items.Add(new ListItem(group.Name, group.Name));
				if(lowerName == admins) {
					lstDefaultAdministratorsGroup.SelectedIndex = -1;
					lstDefaultAdministratorsGroup.Items[lstDefaultAdministratorsGroup.Items.Count - 1].Selected = true;
				}

				lstDefaultAnonymousGroup.Items.Add(new ListItem(group.Name, group.Name));
				if(lowerName == anonymous) {
					lstDefaultAnonymousGroup.SelectedIndex = -1;
					lstDefaultAnonymousGroup.Items[lstDefaultAnonymousGroup.Items.Count - 1].Selected = true;
				}
			}
		}

		/// <summary>
		/// Loads the security configuration.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		private void LoadSecurityConfig(string currentWiki) {
			chkAllowUsersToRegister.Checked = Settings.UsersCanRegister(currentWiki);
			txtPasswordRegEx.Text = GlobalSettings.PasswordRegex;
			txtUsernameRegEx.Text = GlobalSettings.UsernameRegex;
			PopulateAccountActivationMode(Settings.GetAccountActivationMode(currentWiki));
			PopulateDefaultGroups(Settings.GetUsersGroup(currentWiki),
				Settings.GetAdministratorsGroup(currentWiki),
				Settings.GetAnonymousGroup(currentWiki));
			chkEnableCaptchaControl.Checked = !Settings.GetDisableCaptchaControl(currentWiki);
			chkPreventConcurrentEditing.Checked = Settings.GetDisableConcurrentEditing(currentWiki);

			switch(Settings.GetModerationMode(currentWiki)) {
				case ChangeModerationMode.None:
					rdoNoModeration.Checked = true;
					break;
				case ChangeModerationMode.RequirePageViewingPermissions:
					rdoRequirePageViewingPermissions.Checked = true;
					break;
				case ChangeModerationMode.RequirePageEditingPermissions:
					rdoRequirePageEditingPermissions.Checked = true;
					break;
			}

			txtExtensionsAllowed.Text = string.Join(", ", Settings.GetAllowedFileTypes(currentWiki));

			lstFileDownloadCountFilterMode.SelectedIndex = -1;
			switch(Settings.GetFileDownloadCountFilterMode(currentWiki)) {
				case FileDownloadCountFilterMode.CountAll:
					lstFileDownloadCountFilterMode.SelectedIndex = 0;
					txtFileDownloadCountFilter.Enabled = false;
					break;
				case FileDownloadCountFilterMode.CountSpecifiedExtensions:
					lstFileDownloadCountFilterMode.SelectedIndex = 1;
					txtFileDownloadCountFilter.Enabled = true;
					txtFileDownloadCountFilter.Text = string.Join(", ", Settings.GetFileDownloadCountFilter(currentWiki));
					break;
				case FileDownloadCountFilterMode.ExcludeSpecifiedExtensions:
					txtFileDownloadCountFilter.Text = string.Join(", ", Settings.GetFileDownloadCountFilter(currentWiki));
					txtFileDownloadCountFilter.Enabled = true;
					lstFileDownloadCountFilterMode.SelectedIndex = 2;
					break;
				default:
					throw new NotSupportedException();
			}

			txtMaxFileSize.Text = GlobalSettings.MaxFileSize.ToString();
			chkAllowScriptTags.Checked = Settings.GetScriptTagsAllowed(currentWiki);
			txtMaxLogSize.Text = GlobalSettings.MaxLogSize.ToString();
			txtIpHostFilter.Text = Settings.GetIpHostFilter(currentWiki);
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

		protected void btnAutoWikiUrl_Click(object sender, EventArgs e) {
			string url = Tools.GetCurrentUrlFixed();
			// Assume the URL contains AdminConfig.aspx
			url = url.Substring(0, url.ToLowerInvariant().IndexOf("adminconfig.aspx"));
			txtMainUrl.Text = url;
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

		protected void cvDateTimeFormat_ServerValidate(object sender, ServerValidateEventArgs e) {
			try {
				DateTime.Now.ToString(txtDateTimeFormat.Text);
				e.IsValid = true;
			}
			catch {
				e.IsValid = false;
			}
		}


		/// <summary>
		/// Cvs the check old password.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="System.Web.UI.WebControls.ServerValidateEventArgs"/> instance containing the event data.</param>
		protected void cvCheckOldPassword(object sender, ServerValidateEventArgs e) {
			string pwd = Hash.Compute(txtBoxOldPassword.Text);
			if(pwd == Settings.GetMasterPassword(DetectWiki()))
				if((txtNewPassword.Text.Length != 0) && (txtNewPassword.Text != null)) 
					e.IsValid = true;
				else {
					e.IsValid = false;
					((CustomValidator)sender).ErrorMessage = Properties.Messages.PasswordEmpty;
				}
			else {
				e.IsValid = false;
				((CustomValidator)sender).ErrorMessage = Properties.Messages.WrongPassword;
			}
		}
	
		private string[] GetAllowedFileExtensions() {
			return txtExtensionsAllowed.Text.Replace(" ", "").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
		}

		protected void cvExtensionsAllowed_ServerValidate(object sender, ServerValidateEventArgs e) {
			string[] allowed = GetAllowedFileExtensions();

			bool wildcardFound =
				(from s in allowed
				 where s == "*"
				 select s).Any();

			e.IsValid = allowed.Length <= 1 || allowed.Length > 1 && !wildcardFound;
		}

		protected void lstFileDownloadCountFilterMode_SelectedIndexChanged(object sender, EventArgs e) {
			if(lstFileDownloadCountFilterMode.SelectedValue == FileDownloadCountFilterMode.CountAll.ToString()) {
				txtFileDownloadCountFilter.Enabled = false;
			}
			else {
				txtFileDownloadCountFilter.Enabled = true;
			}
		}

		protected void btnSave_Click(object sender, EventArgs e) {
			lblResult.CssClass = "";
			lblResult.Text = "";

			Page.Validate();

			if(!Page.IsValid) return;

			Log.LogEntry("Wiki Configuration change requested", EntryType.General, SessionFacade.CurrentUsername);

			string currentWiki = DetectWiki();

			Settings.BeginBulkUpdate(currentWiki);

			// Save general configuration
			Settings.SetWikiTitle(currentWiki, txtWikiTitle.Text);
			GlobalSettings.MainUrl = txtMainUrl.Text;
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

			// Save content configuration
			Settings.SetTheme(currentWiki, null, ThemeRootSelector.SelectedProvider + "|" + ThemeRootSelector.SelectedThemes);
			Settings.SetDefaultPage(currentWiki, lstMainPage.SelectedValue);
			Settings.SetDateTimeFormat(currentWiki, txtDateTimeFormat.Text);
			Settings.SetDefaultLanguage(currentWiki, lstDefaultLanguage.SelectedValue);
			Settings.SetDefaultTimezone(currentWiki, int.Parse(lstDefaultTimeZone.SelectedValue));
			Settings.SetMaxRecentChangesToDisplay(currentWiki, int.Parse(txtMaxRecentChangesToDisplay.Text));
			Settings.SetRssFeedsMode(currentWiki, (RssFeedsMode)Enum.Parse(typeof(RssFeedsMode), lstRssFeedsMode.SelectedValue));
			Settings.SetEnableDoubleClickEditing(currentWiki, chkEnableDoubleClickEditing.Checked);
			Settings.SetEnableSectionEditing(currentWiki, chkEnableSectionEditing.Checked);
			Settings.SetEnableSectionAnchors(currentWiki, chkEnableSectionAnchors.Checked);
			Settings.SetEnablePageToolbar(currentWiki, chkEnablePageToolbar.Checked);
			Settings.SetEnableViewPageCodeFeature(currentWiki, chkEnableViewPageCode.Checked);
			Settings.SetEnablePageInfoDiv(currentWiki, chkEnablePageInfoDiv.Checked);
			Settings.SetDisableBreadcrumbsTrail(currentWiki, !chkEnableBreadcrumbsTrail.Checked);
			Settings.SetAutoGeneratePageNames(currentWiki, chkAutoGeneratePageNames.Checked);
			Settings.SetProcessSingleLineBreaks(currentWiki, chkProcessSingleLineBreaks.Checked);
			Settings.SetUseVisualEditorAsDefault(currentWiki, chkUseVisualEditorAsDefault.Checked);
			if(txtKeptBackupNumber.Text == "") Settings.SetKeptBackupNumber(currentWiki, -1);
			else Settings.SetKeptBackupNumber(currentWiki, int.Parse(txtKeptBackupNumber.Text));
			Settings.SetDisplayGravatars(currentWiki, chkDisplayGravatars.Checked);
			Settings.SetListSize(currentWiki, int.Parse(txtListSize.Text));
			if(txtBoxOldPassword.Text != "" && txtBoxOldPassword.Text != null && txtBoxOldPassword.Text.Length != 0) {
				if (txtNewPassword.Text.Length != 0){
						if	(Hash.Compute(txtNewPassword.Text) == Hash.Compute(txtReNewPassword.Text))	
							Settings.SetMasterPassword(currentWiki, Hash.Compute(txtNewPassword.Text));
					}
				}
			// Save security configuration
			Settings.SetUsersCanRegister(currentWiki, chkAllowUsersToRegister.Checked);
			GlobalSettings.UsernameRegex = txtUsernameRegEx.Text;
			GlobalSettings.PasswordRegex = txtPasswordRegEx.Text;
			AccountActivationMode mode = AccountActivationMode.Email;
			switch(lstAccountActivationMode.SelectedValue.ToLowerInvariant()) {
				case "email":
					mode = AccountActivationMode.Email;
					break;
				case "admin":
					mode = AccountActivationMode.Administrator;
					break;
				case "auto":
					mode = AccountActivationMode.Auto;
					break;
			}
			Settings.SetAccountActivationMode(currentWiki, mode);
			Settings.SetUsersGroup(currentWiki, lstDefaultUsersGroup.SelectedValue);
			Settings.SetAdministratorsGroup(currentWiki, lstDefaultAdministratorsGroup.SelectedValue);
			Settings.SetAnonymousGroup(currentWiki, lstDefaultAnonymousGroup.SelectedValue);
			Settings.SetDisableCaptchaControl(currentWiki, !chkEnableCaptchaControl.Checked);
			Settings.SetDisableConcurrentEditing(currentWiki, chkPreventConcurrentEditing.Checked);

			if(rdoNoModeration.Checked) Settings.SetModerationMode(currentWiki, ChangeModerationMode.None);
			else if(rdoRequirePageViewingPermissions.Checked) Settings.SetModerationMode(currentWiki, ChangeModerationMode.RequirePageViewingPermissions);
			else if(rdoRequirePageEditingPermissions.Checked) Settings.SetModerationMode(currentWiki, ChangeModerationMode.RequirePageEditingPermissions);

			Settings.SetAllowedFileTypes(currentWiki, GetAllowedFileExtensions());

			Settings.SetFileDownloadCountFilterMode(currentWiki, (FileDownloadCountFilterMode)Enum.Parse(typeof(FileDownloadCountFilterMode), lstFileDownloadCountFilterMode.SelectedValue));
			Settings.SetFileDownloadCountFilter(currentWiki, txtFileDownloadCountFilter.Text.Replace(" ", "").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));

			GlobalSettings.MaxFileSize = int.Parse(txtMaxFileSize.Text);
			Settings.SetScriptTagsAllowed(currentWiki, chkAllowScriptTags.Checked);
			LoggingLevel level = LoggingLevel.AllMessages;
			if(rdoAllMessages.Checked) level = LoggingLevel.AllMessages;
			else if(rdoWarningsAndErrors.Checked) level = LoggingLevel.WarningsAndErrors;
			else if(rdoErrorsOnly.Checked) level = LoggingLevel.ErrorsOnly;
			else level = LoggingLevel.DisableLog;
			GlobalSettings.LoggingLevel = level;
			GlobalSettings.MaxLogSize = int.Parse(txtMaxLogSize.Text);
			Settings.SetIpHostFilter(currentWiki, txtIpHostFilter.Text);

			// Save advanced configuration
			GlobalSettings.DisableAutomaticVersionCheck = !chkEnableAutomaticUpdateChecks.Checked;
			GlobalSettings.EnableViewStateCompression = chkEnableViewStateCompression.Checked;
			GlobalSettings.EnableHttpCompression = chkEnableHttpCompression.Checked;

			Settings.EndBulkUpdate(currentWiki);

			Content.InvalidateAllPages();

			lblResult.CssClass = "resultok";
			lblResult.Text = Properties.Messages.ConfigSaved;
		}

	}

}
