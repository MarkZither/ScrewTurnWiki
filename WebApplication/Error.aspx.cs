
using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

namespace ScrewTurn.Wiki {

	public partial class Error : BasePage {

		protected void Page_Load(object sender, EventArgs e) {
			Page.Title = Properties.Messages.ErrorTitle + " - " + Settings.WikiTitle;

			Exception ex = Session["LastError"] as Exception;
			if(ex != null && SessionFacade.LoginKey != null &&
				AdminMaster.CanManageConfiguration(SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames())) {

				lblException.Text = ex.ToString();
			}
			else {
				pnlException.Visible = false;
			}
			Session["LastError"] = null;
		}

	}

}
