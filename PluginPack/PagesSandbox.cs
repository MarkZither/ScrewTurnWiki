
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki.SearchEngine;

namespace ScrewTurn.Wiki.Plugins.PluginPack {

	/// <summary>
	/// Implements a sandbox plugin for pages.
	/// </summary>
	public class PagesSandbox {

		private IHostV30 host;
		private string config;
		private static readonly ComponentInformation info = new ComponentInformation("Pages Sandbox", "ScrewTurn Software", "3.0.0.180", "http://www.screwturn.eu", "http://www.screwturn.eu/Version/PluginPack/PagesSandbox.txt");

		private List<NamespaceInfo> allNamespaces = new List<NamespaceInfo>(5);

		private List<PageInfo> allPages;
		private Dictionary<PageInfo, PageContent> allContents;
		private Dictionary<PageContent, List<PageContent>> allBackups;
		private Dictionary<PageInfo, PageContent> allDrafts;
		
		private List<CategoryInfo> allCategories;

		private uint freeDocumentId = 1;
		private uint freeWordId = 1;
		private IInMemoryIndex index;

		/// <summary>
		/// Gets a namespace.
		/// </summary>
		/// <param name="name">The name of the namespace (cannot be <c>null</c> or empty).</param>
		/// <returns>The <see cref="T:NamespaceInfo"/>, or <c>null</c> if no namespace is found.</returns>
		public NamespaceInfo GetNamespace(string name) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty");

			return allNamespaces.Find(n => n.Name == name);
		}

		/// <summary>
		/// Gets all the sub-namespaces.
		/// </summary>
		/// <returns>The sub-namespaces, sorted by name.</returns>
		public NamespaceInfo[] GetNamespaces() {
			lock(this) {
				return allNamespaces.ToArray();
			}
		}

		/// <summary>
		/// Adds a new namespace.
		/// </summary>
		/// <param name="name">The name of the namespace.</param>
		/// <returns>The correct <see cref="T:NamespaceInfo"/> object.</returns>
		public NamespaceInfo AddNamespace(string name) {
			throw new NotImplementedException();
			/*if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");

			lock(this) {
				if(GetNamespace(name) != null) return null;

				// This does not compile unless PagesSandbox implements IPagesStorageProviderV30
				NamespaceInfo newSpace = new NamespaceInfo(name, this, null);

				allNamespaces.Add(newSpace);

				return newSpace;
			}*/
		}

		/// <summary>
		/// Renames a namespace.
		/// </summary>
		/// <param name="nspace">The namespace to rename.</param>
		/// <param name="newName">The new name of the namespace.</param>
		/// <returns>The correct <see cref="T:NamespaceInfo"/> object.</returns>
		public NamespaceInfo RenameNamespace(NamespaceInfo nspace, string newName) {
			if(nspace == null) throw new ArgumentNullException("nspace");
			if(newName == null) throw new ArgumentNullException("newName");
			if(newName.Length == 0) throw new ArgumentException("New Name cannot be empty", "newName");

			lock(this) {
				if(GetNamespace(newName) != null) return null;

				nspace.Name = newName;

				return nspace;
			}
		}

		/// <summary>
		/// Sets the default page of a namespace.
		/// </summary>
		/// <param name="nspace">The namespace of which to set the default page.</param>
		/// <param name="page">The page to use as default page, or <c>null</c>.</param>
		/// <returns>The correct <see cref="T:NamespaceInfo"/> object.</returns>
		public NamespaceInfo SetNamespaceDefaultPage(NamespaceInfo nspace, PageInfo page) {
			if(nspace == null) throw new ArgumentNullException("nspace");

			lock(this) {
				nspace.DefaultPage = page;

				return nspace;
			}
		}

		/// <summary>
		/// Removes a namespace.
		/// </summary>
		/// <param name="nspace">The namespace to remove.</param>
		/// <returns><c>true</c> if the namespace is removed, <c>false</c> otherwise.</returns>
		public bool RemoveNamespace(NamespaceInfo nspace) {
			if(nspace == null) throw new ArgumentNullException("nspace");

			lock(this) {
				return allNamespaces.Remove(nspace);
			}
		}

