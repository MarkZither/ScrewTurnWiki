
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

		/// <summary>
		/// Cvs the check old password.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="System.Web.UI.WebControls.ServerValidateEventArgs"/> instance containing the event data.</param>
		protected void cvCheckOldPassword(object sender, ServerValidateEventArgs e) {
			string pwd = Hash.Compute(txtBoxOldPassword.Text);
			if(pwd == GlobalSettings.GetMasterPassword())
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

		protected void cvRePassword_ServerValidate(object sender, ServerValidateEventArgs e) {
			e.IsValid = txtNewPassword.Text == txtReNewPassword.Text;
		}

		protected void btnSave_Click(object sender, EventArgs e) {
			lblResult.CssClass = "";
			lblResult.Text = "";

			Page.Validate();

			if(!Page.IsValid) return;

			Log.LogEntry("Wiki Configuration change requested", EntryType.General, SessionFacade.CurrentUsername, currentWiki);

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

			// Save master password
			if(txtBoxOldPassword.Text != "" && txtBoxOldPassword.Text != null && txtBoxOldPassword.Text.Length != 0) {
				if(txtNewPassword.Text.Length != 0) {
					if(Hash.Compute(txtNewPassword.Text) == Hash.Compute(txtReNewPassword.Text))
						GlobalSettings.SetMasterPassword(Hash.Compute(txtNewPassword.Text));
				}
			}

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

	}

}
