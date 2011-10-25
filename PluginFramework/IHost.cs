
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.IO;

namespace ScrewTurn.Wiki.PluginFramework {

	/// <summary>
	/// It is the interface that the ScrewTurn Wiki's Host object implements.
	/// </summary>
	public interface IHostV40 {

		/// <summary>
		/// Gets the current wiki.
		/// </summary>
		/// <returns>The current wiki.</returns>
		string GetCurrentWiki();

		/// <summary>
		/// Gets the global setting value.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns>The global settings' value.</returns>
		string GetGlobalSettingValue(GlobalSettingName name);

		/// <summary>
		/// Gets the values of the Settings.
		/// </summary>
		/// <param name="name">The Setting's Name.</param>
		/// <returns>The Setting's value.</returns>
		string GetSettingValue(SettingName name);

		/// <summary>
		/// Gets the list of the Users.
		/// </summary>
		/// <returns>The users.</returns>
		UserInfo[] GetUsers();

		/// <summary>
		/// Finds a user by username.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <returns>The <see cref="T:UserInfo" />, or <c>null</c> if no users are found.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="username"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="username"/> is empty.</exception>
		UserInfo FindUser(string username);

		/// <summary>
		/// Gets the authenticated user in the current session, if any.
		/// </summary>
		/// <returns>The authenticated user, or <c>null</c> if no user is authenticated.</returns>
		/// <remarks>If the built-it <i>admin</i> user is authenticated, the returned user 
		/// has <i>admin</i> as Username.</remarks>
		UserInfo GetCurrentUser();

		/// <summary>
		/// Gets the list of the user groups.
		/// </summary>
		/// <returns>The groups.</returns>
		UserGroup[] GetUserGroups();

		/// <summary>
		/// Finds a user group by name.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns>The <see cref="T:UserGroup "/>, or <c>null</c> if no groups are found.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty.</exception>
		UserGroup FindUserGroup(string name);

		/// <summary>
		/// Checks whether an action is allowed for a global resource.
		/// </summary>
		/// <param name="action">The action (see <see cref="Actions.ForGlobals"/> class)</param>
		/// <param name="user">The user.</param>
		/// <returns><c>true</c> if the action is allowed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>action</b> or <b>user</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>action</b> is empty.</exception>
		bool CheckActionForGlobals(string action, UserInfo user);

		/// <summary>
		/// Checks whether an action is allowed for a namespace.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <param name="action">The action (see <see cref="Actions.ForNamespaces"/> class)</param>
		/// <param name="user">The user.</param>
		/// <returns><c>true</c> if the action is allowed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>action</b> or <b>user</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>action</b> is empty.</exception>
		bool CheckActionForNamespace(NamespaceInfo nspace, string action, UserInfo user);

		/// <summary>
		/// Checks whether an action is allowed for a page in the given page.
		/// </summary>
		/// <param name="pageFullName">The full name of the page.</param>
		/// <param name="action">The action (see <see cref="Actions.ForPages"/> class)</param>
		/// <param name="user">The user.</param>
		/// <returns><c>true</c> if the action is allowed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>page</b>, <b>action</b> or <b>user</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>action</b> is empty.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageFullName"/> is empty.</exception>
		bool CheckActionForPage(string pageFullName, string action, UserInfo user);

		/// <summary>
		/// Checks whether an action is allowed for a directory.
		/// </summary>
		/// <param name="directory">The directory.</param>
		/// <param name="action">The action (see <see cref="Actions.ForDirectories"/>).</param>
		/// <param name="user">The user.</param>
		/// <returns><c>true</c> if the action is allowed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>directory</b>, <b>action</b> or <b>user</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>action</b> is empty.</exception>
		bool CheckActionForDirectory(StDirectoryInfo directory, string action, UserInfo user);

		/// <summary>
		/// Gets the theme in use for a namespace.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The theme.</returns>
		string GetTheme(NamespaceInfo nspace);

		/// <summary>
		/// Gets the list of the namespaces.
		/// </summary>
		/// <returns>The namespaces.</returns>
		NamespaceInfo[] GetNamespaces();

		/// <summary>
		/// Finds a namespace by name.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns>The <see cref="T:NamespaceInfo" />, or <c>null</c> if no namespaces are found.</returns>
		NamespaceInfo FindNamespace(string name);

		/// <summary>
		/// Gets the list of the Pages in a namespace.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The pages.</returns>
		PageContent[] GetPages(NamespaceInfo nspace);

