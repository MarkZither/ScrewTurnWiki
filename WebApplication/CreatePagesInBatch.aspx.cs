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
				PageContent page = Pages.SetPageContent("root", "", "pagetest" + i, "Title of Pahe Test " + i, "testuser", DateTime.UtcNow, "", "Content of the test page " + i, new string[0], "", SaveMode.Normal);
				for(int j = 0; j < 200; j++) {
					Pages.SetPageContent("root", "", NameTools.GetLocalName(page.FullName), page.Title, page.User, DateTime.UtcNow, "", "Content of test page" + i + " at revision " + j, page.Keywords, page.Description, SaveMode.Backup);
				}
			}
		}
	}
}