		/// <summary>
		/// Moves a page from its namespace into another.
		/// </summary>
		/// <param name="page">The page to move.</param>
		/// <param name="destination">The destination namespace (<c>null</c> for the root).</param>
		/// <param name="copyCategories">A value indicating whether to copy the page categories in the destination
		/// namespace, if not already available.</param>
		/// <returns>The correct instance of <see cref="T:PageInfo"/>.</returns>
		public PageInfo MovePage(PageInfo page, NamespaceInfo destination, bool copyCategories) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets a category.
		/// </summary>
		/// <param name="fullName">The full name of the category.</param>
		/// <returns>The <see cref="T:CategoryInfo"/>, or <c>null</c> if no category is found.</returns>
		public CategoryInfo GetCategory(string fullName) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets all the Categories in a namespace.
		/// </summary>
		/// <param name="nspace">The namespace.</param>
		/// <returns>All the Categories in the namespace, sorted by name.</returns>
		public CategoryInfo[] GetCategories(NamespaceInfo nspace) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets all the categories of a page.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <returns>The categories, sorted by name.</returns>
		public CategoryInfo[] GetCategoriesForPage(PageInfo page) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Adds a Category.
		/// </summary>
		/// <param name="nspace">The target namespace (<c>null</c> for the root).</param>
		/// <param name="name">The Category name.</param>
		/// <returns>The correct CategoryInfo object.</returns>
		/// <remarks>The moethod should set category's Pages to an empty array.</remarks>
		public CategoryInfo AddCategory(string nspace, string name) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Renames a Category.
		/// </summary>
		/// <param name="category">The Category to rename.</param>
		/// <param name="newName">The new Name.</param>
		/// <returns>The correct CategoryInfo object.</returns>
		public CategoryInfo RenameCategory(CategoryInfo category, string newName) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Removes a Category.
		/// </summary>
		/// <param name="category">The Category to remove.</param>
		/// <returns>True if the Category has been removed successfully.</returns>
		public bool RemoveCategory(CategoryInfo category) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Merges two Categories.
		/// </summary>
		/// <param name="source">The source Category.</param>
		/// <param name="destination">The destination Category.</param>
		/// <returns>The correct <see cref="T:CategoryInfo"/> object.</returns>
		/// <remarks>The <b>destination</b> Category remains, while the <b>source</b> Category is deleted, and all its Pages re-bound
		/// in the <b>destination</b> Category.</remarks>
		public CategoryInfo MergeCategories(CategoryInfo source, CategoryInfo destination) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Performs a search in the index.
		/// </summary>
		/// <param name="parameters">The search parameters.</param>
		/// <returns>The results.</returns>
		public SearchResultCollection PerformSearch(SearchParameters parameters) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Rebuilds the search index.
		/// </summary>
		public void RebuildIndex() {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets some statistics about the search engine index.
		/// </summary>
		/// <param name="documentCount">The total number of documents.</param>
		/// <param name="wordCount">The total number of unique words.</param>
		/// <param name="occurrenceCount">The total number of word-document occurrences.</param>
		/// <param name="size">The approximated size, in bytes, of the search engine index.</param>
		public void GetIndexStats(out int documentCount, out int wordCount, out int occurrenceCount, out long size) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets a value indicating whether the search engine index is corrupted and needs to be rebuilt.
		/// </summary>
		public bool IsIndexCorrupted {
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		/// Gets a page.
		/// </summary>
		/// <param name="fullName">The full name of the page.</param>
		/// <returns>The <see cref="T:PageInfo"/>, or <c>null</c> if no page is found.</returns>
		public PageInfo GetPage(string fullName) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets all the Pages in a namespace.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>All the Pages in the namespace, sorted by name.</returns>
		public PageInfo[] GetPages(NamespaceInfo nspace) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets all the pages in a namespace that are bound to zero categories.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The pages, sorted by name.</returns>
		public PageInfo[] GetUncategorizedPages(NamespaceInfo nspace) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the Content of a Page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>The Page Content object.</returns>
		public PageContent GetContent(PageInfo page) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the content of a draft of a Page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>The draft, or <c>null</c> if no draft exists.</returns>
		public PageContent GetDraft(PageInfo page) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Deletes a draft of a Page.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <returns><c>true</c> if the draft is deleted, <c>false</c> otherwise.</returns>
		public bool DeleteDraft(PageInfo page) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the Backup/Revision numbers of a Page.
		/// </summary>
		/// <param name="page">The Page to get the Backups of.</param>
		/// <returns>The Backup/Revision numbers.</returns>
		public int[] GetBackups(PageInfo page) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the Content of a Backup of a Page.
		/// </summary>
		/// <param name="page">The Page to get the backup of.</param>
		/// <param name="revision">The Backup/Revision number.</param>
		/// <returns>The Page Backup.</returns>
		public PageContent GetBackupContent(PageInfo page, int revision) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Forces to overwrite or create a Backup.
		/// </summary>
		/// <param name="content">The Backup content.</param>
		/// <param name="revision">The revision.</param>
		/// <returns>True if the Backup has been created successfully.</returns>
		public bool SetBackupContent(PageContent content, int revision) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Adds a Page.
		/// </summary>
		/// <param name="nspace">The target namespace (<c>null</c> for the root).</param>
		/// <param name="name">The Page Name.</param>
		/// <param name="creationDateTime">The creation Date/Time.</param>
		/// <returns>The correct PageInfo object or null.</returns>
		/// <remarks>This method should <b>not</b> create the content of the Page.</remarks>
		public PageInfo AddPage(string nspace, string name, DateTime creationDateTime) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Renames a Page.
		/// </summary>
		/// <param name="page">The Page to rename.</param>
		/// <param name="newName">The new Name.</param>
		/// <returns>The correct <see cref="T:PageInfo"/> object.</returns>
		public PageInfo RenamePage(PageInfo page, string newName) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Modifies the Content of a Page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <param name="title">The Title of the Page.</param>
		/// <param name="username">The Username.</param>
		/// <param name="dateTime">The Date/Time.</param>
		/// <param name="comment">The Comment of the editor, about this revision.</param>
		/// <param name="content">The Page Content.</param>
		/// <param name="keywords">The keywords, usually used for SEO.</param>
		/// <param name="description">The description, usually used for SEO.</param>
		/// <param name="saveMode">The save mode for this modification.</param>
		/// <returns><c>true</c> if the Page has been modified successfully, <c>false</c> otherwise.</returns>
		/// <remarks>If <b>saveMode</b> equals <b>Draft</b> and a draft already exists, it is overwritten.</remarks>
		public bool ModifyPage(PageInfo page, string title, string username, DateTime dateTime, string comment, string content, string[] keywords, string description, SaveMode saveMode) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Performs the rollback of a Page to a specified revision.
		/// </summary>
		/// <param name="page">The Page to rollback.</param>
		/// <param name="revision">The Revision to rollback the Page to.</param>
		/// <returns><c>true</c> if the rollback succeeded, <c>false</c> otherwise.</returns>
		public bool RollbackPage(PageInfo page, int revision) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Deletes the Backups of a Page, up to a specified revision.
		/// </summary>
		/// <param name="page">The Page to delete the backups of.</param>
		/// <param name="revision">The newest revision to delete (newer revision are kept) or -1 to delete all the Backups.</param>
		/// <returns><c>true</c> if the deletion succeeded, <c>false</c> otherwise.</returns>
		public bool DeleteBackups(PageInfo page, int revision) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Removes a Page.
		/// </summary>
		/// <param name="page">The Page to remove.</param>
		/// <returns>True if the Page is removed successfully.</returns>
		public bool RemovePage(PageInfo page) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Binds a Page with one or more Categories.
		/// </summary>
		/// <param name="page">The Page to bind.</param>
		/// <param name="categories">The Categories to bind the Page with.</param>
		/// <returns>True if the binding succeeded.</returns>
		/// <remarks>After a successful operation, the Page is bound with all and only the categories passed as argument.</remarks>
		public bool RebindPage(PageInfo page, string[] categories) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the Page Messages.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>The list of the <b>first-level</b> Messages, containing the replies properly nested, sorted by date/time.</returns>
		public Message[] GetMessages(PageInfo page) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the total number of Messages in a Page Discussion.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>The number of messages.</returns>
		public int GetMessageCount(PageInfo page) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Removes all messages for a page and stores the new messages.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <param name="messages">The new messages to store.</param>
		/// <returns><c>true</c> if the messages are stored, <c>false</c> otherwise.</returns>
		public bool BulkStoreMessages(PageInfo page, Message[] messages) {
			throw new NotImplementedException();
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
		/// <returns>True if the Message is added successfully.</returns>
		public bool AddMessage(PageInfo page, string username, string subject, DateTime dateTime, string body, int parent) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Removes a Message.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <param name="id">The ID of the Message to remove.</param>
		/// <param name="removeReplies">A value specifying whether or not to remove the replies.</param>
		/// <returns>True if the Message is removed successfully.</returns>
		public bool RemoveMessage(PageInfo page, int id, bool removeReplies) {
			throw new NotImplementedException();
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
		/// <returns>True if the Message is modified successfully.</returns>
		public bool ModifyMessage(PageInfo page, int id, string username, string subject, DateTime dateTime, string body) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets all the Navigation Paths in a Namespace.
		/// </summary>
		/// <param name="nspace">The Namespace.</param>
		/// <returns>All the Navigation Paths, sorted by name.</returns>
		public NavigationPath[] GetNavigationPaths(NamespaceInfo nspace) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Adds a new Navigation Path.
		/// </summary>
		/// <param name="nspace">The target namespace (<c>null</c> for the root).</param>
		/// <param name="name">The Name of the Path.</param>
		/// <param name="pages">The Pages array.</param>
		/// <returns>The correct <see cref="T:NavigationPath"/> object.</returns>
		public NavigationPath AddNavigationPath(string nspace, string name, PageInfo[] pages) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Modifies an existing navigation path.
		/// </summary>
		/// <param name="path">The navigation path to modify.</param>
		/// <param name="pages">The new pages array.</param>
		/// <returns>The correct <see cref="T:NavigationPath"/> object.</returns>
		public NavigationPath ModifyNavigationPath(NavigationPath path, PageInfo[] pages) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Removes a Navigation Path.
		/// </summary>
		/// <param name="path">The navigation path to remove.</param>
		/// <returns><c>true</c> if the path is removed, <c>false</c> otherwise.</returns>
		public bool RemoveNavigationPath(NavigationPath path) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets all the snippets.
		/// </summary>
		/// <returns>All the snippets, sorted by name.</returns>
		public Snippet[] GetSnippets() {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Adds a new snippet.
		/// </summary>
		/// <param name="name">The name of the snippet.</param>
		/// <param name="content">The content of the snippet.</param>
		/// <returns>The correct <see cref="T:Snippet"/> object.</returns>
		public Snippet AddSnippet(string name, string content) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Modifies an existing snippet.
		/// </summary>
		/// <param name="name">The name of the snippet to modify.</param>
		/// <param name="content">The content of the snippet.</param>
		/// <returns>The correct <see cref="T:Snippet"/> object.</returns>
		public Snippet ModifySnippet(string name, string content) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Removes a new Snippet.
		/// </summary>
		/// <param name="name">The Name of the Snippet to remove.</param>
		/// <returns><c>true</c> if the snippet is removed, <c>false</c> otherwise.</returns>
		public bool RemoveSnippet(string name) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets all the content templates.
		/// </summary>
		/// <returns>All the content templates, sorted by name.</returns>
		public ContentTemplate[] GetContentTemplates() {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Adds a new content template.
		/// </summary>
		/// <param name="name">The name of template.</param>
		/// <param name="content">The content of the template.</param>
		/// <returns>The correct <see cref="T:ContentTemplate"/> object.</returns>
		public ContentTemplate AddContentTemplate(string name, string content) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Modifies an existing content template.
		/// </summary>
		/// <param name="name">The name of the template to modify.</param>
		/// <param name="content">The content of the template.</param>
		/// <returns>The correct <see cref="T:ContentTemplate"/> object.</returns>
		public ContentTemplate ModifyContentTemplate(string name, string content) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Removes a content template.
		/// </summary>
		/// <param name="name">The name of the template to remove.</param>
		/// <returns><c>true</c> if the template is removed, <c>false</c> otherwise.</returns>
		public bool RemoveContentTemplate(string name) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets a value specifying whether the provider is read-only, i.e. it can only provide data and not store it.
		/// </summary>
		public bool ReadOnly {
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		/// Initializes the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <remarks>If the configuration string is not valid, the methoud should throw a <see cref="InvalidConfigurationException"/>.</remarks>
		public void Init(IHostV30 host, string config) {
			if(host == null) throw new ArgumentNullException("host");
			if(config == null) throw new ArgumentNullException("config");

			this.host = host;
			this.config = config;

			allPages = new List<PageInfo>(50);
			allContents = new Dictionary<PageInfo, PageContent>(50);
			allBackups = new Dictionary<PageContent, List<PageContent>>(50);
			allDrafts = new Dictionary<PageInfo, PageContent>(5);

			allCategories = new List<CategoryInfo>(10);

			// Prepare search index
			index = new StandardIndex();
			index.SetBuildDocumentDelegate(BuildDocumentHandler);
			index.IndexChanged += index_IndexChanged;
		}

		private void index_IndexChanged(object sender, IndexChangedEventArgs e) {
			lock(this) {
				if(e.Change == IndexChangeType.DocumentAdded) {
					List<WordId> newWords = new List<WordId>(e.ChangeData.Words.Count);
					foreach(DumpedWord w in e.ChangeData.Words) {
						newWords.Add(new WordId(w.Text, freeWordId));
						freeWordId++;
					}

					e.Result = new IndexStorerResult(freeDocumentId, newWords);
					freeDocumentId++;
				}
				else e.Result = null;
			}
		}

		/// <summary>
		/// Handles the construction of an <see cref="T:IDocument" /> for the search engine.
		/// </summary>
		/// <param name="dumpedDocument">The input dumped document.</param>
		/// <returns>The resulting <see cref="T:IDocument" />.</returns>
		private IDocument BuildDocumentHandler(DumpedDocument dumpedDocument) {
			if(dumpedDocument.TypeTag == PageDocument.StandardTypeTag) {
				string pageName = PageDocument.GetPageName(dumpedDocument.Name);

				PageInfo page = GetPage(pageName);

				if(page == null) return null;
				else return new PageDocument(page, dumpedDocument, TokenizeContent);
			}
			else if(dumpedDocument.TypeTag == MessageDocument.StandardTypeTag) {
				string pageFullName;
				int id;
				MessageDocument.GetMessageDetails(dumpedDocument.Name, out pageFullName, out id);

				PageInfo page = GetPage(pageFullName);
				if(page == null) return null;
				else return new MessageDocument(page, id, dumpedDocument, TokenizeContent);
			}
			else return null;
		}

		/// <summary>
		/// Tokenizes page content.
		/// </summary>
		/// <param name="content">The content to tokenize.</param>
		/// <returns>The tokenized words.</returns>
		private static WordInfo[] TokenizeContent(string content) {
			WordInfo[] words = SearchEngine.Tools.Tokenize(content);
			return words;
		}

		/// <summary>
		/// Indexes a page.
		/// </summary>
		/// <param name="content">The content of the page.</param>
		/// <returns>The number of indexed words, including duplicates.</returns>
		private int IndexPage(PageContent content) {
			lock(this) {
				string documentName = PageDocument.GetDocumentName(content.PageInfo);

				DumpedDocument ddoc = new DumpedDocument(0, documentName, host.PrepareTitleForIndexing(content.PageInfo, content.Title),
					PageDocument.StandardTypeTag, content.LastModified);

				// Store the document
				// The content should always be prepared using IHost.PrepareForSearchEngineIndexing()
				return index.StoreDocument(new PageDocument(content.PageInfo, ddoc, TokenizeContent),
					content.Keywords, host.PrepareContentForIndexing(content.PageInfo, content.Content), null);
			}
		}

		/// <summary>
		/// Removes a page from the search engine index.
		/// </summary>
		/// <param name="content">The content of the page to remove.</param>
		private void UnindexPage(PageContent content) {
			lock(this) {
				string documentName = PageDocument.GetDocumentName(content.PageInfo);

				DumpedDocument ddoc = new DumpedDocument(0, documentName, host.PrepareTitleForIndexing(content.PageInfo, content.Title),
					PageDocument.StandardTypeTag, content.LastModified);
				index.RemoveDocument(new PageDocument(content.PageInfo, ddoc, TokenizeContent), null);
			}
		}

		/// <summary>
		/// Indexes a message.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <param name="id">The message ID.</param>
		/// <param name="subject">The subject.</param>
		/// <param name="dateTime">The date/time.</param>
		/// <param name="body">The body.</param>
		/// <returns>The number of indexed words, including duplicates.</returns>
		private int IndexMessage(PageInfo page, int id, string subject, DateTime dateTime, string body) {
			lock(this) {
				// Trim "RE:" to avoid polluting the search engine index
				if(subject.ToLowerInvariant().StartsWith("re:") && subject.Length > 3) subject = subject.Substring(3).Trim();

				string documentName = MessageDocument.GetDocumentName(page, id);

				DumpedDocument ddoc = new DumpedDocument(0, documentName, host.PrepareTitleForIndexing(null, subject),
					MessageDocument.StandardTypeTag, dateTime);

				// Store the document
				// The content should always be prepared using IHost.PrepareForSearchEngineIndexing()
				return index.StoreDocument(new MessageDocument(page, id, ddoc, TokenizeContent), null,
					host.PrepareContentForIndexing(null, body), null);
			}
		}

		/// <summary>
		/// Indexes a message tree.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <param name="root">The tree root.</param>
		private void IndexMessageTree(PageInfo page, Message root) {
			IndexMessage(page, root.ID, root.Subject, root.DateTime, root.Body);
			foreach(Message reply in root.Replies) {
				IndexMessageTree(page, reply);
			}
		}

		/// <summary>
		/// Removes a message from the search engine index.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <param name="id">The message ID.</param>
		/// <param name="subject">The subject.</param>
		/// <param name="dateTime">The date/time.</param>
		/// <param name="body">The body.</param>
		/// <returns>The number of indexed words, including duplicates.</returns>
		private void UnindexMessage(PageInfo page, int id, string subject, DateTime dateTime, string body) {
			lock(this) {
				// Trim "RE:" to avoid polluting the search engine index
				if(subject.ToLowerInvariant().StartsWith("re:") && subject.Length > 3) subject = subject.Substring(3).Trim();

				string documentName = MessageDocument.GetDocumentName(page, id);

				DumpedDocument ddoc = new DumpedDocument(0, documentName, host.PrepareTitleForIndexing(null, subject),
					MessageDocument.StandardTypeTag, DateTime.Now);
				index.RemoveDocument(new MessageDocument(page, id, ddoc, TokenizeContent), null);
			}
		}

		/// <summary>
		/// Removes a message tree from the search engine index.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <param name="root">The tree root.</param>
		private void UnindexMessageTree(PageInfo page, Message root) {
			UnindexMessage(page, root.ID, root.Subject, root.DateTime, root.Body);
			foreach(Message reply in root.Replies) {
				UnindexMessageTree(page, reply);
			}
		}

		/// <summary>
		/// Method invoked on shutdown.
		/// </summary>
		/// <remarks>This method might not be invoked in some cases.</remarks>
		public void Shutdown() {
			// Nothing do to
		}

		/// <summary>
		/// Gets the Information about the Provider.
		/// </summary>
		public ComponentInformation Information {
			get { return info; }
		}

		/// <summary>
		/// Gets a brief summary of the configuration string format, in HTML. Returns <c>null</c> if no configuration is needed.
		/// </summary>
		public string ConfigHelpHtml {
			get { return null; }
		}

	}

}
