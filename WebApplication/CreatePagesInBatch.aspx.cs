using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {
	public partial class CreatePagesInBatch : System.Web.UI.Page {
		protected void Page_Load(object sender, EventArgs e) {
			for(int i = 0; i < 1000; i++) {
				Pages.SetPageContent("test1", "", "pagetest" + i, "Title of Pahe Test " + i, "testuser", DateTime.Now, "", "Content of the test page " + i, new string[0], "", SaveMode.Normal);
			}
		}
	}
}