		/// <summary>
		/// Gets the list of the Categories in a namespace.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The categories.</returns>
		CategoryInfo[] GetCategories(NamespaceInfo nspace);

		/// <summary>
		/// Creates a new category.
		/// </summary>
		/// <param name="nspace">The destination namespace (<c>null</c> for the root).</param>
		/// <param name="name">The name of the category (without namespace).</param>
		/// <returns><c>true</c> if the category was created, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty or if it is invalid.</exception>
		bool CreateCategory(NamespaceInfo nspace, string name);

		/// <summary>
		/// Renames a category.
		/// </summary>
		/// <param name="category">The category to rename.</param>
		/// <param name="newName">The new name of the category (without namespace).</param>
		/// <returns><c>true</c> if the category was renamed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="category"/> or <paramref name="newName"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="newName"/> is empty or if it is invalid.</exception>
		bool RenameCategory(CategoryInfo category, string newName);

		/// <summary>
		/// Deletes a category.
		/// </summary>
		/// <param name="category">The category to delete.</param>
		/// <returns><c>true</c> if the category was deleted, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="category"/> is <c>null</c>.</exception>
		bool RemoveCategory(CategoryInfo category);

		/// <summary>
		/// Gets the list of Snippets.
		/// </summary>
		/// <returns>The snippets.</returns>
		Snippet[] GetSnippets();

		/// <summary>
		/// Gets the list of Navigation Paths in a namespace.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The navigation paths.</returns>
		NavigationPath[] GetNavigationPaths(NamespaceInfo nspace);

		/// <summary>
		/// Gets the Categories of a Page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>The Categories.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="page"/> is empty.</exception>
		CategoryInfo[] GetCategoriesPerPage(PageContent page);

		/// <summary>
		/// Gets the Wiki Page with the specified full Name.
		/// </summary>
		/// <param name="fullName">The full Name of the Page.</param>
		/// <returns>The Wiki Page, or <c>null</c> if no pages are found.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> is empty.</exception>
		PageContent FindPage(string fullName);

		/// <summary>
		/// Gets the Backup/Revision numbers of a Page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>The Backup/Revision numbers.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		int[] GetBackups(PageContent page);

		/// <summary>
		/// Gets the Content of a Page Backup.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <param name="revision">The revision.</param>
		/// <returns>The Backup Content.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="revision"/> is less than zero.</exception>
		PageContent GetBackupContent(PageContent page, int revision);

		/// <summary>
		/// Gets the formatted content of a Page.
		/// </summary>
		/// <param name="pageFullName">The full name of the page.</param>
		/// <returns>The formatted content of the Page.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageFullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageFullName"/> is empty.</exception>
		string GetFormattedContent(string pageFullName);

		/// <summary>
		/// Creates a new page.
		/// </summary>
		/// <param name="nspace">The target namespace (<c>null</c> for the root).</param>
		/// <param name="name">The page name (without namespace)</param>
		/// <param name="title">The page title.</param>
		/// <param name="username">The username of the user who created the page.</param>
		/// <param name="dateTime">The creation date/time.</param>
		/// <param name="comment">The comment (or <c>null</c>).</param>
		/// <param name="content">The page content.</param>
		/// <param name="keywords">The page keywords.</param>
		/// <param name="description">The page description.</param>
		/// <returns><c>true</c> if the page was created, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/>, <paramref name="title"/>, <paramref name="username"/> or <paramref name="content"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/>, <paramref name="title"/> or <paramref name="username"/> are empty or if <paramref name="name"/> is invalid.</exception>
		bool CreatePage(NamespaceInfo nspace, string name, string title, string username, DateTime dateTime, string comment, string content, string[] keywords, string description);

		/// <summary>
		/// Updates a page.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <param name="title">The page title.</param>
		/// <param name="username">The username of the user who created the page.</param>
		/// <param name="dateTime">The creation date/time.</param>
		/// <param name="comment">The comment (or <c>null</c>).</param>
		/// <param name="content">The page content.</param>
		/// <param name="keywords">The page keywords.</param>
		/// <param name="description">The page description.</param>
		/// <param name="saveMode">The saving mode.</param>
		/// <returns><c>true</c> if the page was updated, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/>, <paramref name="title"/>, <paramref name="username"/> or <paramref name="content"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="title"/> or <paramref name="username"/> are <c>null</c>.</exception>
		bool UpdatePage(PageContent page, string title, string username, DateTime dateTime, string comment, string content, string[] keywords, string description, SaveMode saveMode);

