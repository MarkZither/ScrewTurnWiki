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
		private void RebuildPageLinks(IList<PageContent> pages) {
			foreach(PageContent page in pages) {
				Pages.StorePageOutgoingLinks(page);
			}
		}

		protected void rptIndex_DataBinding(object sender, EventArgs e) {
			List<IndexRow> result = new List<IndexRow>(5);

			foreach(IPagesStorageProviderV40 prov in Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(currentWiki)) {
				result.Add(new IndexRow("PagesRebuild", prov));
			}

			foreach(IFilesStorageProviderV40 prov in Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(currentWiki)) {
				result.Add(new IndexRow("FilesRebuild", prov));
			}

			rptIndex.DataSource = result;
		}

		protected void rptIndex_ItemCommand(object sender, CommandEventArgs e) {
			Log.LogEntry("Index rebuild requested for " + e.CommandArgument as string, EntryType.General, SessionFacade.GetCurrentUsername(), currentWiki);

			if(e.CommandName == "PagesRebuild") {

				// Clear the pages search index for the current wiki
				SearchClass.ClearIndex(currentWiki);

				IPagesStorageProviderV40 pagesProvider = Collectors.CollectorsBox.PagesProviderCollector.GetProvider(e.CommandArgument as string, currentWiki);

				// Index all pages of the wiki
				List<NamespaceInfo> namespaces = new List<NamespaceInfo>(pagesProvider.GetNamespaces());
				namespaces.Add(null);
				foreach(NamespaceInfo nspace in namespaces) {
					// Index pages of the namespace
					PageContent[] pages = pagesProvider.GetPages(nspace);
					foreach(PageContent page in pages) {
						// Index page
						SearchClass.IndexPage(page);

						// Index page messages
						Message[] messages = pagesProvider.GetMessages(page.FullName);
						foreach(Message message in messages) {
							SearchClass.IndexMessage(message, page);

							// Search for replies
							Message[] replies = message.Replies;
							foreach(Message reply in replies) {
								// Index reply
								SearchClass.IndexMessage(reply, page);
							}
						}
					}
				}
			}

			else if(e.CommandName == "FilesRebuild") {
				// Clear the files search index for the current wiki
				SearchClass.ClearFilesIndex(currentWiki);

				IFilesStorageProviderV40 filesProvider = Collectors.CollectorsBox.FilesProviderCollector.GetProvider(e.CommandArgument as string, currentWiki);

				// Index all files of the wiki
				// 1. List all directories (add the root directory: null)
				// 2. List all files in each directory
				// 3. Index each file
				List<string> directories = new List<string>(filesProvider.ListDirectories(null));
				directories.Add(null);
				foreach(string directory in directories) {
					string[] files = filesProvider.ListFiles(directory);
					foreach(string file in files) {
						byte[] fileContent;
						using(MemoryStream stream = new MemoryStream()) {
							filesProvider.RetrieveFile(file, stream);
							fileContent = new byte[stream.Length];
							stream.Seek(0, SeekOrigin.Begin);
							stream.Read(fileContent, 0, (int)stream.Length);
						}

						// Index the file
						string tempDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());
						if(!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
						string tempFile = Path.Combine(tempDir, file.Substring(file.LastIndexOf('/') + 1));
						using(FileStream writer = File.Create(tempFile)) {
							writer.Write(fileContent, 0, fileContent.Length);
						}
						SearchClass.IndexFile(filesProvider.GetType().FullName + "|" + file, tempFile, currentWiki);
						Directory.Delete(tempDir, true);
					}
				}

				// Index all attachment of the wiki
				string[] pagesWithAttachments = filesProvider.GetPagesWithAttachments();
				foreach(string page in pagesWithAttachments) {
					string[] attachments = filesProvider.ListPageAttachments(page);
					foreach(string attachment in attachments) {
						byte[] fileContent;
						using(MemoryStream stream = new MemoryStream()) {
							filesProvider.RetrievePageAttachment(page, attachment, stream);
							fileContent = new byte[stream.Length];
							stream.Seek(0, SeekOrigin.Begin);
							stream.Read(fileContent, 0, (int)stream.Length);
						}

						// Index the attached file
						string tempDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());
						if(!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
						string tempFile = Path.Combine(tempDir, attachment);
						using(FileStream writer = File.Create(tempFile)) {
							writer.Write(fileContent, 0, fileContent.Length);
						}
						SearchClass.IndexPageAttachment(attachment, tempFile, Pages.FindPage(currentWiki, page));
						Directory.Delete(tempDir, true);
					}
				}
			}

			Log.LogEntry("Index rebuild completed for " + e.CommandArgument as string, EntryType.General, SessionFacade.GetCurrentUsername(), currentWiki);
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
				PageContent page = Pages.FindPage(currentWiki, linkingPages[i]);
				if(page != null) {
					sb.AppendFormat(@"<a href=""{0}{1}"" title=""{2}"" target=""_blank"">{2}</a>, ", page.FullName, GlobalSettings.PageExtension,
						FormattingPipeline.PrepareTitle(currentWiki, page.Title, false, FormattingContext.Other, page.FullName));
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

		private string command, provider, providerType;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:IndexRow" /> class.
		/// </summary>
		/// <param name="provider">The original provider.</param>
		public IndexRow(string command, IStorageProviderV40 provider) {
			this.command = command;
			this.provider = provider.Information.Name;
			providerType = provider.GetType().FullName;
		}

		public string Command {
			get { return command; }
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

	}

}
