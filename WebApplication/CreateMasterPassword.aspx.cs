using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {
	public partial class CreateMasterPassword : BasePage {

		private string currentWiki = null;
		private string oldPassword = null;

		protected void Page_Load(object sender, EventArgs e) {
			currentWiki = DetectWiki();
	
			oldPassword = Settings.GetMasterPassword(currentWiki);

			if(!Page.IsPostBack && !string.IsNullOrEmpty(oldPassword)) {
				trOldPassword.Visible = true;
				lblDescriptionPwd.Visible = false;
			}
		}
		protected void btnSave_Click(object sender, EventArgs e) {
			Page.Validate();
			if(!Page.IsValid) return;

			if(!string.IsNullOrEmpty(oldPassword) && oldPassword != Hash.Compute(txtOldPassword.Text)) {
				// Old password is invalid
				lblResult.Visible = true;
				lblResult.CssClass = "resulterror";
				lblResult.Text = Properties.Messages.WrongPassword;
				return;
			}
			Settings.BeginBulkUpdate(currentWiki);
			Settings.SetMasterPassword(currentWiki, Hash.Compute(txtReNewPwd.Text));
			Settings.EndBulkUpdate(currentWiki);
			newAdminPassForm.Visible = false;
			newAdminPassOk.Visible = true;
			lblResult.Visible = true;
			lblResult.CssClass = "resultok";
			lblResult.Text = Properties.Messages.ConfigSaved;
			lnkMainRedirect.Visible = true;
			lnkMainRedirect.NavigateUrl = "~/";
			trOldPassword.Visible = false;
			lblDescriptionPwd.Visible = false;
			lblNewPwd.Visible = false;
			txtNewPwd.Visible = false;
			lblReNewPwd.Visible = false;
			txtReNewPwd.Visible = false;
			BtnSave.Visible = false;
		}
	}
}