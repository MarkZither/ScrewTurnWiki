using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {
	public partial class CreateMasterPassword : BasePage {

		protected void Page_Load(object sender, EventArgs e) {
			
		}

		protected void btnSave_Click(object sender, EventArgs e) {
			Page.Validate();
			if(!Page.IsValid) return;

			GlobalSettings.SetMasterPassword(Hash.Compute(txtReNewPwd.Text));
			newAdminPassForm.Visible = false;
			newAdminPassOk.Visible = true;
			lblResult.CssClass = "resultok";
			lblResult.Text = Properties.Messages.ConfigSaved;
			lnkMainRedirect.Visible = true;
			lnkMainRedirect.NavigateUrl = "~/";
			lblDescriptionPwd.Visible = false;
			lblNewPwd.Visible = false;
			txtNewPwd.Visible = false;
			lblReNewPwd.Visible = false;
			txtReNewPwd.Visible = false;
			BtnSave.Visible = false;
		}
	}
}