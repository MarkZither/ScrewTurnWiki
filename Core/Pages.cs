
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
			foreach(IPagesStorageProviderV30 provider in Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(wiki)) {
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

			IPagesStorageProviderV30 defProv = Collectors.CollectorsBox.PagesProviderCollector.GetProvider(GlobalSettings.DefaultPagesProvider, wiki);
			NamespaceInfo nspace = defProv.GetNamespace(name);
			if(nspace != null) return nspace;

			foreach(IPagesStorageProviderV30 prov in Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(wiki)) {
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
		public static NamespaceInfo FindNamespace(string name, IPagesStorageProviderV30 provider) {
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
		public static bool CreateNamespace(string wiki, string name, IPagesStorageProviderV30 provider) {
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

			List<PageInfo> pages = GetPages(wiki, realNspace);

			bool done = realNspace.Provider.RemoveNamespace(realNspace);
			if(done) {
				DeleteAllAttachments(wiki, pages);

				ResetMetaDataItems(wiki, nspace.Name);

				AuthWriter authWriter = new AuthWriter(Collectors.CollectorsBox.GetSettingsProvider(wiki));
				authWriter.ClearEntriesForNamespace(nspace.Name, pages.ConvertAll((p) => { return NameTools.GetLocalName(p.FullName); }));

				Host.Instance.OnNamespaceActivity(realNspace, null, NamespaceActivity.NamespaceRemoved);

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
		private static void DeleteAllAttachments(string wiki, List<PageInfo> pages) {
			foreach(IFilesStorageProviderV30 prov in Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(wiki)) {
				foreach(PageInfo page in pages) {
					string[] attachments = prov.ListPageAttachments(page);
					foreach(string attachment in attachments) {
						prov.DeletePageAttachment(page, attachment);
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

			List<PageInfo> pages = GetPages(wiki, nspace);
			List<string> pageNames = new List<string>(pages.Count);
			foreach(PageInfo page in pages) pageNames.Add(NameTools.GetLocalName(page.FullName));
			pages = null;

			string oldName = nspace.Name;

			NamespaceInfo newNspace = realNspace.Provider.RenameNamespace(realNspace, newName);
			if(newNspace != null) {
				NotifyFilesProvidersForNamespaceRename(wiki, pageNames, oldName, newName);

				UpdateMetaDataItems(wiki, oldName, newName);

				AuthWriter authWriter = new AuthWriter(Collectors.CollectorsBox.GetSettingsProvider(wiki));
				authWriter.ClearEntriesForNamespace(newName, new List<string>());
				authWriter.ProcessNamespaceRenaming(oldName, pageNames, newName);

				Host.Instance.OnNamespaceActivity(newNspace, oldName, NamespaceActivity.NamespaceRenamed);

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
			foreach(IFilesStorageProviderV30 prov in Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(wiki)) {
				foreach(string page in pages) {
					PageInfo pageToNotify = new PageInfo(NameTools.GetFullName(nspace, page), null, DateTime.Now);
					PageInfo newPage = new PageInfo(NameTools.GetFullName(newName, page), null, DateTime.Now);

					prov.NotifyPageRenaming(pageToNotify, newPage);
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
		public static bool SetNamespaceDefaultPage(string wiki, NamespaceInfo nspace, PageInfo page) {
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

			NamespaceInfo result = pageNamespace.Provider.SetNamespaceDefaultPage(nspace, page);

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
		public static PageInfo FindPage(string wiki, string fullName) {
			if(string.IsNullOrEmpty(fullName)) return null;

			IPagesStorageProviderV30 defProv = Collectors.CollectorsBox.PagesProviderCollector.GetProvider(GlobalSettings.DefaultPagesProvider, wiki);
			PageInfo page = defProv.GetPage(fullName);
			if(page != null) return page;

			foreach(IPagesStorageProviderV30 prov in Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(wiki)) {
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
		public static PageInfo FindPage(string fullName, IPagesStorageProviderV30 provider) {
			if(string.IsNullOrEmpty(fullName)) return null;

			return provider.GetPage(fullName);
		}

		/// <summary>
		/// Gets a page draft, if any.
		/// </summary>
		/// <param name="page">The draft content, or <c>null</c> if no draft exists.</param>
		public static PageContent GetDraft(PageInfo page) {
			if(page == null) return null;

			return page.Provider.GetDraft(page);
		}

		/// <summary>
		/// Deletes the draft of a page (if any).
		/// </summary>
		/// <param name="page">The page of which to delete the draft.</param>
		public static void DeleteDraft(PageInfo page) {
			if(page == null) return;

			if(page.Provider.GetDraft(page) != null) {
				page.Provider.DeleteDraft(page);
			}
		}

		/// <summary>
		/// Gets the Backups/Revisions of a Page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>The list of available Backups/Revision numbers.</returns>
		public static List<int> GetBackups(PageInfo page) {
			int[] temp = page.Provider.GetBackups(page);
			if(temp == null) return null;
			else return new List<int>(temp);
		}

		/// <summary>
		/// Gets the Content of a Page Backup.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <param name="revision">The Backup/Revision number.</param>
		/// <returns>The Content of the Backup.</returns>
		public static PageContent GetBackupContent(PageInfo page, int revision) {
			return page.Provider.GetBackupContent(page, revision);
		}

		/// <summary>
		/// Deletes all the backups of a page.
		/// </summary>
		/// <param name="page">The Page.</param>
		public static bool DeleteBackups(PageInfo page) {
			return DeleteBackups(page, -1);
		}

		/// <summary>
		/// Deletes a subset of the backups of a page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <param name="firstToDelete">The first backup to be deleted (this backup and older backups are deleted).</param>
		public static bool DeleteBackups(PageInfo page, int firstToDelete) {
			if(page.Provider.ReadOnly) return false;

			bool done = page.Provider.DeleteBackups(page, firstToDelete);
			if(done) {
				Log.LogEntry("Backups (0-" + firstToDelete.ToString() + ") deleted for " + page.FullName, EntryType.General, Log.SystemUsername, page.Provider.CurrentWiki);
				Host.Instance.OnPageActivity(page, null, SessionFacade.GetCurrentUsername(), PageActivity.PageBackupsDeleted);
			}
			else {
				Log.LogEntry("Backups (0-" + firstToDelete.ToString() + ") deletion failed for " + page.FullName, EntryType.Error, Log.SystemUsername, page.Provider.CurrentWiki);
			}
			return done;
		}

		/// <summary>
		/// Performs the rollpack of a Page of the specified wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="page">The Page.</param>
		/// <param name="version">The revision to rollback the Page to.</param>
		public static bool Rollback(string wiki, PageInfo page, int version) {
			if(page.Provider.ReadOnly) return false;

			bool done = page.Provider.RollbackPage(page, version);

			if(done) {
				Content.InvalidatePage(page);

				PageContent pageContent = Content.GetPageContent(page);

				// Update page's outgoing links
				string[] linkedPages;
				Formatter.Format(wiki, pageContent.Content, false, FormattingContext.PageContent, page, out linkedPages);
				string[] outgoingLinks = new string[linkedPages.Length];
				for(int i = 0; i < outgoingLinks.Length; i++) {
					outgoingLinks[i] = linkedPages[i];
				}

				Settings.GetProvider(wiki).StoreOutgoingLinks(page.FullName, outgoingLinks);

				Log.LogEntry("Rollback executed for " + page.FullName + " at revision " + version.ToString(), EntryType.General, Log.SystemUsername, wiki);
				RecentChanges.AddChange(wiki, page.FullName, pageContent.Title, null, DateTime.Now, SessionFacade.GetCurrentUsername(), Change.PageRolledBack, "");
				Host.Instance.OnPageActivity(page, null, SessionFacade.GetCurrentUsername(), PageActivity.PageRolledBack);
				return true;
			}
			else {
				Log.LogEntry("Rollback failed for " + page.FullName + " at revision " + version.ToString(), EntryType.Error, Log.SystemUsername, wiki);
				return false;
			}
		}

		/// <summary>
		/// Creates a new Page in the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="nspace">The target namespace (<c>null</c> for the root).</param>
		/// <param name="name">The Page name.</param>
		/// <returns><c>true</c> if the Page is created, <c>false</c> otherwise.</returns>
		public static bool CreatePage(string wiki, NamespaceInfo nspace, string name) {
			string namespaceName = nspace != null ? nspace.Name : null;
			return CreatePage(wiki, namespaceName, name, nspace != null ? nspace.Provider : null);
		}

		/// <summary>
		/// Creates a new Page in the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="nspace">The target namespace (<c>null</c> for the root).</param>
		/// <param name="name">The Page name.</param>
		/// <returns><c>true</c> if the Page is created, <c>false</c> otherwise.</returns>
		public static bool CreatePage(string wiki, string nspace, string name) {
			return CreatePage(wiki, nspace, name, Collectors.CollectorsBox.PagesProviderCollector.GetProvider(GlobalSettings.DefaultPagesProvider, wiki));
		}

		/// <summary>
		/// Creates a new Page in the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="nspace">The target namespace (<c>null</c> for the root).</param>
		/// <param name="name">The Page name.</param>
		/// <param name="provider">The destination provider.</param>
		/// <returns><c>true</c> if the Page is created, <c>false</c> otherwise.</returns>
		public static bool CreatePage(string wiki, NamespaceInfo nspace, string name, IPagesStorageProviderV30 provider) {
			string namespaceName = nspace != null ? nspace.Name : null;
			return CreatePage(wiki, namespaceName, name, provider);
		}

		/// <summary>
		/// Creates a new Page in the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="nspace">The target namespace (<c>null</c> for the root).</param>
		/// <param name="name">The Page name.</param>
		/// <param name="provider">The destination provider.</param>
		/// <returns><c>true</c> if the Page is created, <c>false</c> otherwise.</returns>
		public static bool CreatePage(string wiki, string nspace, string name, IPagesStorageProviderV30 provider) {
			if(provider.ReadOnly) return false;

			string fullName = NameTools.GetFullName(nspace, name);

			if(FindPage(wiki, fullName) != null) return false;

			PageInfo newPage = provider.AddPage(nspace, name, DateTime.Now);

			if(newPage != null) {
				AuthWriter authWriter = new AuthWriter(Collectors.CollectorsBox.GetSettingsProvider(wiki));
				authWriter.ClearEntriesForPage(fullName);

				Content.InvalidateAllPages();
				Log.LogEntry("Page " + fullName + " created", EntryType.General, Log.SystemUsername, wiki);
				Host.Instance.OnPageActivity(newPage, null, SessionFacade.GetCurrentUsername(), PageActivity.PageCreated);
				return true;
			}
			else {
				Log.LogEntry("Page creation failed for " + fullName, EntryType.Error, Log.SystemUsername, wiki);
				return false;
			}
		}

		/// <summary>
		/// Deletes a Page in the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="page">The Page to delete.</param>
		public static bool DeletePage(string wiki, PageInfo page) {
			if(page.Provider.ReadOnly) return false;

			string title = Content.GetPageContent(page).Title;

			bool done = page.Provider.RemovePage(page);

			if(done) {
				AuthWriter authWriter = new AuthWriter(Collectors.CollectorsBox.GetSettingsProvider(wiki));
				authWriter.ClearEntriesForPage(page.FullName);

				foreach(IFilesStorageProviderV30 prov in Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(wiki)) {
					foreach(string attn in prov.ListPageAttachments(page)) {
						prov.DeletePageAttachment(page, attn);
					}
				}

				// Remove the deleted page from the Breadcrumbs Trail and Redirections list
				SessionFacade.Breadcrumbs(wiki).RemovePage(page);
				Redirections.WipePageOut(page);

				Content.InvalidatePage(page);

				// Remove outgoing links
				Settings.GetProvider(wiki).DeleteOutgoingLinks(page.FullName);

				Log.LogEntry("Page " + page.FullName + " deleted", EntryType.General, Log.SystemUsername, wiki);
				RecentChanges.AddChange(wiki, page.FullName, title, null, DateTime.Now, SessionFacade.GetCurrentUsername(), Change.PageDeleted, "");
				Host.Instance.OnPageActivity(page, null, SessionFacade.GetCurrentUsername(), PageActivity.PageDeleted);
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
		/// <param name="wiki">The wiki.</param>
		/// <param name="page">The Page to rename.</param>
		/// <param name="name">The new name.</param>
		public static bool RenamePage(string wiki, PageInfo page, string name) {
			if(page.Provider.ReadOnly) return false;

			string newFullName = NameTools.GetFullName(NameTools.GetNamespace(page.FullName), NameTools.GetLocalName(name));

			if(FindPage(wiki, newFullName) != null) return false;

			string oldName = page.FullName;

			PageContent originalContent = Content.GetPageContent(page);

			Settings.GetProvider(wiki).StoreOutgoingLinks(page.FullName, new string[0]);
			PageInfo pg = page.Provider.RenamePage(page, name);
			if(pg != null) {
				AuthWriter authWriter = new AuthWriter(Collectors.CollectorsBox.GetSettingsProvider(wiki));
				authWriter.ClearEntriesForPage(newFullName);
				authWriter.ProcessPageRenaming(oldName, newFullName);

				foreach(IFilesStorageProviderV30 prov in Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(wiki)) {
					prov.NotifyPageRenaming(new PageInfo(oldName, page.Provider, page.CreationDateTime), pg);
				}

				StorePageOutgoingLinks(wiki, pg, originalContent.Content);

				SessionFacade.Breadcrumbs(wiki).RemovePage(page);
				Redirections.Clear();
				Content.InvalidateAllPages();

				// Page redirect is implemented directly in AdminPages.aspx.cs

				Log.LogEntry("Page " + oldName + " renamed to " + name, EntryType.General, Log.SystemUsername, wiki);
				RecentChanges.AddChange(wiki, page.FullName, originalContent.Title, null, DateTime.Now, SessionFacade.GetCurrentUsername(), Change.PageRenamed, "");
				Host.Instance.OnPageActivity(page, oldName, SessionFacade.GetCurrentUsername(), PageActivity.PageRenamed);
				return true;
			}
			else {
				Log.LogEntry("Page rename failed for " + page.FullName + " (" + name + ")", EntryType.Error, Log.SystemUsername, wiki);
				return false;
			}
		}

		/// <summary>
		/// Migrates a page of the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="page">The page to migrate.</param>
		/// <param name="targetNamespace">The target namespace.</param>
		/// <param name="copyCategories">A value indicating whether to copy the page categories to the target namespace.</param>
		/// <returns><c>true</c> if the page is migrated, <c>false</c> otherwise.</returns>
		public static bool MigratePage(string wiki, PageInfo page, NamespaceInfo targetNamespace, bool copyCategories) {
			string oldName = page.FullName;

			PageInfo result = page.Provider.MovePage(page, targetNamespace, copyCategories);
			if(result != null) {
				Settings.GetProvider(wiki).StoreOutgoingLinks(page.FullName, new string[0]);
				
				PageContent content = Content.GetPageContent(result);
				StorePageOutgoingLinks(wiki, result, content.Content);

				foreach(IFilesStorageProviderV30 prov in Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(wiki)) {
					prov.NotifyPageRenaming(new PageInfo(oldName, page.Provider, page.CreationDateTime), result);
				}
			}
			return result != null;
		}

		/// <summary>
		/// Modifies a Page of the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="page">The Page to modify.</param>
		/// <param name="title">The Title of the Page.</param>
		/// <param name="username">The Username of the user who modified the Page.</param>
		/// <param name="dateTime">The Date/Time of the modification.</param>
		/// <param name="comment">The comment of the editor, about this revision.</param>
		/// <param name="content">The Content.</param>
		/// <param name="keywords">The keywords, usually used for SEO.</param>
		/// <param name="description">The description, usually used for SEO.</param>
		/// <param name="saveMode">The save mode.</param>
		/// <returns>True if the Page has been modified successfully.</returns>
		public static bool ModifyPage(string wiki, PageInfo page, string title, string username, DateTime dateTime, string comment, string content,
			string[] keywords, string description, SaveMode saveMode) {

			if(page.Provider.ReadOnly) return false;

			StringBuilder sb = new StringBuilder(content);
			sb.Replace("~~~~", "��(" + username + "," + dateTime.ToString("yyyy'/'MM'/'dd' 'HH':'mm':'ss") + ")��");
			content = sb.ToString();

			// Because of transclusion and other page-linking features, it is necessary to invalidate all pages
			Content.InvalidateAllPages();

			bool done = page.Provider.ModifyPage(page, title, username, dateTime, comment, content, keywords, description, saveMode);

			if(done) {
				Log.LogEntry("Page Content updated for " + page.FullName, EntryType.General, Log.SystemUsername, wiki);

				StorePageOutgoingLinks(wiki, page, content);

				if(saveMode != SaveMode.Draft) {
					RecentChanges.AddChange(wiki, page.FullName, title, null, dateTime, username, Change.PageUpdated, comment);
					Host.Instance.OnPageActivity(page, null, username, PageActivity.PageModified);
					SendEmailNotificationForPage(wiki, page, Users.FindUser(wiki, username));
				}
				else {
					Host.Instance.OnPageActivity(page, null, username, PageActivity.PageDraftSaved);
				}

				if(saveMode == SaveMode.Backup) {
					// Delete old backups, if needed
					DeleteOldBackupsIfNeeded(wiki, page);
				}
			}
			else Log.LogEntry("Page Content update failed for " + page.FullName, EntryType.Error, Log.SystemUsername, wiki);
			return done;
		}

		/// <summary>
		/// Stores outgoing links for a page of the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="page">The page.</param>
		/// <param name="content">The raw content.</param>
		public static void StorePageOutgoingLinks(string wiki, PageInfo page, string content) {
			string[] linkedPages;
			Formatter.Format(wiki, content, false, FormattingContext.PageContent, page, out linkedPages);

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
		/// <param name="wiki">The wiki.</param>
		/// <param name="page">The page.</param>
		private static void DeleteOldBackupsIfNeeded(string wiki, PageInfo page) {
			int maxBackups = Settings.GetKeptBackupNumber(wiki);
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
		/// <param name="wiki">The wiki.</param>
		/// <param name="page">The page that was modified.</param>
		/// <param name="author">The author of the modification.</param>
		private static void SendEmailNotificationForPage(string wiki, PageInfo page, UserInfo author) {
			if(page == null) return;

			PageContent content = Content.GetPageContent(page);

			UserInfo[] usersToNotify = Users.GetUsersToNotifyForPageChange(wiki, page);
			usersToNotify = RemoveUserFromArray(usersToNotify, author);
			string[] recipients = EmailTools.GetRecipients(usersToNotify);

			string body = Settings.GetProvider(wiki).GetMetaDataItem(MetaDataItem.PageChangeMessage, null);

			string title = FormattingPipeline.PrepareTitle(wiki, content.Title, false, FormattingContext.Other, page);

			EmailTools.AsyncSendMassEmail(recipients, GlobalSettings.SenderEmail,
				Settings.GetWikiTitle(wiki) + " - " + title,
				body.Replace("##PAGE##", title).Replace("##USER##", author != null ? Users.GetDisplayName(author) : "anonymous").Replace("##DATETIME##",
				Preferences.AlignWithServerTimezone(wiki, content.LastModified).ToString(Settings.GetDateTimeFormat(wiki))).Replace("##COMMENT##",
				(string.IsNullOrEmpty(content.Comment) ? Exchanger.ResourceExchanger.GetResource("None") : content.Comment)).Replace("##LINK##",
				Settings.GetMainUrl(wiki) + Tools.UrlEncode(page.FullName) + GlobalSettings.PageExtension).Replace("##WIKITITLE##", Settings.GetWikiTitle(wiki)),
				false);
		}

		/// <summary>
		/// Determines whether a user of the given wiki can edit a page.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="page">The page.</param>
		/// <param name="username">The username.</param>
		/// <param name="groups">The groups.</param>
		/// <param name="canEdit">A value indicating whether the user can edit the page.</param>
		/// <param name="canEditWithApproval">A value indicating whether the user can edit the page with subsequent approval.</param>
		public static void CanEditPage(string wiki, PageInfo page, string username, string[] groups,
			out bool canEdit, out bool canEditWithApproval) {

			canEdit = false;
			canEditWithApproval = false;

			AuthChecker authChecker = new AuthChecker(Collectors.CollectorsBox.GetSettingsProvider(wiki));
			switch(Settings.GetModerationMode(wiki)) {
				case ChangeModerationMode.RequirePageEditingPermissions:
					canEdit = authChecker.CheckActionForPage(page, Actions.ForPages.ManagePage, username, groups);
					canEditWithApproval = authChecker.CheckActionForPage(page, Actions.ForPages.ModifyPage, username, groups);
					break;
				case ChangeModerationMode.RequirePageViewingPermissions:
					canEdit = authChecker.CheckActionForPage(page, Actions.ForPages.ModifyPage, username, groups);
					canEditWithApproval = authChecker.CheckActionForPage(page, Actions.ForPages.ReadPage, username, groups);
					break;
				case ChangeModerationMode.None:
					canEdit = authChecker.CheckActionForPage(page, Actions.ForPages.ModifyPage, username, groups);
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
		/// <param name="page">The page.</param>
		/// <param name="username">The username.</param>
		/// <param name="groups">The groups.</param>
		/// <returns><c>true</c> if the user can approve/reject a draft of the page, <c>false</c> otherwise.</returns>
		public static bool CanApproveDraft(string wiki, PageInfo page, string username, string[] groups) {
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
			return authChecker.CheckActionForPage(page, requiredAction, username, groups);
		}

		/// <summary>
		/// Sends a draft notification to "administrators" of the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="currentPage">The edited page.</param>
		/// <param name="title">The title.</param>
		/// <param name="comment">The comment.</param>
		/// <param name="author">The author.</param>
		public static void SendEmailNotificationForDraft(string wiki, PageInfo currentPage, string title, string comment, string author) {
			// Decide the users to notify based on the ChangeModerationMode
			// Retrieve the list of matching users
			// Asynchronously send the notification

			// Retrieve all the users that have a grant on requiredAction for the current page
			// TODO: make this work when Users.GetUsers does not return all existing users but only a sub-set
			List<UserInfo> usersToNotify = new List<UserInfo>(10);
			foreach(UserInfo user in Users.GetUsers(wiki)) {
				if(user.Active && CanApproveDraft(wiki, currentPage, user.Username, user.Groups)) {
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
				Settings.GetMainUrl(wiki) + UrlTools.BuildUrl(wiki, "Edit.aspx?Page=", Tools.UrlEncode(currentPage.FullName))).Replace("##LINK2##",
				Settings.GetMainUrl(wiki) + "AdminPages.aspx?Admin=" + Tools.UrlEncode(currentPage.FullName)).Replace("##WIKITITLE##",
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
		public static List<PageInfo> GetPages(string wiki, NamespaceInfo nspace) {
			List<PageInfo> allPages = new List<PageInfo>(10000);

			// Retrieve all pages from Pages Providers
			int count = 0;
			foreach(IPagesStorageProviderV30 provider in Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(wiki)) {
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

			foreach(IPagesStorageProviderV30 prov in Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(wiki)) {
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
		/// <param name="page">The page.</param>
		/// <returns>The incoming links.</returns>
		public static string[] GetPageIncomingLinks(string wiki, PageInfo page) {
			if(page == null) return null;

			return GetPageIncomingLinks(page, Settings.GetProvider(wiki).GetAllOutgoingLinks());
		}

		private static string[] GetPageIncomingLinks(PageInfo page, IDictionary<string, string[]> allOutgoingLinks) {
			string[] knownPages = new string[allOutgoingLinks.Count];
			allOutgoingLinks.Keys.CopyTo(knownPages, 0);

			List<string> result = new List<string>(20);

			foreach(string key in knownPages) {
				if(Contains(allOutgoingLinks[key], page.FullName)) {
					// result is likely to be very small, so a linear search is fine
					if(!result.Contains(key)) result.Add(key);
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// Gets all the orphan pages.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="pages">The pages to analyze.</param>
		/// <returns>The orphan pages.</returns>
		public static List<string> GetOrphanedPages(string wiki, IList<PageInfo> pages) {
			IDictionary<string, string[]> allLinks = Settings.GetProvider(wiki).GetAllOutgoingLinks();

			List<string> orphans = new List<string>();
			foreach(var p in pages) {
				if(GetPageIncomingLinks(p, allLinks).Length == 0) {
					orphans.Add(p.FullName);
				}
			}

			return orphans;
		}

		/// <summary>
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="page">The page.</param>
		/// <returns>The outgoing links.</returns>
		public static string[] GetPageOutgoingLinks(string wiki, PageInfo page) {
			if(page == null) return null;
			return Settings.GetProvider(wiki).GetOutgoingLinks(page.FullName);
		}

		/// <summary>
		/// Gets all the pages in a namespace of a wiki without incoming links.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The orphaned pages.</returns>
		public static PageInfo[] GetOrphanedPages(string wiki, NamespaceInfo nspace) {
			List<PageInfo> pages = GetPages(wiki, nspace);
			IDictionary<string, string[]> allLinks = Settings.GetProvider(wiki).GetAllOutgoingLinks();
			string[] knownPages = new string[allLinks.Count];
			allLinks.Keys.CopyTo(knownPages, 0);

			Dictionary<PageInfo, bool> result = new Dictionary<PageInfo, bool>(pages.Count);

			foreach(PageInfo page in pages) {
				result.Add(page, false);
				foreach(string key in knownPages) {
					if(Contains(allLinks[key], page.FullName)) {
						// page has incoming links
						result[page] = true;
					}
				}
			}

			return ExtractNegativeKeys(result);
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

						PageInfo tempPage = FindPage(wiki, link);
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

			IPagesStorageProviderV30 defProv = Collectors.CollectorsBox.PagesProviderCollector.GetProvider(GlobalSettings.DefaultPagesProvider, wiki);
			CategoryInfo category = defProv.GetCategory(fullName);
			if(category != null) return category;

			foreach(IPagesStorageProviderV30 prov in Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(wiki)) {
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
		public static bool CreateCategory(string wiki, NamespaceInfo nspace, string name, IPagesStorageProviderV30 provider) {
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
		public static bool CreateCategory(string wiki, string nspace, string name, IPagesStorageProviderV30 provider) {
			if(provider == null) provider = Collectors.CollectorsBox.PagesProviderCollector.GetProvider(GlobalSettings.DefaultPagesProvider, wiki);

			if(provider.ReadOnly) return false;

			string fullName = NameTools.GetFullName(nspace, name);

			if(FindCategory(wiki, fullName) != null) return false;

			CategoryInfo newCat = provider.AddCategory(nspace, name);
			if(newCat != null) {
				Log.LogEntry("Category " + fullName + " created", EntryType.General, Log.SystemUsername, wiki);

				// Because of transclusion and other page-linking features, it is necessary to invalidate all pages
				Content.InvalidateAllPages();

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

			// Because of transclusion and other page-linking features, it is necessary to invalidate all pages
			Content.InvalidateAllPages();

			return done;
		}

		/// <summary>
		/// Renames a Category in the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="category">The Category to rename.</param>
		/// <param name="newName">The new Name of the Category.</param>
		/// <returns>True if the Category has been renamed successfully.</returns>
		public static bool RenameCategory(string wiki, CategoryInfo category, string newName) {
			if(category.Provider.ReadOnly) return false;

			string newFullName = NameTools.GetFullName(NameTools.GetNamespace(category.FullName), newName);

			if(FindCategory(wiki, newFullName) != null) return false;

			string oldName = category.FullName;

			CategoryInfo newCat = category.Provider.RenameCategory(category, newName);
			if(newCat != null) Log.LogEntry("Category " + oldName + " renamed to " + newFullName, EntryType.General, Log.SystemUsername, wiki);
			else Log.LogEntry("Category rename failed for " + oldName + " (" + newFullName + ")", EntryType.Error, Log.SystemUsername, wiki);

			// Because of transclusion and other page-linking features, it is necessary to invalidate all pages
			Content.InvalidateAllPages();

			return newCat != null;
		}

		/// <summary>
		/// Gets the Categories of a Page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>The Categories of the Page.</returns>
		public static CategoryInfo[] GetCategoriesForPage(PageInfo page) {
			if(page == null) return new CategoryInfo[0];

			CategoryInfo[] categories = page.Provider.GetCategoriesForPage(page);

			return categories;
		}

		/// <summary>
		/// Gets all the Uncategorized Pages in a wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="nspace">The namespace.</param>
		/// <returns>The Uncategorized Pages.</returns>
		public static PageInfo[] GetUncategorizedPages(string wiki, NamespaceInfo nspace) {
			if(nspace == null) {
				List<PageInfo> pages = new List<PageInfo>(1000);

				int count = 0;
				foreach(IPagesStorageProviderV30 prov in Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(wiki)) {
					count++;
					pages.AddRange(prov.GetUncategorizedPages(null));
				}

				if(count > 1) {
					pages.Sort(new PageNameComparer());
				}

				return pages.ToArray();
			}
			else {
				PageInfo[] pages = nspace.Provider.GetUncategorizedPages(nspace);
				return pages;
			}
		}

		/// <summary>
		/// Gets the valid Categories for a Page of a wiki, i.e. the Categories managed by the Page's Provider and in the same namespace as the page.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="page">The Page, or <c>null</c> to use the default provider.</param>
		/// <returns>The valid Categories.</returns>
		public static CategoryInfo[] GetAvailableCategories(string wiki, PageInfo page) {
			NamespaceInfo pageNamespace = FindNamespace(wiki, NameTools.GetNamespace(page.FullName));

			if(page != null) {
				return page.Provider.GetCategories(pageNamespace);
			}
			else {
				return Collectors.CollectorsBox.PagesProviderCollector.GetProvider(GlobalSettings.DefaultPagesProvider, wiki).GetCategories(pageNamespace);
			}
		}

		/// <summary>
		/// Gets the other Categories of the Provider and Namespace of the specified Category in the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="category">The Category.</param>
		/// <returns>The matching Categories.</returns>
		public static CategoryInfo[] GetMatchingCategories(string wiki, CategoryInfo category) {
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
		public static bool Rebind(PageInfo page, CategoryInfo[] cats) {
			if(page.Provider.ReadOnly) return false;

			string[] names = new string[cats.Length];
			for(int i = 0; i < cats.Length; i++) {
				if(cats[i].Provider != page.Provider) return false;
				names[i] = cats[i].FullName; // Saves one cycle
			}
			bool done = page.Provider.RebindPage(page, names);
			if(done) Log.LogEntry("Page " + page.FullName + " rebound", EntryType.General, Log.SystemUsername, page.Provider.CurrentWiki);
			else Log.LogEntry("Page rebind failed for " + page.FullName, EntryType.Error, Log.SystemUsername, page.Provider.CurrentWiki);

			// Because of transclusion and other page-linking features, it is necessary to invalidate all pages
			Content.InvalidateAllPages();

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

			// Because of transclusion and other page-linking features, it is necessary to invalidate all pages
			Content.InvalidateAllPages();

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
			foreach(IPagesStorageProviderV30 provider in Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(wiki)) {
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
		public static Message[] GetPageMessages(PageInfo page) {
			return page.Provider.GetMessages(page);
		}

		/// <summary>
		/// Gets the total number of Messages in a Page Discussion.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>The number of messages.</returns>
		public static int GetMessageCount(PageInfo page) {
			return page.Provider.GetMessageCount(page);
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
		/// Adds a new Message to a Page of the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="page">The Page.</param>
		/// <param name="username">The Username.</param>
		/// <param name="subject">The Subject.</param>
		/// <param name="dateTime">The Date/Time.</param>
		/// <param name="body">The Body.</param>
		/// <param name="parent">The Parent Message ID, or -1.</param>
		/// <returns>True if the Message has been added successfully.</returns>
		public static bool AddMessage(string wiki, PageInfo page, string username, string subject, DateTime dateTime, string body, int parent) {
			if(page.Provider.ReadOnly) return false;

			bool done = page.Provider.AddMessage(page, username, subject, dateTime, body, parent);
			if(done) {
				SendEmailNotificationForMessage(wiki, page, Users.FindUser(wiki, username), Tools.GetMessageIdForAnchor(dateTime), subject, dateTime);

				PageContent content = Content.GetPageContent(page);
				RecentChanges.AddChange(wiki, page.FullName, content.Title, subject, dateTime, username, Change.MessagePosted, "");
				Host.Instance.OnPageActivity(page, null, username, PageActivity.MessagePosted);
			}
			return done;
		}

		/// <summary>
		/// Sends the email notification for a new message.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="page">The page the message was posted to.</param>
		/// <param name="author">The author of the message.</param>
		/// <param name="id">The message ID to be used for anchors.</param>
		/// <param name="subject">The message subject.</param>
		/// <param name="dateTime">The message date/time.</param>
		private static void SendEmailNotificationForMessage(string wiki, PageInfo page, UserInfo author, string id, string subject, DateTime dateTime) {
			if(page == null) return;

			PageContent content = Content.GetPageContent(page);

			UserInfo[] usersToNotify = Users.GetUsersToNotifyForDiscussionMessages(wiki, page);
			usersToNotify = RemoveUserFromArray(usersToNotify, author);
			string[] recipients = EmailTools.GetRecipients(usersToNotify);

			string body = Settings.GetProvider(wiki).GetMetaDataItem(MetaDataItem.DiscussionChangeMessage, null);

			string title = FormattingPipeline.PrepareTitle(wiki, content.Title, false, FormattingContext.Other, page);

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
		/// <param name="wiki">The wiki.</param>
		/// <param name="page">The Page.</param>
		/// <param name="id">The ID of the Message to remove.</param>
		/// <param name="removeReplies">A value specifying whether or not to remove the replies.</param>
		/// <returns>True if the Message has been removed successfully.</returns>
		public static bool RemoveMessage(string wiki, PageInfo page, int id, bool removeReplies) {
			if(page.Provider.ReadOnly) return false;

			Message[] messages = page.Provider.GetMessages(page);
			Message msg = FindMessage(messages, id);

			bool done = page.Provider.RemoveMessage(page, id, removeReplies);
			if(done) {
				PageContent content = Content.GetPageContent(page);
				RecentChanges.AddChange(wiki, page.FullName, content.Title, msg.Subject, DateTime.Now, msg.Username, Change.MessageDeleted, "");
				Host.Instance.OnPageActivity(page, null, null, PageActivity.MessageDeleted);
			}
			return done;
		}

		/// <summary>
		/// Removes all messages of a Page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns><c>true</c> if the messages are removed, <c>false</c> otherwise.</returns>
		public static bool RemoveAllMessages(PageInfo page) {
			if(page.Provider.ReadOnly) return false;

			Message[] messages = GetPageMessages(page);

			bool done = true;
			foreach(Message msg in messages) {
				done &= page.Provider.RemoveMessage(page, msg.ID, true);
			}

			return done;
		}

		/// <summary>
		/// Modifies a Message.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="page">The Page.</param>
		/// <param name="id">The ID of the Message to modify.</param>
		/// <param name="username">The Username.</param>
		/// <param name="subject">The Subject.</param>
		/// <param name="dateTime">The Date/Time.</param>
		/// <param name="body">The Body.</param>
		/// <returns>True if the Message has been modified successfully.</returns>
		public static bool ModifyMessage(string wiki, PageInfo page, int id, string username, string subject, DateTime dateTime, string body) {
			if(page.Provider.ReadOnly) return false;

			bool done = page.Provider.ModifyMessage(page, id, username, subject, dateTime, body);
			if(done) {
				PageContent content = Content.GetPageContent(page);
				RecentChanges.AddChange(wiki, page.FullName, content.Title, subject, dateTime, username, Change.MessageEdited, "");
				Host.Instance.OnPageActivity(page, null, username, PageActivity.MessageModified);
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
				name.Contains("#") || name.Contains("[") || name.Contains("]")) {
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
