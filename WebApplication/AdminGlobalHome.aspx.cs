
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScrewTurn.Wiki.PluginFramework;
using System.Globalization;

namespace ScrewTurn.Wiki {

	public partial class AdminGlobalHome : BasePage {

		protected void Page_Load(object sender, EventArgs e) {
			AdminMaster.RedirectToLoginIfNeeded();

			string currentWiki = DetectWiki();

			if(!AdminMaster.CanManageGlobalConfiguration(SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames(currentWiki))) UrlTools.Redirect("AccessDenied.aspx");

			PrintSystemStatus();
		}

		protected void btnShutdownConfirm_Click(object sender, EventArgs e) {
			Log.LogEntry("WebApp shutdown requested", EntryType.General, SessionFacade.CurrentUsername, null);
			Response.Clear();
			Response.Write(@"Web Application has been shut down, please go to the <a href=""Default.aspx"">home page</a>." + "\n\n");
			Response.Flush();
			Response.Close();
			Log.LogEntry("Executing WebApp shutdown", EntryType.General, Log.SystemUsername, null);
			HttpRuntime.UnloadAppDomain();
		}

		public void PrintSystemStatus() {
			StringBuilder sb = new StringBuilder(500);
			
			sb.Append(Properties.Messages.WikiVersion + ": <b>" + GlobalSettings.WikiVersion + "</b>" + "\n");
			if(!Page.IsPostBack) {
				sb.Append(CheckVersion());
			}
			sb.Append("<br />");
			sb.Append(Properties.Messages.ServerUptime + ": <b>" + Tools.TimeSpanToString(Tools.SystemUptime) + "</b> (" +
				Properties.Messages.MayBeInaccurate + ")");

			lblSystemStatusContent.Text = sb.ToString();
		}

		private string CheckVersion() {
			if(GlobalSettings.DisableAutomaticVersionCheck) return "";

			StringBuilder sb = new StringBuilder(100);
			sb.Append("(");

			string newVersion = null;
			string ignored = null;
			UpdateStatus status = Tools.GetUpdateStatus("http://www.screwturn.eu/Version4.0/Wiki/4.htm",
				GlobalSettings.WikiVersion, out newVersion, out ignored);

			if(status == UpdateStatus.Error) {
				sb.Append(@"<span class=""resulterror"">" + Properties.Messages.VersionCheckError + "</span>");
			}
			else if(status == UpdateStatus.NewVersionFound) {
				sb.Append(@"<span class=""resulterror"">" + Properties.Messages.NewVersionFound + ": <b>" + newVersion + "</b></span>");
			}
			else if(status == UpdateStatus.UpToDate) {
				sb.Append(@"<span class=""resultok"">" + Properties.Messages.WikiUpToDate + "</span>");
			}
			else throw new NotSupportedException();

			sb.Append(")");
			return sb.ToString();
		}

	}

}
