
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

		string currentWiki;

		protected void Page_Load(object sender, EventArgs e) {
			AdminMaster.RedirectToLoginIfNeeded();

			currentWiki = DetectWiki();

			if(!AdminMaster.CanManageConfiguration(SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames(currentWiki))) UrlTools.Redirect("AccessDenied.aspx");

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
			int orphans = Pages.GetOrphanedPages(currentWiki, null as NamespaceInfo).Length;
			foreach(NamespaceInfo nspace in Pages.GetNamespaces(currentWiki)) {
				orphans += Pages.GetOrphanedPages(currentWiki, nspace).Length;
			}
			lblOrphanPagesCount.Text = orphans.ToString();
		}

		protected void rptPages_DataBinding(object sender, EventArgs e) {
			List<WantedPageRow> result = new List<WantedPageRow>(50);

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
			foreach(PageInfo page in pages) {
				PageContent content = Content.GetPageContent(page);
				Pages.StorePageOutgoingLinks(page, content.Content);				
			}
		}

		protected void rptIndex_DataBinding(object sender, EventArgs e) {
			List<IndexRow> result = new List<IndexRow>(5);

			foreach(IPagesStorageProviderV40 prov in Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(currentWiki)) {
				result.Add(new IndexRow(prov));
			}

			rptIndex.DataSource = result;
		}

		protected void rptIndex_ItemCommand(object sender, CommandEventArgs e) {
			Log.LogEntry("Index rebuild requested for " + e.CommandArgument as string, EntryType.General, SessionFacade.GetCurrentUsername(), currentWiki);

			IPagesStorageProviderV40 provider = Collectors.CollectorsBox.PagesProviderCollector.GetProvider(e.CommandArgument as string, currentWiki);
			provider.RebuildIndex();

			Log.LogEntry("Index rebuild completed for " + e.CommandArgument as string, EntryType.General, Log.SystemUsername, currentWiki);

			rptIndex.DataBind();
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
		public IndexRow(IPagesStorageProviderV40 provider) {
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
