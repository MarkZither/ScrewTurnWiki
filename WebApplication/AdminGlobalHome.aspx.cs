﻿
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

			if(!AdminMaster.CanManageConfiguration(SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames(currentWiki))) UrlTools.Redirect("AccessDenied.aspx");

			PrintSystemStatus();

			if(!Page.IsPostBack) {
				string anon = Settings.GetAnonymousGroup(currentWiki);
				foreach(UserGroup group in Users.GetUserGroups(currentWiki)) {
					if(group.Name != anon) {
						ListItem item = new ListItem(group.Name, group.Name);
						item.Selected = true;
						lstGroups.Items.Add(item);
					}
				}
			}
		}

		protected void cvGroups_ServerValidate(object sender, ServerValidateEventArgs e) {
			e.IsValid = false;
			foreach(ListItem item in lstGroups.Items) {
				if(item.Selected) {
					e.IsValid = true;
					break;
				}
			}
		}

		protected void btnSendBulkEmail_Click(object sender, EventArgs e) {
			lblEmailResult.CssClass = "";
			lblEmailResult.Text = "";

			Page.Validate("email");
			if(!Page.IsValid) return;

			string currentWiki = DetectWiki();
			List<string> emails = new List<string>();
			foreach(ListItem item in lstGroups.Items) {
				if(item.Selected) {
					UserGroup group = Users.FindUserGroup(currentWiki, item.Value);
					if(group != null) {
						foreach(string user in group.Users) {
							UserInfo u = Users.FindUser(currentWiki, user);
							if(u != null) emails.Add(u.Email);
						}
					}
				}
			}

			EmailTools.AsyncSendMassEmail(emails.ToArray(), GlobalSettings.SenderEmail,
				txtSubject.Text, txtBody.Text, false);

			lblEmailResult.CssClass = "resultok";
			lblEmailResult.Text = Properties.Messages.MassEmailSent;
		}
		
		/// <summary>
		/// Rebuilds the page links for the specified pages.
		/// </summary>
		/// <param name="pages">The pages.</param>
		private void RebuildPageLinks(IList<PageInfo> pages) {
			string currentWiki = DetectWiki();

			foreach(PageInfo page in pages) {
				PageContent content = Content.GetPageContent(page);
				Pages.StorePageOutgoingLinks(currentWiki, page, content.Content);				
			}
		}

		protected void btnShutdownConfirm_Click(object sender, EventArgs e) {
			Log.LogEntry("WebApp shutdown requested", EntryType.General, SessionFacade.CurrentUsername);
			Response.Clear();
			Response.Write(@"Web Application has been shut down, please go to the <a href=""Default.aspx"">home page</a>." + "\n\n");
			Response.Flush();
			Response.Close();
			Log.LogEntry("Executing WebApp shutdown", EntryType.General, Log.SystemUsername);
			HttpRuntime.UnloadAppDomain();
		}

		public void PrintSystemStatus() {
			StringBuilder sb = new StringBuilder(500);
			int inactive = 0;

			List<UserInfo> users = Users.GetUsers(DetectWiki());
			for(int i = 0; i < users.Count; i++) {
				if(!users[i].Active) inactive++;
			}
			sb.Append(Properties.Messages.UserCount + ": <b>" + users.Count.ToString() + "</b> (" + inactive.ToString() + " " + Properties.Messages.InactiveUsers + ")<br />" + "\n");
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
			UpdateStatus status = Tools.GetUpdateStatus("http://www.screwturn.eu/Version/Wiki/3.htm",
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