		/// <summary>
		/// Rebinds a page to zero or more categories, completely removing existing bindings.
		/// </summary>
		/// <param name="page">The page to rebind.</param>
		/// <param name="categories">The categories to rebind to (must be in the same namespace).</param>
		/// <returns><c>true</c> if the page was rebound, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> or <paramref name="categories"/> are <c>null</c>.</exception>
		bool RebindPage(PageContent page, string[] categories);

		/// <summary>
		/// Renames a page.
		/// </summary>
		/// <param name="page">The page to rename.</param>
		/// <param name="newName">The new name (without the namespace)</param>
		/// <returns><c>true</c> if the page was renamed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> or <paramref name="newName"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="newName"/> is empty or if it is invalid.</exception>
		bool RenamePage(PageContent page, string newName);

		/// <summary>
		/// Removes a page and all its related data.
		/// </summary>
		/// <param name="page">The page to remove.</param>
		/// <returns><c>true</c> if the page was removed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		bool RemovePage(PageContent page);

		/// <summary>
		/// Formats a block of WikiMarkup, using the built-in formatter only.
		/// </summary>
		/// <param name="raw">The block of WikiMarkup.</param>
		/// <returns>The formatted content.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="raw"/> is <c>null</c>.</exception>
		string Format(string raw);

		/// <summary>
		/// Lists directories in a directory.
		/// </summary>
		/// <param name="directory">The directory (<c>null</c> for the root, first invocation).</param>
		/// <returns>The directories.</returns>
		StDirectoryInfo[] ListDirectories(StDirectoryInfo directory);

		/// <summary>
		/// Lists files in a directory.
		/// </summary>
		/// <param name="directory">The directory (<c>null</c> for the root, first invocation).</param>
		/// <returns>The files.</returns>
		StFileInfo[] ListFiles(StDirectoryInfo directory);

		/// <summary>
		/// Stores a file.
		/// </summary>
		/// <param name="fullName">The file full path and name, for example '/directory/sub/file.jpg'.</param>
		/// <param name="source">The source stream.</param>
		/// <param name="overwrite">A value indicating whether to overwrite an existing file.</param>
		/// <returns>The created file, or <c>null</c> if no file was created.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> or <paramref name="source"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> is empty.</exception>
		bool StoreFile(string fullName, Stream source, bool overwrite);
		
		/// <summary>
		/// Retrieves a file.
		/// </summary>
		/// <param name="file">The file to retrieve.</param>
		/// <param name="destination">The destination stream.</param>
		/// <returns><c>true</c> if the file was retrieved, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="file"/> or <paramref name="destination"/> are <c>null</c>.</exception>
		bool RetrieveFile(StFileInfo file, Stream destination);

		/// <summary>
		/// Renames a file.
		/// </summary>
		/// <param name="file">The file to rename.</param>
		/// <param name="newName">The new name (in the same folder).</param>
		/// <returns><c>true</c> if the file was renamed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="file"/> or <paramref name="newName"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="newName"/> is empty.</exception>
		bool RenameFile(StFileInfo file, string newName);

		/// <summary>
		/// Deletes a file.
		/// </summary>
		/// <param name="file">The file to delete.</param>
		/// <returns><c>true</c> if the file was deleted, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="file"/> was <c>null</c>.</exception>
		bool DeleteFile(StFileInfo file);

		/// <summary>
		/// Creates a new directory.
		/// </summary>
		/// <param name="path">The path of the container directory (the new directory will be created here).</param>
		/// <param name="name">The name of the new directory.</param>
		/// <returns><c>true</c> if the directory was created, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="path"/> or <paramref name="name"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty.</exception>
		bool CreateDirectory(string path, string name);

		/// <summary>
		/// Renames a directory.
		/// </summary>
		/// <param name="directory">The directory to rename.</param>
		/// <param name="newName">The new directory name (without path).</param>
		/// <returns><c>true</c> if the directory was renamed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="directory"/> or <paramref name="newName"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="newName"/> is empty.</exception>
		bool RenameDirectory(StDirectoryInfo directory, string newName);

