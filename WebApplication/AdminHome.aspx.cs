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

	public partial class AdminHome : BasePage {

		protected void Page_Load(object sender, EventArgs e) {
			AdminMaster.RedirectToLoginIfNeeded();

			string currentWiki = DetectWiki();

			if(!AdminMaster.CanManageConfiguration(SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames(currentWiki))) UrlTools.Redirect("AccessDenied.aspx");

			PrintSystemStatus();

			if(!Page.IsPostBack) {
				rptPages.DataBind();
				rptIndex.DataBind();

				DisplayOrphansCount();

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

		/// <summary>
		/// Displays the orphan pages count.
		/// </summary>
		private void DisplayOrphansCount() {
			string currentWiki = DetectWiki();
			int orphans = Pages.GetOrphanedPages(currentWiki, null).Length;
			foreach(NamespaceInfo nspace in Pages.GetNamespaces(currentWiki)) {
				orphans += Pages.GetOrphanedPages(currentWiki, nspace).Length;
			}
			lblOrphanPagesCount.Text = orphans.ToString();
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
		
		protected void rptPages_DataBinding(object sender, EventArgs e) {
			List<WantedPageRow> result = new List<WantedPageRow>(50);

			string currentWiki = DetectWiki();

			Dictionary<string, List<string>> links = Pages.GetWantedPages(currentWiki, null);
			foreach(KeyValuePair<string, List<string>> pair in links) {
				result.Add(new WantedPageRow("&lt;root&gt;", "", pair.Key, pair.Value));
			}

			foreach(NamespaceInfo nspace in Pages.GetNamespaces(currentWiki)) {
				links = Pages.GetWantedPages(currentWiki, nspace.Name);
				foreach(KeyValuePair<string, List<string>> pair in links) {
					result.Add(new WantedPageRow(nspace.Name, nspace.Name + ".", pair.Key, pair.Value));
				}
			}

			rptPages.DataSource = result;
		}

		protected void btnRebuildPageLinks_Click(object sender, EventArgs e) {
			string currentWiki = DetectWiki();

			RebuildPageLinks(Pages.GetPages(currentWiki, null));
			foreach(NamespaceInfo nspace in Pages.GetNamespaces(currentWiki)) {
				RebuildPageLinks(Pages.GetPages(currentWiki, nspace));
			}

			DisplayOrphansCount();
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

		protected void rptIndex_DataBinding(object sender, EventArgs e) {
			List<IndexRow> result = new List<IndexRow>(5);

			foreach(IPagesStorageProviderV30 prov in Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(DetectWiki())) {
				result.Add(new IndexRow(prov));
			}

			rptIndex.DataSource = result;
		}

		protected void rptIndex_ItemCommand(object sender, CommandEventArgs e) {
			Log.LogEntry("Index rebuild requested for " + e.CommandArgument as string, EntryType.General, SessionFacade.GetCurrentUsername());

			IPagesStorageProviderV30 provider = Collectors.CollectorsBox.PagesProviderCollector.GetProvider(e.CommandArgument as string, DetectWiki());
			provider.RebuildIndex();

			Log.LogEntry("Index rebuild completed for " + e.CommandArgument as string, EntryType.General, Log.SystemUsername);

			rptIndex.DataBind();
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

	/// <summary>
	/// Represents a missing or orphaned page.
	/// </summary>
	public class WantedPageRow {

		private string nspace, nspacePrefix, name, linkingPages;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:PageRow" /> class.
		/// </summary>
		/// <param name="nspace">The namespace.</param>
		/// <param name="nspacePrefix">The namespace prefix.</param>
		/// <param name="name">The full name.</param>
		/// <param name="linkingPages">The pages that link the wanted page.</param>
		public WantedPageRow(string nspace, string nspacePrefix, string name, List<string> linkingPages) {
			this.nspace = nspace;
			this.nspacePrefix = nspacePrefix;
			this.name = name;

			string currentWiki = Tools.DetectCurrentWiki();

			StringBuilder sb = new StringBuilder(100);
			for(int i = 0; i < linkingPages.Count; i++) {
				PageInfo page = Pages.FindPage(currentWiki, linkingPages[i]);
				if(page != null) {
					PageContent content = Content.GetPageContent(page);

					sb.AppendFormat(@"<a href=""{0}{1}"" title=""{2}"" target=""_blank"">{2}</a>, ", page.FullName, GlobalSettings.PageExtension,
						FormattingPipeline.PrepareTitle(currentWiki, content.Title, false, FormattingContext.Other, page));
				}
			}
			this.linkingPages = sb.ToString().TrimEnd(' ', ',');
		}

		/// <summary>
		/// Gets the namespace.
		/// </summary>
		public string Nspace {
			get { return nspace; }
		}

		/// <summary>
		/// Gets the namespace prefix.
		/// </summary>
		public string NspacePrefix {
			get { return nspacePrefix; }
		}

		/// <summary>
		/// Gets the full name.
		/// </summary>
		public string Name {
			get { return name; }
		}

		/// <summary>
		/// Gets the linker pages.
		/// </summary>
		public string LinkingPages {
			get { return linkingPages; }
		}

	}

	/// <summary>
	/// Represents the status of a search engine index.
	/// </summary>
	public class IndexRow {

		private string provider, providerType;
		private bool isOk;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:IndexRow" /> class.
		/// </summary>
		/// <param name="provider">The original provider.</param>
		public IndexRow(IPagesStorageProviderV30 provider) {
			this.provider = provider.Information.Name;
			providerType = provider.GetType().FullName;

			this.isOk = !provider.IsIndexCorrupted;
		}

		/// <summary>
		/// Gets the provider.
		/// </summary>
		public string Provider {
			get { return provider; }
		}

		/// <summary>
		/// Gets the provider type.
		/// </summary>
		public string ProviderType {
			get { return providerType; }
		}

		/// <summary>
		/// Gets a value indicating whether the index is OK.
		/// </summary>
		public bool IsOK {
			get { return isOk; }
		}

	}

}
