
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using ScrewTurn.Wiki.PluginFramework;
using System.Web;
using System.Globalization;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Allows access to the Pages.
	/// </summary>
	public static class Pages {

		#region Namespaces

		/// <summary>
		/// Gets all the namespaces of the given wiki, sorted.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <returns>The namespaces, sorted.</returns>
		public static List<NamespaceInfo> GetNamespaces(string wiki) {
			List<NamespaceInfo> result = new List<NamespaceInfo>(10);

			int count = 0;
			foreach(IPagesStorageProviderV40 provider in Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(wiki)) {
				count++;
				result.AddRange(provider.GetNamespaces());
			}

			if(count > 1) {
				result.Sort(new NamespaceComparer());
			}

			return result;
		}

		/// <summary>
		/// Finds a namespace in the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="name">The name of the namespace to find.</param>
		/// <returns>The namespace, or <c>null</c> if no namespace is found.</returns>
		public static NamespaceInfo FindNamespace(string wiki, string name) {
			if(string.IsNullOrEmpty(name)) return null;

			IPagesStorageProviderV40 defProv = Collectors.CollectorsBox.PagesProviderCollector.GetProvider(GlobalSettings.DefaultPagesProvider, wiki);
			NamespaceInfo nspace = defProv.GetNamespace(name);
			if(nspace != null) return nspace;

			foreach(IPagesStorageProviderV40 prov in Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(wiki)) {
				if(prov != defProv) {
					nspace = prov.GetNamespace(name);
					if(nspace != null) return nspace;
				}
			}

			return null;
		}

		/// <summary>
		/// Finds a namespace.
		/// </summary>
		/// <param name="name">The name of the namespace to find.</param>
		/// <param name="provider">The provider to look into.</param>
		/// <returns>The namespace, or <c>null</c> if the namespace is not found.</returns>
		public static NamespaceInfo FindNamespace(string name, IPagesStorageProviderV40 provider) {
			if(string.IsNullOrEmpty(name)) return null;

			return provider.GetNamespace(name);
		}

		/// <summary>
		/// Creates a new namespace in the default pages storage provider in the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="name">The name of the namespace to add.</param>
		/// <returns><c>true</c> if the namespace is created, <c>false</c> otherwise.</returns>
		public static bool CreateNamespace(string wiki, string name) {
			return CreateNamespace(wiki, name, Collectors.CollectorsBox.PagesProviderCollector.GetProvider(GlobalSettings.DefaultPagesProvider, wiki));
		}

		/// <summary>
		/// Creates a new namespace in the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="name">The name of the namespace to add.</param>
		/// <param name="provider">The provider to create the namespace into.</param>
		/// <returns><c>true</c> if the namespace is created, <c>false</c> otherwise.</returns>
		public static bool CreateNamespace(string wiki, string name, IPagesStorageProviderV40 provider) {
			if(provider.ReadOnly) return false;

			if(FindNamespace(wiki, name) != null) return false;

			NamespaceInfo result = provider.AddNamespace(name);

			if(result != null) {
				InitMetaDataItems(wiki, name);

				AuthWriter authWriter = new AuthWriter(Collectors.CollectorsBox.GetSettingsProvider(wiki));
				authWriter.ClearEntriesForNamespace(name, new List<string>());

				Host.Instance.OnNamespaceActivity(result, null, NamespaceActivity.NamespaceAdded);

				Log.LogEntry("Namespace " + name + " created", EntryType.General, Log.SystemUsername, wiki);
				return true;
			}
			else {
				Log.LogEntry("Namespace creation failed for " + name, EntryType.Error, Log.SystemUsername, wiki);
				return false;
			}
		}

		/// <summary>
		/// Removes a namespace from the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="nspace">The namespace to remove.</param>
		/// <returns><c>true</c> if the namespace is removed, <c>false</c> otherwise.</returns>
		public static bool RemoveNamespace(string wiki, NamespaceInfo nspace) {
			if(nspace.Provider.ReadOnly) return false;

			NamespaceInfo realNspace = FindNamespace(wiki, nspace.Name);
			if(realNspace == null) return false;

			List<PageContent> pages = GetPages(wiki, realNspace);

			bool done = realNspace.Provider.RemoveNamespace(realNspace);
			if(done) {
				DeleteAllAttachments(wiki, pages);

				ResetMetaDataItems(wiki, nspace.Name);

				AuthWriter authWriter = new AuthWriter(Collectors.CollectorsBox.GetSettingsProvider(wiki));
				authWriter.ClearEntriesForNamespace(nspace.Name, pages.ConvertAll((p) => { return NameTools.GetLocalName(p.FullName); }));

				Host.Instance.OnNamespaceActivity(realNspace, null, NamespaceActivity.NamespaceRemoved);

				// Unindexing all pages
				foreach(PageContent page in pages) {
					SearchClass.UnindexPage(page);
				}

				Log.LogEntry("Namespace " + realNspace.Name + " removed", EntryType.General, Log.SystemUsername, wiki);
				return true;
			}
			else {
				Log.LogEntry("Namespace deletion failed for " + realNspace.Name, EntryType.General, Log.SystemUsername, wiki);
				return false;
			}
		}

		/// <summary>
		/// Deletes all page attachments for a whole namespace in the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="pages">The pages in the namespace.</param>
		private static void DeleteAllAttachments(string wiki, List<PageContent> pages) {
			foreach(IFilesStorageProviderV40 prov in Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(wiki)) {
				foreach(PageContent page in pages) {
					string[] attachments = prov.ListPageAttachments(page.FullName);
					foreach(string attachment in attachments) {
						prov.DeletePageAttachment(page.FullName, attachment);
					}
				}
			}
		}

		/// <summary>
		/// Renames a namespace in the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="nspace">The namespace to rename.</param>
		/// <param name="newName">The new name.</param>
		/// <returns><c>true</c> if the namespace is removed, <c>false</c> otherwise.</returns>
		public static bool RenameNamespace(string wiki, NamespaceInfo nspace, string newName) {
			if(nspace.Provider.ReadOnly) return false;

			NamespaceInfo realNspace = FindNamespace(wiki, nspace.Name);
			if(realNspace == null) return false;
			if(FindNamespace(wiki, newName) != null) return false;

			List<PageContent> pages = GetPages(wiki, nspace);
			List<string> pageNames = new List<string>(pages.Count);
			foreach(PageContent page in pages) pageNames.Add(NameTools.GetLocalName(page.FullName));

			string oldName = nspace.Name;

			NamespaceInfo newNspace = realNspace.Provider.RenameNamespace(realNspace, newName);
			if(newNspace != null) {
				NotifyFilesProvidersForNamespaceRename(wiki, pageNames, oldName, newName);

				UpdateMetaDataItems(wiki, oldName, newName);

				AuthWriter authWriter = new AuthWriter(Collectors.CollectorsBox.GetSettingsProvider(wiki));
				authWriter.ClearEntriesForNamespace(newName, new List<string>());
				authWriter.ProcessNamespaceRenaming(oldName, pageNames, newName);

				Host.Instance.OnNamespaceActivity(newNspace, oldName, NamespaceActivity.NamespaceRenamed);

				// Unindex pages with old full name and index new ones.
				foreach(PageContent page in pages) {
					SearchClass.UnindexPage(page);
					page.FullName = NameTools.GetFullName(newNspace.Name, NameTools.GetLocalName(page.FullName));
					SearchClass.IndexPage(page);
				}

				Log.LogEntry("Namespace " + nspace.Name + " renamed to " + newName, EntryType.General, Log.SystemUsername, wiki);
				return true;
			}
			else {
				Log.LogEntry("Namespace rename failed for " + nspace.Name, EntryType.General, Log.SystemUsername, wiki);
				return false;
			}
		}

		/// <summary>
		/// Notifies all files providers that a namespace of the given wiki was renamed.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="pages">The pages in the renamed namespace.</param>
		/// <param name="nspace">The name of the renamed namespace.</param>
		/// <param name="newName">The new name of the namespace.</param>
		private static void NotifyFilesProvidersForNamespaceRename(string wiki, List<string> pages, string nspace, string newName) {
			foreach(IFilesStorageProviderV40 prov in Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(wiki)) {
				foreach(string page in pages) {
					prov.NotifyPageRenaming(NameTools.GetFullName(nspace, page), NameTools.GetFullName(newName, page));
				}
			}
		}

		/// <summary>
		/// Initializes the namespace-specific meta-data items for a namespace in the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="nspace">The namespace to initialize meta-data items for.</param>
		private static void InitMetaDataItems(string wiki, string nspace) {
			// Footer, Header, HtmlHead, PageFooter, PageHeader, Sidebar

			Settings.GetProvider(wiki).SetMetaDataItem(MetaDataItem.EditNotice, nspace, Defaults.EditNoticeContent);
			Settings.GetProvider(wiki).SetMetaDataItem(MetaDataItem.Footer, nspace, Defaults.FooterContent);
			Settings.GetProvider(wiki).SetMetaDataItem(MetaDataItem.Header, nspace, Defaults.HeaderContent);
			Settings.GetProvider(wiki).SetMetaDataItem(MetaDataItem.HtmlHead, nspace, "");
			Settings.GetProvider(wiki).SetMetaDataItem(MetaDataItem.PageFooter, nspace, "");
			Settings.GetProvider(wiki).SetMetaDataItem(MetaDataItem.PageHeader, nspace, "");
			Settings.GetProvider(wiki).SetMetaDataItem(MetaDataItem.Sidebar, nspace, Defaults.SidebarContentForSubNamespace);
		}

		/// <summary>
		/// Resets the namespace-specific meta-data items for a namespace in the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="nspace">The namespace to reset meta-data items for.</param>
		private static void ResetMetaDataItems(string wiki, string nspace) {
			// Footer, Header, HtmlHead, PageFooter, PageHeader, Sidebar

			Settings.GetProvider(wiki).SetMetaDataItem(MetaDataItem.EditNotice, nspace, "");
			Settings.GetProvider(wiki).SetMetaDataItem(MetaDataItem.Footer, nspace, "");
			Settings.GetProvider(wiki).SetMetaDataItem(MetaDataItem.Header, nspace, "");
			Settings.GetProvider(wiki).SetMetaDataItem(MetaDataItem.HtmlHead, nspace, "");
			Settings.GetProvider(wiki).SetMetaDataItem(MetaDataItem.PageFooter, nspace, "");
			Settings.GetProvider(wiki).SetMetaDataItem(MetaDataItem.PageHeader, nspace, "");
			Settings.GetProvider(wiki).SetMetaDataItem(MetaDataItem.Sidebar, nspace, "");
		}

		/// <summary>
		/// Updates the namespace-specific meta-data items for a namespace in the given wiki when it is renamed.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="nspace">The renamed namespace to update the meta-data items for.</param>
		/// <param name="newName">The new name of the namespace.</param>
		private static void UpdateMetaDataItems(string wiki, string nspace, string newName) {
			// Footer, Header, HtmlHead, PageFooter, PageHeader, Sidebar

			Settings.GetProvider(wiki).SetMetaDataItem(MetaDataItem.EditNotice, newName,
				Settings.GetProvider(wiki).GetMetaDataItem(MetaDataItem.EditNotice, nspace));
			Settings.GetProvider(wiki).SetMetaDataItem(MetaDataItem.Footer, newName,
				Settings.GetProvider(wiki).GetMetaDataItem(MetaDataItem.Footer, nspace));
			Settings.GetProvider(wiki).SetMetaDataItem(MetaDataItem.Header, newName,
				Settings.GetProvider(wiki).GetMetaDataItem(MetaDataItem.Header, nspace));
			Settings.GetProvider(wiki).SetMetaDataItem(MetaDataItem.HtmlHead, newName,
				Settings.GetProvider(wiki).GetMetaDataItem(MetaDataItem.HtmlHead, nspace));
			Settings.GetProvider(wiki).SetMetaDataItem(MetaDataItem.PageFooter, newName,
				Settings.GetProvider(wiki).GetMetaDataItem(MetaDataItem.PageFooter, nspace));
			Settings.GetProvider(wiki).SetMetaDataItem(MetaDataItem.PageHeader, newName,
				Settings.GetProvider(wiki).GetMetaDataItem(MetaDataItem.PageHeader, nspace));
			Settings.GetProvider(wiki).SetMetaDataItem(MetaDataItem.Sidebar, newName,
				Settings.GetProvider(wiki).GetMetaDataItem(MetaDataItem.Sidebar, nspace));

			ResetMetaDataItems(wiki, nspace);
		}

		/// <summary>
		/// Sets the default page of a namespace of the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <param name="page">The page.</param>
		/// <returns><c>true</c> if the default page is set, <c>false</c> otherwise.</returns>
		public static bool SetNamespaceDefaultPage(string wiki, NamespaceInfo nspace, PageContent page) {
			if(nspace == null) {
				// Root namespace, default to classic settings storage
				Settings.SetDefaultPage(wiki, page.FullName);
				return true;
			}

			if(nspace.Provider.ReadOnly) return false;

			NamespaceInfo pageNamespace = FindNamespace(NameTools.GetNamespace(page.FullName), page.Provider);

			if(pageNamespace == null) return false;

			NamespaceComparer comp = new NamespaceComparer();
			if(comp.Compare(pageNamespace, nspace) != 0) return false;

			NamespaceInfo result = pageNamespace.Provider.SetNamespaceDefaultPage(nspace, page.FullName);

			if(result != null) {
				Host.Instance.OnNamespaceActivity(result, null, NamespaceActivity.NamespaceModified);

				Log.LogEntry("Default Page set for " + nspace.Name + " (" + page.FullName + ")", EntryType.General, Log.SystemUsername, wiki);
				return true;
			}
			else {
				Log.LogEntry("Default Page setting failed for " + nspace.Name + " (" + page.FullName + ")", EntryType.Error, Log.SystemUsername, wiki);
				return false;
			}
		}

		#endregion

		#region Pages

		/// <summary>
		/// Finds a Page in the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="fullName">The full name of the page to find (case <b>unsensitive</b>).</param>
		/// <returns>The correct <see cref="T:PageInfo"/> object, if any, <c>null</c> otherwise.</returns>
		public static PageContent FindPage(string wiki, string fullName) {
			if(string.IsNullOrEmpty(fullName)) return null;

			IPagesStorageProviderV40 defProv = Collectors.CollectorsBox.PagesProviderCollector.GetProvider(GlobalSettings.DefaultPagesProvider, wiki);
			PageContent page = defProv.GetPage(fullName);
			if(page != null) return page;

			foreach(IPagesStorageProviderV40 prov in Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(wiki)) {
				if(prov != defProv) {
					page = prov.GetPage(fullName);
					if(page != null) return page;
				}
			}

			return null;
		}

		/// <summary>
		/// Finds a Page in a specific Provider.
		/// </summary>
		/// <param name="fullName">The full name of the page to find (case <b>unsensitive</b>).</param>
		/// <param name="provider">The Provider.</param>
		/// <returns>The correct <see cref="T:PageInfo" /> object, if any, <c>null</c> otherwise.</returns>
		public static PageContent FindPage(string fullName, IPagesStorageProviderV40 provider) {
			if(string.IsNullOrEmpty(fullName)) return null;

			return provider.GetPage(fullName);
		}

		/// <summary>
		/// Gets a page draft, if any.
		/// </summary>
		/// <param name="page">The draft content, or <c>null</c> if no draft exists.</param>
		public static PageContent GetDraft(PageContent page) {
			if(page == null) return null;

			return page.Provider.GetDraft(page.FullName);
		}

		/// <summary>
		/// Deletes the draft of a page (if any).
		/// </summary>
		/// <param name="pageFullName">The full name of the page of which to delete the draft.</param>
		/// <param name="provider">The provider the page belongs to.</param>
		public static void DeleteDraft(string pageFullName, IPagesStorageProviderV40 provider) {
			if(string.IsNullOrEmpty(pageFullName)) return;

			if(provider.GetDraft(pageFullName) != null) {
				provider.DeleteDraft(pageFullName);
			}
		}

		/// <summary>
		/// Gets the Backups/Revisions of a Page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>The list of available Backups/Revision numbers.</returns>
		public static List<int> GetBackups(PageContent page) {
			int[] temp = page.Provider.GetBackups(page.FullName);
			if(temp == null) return null;
			else return new List<int>(temp);
		}

		/// <summary>
		/// Gets the Content of a Page Backup.
		/// </summary>
		/// <param name="page">The page full name.</param>
		/// <param name="revision">The Backup/Revision number.</param>
		/// <returns>The Content of the Backup.</returns>
		public static PageContent GetBackupContent(PageContent page, int revision) {
			return page.Provider.GetBackupContent(page.FullName, revision);
		}

		/// <summary>
		/// Deletes all the backups of a page.
		/// </summary>
		/// <param name="page">The Page.</param>
		public static bool DeleteBackups(PageContent page) {
			return DeleteBackups(page, -1);
		}

		/// <summary>
		/// Deletes a subset of the backups of a page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <param name="firstToDelete">The first backup to be deleted (this backup and older backups are deleted).</param>
		public static bool DeleteBackups(PageContent page, int firstToDelete) {
			if(page.Provider.ReadOnly) return false;

			bool done = page.Provider.DeleteBackups(page.FullName, firstToDelete);
			if(done) {
				Log.LogEntry("Backups (0-" + firstToDelete.ToString() + ") deleted for " + page.FullName, EntryType.General, Log.SystemUsername, page.Provider.CurrentWiki);
				Host.Instance.OnPageActivity(page.FullName, null, SessionFacade.GetCurrentUsername(), PageActivity.PageBackupsDeleted);
			}
			else {
				Log.LogEntry("Backups (0-" + firstToDelete.ToString() + ") deletion failed for " + page.FullName, EntryType.Error, Log.SystemUsername, page.Provider.CurrentWiki);
			}
			return done;
		}

		/// <summary>
		/// Performs the rollpack of a Page of the specified wiki.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <param name="version">The revision to rollback the Page to.</param>
		public static bool Rollback(PageContent page, int version) {
			if(page.Provider.ReadOnly) return false;

			string wiki = page.Provider.CurrentWiki;

			bool done = page.Provider.RollbackPage(page.FullName, version);

			if(done) {
				// Unindex old content
				SearchClass.UnindexPage(page);

				PageContent newPage = page.Provider.GetPage(page.FullName);

				// Index the new content
				SearchClass.IndexPage(newPage);

				// Update page's outgoing links
				string[] linkedPages;
				Formatter.Format(wiki, newPage.Content, false, FormattingContext.PageContent, newPage.FullName, out linkedPages);
				string[] outgoingLinks = new string[linkedPages.Length];
				for(int i = 0; i < outgoingLinks.Length; i++) {
					outgoingLinks[i] = linkedPages[i];
				}

				Settings.GetProvider(wiki).StoreOutgoingLinks(newPage.FullName, outgoingLinks);

				Log.LogEntry("Rollback executed for " + newPage.FullName + " at revision " + version.ToString(), EntryType.General, Log.SystemUsername, wiki);
				RecentChanges.AddChange(wiki, newPage.FullName, newPage.Title, null, DateTime.Now, SessionFacade.GetCurrentUsername(), Change.PageRolledBack, "");
				Host.Instance.OnPageActivity(newPage.FullName, null, SessionFacade.GetCurrentUsername(), PageActivity.PageRolledBack);
				return true;
			}
			else {
				Log.LogEntry("Rollback failed for " + page.FullName + " at revision " + version.ToString(), EntryType.Error, Log.SystemUsername, wiki);
				return false;
			}
		}

		/// <summary>
		/// Sets a new page with an empty content.
		/// </summary>
		/// <param name="nspace">The target namespace (<c>null</c> for the root).</param>
		/// <param name="name">The Page name.</param>
		/// <param name="provider">The destination provider.</param>
		/// <param name="dateTime">The Date/Time of the creation.</param>
		/// <returns>The newly created <see cref="PageContent"/> or <c>null</c> if something was wrong.</returns>
		public static PageContent SetPageWithEmptyContent(string nspace, string name, IPagesStorageProviderV40 provider, DateTime dateTime) {
			PageContent temp = PageContent.GetEmpty(NameTools.GetFullName(nspace, name), provider, dateTime);
			return SetPageContent(provider.CurrentWiki, nspace, name, provider, "title", "test-user", dateTime, temp.Comment, temp.Content, temp.Keywords, temp.Description, SaveMode.Normal);
		}

		/// <summary>
		/// Set the content of a page in the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="nspace">The target namespace (<c>null</c> for the root).</param>
		/// <param name="name">The Page name.</param>
		/// <param name="title">The Title of the Page.</param>
		/// <param name="username">The Username of the user who modified the Page.</param>
		/// <param name="dateTime">The Date/Time of the modification.</param>
		/// <param name="comment">The comment of the editor, about this revision.</param>
		/// <param name="content">The Content.</param>
		/// <param name="keywords">The keywords, usually used for SEO.</param>
		/// <param name="description">The description, usually used for SEO.</param>
		/// <param name="saveMode">The save mode.</param>
		/// <returns>The newly created <see cref="PageContent"/> or <c>null</c> if something was wrong.</returns>
		public static PageContent SetPageContent(string wiki, string nspace, string name, string title, string username, DateTime dateTime, string comment, string content,
			string[] keywords, string description, SaveMode saveMode) {
			return SetPageContent(wiki, nspace, name, Collectors.CollectorsBox.PagesProviderCollector.GetProvider(GlobalSettings.DefaultPagesProvider, wiki), title, username, dateTime, comment, content, keywords, description, saveMode);
		}

		/// <summary>
		/// Modifies a Page of the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="nspace">The target namespace (<c>null</c> for the root).</param>
		/// <param name="name">The Page name.</param>
		/// <param name="provider">The destination provider.</param>
		/// <param name="title">The Title of the Page.</param>
		/// <param name="username">The Username of the user who modified the Page.</param>
		/// <param name="dateTime">The Date/Time of the modification.</param>
		/// <param name="comment">The comment of the editor, about this revision.</param>
		/// <param name="content">The Content.</param>
		/// <param name="keywords">The keywords, usually used for SEO.</param>
		/// <param name="description">The description, usually used for SEO.</param>
		/// <param name="saveMode">The save mode.</param>
		/// <returns>The newly created <see cref="PageContent"/> or <c>null</c> if something was wrong.</returns>
		public static PageContent SetPageContent(string wiki, string nspace, string name, IPagesStorageProviderV40 provider, string title, string username, DateTime dateTime, string comment, string content,
			string[] keywords, string description, SaveMode saveMode) {

			if(provider.ReadOnly) return null;

			StringBuilder sb = new StringBuilder(content);
			sb.Replace("~~~~", "§§(" + username + "," + dateTime.ToString("yyyy'/'MM'/'dd' 'HH':'mm':'ss") + ")§§");
			content = sb.ToString();

			PageContent currentContent = provider.GetPage(NameTools.GetFullName(nspace, name));
			if(saveMode != SaveMode.Draft && currentContent != null) {
				// Unindex old current
				SearchClass.UnindexPage(currentContent);
			}

			PageContent pageContent = provider.SetPageContent(nspace, name, DateTime.Now, title, username, dateTime, comment, content, keywords, description, saveMode);

			if(pageContent != null) {
				Log.LogEntry("Page Content updated for " + pageContent.FullName, EntryType.General, Log.SystemUsername, wiki);

				StorePageOutgoingLinks(pageContent);

				if(saveMode != SaveMode.Draft) {
					RecentChanges.AddChange(wiki, pageContent.FullName, title, null, dateTime, username, Change.PageUpdated, comment);
					Host.Instance.OnPageActivity(pageContent.FullName, null, username, PageActivity.PageModified);
					SendEmailNotificationForPage(pageContent, Users.FindUser(wiki, username));

					// Index the new content
					SearchClass.IndexPage(pageContent);
				}
				else {
					Host.Instance.OnPageActivity(pageContent.FullName, null, username, PageActivity.PageDraftSaved);
				}

				if(saveMode == SaveMode.Backup) {
					// Delete old backups, if needed
					DeleteOldBackupsIfNeeded(pageContent);
				}
			}
			else Log.LogEntry("Page Content update failed for " + pageContent.FullName, EntryType.Error, Log.SystemUsername, wiki);
			return pageContent;
		}

		/// <summary>
		/// Deletes a Page in the given wiki.
		/// </summary>
		/// <param name="page">The Page to delete.</param>
		public static bool DeletePage(PageContent page) {
			if(page.Provider.ReadOnly) return false;

			string wiki = page.Provider.CurrentWiki;
			string title = page.Title;

			bool done = page.Provider.RemovePage(page.FullName);

			if(done) {
				AuthWriter authWriter = new AuthWriter(Collectors.CollectorsBox.GetSettingsProvider(wiki));
				authWriter.ClearEntriesForPage(page.FullName);

				foreach(IFilesStorageProviderV40 prov in Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(wiki)) {
					foreach(string attn in prov.ListPageAttachments(page.FullName)) {
						prov.DeletePageAttachment(page.FullName, attn);
					}
				}

				// Remove the deleted page from the Breadcrumbs Trail and Redirections list
				SessionFacade.Breadcrumbs(wiki).RemovePage(page.FullName);
				Redirections.WipePageOut(page.FullName);

				// Remove outgoing links
				Settings.GetProvider(wiki).DeleteOutgoingLinks(page.FullName);
                                
                                RebuildPageLinks(Pages.GetPages(null));
				foreach(NamespaceInfo nspace in GetNamespaces()) {
					RebuildPageLinks(GetPages(nspace));
				}

				Log.LogEntry("Page " + page.FullName + " deleted", EntryType.General, Log.SystemUsername, wiki);
				RecentChanges.AddChange(wiki, page.FullName, title, null, DateTime.Now, SessionFacade.GetCurrentUsername(), Change.PageDeleted, "");
				Host.Instance.OnPageActivity(page.FullName, null, SessionFacade.GetCurrentUsername(), PageActivity.PageDeleted);

				// Unindex the page
				SearchClass.UnindexPage(page);
				return true;
			}
			else {
				Log.LogEntry("Page deletion failed for " + page.FullName, EntryType.Error, Log.SystemUsername, wiki);
				return false;
			}
		}

		/// <summary>
		/// Renames a Page in the given wiki.
		/// </summary>
		/// <param name="originalPageContent">The Page to rename.</param>
		/// <param name="newName">The new name.</param>
		public static bool RenamePage(PageContent originalPageContent, string newName) {
			if(originalPageContent.Provider.ReadOnly) return false;

			string wiki = originalPageContent.Provider.CurrentWiki;
			string newFullName = NameTools.GetFullName(NameTools.GetNamespace(originalPageContent.FullName), NameTools.GetLocalName(newName));

			if(FindPage(wiki, newFullName) != null) return false;

			string oldName = originalPageContent.FullName;

			Settings.GetProvider(wiki).StoreOutgoingLinks(originalPageContent.FullName, new string[0]);
			PageContent newPageContent = originalPageContent.Provider.RenamePage(originalPageContent.FullName, newName);
			if(newPageContent != null) {
				AuthWriter authWriter = new AuthWriter(Collectors.CollectorsBox.GetSettingsProvider(wiki));
				authWriter.ClearEntriesForPage(newFullName);
				authWriter.ProcessPageRenaming(oldName, newFullName);

				foreach(IFilesStorageProviderV40 prov in Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(wiki)) {
					prov.NotifyPageRenaming(oldName, newPageContent.FullName);
				}

				StorePageOutgoingLinks(newPageContent);

				SessionFacade.Breadcrumbs(wiki).RemovePage(originalPageContent.FullName);
				Redirections.Clear();

				// Page redirect is implemented directly in AdminPages.aspx.cs

				Log.LogEntry("Page " + oldName + " renamed to " + newName, EntryType.General, Log.SystemUsername, wiki);
				RecentChanges.AddChange(wiki, originalPageContent.FullName, originalPageContent.Title, null, DateTime.Now, SessionFacade.GetCurrentUsername(), Change.PageRenamed, "");
				Host.Instance.OnPageActivity(originalPageContent.FullName, oldName, SessionFacade.GetCurrentUsername(), PageActivity.PageRenamed);
				return true;
			}
			else {
				Log.LogEntry("Page rename failed for " + originalPageContent.FullName + " (" + newName + ")", EntryType.Error, Log.SystemUsername, wiki);
				return false;
			}
		}

		/// <summary>
		/// Migrates a page of the given wiki.
		/// </summary>
		/// <param name="page">The page to migrate.</param>
		/// <param name="targetNamespace">The target namespace.</param>
		/// <param name="copyCategories">A value indicating whether to copy the page categories to the target namespace.</param>
		/// <returns><c>true</c> if the page is migrated, <c>false</c> otherwise.</returns>
		public static bool MigratePage(PageContent page, NamespaceInfo targetNamespace, bool copyCategories) {
			string oldName = page.FullName;

			// Unindex old page
			SearchClass.UnindexPage(page);

			PageContent result = page.Provider.MovePage(page.FullName, targetNamespace, copyCategories);
			if(result != null) {
				string wiki = page.Provider.CurrentWiki;
				Settings.GetProvider(wiki).StoreOutgoingLinks(page.FullName, new string[0]);
				
				StorePageOutgoingLinks(result);

				foreach(IFilesStorageProviderV40 prov in Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(wiki)) {
					prov.NotifyPageRenaming(oldName, result.FullName);
				}

				// Index the new page
				SearchClass.IndexPage(result);
			}
			return result != null;
		}

		/// <summary>
		/// Stores outgoing links for a page of the given wiki.
		/// </summary>
		/// <param name="page">The page.</param>
		public static void StorePageOutgoingLinks(PageContent page) {
			string[] linkedPages;
			string wiki = page.Provider.CurrentWiki;
			Formatter.Format(wiki, page.Content, false, FormattingContext.PageContent, page.FullName, out linkedPages);

			string lowercaseName = page.FullName.ToLowerInvariant();

			// Avoid self-references
			List<string> cleanLinkedPages = new List<string>(linkedPages);
			for(int i = cleanLinkedPages.Count - 1; i >= 0; i--) {
				if(cleanLinkedPages[i] == null || cleanLinkedPages[i].Length == 0) {
					cleanLinkedPages.RemoveAt(i);
				}
				else if(cleanLinkedPages[i].ToLowerInvariant() == lowercaseName) {
					cleanLinkedPages.RemoveAt(i);
				}
			}

			bool doneLinks = Settings.GetProvider(wiki).StoreOutgoingLinks(page.FullName, cleanLinkedPages.ToArray());
			if(!doneLinks) {
				Log.LogEntry("Could not store outgoing links for page " + page.FullName, EntryType.Error, Log.SystemUsername, wiki);
			}
		}

		/// <summary>
		/// Deletes the old backups if the current number of backups exceeds the limit in the given wiki.
		/// </summary>
		/// <param name="page">The page.</param>
		private static void DeleteOldBackupsIfNeeded(PageContent page) {
			int maxBackups = Settings.GetKeptBackupNumber(page.Provider.CurrentWiki);
			if(maxBackups == -1) return;

			// Oldest to newest: 0, 1, 2, 3
			List<int> backups = GetBackups(page);
			if(backups.Count > maxBackups) {
				backups.Reverse();
				DeleteBackups(page, backups[maxBackups]);
			}
		}

		/// <summary>
		/// Removes a user from an array.
		/// </summary>
		/// <param name="users">The array of users.</param>
		/// <param name="userToRemove">The user to remove.</param>
		/// <returns>The resulting array without the specified user.</returns>
		private static UserInfo[] RemoveUserFromArray(UserInfo[] users, UserInfo userToRemove) {
			if(userToRemove == null) return users;

			List<UserInfo> temp = new List<UserInfo>(users);
			UsernameComparer comp = new UsernameComparer();
			temp.RemoveAll(delegate(UserInfo elem) { return comp.Compare(elem, userToRemove) == 0; });

			return temp.ToArray();
		}

		/// <summary>
		/// Sends the email notification for a page change in the given wiki.
		/// </summary>
		/// <param name="page">The page that was modified.</param>
		/// <param name="author">The author of the modification.</param>
		private static void SendEmailNotificationForPage(PageContent page, UserInfo author) {
			if(page == null) return;

			string wiki = page.Provider.CurrentWiki;
			
			UserInfo[] usersToNotify = Users.GetUsersToNotifyForPageChange(wiki, page.FullName);
			usersToNotify = RemoveUserFromArray(usersToNotify, author);
			string[] recipients = EmailTools.GetRecipients(usersToNotify);

			string body = Settings.GetProvider(wiki).GetMetaDataItem(MetaDataItem.PageChangeMessage, null);

			string title = FormattingPipeline.PrepareTitle(wiki, page.Title, false, FormattingContext.Other, page.FullName);

			EmailTools.AsyncSendMassEmail(recipients, GlobalSettings.SenderEmail,
				Settings.GetWikiTitle(wiki) + " - " + title,
				body.Replace("##PAGE##", title).Replace("##USER##", author != null ? Users.GetDisplayName(author) : "anonymous").Replace("##DATETIME##",
				Preferences.AlignWithServerTimezone(wiki, page.LastModified).ToString(Settings.GetDateTimeFormat(wiki))).Replace("##COMMENT##",
				(string.IsNullOrEmpty(page.Comment) ? Exchanger.ResourceExchanger.GetResource("None") : page.Comment)).Replace("##LINK##",
				Settings.GetMainUrl(wiki) + Tools.UrlEncode(page.FullName) + GlobalSettings.PageExtension).Replace("##WIKITITLE##", Settings.GetWikiTitle(wiki)),
				false);
		}

		/// <summary>
		/// Determines whether a user of the given wiki can edit a page.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="pageFullName">The page full name.</param>
		/// <param name="username">The username.</param>
		/// <param name="groups">The groups.</param>
		/// <param name="canEdit">A value indicating whether the user can edit the page.</param>
		/// <param name="canEditWithApproval">A value indicating whether the user can edit the page with subsequent approval.</param>
		public static void CanEditPage(string wiki, string pageFullName, string username, string[] groups,
			out bool canEdit, out bool canEditWithApproval) {

			canEdit = false;
			canEditWithApproval = false;

			AuthChecker authChecker = new AuthChecker(Collectors.CollectorsBox.GetSettingsProvider(wiki));
			switch(Settings.GetModerationMode(wiki)) {
				case ChangeModerationMode.RequirePageEditingPermissions:
					canEdit = authChecker.CheckActionForPage(pageFullName, Actions.ForPages.ManagePage, username, groups);
					canEditWithApproval = authChecker.CheckActionForPage(pageFullName, Actions.ForPages.ModifyPage, username, groups);
					break;
				case ChangeModerationMode.RequirePageViewingPermissions:
					canEdit = authChecker.CheckActionForPage(pageFullName, Actions.ForPages.ModifyPage, username, groups);
					canEditWithApproval = authChecker.CheckActionForPage(pageFullName, Actions.ForPages.ReadPage, username, groups);
					break;
				case ChangeModerationMode.None:
					canEdit = authChecker.CheckActionForPage(pageFullName, Actions.ForPages.ModifyPage, username, groups);
					canEditWithApproval = false;
					break;
			}
			if(canEditWithApproval && canEdit) canEditWithApproval = false;

			bool isAdminstrator = false;
			foreach(string group in groups) {
				if(group == Settings.GetAdministratorsGroup(wiki)) isAdminstrator = true;
			}
			if(canEdit && !string.IsNullOrEmpty(Settings.GetIpHostFilter(wiki)) && !isAdminstrator)
				canEdit = VerifyIpHostFilter(wiki);
		}

		/// <summary>
		/// Verifies whether or not the current user's ip address is in the host filter of the given wiki or not.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		private static bool VerifyIpHostFilter(string wiki) {
			const RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace;
			var hostAddress = HttpContext.Current.Request.UserHostAddress;
			var ips = Settings.GetIpHostFilter(wiki).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			// For each IP in the host filter setting
			foreach(var ip in ips) {

				// Split each by the .
				var digits = ip.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
				var regExpression = string.Empty;
				foreach(var digit in digits) {

					// Build a regex to check against the host ip.
					if(regExpression != string.Empty)
						regExpression += "\\.";

					if(digit == "*")
						regExpression += "\\d{1,3}";
					else
						regExpression += digit;
				}

				// If we match, then the user is in the filter, return false.
				var regex = new Regex(regExpression, options);
				if(regex.IsMatch(hostAddress))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Determines whether a user can approve/reject a draft of a page, in the specified wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="pageFullName">The page.</param>
		/// <param name="username">The username.</param>
		/// <param name="groups">The groups.</param>
		/// <returns><c>true</c> if the user can approve/reject a draft of the page, <c>false</c> otherwise.</returns>
		public static bool CanApproveDraft(string wiki, string pageFullName, string username, string[] groups) {
			string requiredAction = Actions.ForPages.ManagePage;

			// TODO: decide whether it is incorrect to require only ModifyPage permission
			/*switch(Settings.ChangeModerationMode) {
				case ChangeModerationMode.None:
					return;
				case ChangeModerationMode.RequirePageViewingPermissions:
					requiredAction = Actions.ForPages.ModifyPage;
					break;
				case ChangeModerationMode.RequirePageEditingPermissions:
					requiredAction = Actions.ForPages.ManagePage;
					break;
				default:
					throw new NotSupportedException();
			}*/

			AuthChecker authChecker = new AuthChecker(Collectors.CollectorsBox.GetSettingsProvider(wiki));
			return authChecker.CheckActionForPage(pageFullName, requiredAction, username, groups);
		}

		/// <summary>
		/// Sends a draft notification to "administrators".
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="currentPageFullName">The edited page.</param>
		/// <param name="title">The title.</param>
		/// <param name="comment">The comment.</param>
		/// <param name="author">The author.</param>
		public static void SendEmailNotificationForDraft(string wiki, string currentPageFullName, string title, string comment, string author) {
			// Decide the users to notify based on the ChangeModerationMode
			// Retrieve the list of matching users
			// Asynchronously send the notification

			// Retrieve all the users that have a grant on requiredAction for the current page
			// TODO: make this work when Users.GetUsers does not return all existing users but only a sub-set
			List<UserInfo> usersToNotify = new List<UserInfo>(10);
			foreach(UserInfo user in Users.GetUsers(wiki)) {
				if(user.Active && CanApproveDraft(wiki, currentPageFullName, user.Username, user.Groups)) {
					usersToNotify.Add(user);
				}
			}
			usersToNotify.Add(new UserInfo("admin", "Administrator", GlobalSettings.ContactEmail, true, DateTime.Now, null));

			UserInfo actualUser = Users.FindUser(wiki, author);
			string displayName = actualUser == null ? author : Users.GetDisplayName(actualUser);

			string subject = Settings.GetWikiTitle(wiki) + " - " + Exchanger.ResourceExchanger.GetResource("ApproveRejectDraft") + ": " + title;
			string body = Settings.GetProvider(wiki).GetMetaDataItem(MetaDataItem.ApproveDraftMessage, null);
			body = body.Replace("##PAGE##", title).Replace("##USER##", displayName).Replace("##DATETIME##",
				Preferences.AlignWithServerTimezone(wiki, DateTime.Now).ToString(Settings.GetDateTimeFormat(wiki))).Replace("##COMMENT##",
				string.IsNullOrEmpty(comment) ? Exchanger.ResourceExchanger.GetResource("None") : comment).Replace("##LINK##",
				Settings.GetMainUrl(wiki) + UrlTools.BuildUrl(wiki, "Edit.aspx?Page=", Tools.UrlEncode(currentPageFullName))).Replace("##LINK2##",
				Settings.GetMainUrl(wiki) + "AdminPages.aspx?Admin=" + Tools.UrlEncode(currentPageFullName)).Replace("##WIKITITLE##",
				Settings.GetWikiTitle(wiki));

			EmailTools.AsyncSendMassEmail(EmailTools.GetRecipients(usersToNotify.ToArray()),
				GlobalSettings.SenderEmail, subject, body, false);
		}

		/// <summary>
		/// Gets the list of all the Pages of a namespace in the specified wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The pages.</returns>
		public static List<PageContent> GetPages(string wiki, NamespaceInfo nspace) {
			List<PageContent> allPages = new List<PageContent>(10000);

			// Retrieve all pages from Pages Providers
			int count = 0;
			foreach(IPagesStorageProviderV40 provider in Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(wiki)) {
				count++;
				allPages.AddRange(provider.GetPages(nspace));
			}

			if(count > 1) {
				allPages.Sort(new PageNameComparer());
			}

			return allPages;
		}

		/// <summary>
		/// Gets the global number of pages in a wiki.
		/// </summary>
		/// <returns>The number of pages.</returns>
		public static int GetGlobalPageCount(string wiki) {
			int count = 0;

			foreach(IPagesStorageProviderV40 prov in Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(wiki)) {
				count += prov.GetPages(null).Length;
				foreach(NamespaceInfo nspace in prov.GetNamespaces()) {
					count += prov.GetPages(nspace).Length;
				}
			}

			return count;
		}

		/// <summary>
		/// Gets the incoming links for a page in a wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="pageFullName">The page full name.</param>
		/// <returns>The incoming links.</returns>
		public static string[] GetPageIncomingLinks(string wiki, string pageFullName) {
			if(pageFullName == null) return null;

			return GetPageIncomingLinks(pageFullName, Settings.GetProvider(wiki).GetAllOutgoingLinks());
		}

		private static string[] GetPageIncomingLinks(string pageFullName, IDictionary<string, string[]> allOutgoingLinks) {
			string[] knownPages = new string[allOutgoingLinks.Count];
			allOutgoingLinks.Keys.CopyTo(knownPages, 0);

			List<string> result = new List<string>(20);

			foreach(string key in knownPages) {
				if(Contains(allOutgoingLinks[key], pageFullName)) {
					// result is likely to be very small, so a linear search is fine
					if(!result.Contains(key)) result.Add(key);
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="pageFullName">The page full name.</param>
		/// <returns>The outgoing links.</returns>
		public static string[] GetPageOutgoingLinks(string wiki, string pageFullName) {
			if(pageFullName == null) return null;
			return Settings.GetProvider(wiki).GetOutgoingLinks(pageFullName);
		}

		/// <summary>
		/// Gets all the pages in a namespace of a wiki without incoming links.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The orphaned pages.</returns>
		public static string[] GetOrphanedPages(string wiki, NamespaceInfo nspace) {
			List<PageContent> pages = GetPages(wiki, nspace);
			IDictionary<string, string[]> allLinks = Settings.GetProvider(wiki).GetAllOutgoingLinks();
			string[] knownPages = new string[allLinks.Count];
			allLinks.Keys.CopyTo(knownPages, 0);

			Dictionary<string, bool> result = new Dictionary<string, bool>(pages.Count);

			foreach(PageContent page in pages) {
				result.Add(page.FullName, false);
				foreach(string key in knownPages) {
					if(Contains(allLinks[key], page.FullName)) {
						// page has incoming links
						result[page.FullName] = true;
					}
				}
			}

			return ExtractNegativeKeys(result);
		}

		/// <summary>
		/// Rebuilds the page links for the specified pages.
		/// </summary>
		/// <param name="pages">The pages.</param>
		public static void RebuildPageLinks(IList<PageInfo> pages) {
			foreach(PageInfo page in pages) {
				PageContent content = Content.GetPageContent(page, false);
				StorePageOutgoingLinks(page, content.Content);
			}
		}

		/// <summary>
		/// Gets the wanted/inexistent pages in all namespaces of a wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The wanted/inexistent pages (dictionary wanted_page-&gt;linking_pages).</returns>
		public static Dictionary<string, List<string>> GetWantedPages(string wiki, string nspace) {
			if(string.IsNullOrEmpty(nspace)) nspace = null;

			IDictionary<string, string[]> allLinks = Settings.GetProvider(wiki).GetAllOutgoingLinks();
			string[] knownPages = new string[allLinks.Count];
			allLinks.Keys.CopyTo(knownPages, 0);

			Dictionary<string, List<string>> result = new Dictionary<string, List<string>>(100);

			foreach(string key in knownPages) {
				foreach(string link in allLinks[key]) {
					string linkNamespace = NameTools.GetNamespace(link);
					if(linkNamespace == nspace) {

						PageContent tempPage = FindPage(wiki, link);
						if(tempPage == null) {
							if(!result.ContainsKey(link)) result.Add(link, new List<string>(3));
							result[link].Add(key);
						}
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Determines whether an array contains a value.
		/// </summary>
		/// <typeparam name="T">The type of the elements in the array.</typeparam>
		/// <param name="array">The array.</param>
		/// <param name="value">The value.</param>
		/// <returns><c>true</c> if the array contains the value, <c>false</c> otherwise.</returns>
		private static bool Contains<T>(T[] array, T value) {
			return Array.IndexOf(array, value) >= 0;
		}

		/// <summary>
		/// Extracts the negative keys from a dictionary.
		/// </summary>
		/// <typeparam name="T">The type of the key.</typeparam>
		/// <param name="data">The dictionary.</param>
		/// <returns>The negative keys.</returns>
		private static T[] ExtractNegativeKeys<T>(Dictionary<T, bool> data) {
			List<T> result = new List<T>(data.Count);

			foreach(KeyValuePair<T, bool> pair in data) {
				if(!pair.Value) result.Add(pair.Key);
			}

			return result.ToArray();
		}

		#endregion

		#region Categories

		/// <summary>
		/// Finds a Category in the specified wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="fullName">The full name of the Category to Find (case <b>unsensitive</b>).</param>
		/// <returns>The correct <see cref="T:CategoryInfo"/> object or <c>null</c> if no category is found.</returns>
		public static CategoryInfo FindCategory(string wiki, string fullName) {
			if(string.IsNullOrEmpty(fullName)) return null;

			IPagesStorageProviderV40 defProv = Collectors.CollectorsBox.PagesProviderCollector.GetProvider(GlobalSettings.DefaultPagesProvider, wiki);
			CategoryInfo category = defProv.GetCategory(fullName);
			if(category != null) return category;

			foreach(IPagesStorageProviderV40 prov in Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(wiki)) {
				if(prov != defProv) {
					category = prov.GetCategory(fullName);
					if(category != null) return category;
				}
			}

			return null;
		}

		/// <summary>
		/// Creates a new Category in the specified wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="nspace">The target namespace (<c>null</c> for the root).</param>
		/// <param name="name">The Name of the Category.</param>
		/// <returns><c>true</c> if the category is created, <c>false</c> otherwise.</returns>
		public static bool CreateCategory(string wiki, NamespaceInfo nspace, string name) {
			string namespaceName = nspace != null ? nspace.Name : null;
			return CreateCategory(wiki, namespaceName, name);
		}

		/// <summary>
		/// Creates a new Category in the specified wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="nspace">The target namespace (<c>null</c> for the root).</param>
		/// <param name="name">The Name of the Category.</param>
		/// <returns><c>true</c> if the category is created, <c>false</c> otherwise.</returns>
		public static bool CreateCategory(string wiki, string nspace, string name) {
			return CreateCategory(wiki, nspace, name, Collectors.CollectorsBox.PagesProviderCollector.GetProvider(GlobalSettings.DefaultPagesProvider, wiki));
		}

		/// <summary>
		/// Creates a new Category for the given wiki in the specified Provider.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="nspace">The target namespace (<c>null</c> for the root).</param>
		/// <param name="name">The Name of the Category.</param>
		/// <param name="provider">The Provider.</param>
		/// <returns><c>true</c> if the category is created, <c>false</c> otherwise.</returns>
		public static bool CreateCategory(string wiki, NamespaceInfo nspace, string name, IPagesStorageProviderV40 provider) {
			string namespaceName = nspace != null ? nspace.Name : null;
			return CreateCategory(wiki, namespaceName, name, provider);
		}

		/// <summary>
		/// Creates a new Category for the given wiki in the specified Provider.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="nspace">The target namespace (<c>null</c> for the root).</param>
		/// <param name="name">The Name of the Category.</param>
		/// <param name="provider">The Provider.</param>
		/// <returns><c>true</c> if the category is created, <c>false</c> otherwise.</returns>
		public static bool CreateCategory(string wiki, string nspace, string name, IPagesStorageProviderV40 provider) {
			if(provider == null) provider = Collectors.CollectorsBox.PagesProviderCollector.GetProvider(GlobalSettings.DefaultPagesProvider, wiki);

			if(provider.ReadOnly) return false;

			string fullName = NameTools.GetFullName(nspace, name);

			if(FindCategory(wiki, fullName) != null) return false;

			CategoryInfo newCat = provider.AddCategory(nspace, name);
			if(newCat != null) {
				Log.LogEntry("Category " + fullName + " created", EntryType.General, Log.SystemUsername, wiki);

				return true;
			}
			else {
				Log.LogEntry("Category creation failed for " + fullName, EntryType.Error, Log.SystemUsername, wiki);
				return false;
			}
		}

		/// <summary>
		/// Removes a Category.
		/// </summary>
		/// <param name="category">The Category to remove.</param>
		/// <returns>True if the Category has been removed successfully.</returns>
		public static bool RemoveCategory(CategoryInfo category) {
			if(category.Provider.ReadOnly) return false;

			bool done = category.Provider.RemoveCategory(category);
			if(done) Log.LogEntry("Category " + category.FullName + " removed", EntryType.General, Log.SystemUsername, category.Provider.CurrentWiki);
			else Log.LogEntry("Category deletion failed for " + category.FullName, EntryType.Error, Log.SystemUsername, category.Provider.CurrentWiki);

			return done;
		}

		/// <summary>
		/// Renames a Category.
		/// </summary>
		/// <param name="category">The Category to rename.</param>
		/// <param name="newName">The new Name of the Category.</param>
		/// <returns>True if the Category has been renamed successfully.</returns>
		public static bool RenameCategory(CategoryInfo category, string newName) {
			if(category.Provider.ReadOnly) return false;

			string wiki = category.Provider.CurrentWiki;
			string newFullName = NameTools.GetFullName(NameTools.GetNamespace(category.FullName), newName);

			if(FindCategory(wiki, newFullName) != null) return false;

			string oldName = category.FullName;

			CategoryInfo newCat = category.Provider.RenameCategory(category, newName);
			if(newCat != null) Log.LogEntry("Category " + oldName + " renamed to " + newFullName, EntryType.General, Log.SystemUsername, wiki);
			else Log.LogEntry("Category rename failed for " + oldName + " (" + newFullName + ")", EntryType.Error, Log.SystemUsername, wiki);

			return newCat != null;
		}

		/// <summary>
		/// Gets the Categories of a Page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>The Categories of the Page.</returns>
		public static CategoryInfo[] GetCategoriesForPage(PageContent page) {
			if(page == null) return new CategoryInfo[0];

			CategoryInfo[] categories = page.Provider.GetCategoriesForPage(page.FullName);

			return categories;
		}

		/// <summary>
		/// Gets all the Uncategorized Pages in a wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="nspace">The namespace.</param>
		/// <returns>The Uncategorized Pages.</returns>
		public static PageContent[] GetUncategorizedPages(string wiki, NamespaceInfo nspace) {
			if(nspace == null) {
				List<PageContent> pages = new List<PageContent>(1000);

				int count = 0;
				foreach(IPagesStorageProviderV40 prov in Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(wiki)) {
					count++;
					pages.AddRange(prov.GetUncategorizedPages(null));
				}

				if(count > 1) {
					pages.Sort(new PageNameComparer());
				}

				return pages.ToArray();
			}
			else {
				PageContent[] pages = nspace.Provider.GetUncategorizedPages(nspace);
				return pages;
			}
		}

		/// <summary>
		/// Gets the other Categories of the Provider, Wiki and Namespace of the specified Category.
		/// </summary>
		/// <param name="category">The Category.</param>
		/// <returns>The matching Categories.</returns>
		public static CategoryInfo[] GetMatchingCategories(CategoryInfo category) {
			string wiki = category.Provider.CurrentWiki;
			NamespaceInfo nspace = FindNamespace(wiki, NameTools.GetNamespace(category.FullName));

			List<CategoryInfo> allCategories = GetCategories(wiki, nspace);
			List<CategoryInfo> result = new List<CategoryInfo>(10);

			for(int i = 0; i < allCategories.Count; i++) {
				if(allCategories[i].Provider == category.Provider && allCategories[i] != category) {
					result.Add(allCategories[i]);
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// Binds a Page with some Categories.
		/// </summary>
		/// <param name="page">The Page to rebind.</param>
		/// <param name="cats">The Categories to bind the Page with.</param>
		/// <remarks>
		/// The specified Categories must be managed by the same Provider that manages the Page.
		/// The operation removes all the previous bindings.
		/// </remarks>
		/// <returns>True if the binding succeeded.</returns>
		public static bool Rebind(PageContent page, CategoryInfo[] cats) {
			if(page.Provider.ReadOnly) return false;

			string[] names = new string[cats.Length];
			for(int i = 0; i < cats.Length; i++) {
				if(cats[i].Provider != page.Provider) return false;
				names[i] = cats[i].FullName; // Saves one cycle
			}
			bool done = page.Provider.RebindPage(page.FullName, names);
			if(done) Log.LogEntry("Page " + page.FullName + " rebound", EntryType.General, Log.SystemUsername, page.Provider.CurrentWiki);
			else Log.LogEntry("Page rebind failed for " + page.FullName, EntryType.Error, Log.SystemUsername, page.Provider.CurrentWiki);

			return done;
		}

		/// <summary>
		/// Merges two Categories.
		/// </summary>
		/// <param name="source">The source Category.</param>
		/// <param name="destination">The destination Category.</param>
		/// <returns>True if the Categories have been merged successfully.</returns>
		/// <remarks>The <b>destination</b> Category remains, while the <b>source</b> Category is deleted, and all its Pages re-binded in the <b>destination</b> Category.
		/// The two Categories must have the same provider.</remarks>
		public static bool MergeCategories(CategoryInfo source, CategoryInfo destination) {
			if(source.Provider != destination.Provider) return false;
			if(source.Provider.ReadOnly) return false;

			CategoryInfo newCat = source.Provider.MergeCategories(source, destination);

			if(newCat != null) Log.LogEntry("Category " + source.FullName + " merged into " + destination.FullName, EntryType.General, Log.SystemUsername, source.Provider.CurrentWiki);
			else Log.LogEntry("Categories merging failed for " + source.FullName + " into " + destination.FullName, EntryType.Error, Log.SystemUsername, source.Provider.CurrentWiki);

			return newCat != null;
		}

		/// <summary>
		/// Gets the list of all the Categories of a namespace of a wiki. The list shouldn't be modified.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The categories, sorted by name.</returns>
		public static List<CategoryInfo> GetCategories(string wiki, NamespaceInfo nspace) {
			List<CategoryInfo> allCategories = new List<CategoryInfo>(50);

			// Retrieve all the categories from Pages Provider
			int count = 0;
			foreach(IPagesStorageProviderV40 provider in Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(wiki)) {
				count++;
				allCategories.AddRange(provider.GetCategories(nspace));
			}

			if(count > 1) {
				allCategories.Sort(new CategoryNameComparer());
			}

			return allCategories;
		}

		#endregion

		#region Page Discussion

		/// <summary>
		/// Gets the Page Messages.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>The list of the <b>first-level</b> Messages, containing the replies properly nested.</returns>
		public static Message[] GetPageMessages(PageContent page) {
			return page.Provider.GetMessages(page.FullName);
		}

		/// <summary>
		/// Gets the total number of Messages in a Page Discussion.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>The number of messages.</returns>
		public static int GetMessageCount(PageContent page) {
			return page.Provider.GetMessageCount(page.FullName);
		}

		/// <summary>
		/// Finds a Message.
		/// </summary>
		/// <param name="messages">The Messages.</param>
		/// <param name="id">The Message ID.</param>
		/// <returns>The Message or null.</returns>
		public static Message FindMessage(Message[] messages, int id) {
			Message result = null;
			for(int i = 0; i < messages.Length; i++) {
				if(messages[i].ID == id) {
					result = messages[i];
				}
				if(result == null) {
					result = FindMessage(messages[i].Replies, id);
				}
				if(result != null) break;
			}
			return result;
		}

		/// <summary>
		/// Adds a new Message to a Page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <param name="username">The Username.</param>
		/// <param name="subject">The Subject.</param>
		/// <param name="dateTime">The Date/Time.</param>
		/// <param name="body">The Body.</param>
		/// <param name="parent">The Parent Message ID, or -1.</param>
		/// <returns>True if the Message has been added successfully.</returns>
		public static bool AddMessage(PageContent page, string username, string subject, DateTime dateTime, string body, int parent) {
			if(page.Provider.ReadOnly) return false;

			int messageId = page.Provider.AddMessage(page.FullName, username, subject, dateTime, body, parent);
			if(messageId != -1) {
				string wiki = page.Provider.CurrentWiki;
				SendEmailNotificationForMessage(page, Users.FindUser(wiki, username), Tools.GetMessageIdForAnchor(dateTime), subject, dateTime);

				RecentChanges.AddChange(wiki, page.FullName, page.Title, subject, dateTime, username, Change.MessagePosted, "");
				Host.Instance.OnPageActivity(page.FullName, null, username, PageActivity.MessagePosted);

				// Index message
				Message message = new Message(messageId, username, subject, dateTime, body);
				SearchClass.IndexMessage(message, page);
			}
			return messageId != -1;
		}

		/// <summary>
		/// Sends the email notification for a new message.
		/// </summary>
		/// <param name="page">The page the message was posted to.</param>
		/// <param name="author">The author of the message.</param>
		/// <param name="id">The message ID to be used for anchors.</param>
		/// <param name="subject">The message subject.</param>
		/// <param name="dateTime">The message date/time.</param>
		private static void SendEmailNotificationForMessage(PageContent page, UserInfo author, string id, string subject, DateTime dateTime) {
			if(page == null) return;

			string wiki = page.Provider.CurrentWiki;
			
			UserInfo[] usersToNotify = Users.GetUsersToNotifyForDiscussionMessages(wiki, page.FullName);
			usersToNotify = RemoveUserFromArray(usersToNotify, author);
			string[] recipients = EmailTools.GetRecipients(usersToNotify);

			string body = Settings.GetProvider(wiki).GetMetaDataItem(MetaDataItem.DiscussionChangeMessage, null);

			string title = FormattingPipeline.PrepareTitle(wiki, page.Title, false, FormattingContext.Other, page.FullName);

			EmailTools.AsyncSendMassEmail(recipients, GlobalSettings.SenderEmail,
				Settings.GetWikiTitle(wiki) + " - " + title,
				body.Replace("##PAGE##", title).Replace("##USER##", author != null ? Users.GetDisplayName(author) : "anonymous").Replace("##DATETIME##",
				Preferences.AlignWithServerTimezone(wiki, dateTime).ToString(Settings.GetDateTimeFormat(wiki))).Replace("##SUBJECT##",
				subject).Replace("##LINK##", Settings.GetMainUrl(wiki) + Tools.UrlEncode(page.FullName) +
				GlobalSettings.PageExtension + "?Discuss=1#" + id).Replace("##WIKITITLE##", Settings.GetWikiTitle(wiki)),
				false);
		}

		/// <summary>
		/// Removes a Message.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <param name="id">The ID of the Message to remove.</param>
		/// <param name="removeReplies">A value specifying whether or not to remove the replies.</param>
		/// <returns>True if the Message has been removed successfully.</returns>
		public static bool RemoveMessage(PageContent page, int id, bool removeReplies) {
			if(page.Provider.ReadOnly) return false;

			Message[] messages = page.Provider.GetMessages(page.FullName);
			Message msg = FindMessage(messages, id);

			bool done = page.Provider.RemoveMessage(page.FullName, id, removeReplies);
			if(done) {
				RecentChanges.AddChange(page.Provider.CurrentWiki, page.FullName, page.Title, msg.Subject, DateTime.Now, msg.Username, Change.MessageDeleted, "");
				Host.Instance.OnPageActivity(page.FullName, null, null, PageActivity.MessageDeleted);

				// Unindex message
				SearchClass.UnindexMessage(msg.ID, page);
			}
			return done;
		}

		/// <summary>
		/// Removes all messages of a Page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns><c>true</c> if the messages are removed, <c>false</c> otherwise.</returns>
		public static bool RemoveAllMessages(PageContent page) {
			if(page.Provider.ReadOnly) return false;

			Message[] messages = GetPageMessages(page);

			bool done = true;
			foreach(Message msg in messages) {
				bool tempDone = page.Provider.RemoveMessage(page.FullName, msg.ID, true);
				done &= tempDone;

				// Unindex message
				if(tempDone) {
					SearchClass.UnindexMessage(msg.ID, page);
				}
			}

			return done;
		}

		/// <summary>
		/// Modifies a Message.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <param name="id">The ID of the Message to modify.</param>
		/// <param name="username">The Username.</param>
		/// <param name="subject">The Subject.</param>
		/// <param name="dateTime">The Date/Time.</param>
		/// <param name="body">The Body.</param>
		/// <returns>True if the Message has been modified successfully.</returns>
		public static bool ModifyMessage(PageContent page, int id, string username, string subject, DateTime dateTime, string body) {
			if(page.Provider.ReadOnly) return false;

			bool done = page.Provider.ModifyMessage(page.FullName, id, username, subject, dateTime, body);
			if(done) {
				RecentChanges.AddChange(page.Provider.CurrentWiki, page.FullName, page.Title, subject, dateTime, username, Change.MessageEdited, "");
				Host.Instance.OnPageActivity(page.FullName, null, username, PageActivity.MessageModified);

				// Unindex old message
				SearchClass.UnindexMessage(id, page);
				// Index the new one
				SearchClass.IndexMessage(new Message(id, username, subject, dateTime, body), page);
			}
			return done;
		}

		#endregion

		/// <summary>
		/// Checks the validity of a Page name.
		/// </summary>
		/// <param name="name">The Page name.</param>
		/// <returns>True if the name is valid.</returns>
		public static bool IsValidName(string name) {
			if(name == null) return false;
			if(name.Replace(" ", "").Length == 0 || name.Length > 100 ||
				name.Contains("?") || name.Contains("<") || name.Contains(">") || name.Contains("|") || name.Contains(":") ||
				name.Contains("*") || name.Contains("\"") || name.Contains("/") || name.Contains("\\") || name.Contains("&") ||
				name.Contains("%") || name.Contains("'") || name.Contains("\"") || name.Contains("+") || name.Contains(".") ||
				name.Contains("#") || name.Contains("[") || name.Contains("]") || name.Contains("__")) {
				return false;
			}
			else return true;
		}

	}

	/// <summary>
	/// Compares PageContent objects.
	/// </summary>
	public class PageContentDateComparer : IComparer<PageContent> {

		/// <summary>
		/// Compares two PageContent objects, using the DateTime as parameter.
		/// </summary>
		/// <param name="x">The first object.</param>
		/// <param name="y">The second object.</param>
		/// <returns>The result of the comparison (1, 0 or -1).</returns>
		public int Compare(PageContent x, PageContent y) {
			return x.LastModified.CompareTo(y.LastModified);
		}

	}

}