		/// <summary>
		/// Deletes a directroy.
		/// </summary>
		/// <param name="directory">The directory to delete.</param>
		/// <returns><c>true</c> if the directory was deleted, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="directory"/> is <c>null</c>.</exception>
		bool DeleteDirectory(StDirectoryInfo directory);

		/// <summary>
		/// Lists page attachments.
		/// </summary>
		/// <param name="pageFullName">The page.</param>
		/// <returns>The attachments.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageFullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageFullName"/> is empty.</exception>
		StFileInfo[] ListPageAttachments(string pageFullName);

		/// <summary>
		/// Stores a page attachment.
		/// </summary>
		/// <param name="pageFullName">The page full name.</param>
		/// <param name="name">The name of the attachment.</param>
		/// <param name="source">The source stream.</param>
		/// <param name="overwrite"><c>true</c> to overwrite an existing attachment with the same name, <c>false</c> otherwise.</param>
		/// <returns><c>true</c> if the attachment was stored, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageFullName"/>, <paramref name="name"/> or <paramref name="source"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageFullName"/> or <paramref name="name"/> are empty.</exception>
		bool StorePageAttachment(string pageFullName, string name, Stream source, bool overwrite);

		/// <summary>
		/// Retrieves a page attachment.
		/// </summary>
		/// <param name="pageFullName">The page full name.</param>
		/// <param name="file">The attachment to retrieve.</param>
		/// <param name="destination">The destination stream.</param>
		/// <returns><c>true</c> if the file was retrieved, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageFullName"/>, <paramref name="file"/> or <paramref name="destination"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageFullName"/> is empty.</exception>
		bool RetrievePageAttachment(string pageFullName, StFileInfo file, Stream destination);

		/// <summary>
		/// Renames a page attachment.
		/// </summary>
		/// <param name="pageFullName">The full page name.</param>
		/// <param name="file">The attachment to rename.</param>
		/// <param name="newName">The new name of the attachment.</param>
		/// <returns><c>true</c> if the attachment was renamed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageFullName"/>, <paramref name="file"/> or <paramref name="newName"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageFullName"/> or <paramref name="newName"/> are empty.</exception>
		bool RenamePageAttachment(string pageFullName, StFileInfo file, string newName);

		/// <summary>
		/// Deletes a page attachment.
		/// </summary>
		/// <param name="pageFullName">The page full name.</param>
		/// <param name="file">The attachment to delete.</param>
		/// <returns><c>true</c> if the attachment was deleted, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageFullName"/> or <paramref name="file"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageFullName"/> is empty.</exception>
		bool DeletePageAttachment(string pageFullName, StFileInfo file);

		/// <summary>
		/// Sends an Email.
		/// </summary>
		/// <param name="recipient">The Recipient Email address.</param>
		/// <param name="sender">The Sender's Email address.</param>
		/// <param name="subject">The Subject.</param>
		/// <param name="body">The Body.</param>
		/// <param name="html">True if the message is HTML.</param>
		/// <returns>True if the message has been sent successfully.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="recipient"/>, <paramref name="sender"/>, <paramref name="subject"/> or <paramref name="body"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="recipient"/>, <paramref name="sender"/>, <paramref name="subject"/> or <paramref name="body"/> are empty.</exception>
		bool SendEmail(string recipient, string sender, string subject, string body, bool html);

		/// <summary>
		/// Logs a new message.
		/// </summary>
		/// <param name="message">The Message.</param>
		/// <param name="entryType">The Entry Type.</param>
		/// <param name="user">The user, or <c>null</c>. If <c>null</c>, the system will log "PluginName+System".</param>
		/// <param name="caller">The Component that calls the method. The caller cannot be null.</param>
		/// <param name="wiki">The wiki or <c>null</c> for application-wide events.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="message"/> or <paramref name="caller"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="message"/> is empty.</exception>
		void LogEntry(string message, LogEntryType entryType, string user, object caller, string wiki = null);

		/// <summary>
		/// Changes the language of the current user.
		/// </summary>
		/// <param name="language">The language code.</param>
		void ChangeCurrentUserLanguage(string language);

		/// <summary>
		/// Aligns a Date and Time object to the User's Time Zone preferences.
		/// </summary>
		/// <param name="dt">The Date/Time to align.</param>
		/// <returns>The aligned Date/Time.</returns>
		/// <remarks>The method takes care of daylight saving settings.</remarks>
		DateTime AlignDateTimeWithPreferences(DateTime dt);

