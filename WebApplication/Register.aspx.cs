
using System;
using System.Data;
using System.Drawing;
using System.Configuration;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using ScrewTurn.Wiki.PluginFramework;
using System.Collections.Generic;

namespace ScrewTurn.Wiki {

	public partial class Register : BasePage {

		private string currentWiki = null;

		protected void Page_Load(object sender, EventArgs e) {
			currentWiki = DetectWiki();
			
			if(SessionFacade.LoginKey != null) {
				UrlTools.Redirect("Profile.aspx");
				return;
			}

			// Test whether the default Users Provider is read-only
			IUsersStorageProviderV40 p = Collectors.CollectorsBox.UsersProviderCollector.GetProvider(GlobalSettings.DefaultUsersProvider, currentWiki);
			if(p.UserAccountsReadOnly) {
				Log.LogEntry("Default Users Provider (" + p.Information.Name + ") is read-only, aborting Account Creation", EntryType.Warning, Log.SystemUsername, currentWiki);
				UrlTools.Redirect(UrlTools.BuildUrl(currentWiki, "Error.aspx"));
			}

			PrintRegisterNotice();

			Page.Title = Properties.Messages.RegisterTitle + " - " + Settings.GetWikiTitle(currentWiki);

			if(!Settings.UsersCanRegister(currentWiki)) {
				UrlTools.Redirect(UrlTools.BuildUrl(currentWiki, "AccessDenied.aspx"));
			}

			switch(Settings.GetAccountActivationMode(currentWiki)) {
				case AccountActivationMode.Email:
					lblAccountActivationMode.Text = Properties.Messages.ActivationEmail;
					break;
				case AccountActivationMode.Administrator:
					lblAccountActivationMode.Text = Properties.Messages.ActivationAdmin;
					break;
				case AccountActivationMode.Auto:
					lblAccountActivationMode.Text = Properties.Messages.ActivationAuto;
					break;
			}

			if(Settings.GetDisableCaptchaControl(currentWiki)) {
				lblCaptcha.Visible = false;
				captcha.Visible = false;
			}

			if(!Page.IsPostBack) {
				rxvUserName.ValidationExpression = GlobalSettings.UsernameRegex;
				rxvDisplayName.ValidationExpression = GlobalSettings.DisplayNameRegex;
				rxvEmail1.ValidationExpression = GlobalSettings.EmailRegex;
				rxvPassword1.ValidationExpression = GlobalSettings.PasswordRegex;
			}

			if(Page.IsPostBack) {
				// Preserve password value (a bit insecure but much more usable)
				txtPassword1.Attributes.Add("value", txtPassword1.Text);
				txtPassword2.Attributes.Add("value", txtPassword2.Text);
			}
		}

		/// <summary>
		/// Prints the register notice.
		/// </summary>
		private void PrintRegisterNotice() {
			string n = Settings.GetProvider(currentWiki).GetMetaDataItem(MetaDataItem.RegisterNotice, null);
			if(!string.IsNullOrEmpty(n)) {
				n = FormattingPipeline.FormatWithPhase1And2(currentWiki, n, false, FormattingContext.Other, null);

			}
			if(!string.IsNullOrEmpty(n)) lblRegisterDescription.Text = FormattingPipeline.FormatWithPhase3(currentWiki, n, FormattingContext.Other, null);
		}

		protected void btnRegister_Click(object sender, EventArgs e) {
			if(!Settings.UsersCanRegister(currentWiki)) return;

			lblResult.Text = "";
			lblResult.CssClass = "";

			Page.Validate();
			if(!Page.IsValid) return;

			// Ready to save the user
			Log.LogEntry("Account creation requested for " + txtUsername.Text, EntryType.General, Log.SystemUsername, currentWiki);
			Users.AddUser(currentWiki, txtUsername.Text, txtDisplayName.Text, txtPassword1.Text, txtEmail1.Text,
				Settings.GetAccountActivationMode(currentWiki) == AccountActivationMode.Auto, null);

			UserInfo newUser = Users.FindUser(currentWiki, txtUsername.Text);

			// Set membership to default Users group
			Users.SetUserMembership(newUser, new string[] { Settings.GetUsersGroup(currentWiki) });

			if(Settings.GetAccountActivationMode(currentWiki) == AccountActivationMode.Email) {
				string body = Settings.GetProvider(currentWiki).GetMetaDataItem(MetaDataItem.AccountActivationMessage, null);
				body = body.Replace("##WIKITITLE##", Settings.GetWikiTitle(currentWiki)).Replace("##USERNAME##", newUser.Username).Replace("##EMAILADDRESS##", GlobalSettings.ContactEmail);
				body = body.Replace("##ACTIVATIONLINK##", Settings.GetMainUrl(currentWiki) + "Login.aspx?Activate=" + Tools.ComputeSecurityHash(currentWiki, newUser.Username, newUser.Email, newUser.DateTime) + "&Username=" + Tools.UrlEncode(newUser.Username));
				EmailTools.AsyncSendEmail(txtEmail1.Text, GlobalSettings.SenderEmail, "Account Activation - " + Settings.GetWikiTitle(currentWiki), body, false);
			}

			lblResult.CssClass = "resultok";
			lblResult.Text = "<br /><br />" + Properties.Messages.AccountCreated;
			btnRegister.Enabled = false;
			pnlRegister.Visible = false;
		}
		
		protected void cvUsername_ServerValidate(object source, ServerValidateEventArgs args) {
			txtUsername.Text = txtUsername.Text.Trim();

			if(txtUsername.Text.ToLowerInvariant().Equals("admin") || txtUsername.Text.ToLowerInvariant().Equals("guest")) {
				args.IsValid = false;
			}
			else {
				UserInfo u = Users.FindUser(currentWiki, txtUsername.Text);
				if(u != null) {
					args.IsValid = false;
				}
			}
		}
		
		protected void cvPassword1_ServerValidate(object source, ServerValidateEventArgs args) {
			args.IsValid = (string.Compare(txtPassword1.Text, txtPassword2.Text, true) == 0);
		}
		
		protected void cvPassword2_ServerValidate(object source, ServerValidateEventArgs args) {
			args.IsValid = true;
		}
		
		protected void cvEmail1_ServerValidate(object source, ServerValidateEventArgs args) {
			args.IsValid = (string.Compare(txtEmail1.Text, txtEmail2.Text, true) == 0);
		}
		
		protected void cvEmail2_ServerValidate(object source, ServerValidateEventArgs args) {
			args.IsValid = true;
		}
		
	}

}