		/// <summary>
		/// Adds an item in the Editing Toolbar.
		/// </summary>
		/// <param name="caller">The caller.</param>
		/// <param name="item">The item to add.</param>
		/// <param name="text">The text of the item showed in the toolbar.</param>
		/// <param name="value">The value of the item, placed in the content: if <b>item</b> is <b>ToolbarItem.SpecialTagWrap</b>, separate start and end tag with a pipe.</param>
		/// <returns>A token to use to remove the toolbar item with <see cref="RemoveToolbarItem"/>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="text"/> or <paramref name="value"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="text"/> or <paramref name="value"/> are empty, or if they contain single or double quotes, 
		/// or if <paramref name="value"/> does not contain a pipe when <paramref name="item"/> is <b>SpecialTagWrap</b>.</exception>
		string AddToolbarItem(IProviderV40 caller, ToolbarItem item, string text, string value);

		/// <summary>
		/// Removes a toolbar item.
		/// </summary>
		/// <param name="token">The token returned by <see cref="AddToolbarItem"/>.</param>
		void RemoveToolbarItem(string token);

		/// <summary>
		/// Gets the default provider of the specified type.
		/// </summary>
		/// <param name="providerType">The type of the provider (
		/// <see cref="T:IPagesStorageProviderV40" />, 
		/// <see cref="T:IUsersStorageProviderV40" />, 
		/// <see cref="T:IFilesStorageProviderV40" />,
		/// <see cref="T:IFormatterProviderV40" />).</param>
		/// <returns>The Full type name of the default provider of the specified type or <c>null</c>.</returns>
		string GetDefaultProvider(Type providerType);

		/// <summary>
		/// Gets the pages storage providers.
		/// </summary>
		/// <returns>The providers.</returns>
		IPagesStorageProviderV40[] GetPagesStorageProviders();

		/// <summary>
		/// Gets the users storage providers.
		/// </summary>
		/// <returns>The providers.</returns>
		IUsersStorageProviderV40[] GetUsersStorageProviders();

		/// <summary>
		/// Gets the files storage providers.
		/// </summary>
		/// <returns>The providers.</returns>
		IFilesStorageProviderV40[] GetFilesStorageProviders();

		/// <summary>
		/// Gets the theme providers.
		/// </summary>
		/// <returns>The providers.</returns>
		IThemesStorageProviderV40[] GetThemesProviders();

		/// <summary>
		/// Gets the formatter providers, either enabled or disabled.
		/// </summary>
		/// <param name="enabled"><c>true</c> to get enabled providers, <c>false</c> to get disabled providers.</param>
		/// <returns>The providers.</returns>
		IFormatterProviderV40[] GetFormatterProviders(bool enabled);

		/// <summary>
		/// Gets the current settings storage provider.
		/// </summary>
		/// <returns>The global settings storage provider.</returns>
		ISettingsStorageProviderV40 GetSettingsStorageProvider();

		/// <summary>
		/// Gets the current global settings storage provider.
		/// </summary>
		/// <returns>The global settings storage provider.</returns>
		IGlobalSettingsStorageProviderV40 GetGlobalSettingsStorageProvider();

		/// <summary>
		/// Gets the configuration of a storage provider.
		/// </summary>
		/// <param name="providerTypeName">The type name of the provider, such as 'Vendor.Namespace.Provider'.</param>
		/// <returns>The configuration (can be empty or <c>null</c>).</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="providerTypeName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="providerTypeName"/> is empty.</exception>
		string GetProviderConfiguration(string providerTypeName);

		/// <summary>
		/// Gets the configuration of a plugin(formatter provider).
		/// </summary>
		/// <param name="providerTypeName">The type name of the provider, such as 'Vendor.Namespace.Provider'.</param>
		/// <returns>The configuration (can be empty or <c>null</c>).</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="providerTypeName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="providerTypeName"/> is empty.</exception>
		string GetPluginConfiguration(string providerTypeName);

		/// <summary>
		/// Sets the configuration of a provider.
		/// </summary>
		/// <param name="provider">The provider of which to set the configuration.</param>
		/// <param name="configuration">The configuration to set.</param>
		/// <returns><c>true</c> if the configuration is set, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="provider"/> is <c>null</c>.</exception>
		bool SetPluginConfiguration(IProviderV40 provider, string configuration);

		/// <summary>
		/// Overrides the public directory.
		/// </summary>
		/// <param name="fullPath">The new full path of the public directory.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="fullPath"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullPath"/> is empty.</exception>
		/// <exception cref="InvalidOperationException">If it's too late to override the public directory.</exception>
		void OverridePublicDirectory(string fullPath);

		/// <summary>
		/// Event fired whenever an activity is performed on a User Account.
		/// </summary>
		event EventHandler<UserAccountActivityEventArgs> UserAccountActivity;

		/// <summary>
		/// Event fired whenever an activity is performed on a user group.
		/// </summary>
		event EventHandler<UserGroupActivityEventArgs> UserGroupActivity;

		/// <summary>
		/// Event fired whenever an activity is performed on a namespace.
		/// </summary>
		event EventHandler<NamespaceActivityEventArgs> NamespaceActivity;

		/// <summary>
		/// Even fired whenever an activity is performed on a Page.
		/// </summary>
		event EventHandler<PageActivityEventArgs> PageActivity;

		/// <summary>
		/// Event fired whenever an activity is performed on a file, directory or attachment.
		/// </summary>
		event EventHandler<FileActivityEventArgs> FileActivity;

		/// <summary>
		/// Registers a HTTP request handler (typically during the SetUp phase of a provider).
		/// </summary>
		/// <param name="caller">The caller.</param>
		/// <param name="urlRegex">The regular expression used to filter URLs to decide whether or not a request must be handled.</param>
		/// <param name="methods">The HTTP request methods to consider, such as GET and POST.</param>
		/// <returns>A token to use to unregister the handler (<see cref="UnregisterRequestHandler"/>).</returns>
		/// <remarks>The <paramref name="urlRegex"/> will be treated as culture-invariant and case-insensitive.</remarks>
		string RegisterRequestHandler(IProviderV40 caller, string urlRegex, string[] methods);

		/// <summary>
		/// Unregisters a request handler.
		/// </summary>
		/// <param name="token">The token returned by <see cref="RegisterRequestHandler"/>.</param>
		void UnregisterRequestHandler(string token);

		/// <summary>
		/// Allows to inject content into the HTML head, like scripts and css (typically during the SetUp phase of a provider).
		/// </summary>
		/// <param name="caller">The caller.</param>
		/// <param name="content">The content to inject.</param>
		/// <returns>A token to use to remove the injected content.</returns>
		string AddHtmlHeadContent(IProviderV40 caller, string content);

		/// <summary>
		/// Removes injected HTML head content.
		/// </summary>
		/// <param name="token">The token returned by <see cref="AddHtmlHeadContent"/>.</param>
		void RemoveHtmlHeadContent(string token);

	}

	/// <summary>
	/// Defines a delegate for handling HTTP requests.
	/// </summary>
	/// <param name="context">The HTTP context.</param>
	/// <param name="match">The regular expression match.</param>
	/// <returns><c>true</c> if the request was handled, <c>false</c> otherwise.</returns>
	public delegate bool RequestHandler(HttpContext context, Match match);

	/// <summary>
	/// Enumerates the Types of Log Entries.
	/// </summary>
	public enum LogEntryType {
		/// <summary>
		/// Represents a simple Message.
		/// </summary>
		General,
		/// <summary>
		/// Represents a Warning.
		/// </summary>
		Warning,
		/// <summary>
		/// Represents an Error.
		/// </summary>
		Error
	}

	/// <summary>
	/// Enumerates the Global Setting values' name.
	/// </summary>
	public enum GlobalSettingName {
		/// <summary>
		/// The Public directory.
		/// </summary>
		PublicDirectory,
		/// <summary>
		/// The Contact Email.
		/// </summary>
		ContactEmail,
		/// <summary>
		/// The Sender Email.
		/// </summary>
		SenderEmail,
		/// <summary>
		/// The regex used to validate usernames.
		/// </summary>
		UsernameRegex,
		/// <summary>
		/// The regex used to validate passwords.
		/// </summary>
		PasswordRegex,
		/// <summary>
		/// The regex used to validate email addresses.
		/// </summary>
		EmailRegex,
		/// <summary>
		/// The regex used to validate the main Wiki URL.
		/// </summary>
		MainUrlRegex,
		/// <summary>
		/// A value (true/false) indicating whether to disable the automatic version check.
		/// </summary>
		DisableAutomaticVersionCheck,
		/// <summary>
		/// A value (true/false) indicating whether to enable HTTP compression.
		/// </summary>
		EnableHttpCompression,
		/// <summary>
		/// A value (true/false) indicating whether to enable View State compression.
		/// </summary>
		EnableViewStateCompression,
		/// <summary>
		/// The max size for uploaded files (bytes).
		/// </summary>
		MaxFileSize,
		/// <summary>
		/// The extension used for pages.
		/// </summary>
		PageExtension,
		/// <summary>
		/// The version of the Wiki Engine.
		/// </summary>
		WikiVersion,
		/// <summary>
		/// The default pages provider.
		/// </summary>
		DefaultPagesStorageProvider,
		/// <summary>
		/// The default users provider.
		/// </summary>
		DefaultUsersStorageProvider,
		/// <summary>
		/// The default files provider.
		/// </summary>
		DefaultFilesStorageProvider,
		/// <summary>
		/// The logging level.
		/// </summary>
		LoggingLevel,
		/// <summary>
		/// The max size, in KB, of the log.
		/// </summary>
		MaxLogSize
	}

	/// <summary>
	/// Enumerates the Setting values' names.
	/// </summary>
	public enum SettingName {
		/// <summary>
		/// The Title of the Wiki.
		/// </summary>
		WikiTitle,
		/// <summary>
		/// The Main URL of the Wiki.
		/// </summary>
		MainUrl,
		/// <summary>
		/// The default page of the root namespace.
		/// </summary>
		RootNamespaceDefaultPage,
		/// <summary>
		/// The Date/Time format.
		/// </summary>
		DateTimeFormat,
		/// <summary>
		/// The default Language (for example <b>en-US</b>).
		/// </summary>
		DefaultLanguage,
		/// <summary>
		/// The default Time Zone (a string representing the shift in <b>minutes</b> respect to the Greenwich time, for example <b>-120</b>).
		/// </summary>
		DefaultTimeZone,
		/// <summary>
		/// The Themes directory.
		/// </summary>
		ThemesDirectory,
		/// <summary>
		/// A value (true/false) indicating whether users can create new accounts.
		/// </summary>
		UsersCanRegister,
		/// <summary>
		/// A value (true/false) indicating whether to activate page editing with a double click.
		/// </summary>
		EnableDoubleClickEditing,
		/// <summary>
		/// A value (true/false) indicating whether to process single line breaks in WikiMarkup.
		/// </summary>
		ProcessSingleLineBreaks,
		/// <summary>
		/// The account activation mode.
		/// </summary>
		AccountActivationMode,
		/// <summary>
		/// The file types allowed for upload.
		/// </summary>
		AllowedFileTypes,
		/// <summary>
		/// A value (true/false) indicating whether to disable the breadcrumbs trail.
		/// </summary>
		DisableBreadcrumbsTrail,
		/// <summary>
		/// A value (true/false) indicating whether to disable the captcha control in public functionalities.
		/// </summary>
		DisableCaptchaControl,
		/// <summary>
		/// A value (true/false) indicating whether to disable concurrent page editing.
		/// </summary>
		DisableConcurrentEditing,
		/// <summary>
		/// A value (true/false) indicating whether to SCRIPT tags are allowed in WikiMarkup.
		/// </summary>
		ScriptTagsAllowed,
		/// <summary>
		/// The max number of recent changes to log.
		/// </summary>
		MaxRecentChanges,
		/// <summary>
		/// The timeout, in seconds, after which a page editing session is considered to be dead.
		/// </summary>
		EditingSessionTimeout,
		/// <summary>
		/// The default administrators group.
		/// </summary>
		AdministratorsGroup,
		/// <summary>
		/// The default users group.
		/// </summary>
		UsersGroup,
		/// <summary>
		/// The default anonymous users group.
		/// </summary>
		AnonymousGroup,
		/// <summary>
		/// The page change moderation mode.
		/// </summary>
		ChangeModerationMode,
		/// <summary>
		/// The name of the theme of the root namespace.
		/// </summary>
		RootNamespaceTheme,
		/// <summary>
		/// The number of items to display in lists of items before enabling paging.
		/// </summary>
		ListSize
	}
	
	/// <summary>
	/// Enumerates the toolbar items that can be added.
	/// </summary>
	public enum ToolbarItem {
		/// <summary>
		/// A Special Tag that is inserted in the text.
		/// </summary>
		SpecialTag,
		/// <summary>
		/// A special tag that wraps the selected text.
		/// </summary>
		SpecialTagWrap
	}

}
