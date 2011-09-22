
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using System.Web.Script.Serialization;
using System.Collections;
using ScrewTurn.Wiki.AclEngine;
using Ionic.Zip;
using System.IO;
using System.Net;

namespace ScrewTurn.Wiki.BackupRestore {

	/// <summary>
	/// Implements a Backup and Restore procedure for settings storage providers.
	/// </summary>
	public static class BackupRestore {

		private const string BACKUP_RESTORE_UTILITY_VERSION = "1.0";

		private static VersionFile generateVersionFile(string backupName) {
			return new VersionFile() {
				BackupRestoreVersion = BACKUP_RESTORE_UTILITY_VERSION,
				WikiVersion = typeof(BackupRestore).Assembly.GetName().Version.ToString(),
				BackupName = backupName
			};
		}

		private static VersionFile DeserializeVersionFile(string json) {
			JavaScriptSerializer serializer = new JavaScriptSerializer();
			serializer.MaxJsonLength = serializer.MaxJsonLength * 10;

			return serializer.Deserialize<VersionFile>(json);
		}

		private static byte[] ExtractEntry(ZipEntry zipEntry) {
			using(MemoryStream stream = new MemoryStream()) {
				zipEntry.Extract(stream);
				stream.Seek(0, SeekOrigin.Begin);
				byte[] buffer = new byte[stream.Length];
				stream.Read(buffer, 0, (int)stream.Length);
				return buffer;
			}
		}

		private static void ExtractEntry(ZipEntry zipEntry, string destinationPath) {
			zipEntry.Extract(destinationPath);
		}

		/// <summary>
		/// Backups all the providers (excluded global settings storage provider).
		/// </summary>
		/// <param name="backupZipFileName">The name of the zip file where to store the backup file.</param>
		/// <param name="wiki">The wiki.</param>
		/// <param name="plugins">The available plugins.</param>
		/// <param name="settingsStorageProvider">The settings storage provider.</param>
		/// <param name="pagesStorageProviders">The pages storage providers.</param>
		/// <param name="usersStorageProviders">The users storage providers.</param>
		/// <param name="filesStorageProviders">The files storage providers.</param>
		/// <returns><c>true</c> if the backup has been succesfull.</returns>
		public static bool BackupAll(string backupZipFileName, string wiki, string[] plugins, ISettingsStorageProviderV40 settingsStorageProvider, IPagesStorageProviderV40[] pagesStorageProviders, IUsersStorageProviderV40[] usersStorageProviders, IFilesStorageProviderV40[] filesStorageProviders) {
			string tempPath = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());
			Directory.CreateDirectory(tempPath);

			using(ZipFile backupZipFile = new ZipFile(backupZipFileName)) {

				// Find all namespaces
				List<string> namespaces = new List<string>();
				foreach(IPagesStorageProviderV40 pagesStorageProvider in pagesStorageProviders) {
					foreach(NamespaceInfo ns in pagesStorageProvider.GetNamespaces()) {
						namespaces.Add(ns.Name);
					}
				}

				// Backup settings storage provider
				string zipSettingsBackup = Path.Combine(tempPath, "SettingsBackup-" + settingsStorageProvider.GetType().FullName + "-" + wiki + ".zip");
				BackupSettingsStorageProvider(zipSettingsBackup, settingsStorageProvider, namespaces.ToArray(), plugins);
				backupZipFile.AddFile(zipSettingsBackup, "");
				
				// Backup pages storage providers
				foreach(IPagesStorageProviderV40 pagesStorageProvider in pagesStorageProviders) {
					string zipPagesBackup = Path.Combine(tempPath, "PagesBackup-" + pagesStorageProvider.GetType().FullName + "-" + wiki + ".zip");
					BackupPagesStorageProvider(zipPagesBackup, pagesStorageProvider);
					backupZipFile.AddFile(zipPagesBackup, "");
				}

				// Backup users storage providers
				foreach(IUsersStorageProviderV40 usersStorageProvider in usersStorageProviders) {
					string zipUsersProvidersBackup = Path.Combine(tempPath, "UsersBackup-" + usersStorageProvider.GetType().FullName + "-" + wiki + ".zip");
					BackupUsersStorageProvider(zipUsersProvidersBackup, usersStorageProvider);
					backupZipFile.AddFile(zipUsersProvidersBackup, "");
				}

				// Backup files storage providers
				foreach(IFilesStorageProviderV40 filesStorageProvider in filesStorageProviders) {
					string zipFilesProviderBackup = Path.Combine(tempPath, "FilesBackup-" + filesStorageProvider.GetType().FullName + "-" + wiki + ".zip");
					BackupFilesStorageProvider(zipFilesProviderBackup, filesStorageProvider);
					backupZipFile.AddFile(zipFilesProviderBackup, "");
				}

				backupZipFile.Save();
			}
			Directory.Delete(tempPath, true);
			return true;
		}

		/// <summary>
		/// Restores all.
		/// </summary>
		/// <param name="backupFileAddress">The backup file address.</param>
		/// <param name="globalSettingsStorageProvider">The global settings storage provider.</param>
		/// <param name="settingsStorageProvider">The settings storage provider.</param>
		/// <param name="pagesStorageProvider">The pages storage provider.</param>
		/// <param name="usersStorageProvider">The users storage provider.</param>
		/// <param name="filesStorageProvider">The files storage provider.</param>
		/// <param name="isSettingGlobal">A function to check if a setting is global or not.</param>
		/// <returns><c>true</c> if the restore has been succesfull.</returns>
		public static bool RestoreAll(string backupFileAddress, IGlobalSettingsStorageProviderV40 globalSettingsStorageProvider, ISettingsStorageProviderV40 settingsStorageProvider, IPagesStorageProviderV40 pagesStorageProvider, IUsersStorageProviderV40 usersStorageProvider, IFilesStorageProviderV40 filesStorageProvider, Func<string, bool> isSettingGlobal) {
			string tempPath = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());
			Directory.CreateDirectory(tempPath);
			
			WebClient webClient = new WebClient();
			webClient.DownloadFile(backupFileAddress, Path.Combine(tempPath, "Backup.zip"));

			using(ZipFile backupZipFile = ZipFile.Read(Path.Combine(tempPath, "Backup.zip"))) {
				// Restore settings
				ZipEntry settingsEntry = (from e in backupZipFile
										  where e.FileName.StartsWith("SettingsBackup-")
										  select e).FirstOrDefault();
				string extractedGlobalSettingsZipFilePath = Path.Combine(tempPath, settingsEntry.FileName);
				ExtractEntry(settingsEntry, tempPath);
				RestoreSettingsStorageProvider(extractedGlobalSettingsZipFilePath, globalSettingsStorageProvider, settingsStorageProvider, isSettingGlobal);
				File.Delete(extractedGlobalSettingsZipFilePath);

				// Restore pages
				ZipEntry[] pagesEntries = (from e in backupZipFile
										   where e.FileName.StartsWith("PagesBackup-")
										   select e).ToArray();
				foreach(ZipEntry pagesEntry in pagesEntries) {
					string extractedZipFilePath = Path.Combine(tempPath, pagesEntry.FileName);
					ExtractEntry(pagesEntry, tempPath);
					RestorePagesStorageProvider(extractedZipFilePath, pagesStorageProvider);
					File.Delete(extractedZipFilePath);
				}

				// Restore users
				ZipEntry[] usersEntries = (from e in backupZipFile
										   where e.FileName.StartsWith("UsersBackup-")
										   select e).ToArray();
				foreach(ZipEntry usersEntry in usersEntries) {
					string extractedZipFilePath = Path.Combine(tempPath, usersEntry.FileName);
					ExtractEntry(usersEntry, tempPath);
					RestoreUsersStorageProvider(extractedZipFilePath, usersStorageProvider);
					File.Delete(extractedZipFilePath);
				}

				// Restore files
				ZipEntry[] filesEntries = (from e in backupZipFile
										   where e.FileName.StartsWith("FilesBackup-")
										   select e).ToArray();
				foreach(ZipEntry filesEntry in filesEntries) {
					string extractedZipFilePath = Path.Combine(tempPath, filesEntry.FileName);
					ExtractEntry(filesEntry, tempPath);
					RestoreFilesStorageProvider(extractedZipFilePath, filesStorageProvider);
					File.Delete(extractedZipFilePath);
				}
			}
			return true;
		}

		/// <summary>
		/// Backups the specified global settings storage provider.
		/// </summary>
		/// <param name="globalSettingsStorageProvider">The global settings storage provider.</param>
		/// <returns>The zip backup file.</returns>
		public static byte[] BackupGlobalSettingsStorageProvider(IGlobalSettingsStorageProviderV40 globalSettingsStorageProvider) {
			GlobalSettingsBackup globalSettingsBackup = new GlobalSettingsBackup();

			// Global Settings
			globalSettingsBackup.Settings = (Dictionary<string, string>)globalSettingsStorageProvider.GetAllSettings();

			ZipFile globalSettingsBackupZipFile = new ZipFile();
			
			JavaScriptSerializer serializer = new JavaScriptSerializer();
			serializer.MaxJsonLength = serializer.MaxJsonLength * 10;

			globalSettingsBackupZipFile.AddEntry("GlobalSettings.json", Encoding.Unicode.GetBytes(serializer.Serialize(globalSettingsBackup)));

			globalSettingsBackupZipFile.AddEntry("Version.json", Encoding.Unicode.GetBytes(serializer.Serialize(generateVersionFile("GlobalSettings"))));

			byte[] buffer;
			using(MemoryStream stream = new MemoryStream()) {
				globalSettingsBackupZipFile.Save(stream);
				stream.Seek(0, SeekOrigin.Begin);
				buffer = new byte[stream.Length];
				stream.Read(buffer, 0, (int)stream.Length);
			}

			return buffer;
		}

		/// <summary>
		/// Restores the global settings from a zip backup file.
		/// </summary>
		/// <param name="backupFile">The jason backup file.</param>
		/// <param name="globalSettingsStorageProvider">The destination global settings storage provider.</param>
		/// <returns><c>true</c> if the restore is succesfull, <c>false</c> otherwise.</returns>
		public static bool RestoreGlobalSettingsStorageProvider(byte[] backupFile, IGlobalSettingsStorageProviderV40 globalSettingsStorageProvider) {
			VersionFile versionFile;

			using(ZipFile globalSettingsBackupZipFile = ZipFile.Read(backupFile)) {
				ZipEntry versionEntry = (from e in globalSettingsBackupZipFile
										 where e.FileName == "Version.json"
										 select e).FirstOrDefault();
				versionFile = DeserializeVersionFile(Encoding.Unicode.GetString(ExtractEntry(versionEntry)));
				ZipEntry globalSettingsEntry = (from e in globalSettingsBackupZipFile
												where e.FileName == "GlobalSettings.json"
												select e).FirstOrDefault();
				DeserializeGlobalSettingsBackup(Encoding.Unicode.GetString(ExtractEntry(globalSettingsEntry)), globalSettingsStorageProvider, versionFile);
			}
			return true;
		}

		private static void DeserializeGlobalSettingsBackup(string json, IGlobalSettingsStorageProviderV40 globalSettingsStorageProvider, VersionFile versionFile) {
			JavaScriptSerializer serializer = new JavaScriptSerializer();
			serializer.MaxJsonLength = serializer.MaxJsonLength * 10;

			GlobalSettingsBackup globalSettingsBackup = serializer.Deserialize<GlobalSettingsBackup>(json);

			foreach(var pair in globalSettingsBackup.Settings) {
				globalSettingsStorageProvider.SetSetting(pair.Key, pair.Value);
			}
		}

		/// <summary>
		/// Backups the specified settings provider.
		/// </summary>
		/// <param name="zipFileName">The zip file name where to store the backup.</param>
		/// <param name="settingsStorageProvider">The source settings provider.</param>
		/// <param name="knownNamespaces">The currently known page namespaces.</param>
		/// <param name="knownPlugins">The currently known plugins.</param>	
		/// <returns><c>true</c> if the backup file has been succesfully created.</returns>
		public static bool BackupSettingsStorageProvider(string zipFileName, ISettingsStorageProviderV40 settingsStorageProvider, string[] knownNamespaces, string[] knownPlugins) {
			SettingsBackup settingsBackup = new SettingsBackup();

			// Settings
			settingsBackup.Settings = (Dictionary<string, string>)settingsStorageProvider.GetAllSettings();

			// Plugins Status and Configuration
			settingsBackup.PluginsFileNames = knownPlugins.ToList();
			Dictionary<string, bool> pluginsStatus = new Dictionary<string, bool>();
			Dictionary<string, string> pluginsConfiguration = new Dictionary<string, string>();
			foreach(string plugin in knownPlugins) {
				pluginsStatus[plugin] = settingsStorageProvider.GetPluginStatus(plugin);
				pluginsConfiguration[plugin] = settingsStorageProvider.GetPluginConfiguration(plugin);
			}
			settingsBackup.PluginsStatus = pluginsStatus;
			settingsBackup.PluginsConfiguration = pluginsConfiguration;

			// Metadata
			List<MetaData> metadataList = new List<MetaData>();
			// Meta-data (global)
			metadataList.Add(new MetaData() {
				Item = MetaDataItem.AccountActivationMessage,
				Tag = null,
				Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.AccountActivationMessage, null)
			});
			metadataList.Add(new MetaData() { Item = MetaDataItem.PasswordResetProcedureMessage, Tag = null, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.PasswordResetProcedureMessage, null) });
			metadataList.Add(new MetaData() { Item = MetaDataItem.LoginNotice, Tag = null, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.LoginNotice, null) });
			metadataList.Add(new MetaData() { Item = MetaDataItem.PageChangeMessage, Tag = null, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.PageChangeMessage, null) });
			metadataList.Add(new MetaData() { Item = MetaDataItem.DiscussionChangeMessage, Tag = null, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.DiscussionChangeMessage, null) });
			// Meta-data (ns-specific)
			List<string> namespacesToProcess = new List<string>();
			namespacesToProcess.Add("");
			namespacesToProcess.AddRange(knownNamespaces);
			foreach(string nspace in namespacesToProcess) {
				metadataList.Add(new MetaData() { Item = MetaDataItem.EditNotice, Tag = nspace, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.EditNotice, nspace) });
				metadataList.Add(new MetaData() { Item = MetaDataItem.Footer, Tag = nspace, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.Footer, nspace) });
				metadataList.Add(new MetaData() { Item = MetaDataItem.Header, Tag = nspace, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.Header, nspace) });
				metadataList.Add(new MetaData() { Item = MetaDataItem.HtmlHead, Tag = nspace, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.HtmlHead, nspace) });
				metadataList.Add(new MetaData() { Item = MetaDataItem.PageFooter, Tag = nspace, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.PageFooter, nspace) });
				metadataList.Add(new MetaData() { Item = MetaDataItem.PageHeader, Tag = nspace, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.PageHeader, nspace) });
				metadataList.Add(new MetaData() { Item = MetaDataItem.Sidebar, Tag = nspace, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.Sidebar, nspace) });
			}
			settingsBackup.Metadata = metadataList;

			// RecentChanges
			settingsBackup.RecentChanges = settingsStorageProvider.GetRecentChanges().ToList();

			// OutgoingLinks
			settingsBackup.OutgoingLinks = (Dictionary<string, string[]>)settingsStorageProvider.GetAllOutgoingLinks();

			// ACLEntries
			AclEntry[] aclEntries = settingsStorageProvider.AclManager.RetrieveAllEntries();
			settingsBackup.AclEntries = new List<AclEntryBackup>(aclEntries.Length);
			foreach(AclEntry aclEntry in aclEntries) {
				settingsBackup.AclEntries.Add(new AclEntryBackup() {
					Action = aclEntry.Action,
					Resource = aclEntry.Resource,
					Subject = aclEntry.Subject,
					Value = aclEntry.Value
				});
			}

			JavaScriptSerializer javascriptSerializer = new JavaScriptSerializer();
			javascriptSerializer.MaxJsonLength = javascriptSerializer.MaxJsonLength * 10;

			string tempDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());
			Directory.CreateDirectory(tempDir);

			FileStream tempFile = File.Create(Path.Combine(tempDir, "Settings.json"));
			byte[] buffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(settingsBackup));
			tempFile.Write(buffer, 0, buffer.Length);
			tempFile.Close();

			tempFile = File.Create(Path.Combine(tempDir, "Version.json"));
			buffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(generateVersionFile("Settings")));
			tempFile.Write(buffer, 0, buffer.Length);
			tempFile.Close();

			using(ZipFile zipFile = new ZipFile()) {
				zipFile.AddDirectory(tempDir, "");
				zipFile.Save(zipFileName);
			}
			Directory.Delete(tempDir, true);

			return true;
		}

		/// <summary>
		/// Restores the settings from a zip file.
		/// </summary>
		/// <param name="backupFile">The zip backup file.</param>
		/// <param name="globalSettingsStorageProvider">The destination global settings storage provider.</param>
		/// <param name="settingsStorageProvider">The destination settings storage provider.</param>
		/// <param name="isSettingGlobal">A function to check if the settings is global or not.</param>
		/// <returns><c>true</c> if the restore is succesful <c>false</c> otherwise.</returns>
		public static bool RestoreSettingsStorageProvider(string backupFile, IGlobalSettingsStorageProviderV40 globalSettingsStorageProvider, ISettingsStorageProviderV40 settingsStorageProvider, Func<string, bool> isSettingGlobal) {
			VersionFile versionFile;
			using(ZipFile settingsBackupZipFile = ZipFile.Read(backupFile)) {
				ZipEntry versionEntry = (from e in settingsBackupZipFile
										  where e.FileName == "Version.json"
										  select e).FirstOrDefault();
				versionFile = DeserializeVersionFile(Encoding.Unicode.GetString(ExtractEntry(versionEntry)));
				ZipEntry settingsEntry = (from e in settingsBackupZipFile
										where e.FileName == "Settings.json"
										select e).FirstOrDefault();
				DeserializeSettingsBackup(Encoding.Unicode.GetString(ExtractEntry(settingsEntry)), globalSettingsStorageProvider, settingsStorageProvider, versionFile, isSettingGlobal);
			}
			return true;
		}

		private static void DeserializeSettingsBackup(string json, IGlobalSettingsStorageProviderV40 globalSettingsStorageProvider, ISettingsStorageProviderV40 settingsStorageProvider, VersionFile versionFile, Func<string, bool> isSettingGlobal) {
			JavaScriptSerializer javascriptSerializer = new JavaScriptSerializer();
			javascriptSerializer.MaxJsonLength = javascriptSerializer.MaxJsonLength * 10;

			SettingsBackup settingsBackup = javascriptSerializer.Deserialize<SettingsBackup>(json);

			// Settings
			settingsStorageProvider.BeginBulkUpdate();
			// Wiki version 3.0
			if(versionFile.WikiVersion.StartsWith("3.")) {
				foreach(var pair in settingsBackup.Settings) {
					if(pair.Key.StartsWith("Theme") && pair.Value.Split(new char[] { '|' }).Length == 1) {
						settingsStorageProvider.SetSetting(pair.Key, "standard|" + pair.Value);
					}
					else if(isSettingGlobal(pair.Key)) {
						globalSettingsStorageProvider.SetSetting(pair.Key, pair.Value);
					}
					else {
						settingsStorageProvider.SetSetting(pair.Key, pair.Value);
					}
				}
			}
			// Wiki version 4.0+
			else {
				foreach(var pair in settingsBackup.Settings) {
					settingsStorageProvider.SetSetting(pair.Key, pair.Value);
				}
			}
			settingsStorageProvider.EndBulkUpdate();

			// Plugins Status
			foreach(var pair in settingsBackup.PluginsStatus) {
				settingsStorageProvider.SetPluginStatus(pair.Key, pair.Value);
			}

			// Plugins Configuration
			foreach(var pair in settingsBackup.PluginsConfiguration) {
				settingsStorageProvider.SetPluginConfiguration(pair.Key, pair.Value);
			}

			// MetaData
			foreach(MetaData item in settingsBackup.Metadata) {
				settingsStorageProvider.SetMetaDataItem(item.Item, item.Tag, item.Content);
			}

			// Recent Changes
			foreach(RecentChange recentChange in settingsBackup.RecentChanges) {
				settingsStorageProvider.AddRecentChange(recentChange.Page, recentChange.Title, recentChange.MessageSubject, recentChange.DateTime, recentChange.User, recentChange.Change, recentChange.Description);
			}

			// OutgoingLinks
			foreach(var pair in settingsBackup.OutgoingLinks) {
				settingsStorageProvider.StoreOutgoingLinks(pair.Key, pair.Value);
			}

			// ACLEntries
			AclEntry[] oldEntries = settingsStorageProvider.AclManager.RetrieveAllEntries();
			foreach(AclEntry oldEntry in oldEntries) {
				settingsStorageProvider.AclManager.DeleteEntry(oldEntry.Resource, oldEntry.Action, oldEntry.Subject);
			}
			List<AclEntry> aclEntries = new List<AclEntry>(settingsBackup.AclEntries.Count);
			foreach(AclEntryBackup entry in settingsBackup.AclEntries) {
				settingsStorageProvider.AclManager.StoreEntry(entry.Resource, entry.Action, entry.Subject, entry.Value);
			}
		}

		/// <summary>
		/// Backups the pages storage provider.
		/// </summary>
		/// <param name="zipFileName">The zip file name where to store the backup.</param>
		/// <param name="pagesStorageProvider">The pages storage provider.</param>
		/// <returns><c>true</c> if the backup file has been succesfully created.</returns>
		public static bool BackupPagesStorageProvider(string zipFileName, IPagesStorageProviderV40 pagesStorageProvider) {
			JavaScriptSerializer javascriptSerializer = new JavaScriptSerializer();
			javascriptSerializer.MaxJsonLength = javascriptSerializer.MaxJsonLength * 10;

			string tempDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());
			Directory.CreateDirectory(tempDir);

			List<NamespaceInfo> nspaces = new List<NamespaceInfo>(pagesStorageProvider.GetNamespaces());
			nspaces.Add(null);
			List<NamespaceBackup> namespaceBackupList = new List<NamespaceBackup>(nspaces.Count);
			foreach(NamespaceInfo nspace in nspaces) {

				// Backup categories
				CategoryInfo[] categories = pagesStorageProvider.GetCategories(nspace);
				List<CategoryBackup> categoriesBackup = new List<CategoryBackup>(categories.Length);
				foreach(CategoryInfo category in categories) {
					// Add this category to the categoriesBackup list
					categoriesBackup.Add(new CategoryBackup() {
						FullName = category.FullName,
						Pages = category.Pages
					});
				}

				// Backup NavigationPaths
				NavigationPath[] navigationPaths = pagesStorageProvider.GetNavigationPaths(nspace);
				List<NavigationPathBackup> navigationPathsBackup = new List<NavigationPathBackup>(navigationPaths.Length);
				foreach(NavigationPath navigationPath in navigationPaths) {
					navigationPathsBackup.Add(new NavigationPathBackup() {
						FullName = navigationPath.FullName,
						Pages = navigationPath.Pages
					});
				}

				// Add this namespace to the namespaceBackup list
				namespaceBackupList.Add(new NamespaceBackup() {
					Name = nspace == null ? "" : nspace.Name,
					DefaultPageFullName = nspace == null ? "" : nspace.DefaultPageFullName,
					Categories = categoriesBackup,
					NavigationPaths = navigationPathsBackup
				});

				// Backup pages (one json file for each page containing a maximum of 100 revisions)
				PageContent[] pages = pagesStorageProvider.GetPages(nspace);
				foreach(PageContent page in pages) {
					PageBackup pageBackup = new PageBackup();
					pageBackup.FullName = page.FullName;
					pageBackup.CreationDateTime = page.CreationDateTime;
					pageBackup.LastModified = page.LastModified;
					pageBackup.Content = page.Content;
					pageBackup.Comment = page.Comment;
					pageBackup.Description = page.Description;
					pageBackup.Keywords = page.Keywords;
					pageBackup.Title = page.Title;
					pageBackup.User = page.User;
					pageBackup.LinkedPages = page.LinkedPages;
					pageBackup.Categories = (from c in pagesStorageProvider.GetCategoriesForPage(page.FullName)
											 select c.FullName).ToArray();

					// Backup the 100 most recent versions of the page
					List<PageRevisionBackup> pageContentBackupList = new List<PageRevisionBackup>();
					int[] revisions = pagesStorageProvider.GetBackups(page.FullName);
					for(int i = 0; i < Math.Min(revisions.Length, 100); i++) {
						PageContent pageRevision = pagesStorageProvider.GetBackupContent(page.FullName, revisions[i]);
						PageRevisionBackup pageContentBackup = new PageRevisionBackup() {
							Revision = revisions[i],
							Content = pageRevision.Content,
							Comment = pageRevision.Comment,
							Description = pageRevision.Description,
							Keywords = pageRevision.Keywords,
							Title = pageRevision.Title,
							User = pageRevision.User,
							LastModified = pageRevision.LastModified
						};
						pageContentBackupList.Add(pageContentBackup);
					}
					pageBackup.Revisions = pageContentBackupList;

					// Backup draft of the page
					PageContent draft = pagesStorageProvider.GetDraft(page.FullName);
					if(draft != null) {
						pageBackup.Draft = new PageRevisionBackup() {
							Content = draft.Content,
							Comment = draft.Comment,
							Description = draft.Description,
							Keywords = draft.Keywords,
							Title = draft.Title,
							User = draft.User,
							LastModified = draft.LastModified
						};
					}

					// Backup all messages of the page
					List<MessageBackup> messageBackupList = new List<MessageBackup>();
					foreach(Message message in pagesStorageProvider.GetMessages(page.FullName)) {
						messageBackupList.Add(BackupMessage(message));
					}
					pageBackup.Messages = messageBackupList;

					FileStream tempFile = File.Create(Path.Combine(tempDir, page.FullName + ".json"));
					byte[] buffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(pageBackup));
					tempFile.Write(buffer, 0, buffer.Length);
					tempFile.Close();
				}
			}
			FileStream tempNamespacesFile = File.Create(Path.Combine(tempDir, "Namespaces.json"));
			byte[] namespacesBuffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(namespaceBackupList));
			tempNamespacesFile.Write(namespacesBuffer, 0, namespacesBuffer.Length);
			tempNamespacesFile.Close();

			// Backup content templates
			ContentTemplate[] contentTemplates = pagesStorageProvider.GetContentTemplates();
			List<ContentTemplateBackup> contentTemplatesBackup = new List<ContentTemplateBackup>(contentTemplates.Length);
			foreach(ContentTemplate contentTemplate in contentTemplates) {
				contentTemplatesBackup.Add(new ContentTemplateBackup() {
					Name = contentTemplate.Name,
					Content = contentTemplate.Content
				});
			}
			FileStream tempContentTemplatesFile = File.Create(Path.Combine(tempDir, "ContentTemplates.json"));
			byte[] contentTemplatesBuffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(contentTemplatesBackup));
			tempContentTemplatesFile.Write(contentTemplatesBuffer, 0, contentTemplatesBuffer.Length);
			tempContentTemplatesFile.Close();

			// Backup Snippets
			Snippet[] snippets = pagesStorageProvider.GetSnippets();
			List<SnippetBackup> snippetsBackup = new List<SnippetBackup>(snippets.Length);
			foreach(Snippet snippet in snippets) {
				snippetsBackup.Add(new SnippetBackup() {
					Name = snippet.Name,
					Content = snippet.Content
				});
			}
			FileStream tempSnippetsFile = File.Create(Path.Combine(tempDir, "Snippets.json"));
			byte[] snippetsBuffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(snippetsBackup));
			tempSnippetsFile.Write(snippetsBuffer, 0, snippetsBuffer.Length);
			tempSnippetsFile.Close();

			FileStream tempVersionFile = File.Create(Path.Combine(tempDir, "Version.json"));
			byte[] versionBuffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(generateVersionFile("Pages")));
			tempVersionFile.Write(versionBuffer, 0, versionBuffer.Length);
			tempVersionFile.Close();


			using(ZipFile zipFile = new ZipFile()) {
				zipFile.AddDirectory(tempDir, "");
				zipFile.Save(zipFileName);
			}
			Directory.Delete(tempDir, true);

			return true;
		}

		// Backup a message with a recursive function to backup all its replies.
		private static MessageBackup BackupMessage(Message message) {
			MessageBackup messageBackup = new MessageBackup() {
				Id = message.ID,
				Subject = message.Subject,
				Body = message.Body,
				DateTime = message.DateTime,
				Username = message.Username
			};
			List<MessageBackup> repliesBackup = new List<MessageBackup>(message.Replies.Length);
			foreach(Message reply in message.Replies) {
				repliesBackup.Add(BackupMessage(reply));
			}
			messageBackup.Replies = repliesBackup;
			return messageBackup;
		}

		/// <summary>
		/// Restores pages from a zip file.
		/// </summary>
		/// <param name="backupFile">The zip backup file.</param>
		/// <param name="pagesStorageProvider">The destination pages storage provider.</param>
		/// <returns><c>true</c> if the restore is succesful <c>false</c> otherwise.</returns>
		public static bool RestorePagesStorageProvider(string backupFile, IPagesStorageProviderV40 pagesStorageProvider) {
			VersionFile versionFile;
			using(ZipFile pagesBackupZipFile = ZipFile.Read(backupFile)) {
				ZipEntry versionEntry = (from e in pagesBackupZipFile
										 where e.FileName == "Version.json"
										 select e).FirstOrDefault();
				versionFile = DeserializeVersionFile(Encoding.Unicode.GetString(ExtractEntry(versionEntry)));

				// Restore namespaces
				ZipEntry namespacesEntry = (from e in pagesBackupZipFile
											where e.FileName == "Namespaces.json"
											select e).FirstOrDefault();
				if(namespacesEntry != null) {
					DeserializeNamepsacesBackupStep1(Encoding.Unicode.GetString(ExtractEntry(namespacesEntry)), pagesStorageProvider, versionFile);
				}

				// Restore pages
				List<ZipEntry> pageEntries = (from e in pagesBackupZipFile
											  where e.FileName != "Namespaces.json" &&
													e.FileName != "ContentTemplates.json" &&
													e.FileName != "Snippets.json" &&
													e.FileName != "Version.json"
											  select e).ToList();
				foreach(ZipEntry pageEntry in pageEntries) {
					DeserializePageBackup(Encoding.Unicode.GetString(ExtractEntry(pageEntry)), pagesStorageProvider, versionFile);
				}

				// Restore content templates
				ZipEntry contentTemplatesEntry = (from e in pagesBackupZipFile
											where e.FileName == "ContentTemplates.json"
											select e).FirstOrDefault();
				if(contentTemplatesEntry != null) {
					DeserializeContentTemplatesBackup(Encoding.Unicode.GetString(ExtractEntry(contentTemplatesEntry)), pagesStorageProvider, versionFile);
				}

				// Restore snippets
				ZipEntry snippetsEntry = (from e in pagesBackupZipFile
											where e.FileName == "Snippets.json"
											select e).FirstOrDefault();
				if(snippetsEntry != null) {
					DeserializeSnippetsBackup(Encoding.Unicode.GetString(ExtractEntry(snippetsEntry)), pagesStorageProvider, versionFile);
				}

				// Restore namespaces a second time to correctly set default pages.
				if(namespacesEntry != null) {
					DeserializeNamepsacesBackupStep2(Encoding.Unicode.GetString(ExtractEntry(namespacesEntry)), pagesStorageProvider, versionFile);
				}
			}
			return true;
		}

		private static void DeserializeNamepsacesBackupStep1(string json, IPagesStorageProviderV40 pagesStorageProvider, VersionFile versionFile) {
			JavaScriptSerializer javascriptSerializer = new JavaScriptSerializer();
			javascriptSerializer.MaxJsonLength = javascriptSerializer.MaxJsonLength * 10;

			List<NamespaceBackup> namespacesBackup = javascriptSerializer.Deserialize<List<NamespaceBackup>>(json);

			// Restore namespaces
			foreach(NamespaceBackup namespaceBackup in namespacesBackup) {
				if(namespaceBackup.Name != "") {
					pagesStorageProvider.AddNamespace(namespaceBackup.Name);
				}

				// Restore pages categories
				foreach(CategoryBackup category in namespaceBackup.Categories) {
					pagesStorageProvider.AddCategory(namespaceBackup.Name, NameTools.GetLocalName(category.FullName));
				}
			}
		}

		private static void DeserializeNamepsacesBackupStep2(string json, IPagesStorageProviderV40 pagesStorageProvider, VersionFile versionFile) {
			JavaScriptSerializer javascriptSerializer = new JavaScriptSerializer();
			javascriptSerializer.MaxJsonLength = javascriptSerializer.MaxJsonLength * 10;

			List<NamespaceBackup> namespacesBackup = javascriptSerializer.Deserialize<List<NamespaceBackup>>(json);

			// Restore namespaces
			foreach(NamespaceBackup namespaceBackup in namespacesBackup) {
				if(namespaceBackup.Name != "") {
					pagesStorageProvider.SetNamespaceDefaultPage(new NamespaceInfo(namespaceBackup.Name, pagesStorageProvider, namespaceBackup.DefaultPageFullName), namespaceBackup.DefaultPageFullName);
				}

				// Restore navigation paths
				foreach(NavigationPathBackup navigationPath in namespaceBackup.NavigationPaths) {
					pagesStorageProvider.AddNavigationPath(namespaceBackup.Name, NameTools.GetLocalName(navigationPath.FullName), navigationPath.Pages);
				}
			}
		}

		private static void DeserializeContentTemplatesBackup(string json, IPagesStorageProviderV40 pagesStorageProvider, VersionFile versionFile) {
			JavaScriptSerializer javascriptSerializer = new JavaScriptSerializer();
			javascriptSerializer.MaxJsonLength = javascriptSerializer.MaxJsonLength * 10;

			List<ContentTemplateBackup> contentTemplatesBackup = javascriptSerializer.Deserialize<List<ContentTemplateBackup>>(json);
			foreach(ContentTemplateBackup contentTemplate in contentTemplatesBackup) {
				pagesStorageProvider.AddContentTemplate(contentTemplate.Name, contentTemplate.Content);
			}
		}

		private static void DeserializeSnippetsBackup(string json, IPagesStorageProviderV40 pagesStorageProvider, VersionFile versionFile) {
			JavaScriptSerializer javascriptSerializer = new JavaScriptSerializer();
			javascriptSerializer.MaxJsonLength = javascriptSerializer.MaxJsonLength * 10;

			List<SnippetBackup> snippetsBackup = javascriptSerializer.Deserialize<List<SnippetBackup>>(json);
			foreach(SnippetBackup snippet in snippetsBackup) {
				pagesStorageProvider.AddSnippet(snippet.Name, snippet.Content);
			}
		}

		private static void DeserializePageBackup(string json, IPagesStorageProviderV40 pagesStorageProvider, VersionFile versionFile) {
			JavaScriptSerializer javascriptSerializer = new JavaScriptSerializer();
			javascriptSerializer.MaxJsonLength = javascriptSerializer.MaxJsonLength * 10;

			PageBackup pageBackup = javascriptSerializer.Deserialize<PageBackup>(json);

			// CurrentVersion
			pagesStorageProvider.SetPageContent(NameTools.GetNamespace(pageBackup.FullName), NameTools.GetLocalName(pageBackup.FullName), pageBackup.CreationDateTime, pageBackup.Title,
												pageBackup.User, pageBackup.LastModified, pageBackup.Comment, pageBackup.Content, pageBackup.Keywords, pageBackup.Description, SaveMode.Normal);
			
			// Draft
			if(pageBackup.Draft != null) {
				pagesStorageProvider.SetPageContent(NameTools.GetNamespace(pageBackup.FullName), NameTools.GetLocalName(pageBackup.FullName), pageBackup.CreationDateTime, pageBackup.Draft.Title,
												pageBackup.Draft.User, pageBackup.Draft.LastModified, pageBackup.Draft.Comment, pageBackup.Draft.Content, pageBackup.Draft.Keywords, pageBackup.Draft.Description, SaveMode.Draft);
			}

			// Revisions
			List<PageRevisionBackup> revisionsBackup = pageBackup.Revisions;
			foreach(PageRevisionBackup revision in revisionsBackup) {
				pagesStorageProvider.SetBackupContent(new PageContent(pageBackup.FullName, pagesStorageProvider, pageBackup.CreationDateTime, revision.Title, revision.User, revision.LastModified, revision.Comment, revision.Content, revision.Keywords, revision.Description), revision.Revision);
			}

			// Restore messages
			pagesStorageProvider.BulkStoreMessages(pageBackup.FullName, DeserializeMessages(pageBackup.Messages).ToArray());

			// Restore categories binding
			pagesStorageProvider.RebindPage(pageBackup.FullName, pageBackup.Categories);
		}

		private static List<Message> DeserializeMessages(List<MessageBackup> messagesBackup) {
			List<Message> ret = new List<Message>();
			foreach(MessageBackup messageBackup in messagesBackup) {
				Message message = new Message(messageBackup.Id, messageBackup.Username, messageBackup.Subject, messageBackup.DateTime, messageBackup.Body);
				message.Replies = DeserializeMessages(messageBackup.Replies).ToArray();
				ret.Add(message);
			}
			return ret;
		}

		/// <summary>
		/// Backups the users storage provider.
		/// </summary>
		/// <param name="zipFileName">The zip file name where to store the backup.</param>
		/// <param name="usersStorageProvider">The users storage provider.</param>
		/// <returns><c>true</c> if the backup file has been succesfully created.</returns>
		public static bool BackupUsersStorageProvider(string zipFileName, IUsersStorageProviderV40 usersStorageProvider) {
			JavaScriptSerializer javascriptSerializer = new JavaScriptSerializer();
			javascriptSerializer.MaxJsonLength = javascriptSerializer.MaxJsonLength * 10;

			string tempDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());
			Directory.CreateDirectory(tempDir);

			// Backup users
			UserInfo[] users = usersStorageProvider.GetUsers();
			List<UserBackup> usersBackup = new List<UserBackup>(users.Length);
			foreach(UserInfo user in users) {
				usersBackup.Add(new UserBackup() {
					Username = user.Username,
					Active = user.Active,
					DateTime = user.DateTime,
					DisplayName = user.DisplayName,
					Email = user.Email,
					Groups = user.Groups,
					UserData = usersStorageProvider.RetrieveAllUserData(user)
				});
			}
			FileStream tempFile = File.Create(Path.Combine(tempDir, "Users.json"));
			byte[] buffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(usersBackup));
			tempFile.Write(buffer, 0, buffer.Length);
			tempFile.Close();

			// Backup UserGroups
			UserGroup[] userGroups = usersStorageProvider.GetUserGroups();
			List<UserGroupBackup> userGroupsBackup = new List<UserGroupBackup>(userGroups.Length);
			foreach(UserGroup userGroup in userGroups) {
				userGroupsBackup.Add(new UserGroupBackup() {
					Name = userGroup.Name,
					Description = userGroup.Description
				});
			}
			tempFile = File.Create(Path.Combine(tempDir, "Groups.json"));
			buffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(userGroupsBackup));
			tempFile.Write(buffer, 0, buffer.Length);
			tempFile.Close();

			tempFile = File.Create(Path.Combine(tempDir, "Version.json"));
			buffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(generateVersionFile("Users")));
			tempFile.Write(buffer, 0, buffer.Length);
			tempFile.Close();


			using(ZipFile zipFile = new ZipFile()) {
				zipFile.AddDirectory(tempDir, "");
				zipFile.Save(zipFileName);
			}
			Directory.Delete(tempDir, true);

			return true;
		}

		/// <summary>
		/// Restores users from a zip file.
		/// </summary>
		/// <param name="backupFile">The zip backup file.</param>
		/// <param name="usersStorageProvider">The destination users storage provider.</param>
		/// <returns><c>true</c> if the restore is succesful <c>false</c> otherwise.</returns>
		public static bool RestoreUsersStorageProvider(string backupFile, IUsersStorageProviderV40 usersStorageProvider) {
			VersionFile versionFile;

			using(ZipFile usersBackupZipFile = ZipFile.Read(backupFile)) {
				ZipEntry versionEntry = (from e in usersBackupZipFile
										 where e.FileName == "Version.json"
										 select e).FirstOrDefault();
				versionFile = DeserializeVersionFile(Encoding.Unicode.GetString(ExtractEntry(versionEntry)));
				// Restore groups
				ZipEntry groupsEntry = (from e in usersBackupZipFile
										where e.FileName == "Groups.json"
										select e).FirstOrDefault();
				DeserializeGroupsBackup(Encoding.Unicode.GetString(ExtractEntry(groupsEntry)), usersStorageProvider, versionFile);
				
				// Restore Users
				ZipEntry usersEntry = (from e in usersBackupZipFile
										where e.FileName == "Users.json"
										select e).FirstOrDefault();
				DeserializeUsersBackup(Encoding.Unicode.GetString(ExtractEntry(usersEntry)), usersStorageProvider, versionFile);
			}
			return true;
		}

		private static void DeserializeGroupsBackup(string json, IUsersStorageProviderV40 usersStorageProvider, VersionFile versionFile) {
			JavaScriptSerializer javascriptSerializer = new JavaScriptSerializer();
			javascriptSerializer.MaxJsonLength = javascriptSerializer.MaxJsonLength * 10;

			List<UserGroupBackup> userGroupsBackup = javascriptSerializer.Deserialize<List<UserGroupBackup>>(json);

			UserGroup[] userGroups = usersStorageProvider.GetUserGroups();

			foreach(UserGroupBackup userGroup in userGroupsBackup) {
				UserGroup existingGroup = (from g in userGroups
										   where g.Name == userGroup.Name
										   select g).FirstOrDefault();
				if(existingGroup != null) {
					usersStorageProvider.ModifyUserGroup(existingGroup, userGroup.Description);
				}
				else {
					usersStorageProvider.AddUserGroup(userGroup.Name, userGroup.Description);
				}
			}
		}

		private static void DeserializeUsersBackup(string json, IUsersStorageProviderV40 usersStorageProvider, VersionFile versionFile) {
			JavaScriptSerializer javascriptSerializer = new JavaScriptSerializer();
			javascriptSerializer.MaxJsonLength = javascriptSerializer.MaxJsonLength * 10;

			List<UserBackup> usersBackup = javascriptSerializer.Deserialize<List<UserBackup>>(json);

			foreach(UserBackup user in usersBackup) {
				UserInfo userInfo = new UserInfo(user.Username, user.DisplayName, user.Email, user.Active, user.DateTime, usersStorageProvider);
				
				usersStorageProvider.AddUser(user.Username, user.DisplayName, "temppassword", user.Email, user.Active, user.DateTime);
				
				// User group membership
				usersStorageProvider.SetUserMembership(userInfo, user.Groups);

				// User metadata
				foreach(var pair in user.UserData) {
					usersStorageProvider.StoreUserData(userInfo, pair.Key, pair.Value);
				}
			}
		}

		/// <summary>
		/// Backups the files storage provider.
		/// </summary>
		/// <param name="zipFileName">The zip file name where to store the backup.</param>
		/// <param name="filesStorageProvider">The files storage provider.</param>
		/// <returns><c>true</c> if the backup file has been succesfully created.</returns>
		public static bool BackupFilesStorageProvider(string zipFileName, IFilesStorageProviderV40 filesStorageProvider) {
			JavaScriptSerializer javascriptSerializer = new JavaScriptSerializer();
			javascriptSerializer.MaxJsonLength = javascriptSerializer.MaxJsonLength * 10;

			string tempDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());
			Directory.CreateDirectory(tempDir);

			DirectoryBackup directoriesBackup = BackupDirectory(filesStorageProvider, tempDir, null);

			FileStream tempFile = File.Create(Path.Combine(tempDir, "Files.json"));
			byte[] buffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(directoriesBackup));
			tempFile.Write(buffer, 0, buffer.Length);
			tempFile.Close();

			// Backup Pages Attachments
			string[] pagesWithAttachment = filesStorageProvider.GetPagesWithAttachments();
			foreach(string pageWithAttachment in pagesWithAttachment) {
				string[] attachments = filesStorageProvider.ListPageAttachments(pageWithAttachment);
				List<AttachmentBackup> attachmentsBackup = new List<AttachmentBackup>(attachments.Length);
				foreach(string attachment in attachments) {
					FileDetails attachmentDetails = filesStorageProvider.GetPageAttachmentDetails(pageWithAttachment, attachment);
					attachmentsBackup.Add(new AttachmentBackup() {
						Name = attachment,
						PageFullName = pageWithAttachment,
						LastModified = attachmentDetails.LastModified,
						Size = attachmentDetails.Size
					});
					using(MemoryStream stream = new MemoryStream()) {
						filesStorageProvider.RetrievePageAttachment(pageWithAttachment, attachment, stream);
						stream.Seek(0, SeekOrigin.Begin);
						byte[] tempBuffer = new byte[stream.Length];
						stream.Read(tempBuffer, 0, (int)stream.Length);

						DirectoryInfo dir = Directory.CreateDirectory(Path.Combine(tempDir, Path.Combine("__attachments", pageWithAttachment)));
						tempFile = File.Create(Path.Combine(dir.FullName, attachment));
						tempFile.Write(tempBuffer, 0, tempBuffer.Length);
						tempFile.Close();
					}
				}
				tempFile = File.Create(Path.Combine(tempDir, Path.Combine("__attachments", Path.Combine(pageWithAttachment, "Attachments.json"))));
				buffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(attachmentsBackup));
				tempFile.Write(buffer, 0, buffer.Length);
				tempFile.Close();
			}
			tempFile = File.Create(Path.Combine(tempDir, "Version.json"));
			buffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(generateVersionFile("Files")));
			tempFile.Write(buffer, 0, buffer.Length);
			tempFile.Close();

			using(ZipFile zipFile = new ZipFile()) {
				zipFile.AddDirectory(tempDir, "");
				zipFile.Save(zipFileName);
			}
			Directory.Delete(tempDir, true);

			return true;
		}

		private static DirectoryBackup BackupDirectory(IFilesStorageProviderV40 filesStorageProvider, string zipTempDir, string directory) {
			DirectoryBackup directoryBackup = new DirectoryBackup();

			string[] files = filesStorageProvider.ListFiles(directory);
			List<FileBackup> filesBackup = new List<FileBackup>(files.Length);
			foreach(string file in files) {
				FileDetails fileDetails = filesStorageProvider.GetFileDetails(file);
				filesBackup.Add(new FileBackup() {
					Name = file,
					Size = fileDetails.Size,
					LastModified = fileDetails.LastModified
				});

				FileStream tempFile = File.Create(Path.Combine(zipTempDir.Trim('/').Trim('\\'), file.Trim('/').Trim('\\')));
				using(MemoryStream stream = new MemoryStream()) {
					filesStorageProvider.RetrieveFile(file, stream);
					stream.Seek(0, SeekOrigin.Begin);
					byte[] buffer = new byte[stream.Length];
					stream.Read(buffer, 0, (int)stream.Length);
					tempFile.Write(buffer, 0, buffer.Length);
					tempFile.Close();
				}
			}
			directoryBackup.Name = directory;
			directoryBackup.Files = filesBackup;

			string[] directories = filesStorageProvider.ListDirectories(directory);
			List<DirectoryBackup> subdirectoriesBackup = new List<DirectoryBackup>(directories.Length);
			foreach(string d in directories) {
				Directory.CreateDirectory(Path.Combine(zipTempDir.Trim('/').Trim('\\'), d.Trim('/').Trim('\\')));
				subdirectoriesBackup.Add(BackupDirectory(filesStorageProvider, zipTempDir, d));
			}
			directoryBackup.SubDirectories = subdirectoriesBackup;

			return directoryBackup;
		}

		/// <summary>
		/// Restores files from a zip file.
		/// </summary>
		/// <param name="backupFileName">The zip backup file.</param>
		/// <param name="filesStorageProvider">The destination files storage provider.</param>
		/// <returns><c>true</c> if the restore is succesful <c>false</c> otherwise.</returns>
		public static bool RestoreFilesStorageProvider(string backupFileName, IFilesStorageProviderV40 filesStorageProvider) {
			VersionFile versionFile;
			
			using(ZipFile filesBackupZipFile = ZipFile.Read(backupFileName)) {
				ZipEntry versionEntry = (from e in filesBackupZipFile
										 where e.FileName == "Version.json"
										 select e).FirstOrDefault();
				versionFile = DeserializeVersionFile(Encoding.Unicode.GetString(ExtractEntry(versionEntry)));
				
				foreach(ZipEntry zipEntry in filesBackupZipFile) {
					if(zipEntry.FileName != "Version.json") {
						using(MemoryStream stream = new MemoryStream()) {
							zipEntry.Extract(stream);
							stream.Seek(0, SeekOrigin.Begin);
							byte[] buffer = new byte[stream.Length];
							stream.Read(buffer, 0, (int)stream.Length);

							if(zipEntry.FileName == "Files.json") {
								DeserializeFilesBackup(filesBackupZipFile, Encoding.Unicode.GetString(buffer), filesStorageProvider);
							}
							else if(zipEntry.FileName.EndsWith("Attachments.json")) {
								DeserializeAttachmentsBackup(filesBackupZipFile, Encoding.Unicode.GetString(buffer), filesStorageProvider);
							}
						}
					}
				}
			}
			return true;
		}

		private static void DeserializeFilesBackup(ZipFile filesBackupZipFile, string json, IFilesStorageProviderV40 filesStorageProvider) {
			JavaScriptSerializer javascriptSerializer = new JavaScriptSerializer();
			javascriptSerializer.MaxJsonLength = javascriptSerializer.MaxJsonLength * 10;

			DirectoryBackup directoryBackup = javascriptSerializer.Deserialize<DirectoryBackup>(json);

			string path = "";
			RestoreDirectory(filesStorageProvider, filesBackupZipFile, directoryBackup, path);
		}

		private static void RestoreDirectory(IFilesStorageProviderV40 filesStorageProvider, ZipFile filesBackupZipFile, DirectoryBackup directoryBackup, string path) {
			if(!string.IsNullOrEmpty(directoryBackup.Name)) {
				filesStorageProvider.CreateDirectory("/", directoryBackup.Name);
				path += "/" + directoryBackup.Name;
			}
			foreach(FileBackup file in directoryBackup.Files) {
				ZipEntry entry = filesBackupZipFile.FirstOrDefault(e => e.FileName == file.Name.TrimStart('/'));
				if(entry != null) {
					using(MemoryStream stream = new MemoryStream()) {
						entry.Extract(stream);
						stream.Seek(0, SeekOrigin.Begin);
						filesStorageProvider.StoreFile(file.Name, stream, true);
					}
				}
			}
			foreach(DirectoryBackup subDirectory in directoryBackup.SubDirectories) {
				RestoreDirectory(filesStorageProvider, filesBackupZipFile, subDirectory, path);
			}
		}

		private static void DeserializeAttachmentsBackup(ZipFile filesBackupZipFile, string json, IFilesStorageProviderV40 filesStorageProvider) {
			JavaScriptSerializer javascriptSerializer = new JavaScriptSerializer();
			javascriptSerializer.MaxJsonLength = javascriptSerializer.MaxJsonLength * 10;

			List<AttachmentBackup> attachmentsBackup = javascriptSerializer.Deserialize<List<AttachmentBackup>>(json);

			foreach(AttachmentBackup attachment in attachmentsBackup) {
				ZipEntry entry = filesBackupZipFile.FirstOrDefault(e => e.FileName == "__attachments/" + attachment.PageFullName + "/" + attachment.Name);
				if(entry != null) {
					using(MemoryStream stream = new MemoryStream()) {
						entry.Extract(stream);
						stream.Seek(0, SeekOrigin.Begin);
						filesStorageProvider.StorePageAttachment(attachment.PageFullName, attachment.Name, stream, true);
					}
				}
			}
		}

		/// <summary>
		/// Backups the themes storage provider.
		/// </summary>
		/// <param name="themesStorageProvider">The themes storage provider.</param>
		/// <returns>The zip backup file.</returns>
		public static byte[] BackupThemesStorageProvider(IThemesStorageProviderV40 themesStorageProvider) {
			throw new NotImplementedException();
		}

	}

	internal class GlobalSettingsBackup {
		public Dictionary<string, string> Settings { get; set; }
		public List<string> PluginsFileNames { get; set; }
	}

	internal class SettingsBackup {
		public Dictionary<string, string> Settings { get; set; }
		public List<string> PluginsFileNames { get; set; }
		public Dictionary<string, bool> PluginsStatus { get; set; }
		public Dictionary<string, string> PluginsConfiguration { get; set; }
		public List<MetaData> Metadata { get; set; }
		public List<RecentChange> RecentChanges { get; set; }
		public Dictionary<string, string[]> OutgoingLinks { get; set; }
		public List<AclEntryBackup> AclEntries { get; set; }
	}

	internal class AclEntryBackup {
		public Value Value { get; set; }
		public string Subject { get; set; }
		public string Resource { get; set; }
		public string Action { get; set; }
	}

	internal class MetaData {
		public MetaDataItem Item {get; set;}
		public string Tag {get; set;}
		public string Content {get; set;}
	}

	internal class PageBackup {
		public String FullName { get; set; }
		public DateTime CreationDateTime { get; set; }
		public DateTime LastModified { get; set; }
		public string Content { get; set; }
		public string Comment { get; set; }
		public string Description { get; set; }
		public string[] Keywords { get; set; }
		public string Title { get; set; }
		public string User { get; set; }
		public string[] LinkedPages { get; set; }
		public List<PageRevisionBackup> Revisions { get; set; }
		public PageRevisionBackup Draft { get; set; }
		public List<MessageBackup> Messages { get; set; }
		public string[] Categories { get; set; }
	}

	internal class PageRevisionBackup {
		public string Content { get; set; }
		public string Comment { get; set; }
		public string Description { get; set; }
		public string[] Keywords { get; set; }
		public string Title { get; set; }
		public string User { get; set; }
		public DateTime LastModified { get; set; }
		public int Revision { get; set; }
	}

	internal class NamespaceBackup {
		public string Name { get; set; }
		public string DefaultPageFullName { get; set; }
		public List<CategoryBackup> Categories { get; set; }
		public List<NavigationPathBackup> NavigationPaths { get; set; }
	}

	internal class CategoryBackup {
		public string FullName { get; set; }
		public string[] Pages { get; set; }
	}

	internal class ContentTemplateBackup {
		public string Name { get; set; }
		public string Content { get; set; }
	}

	internal class MessageBackup {
		public List<MessageBackup> Replies { get; set; }
		public int Id { get; set; }
		public string Subject { get; set; }
		public string Body { get; set; }
		public DateTime DateTime { get; set; }
		public string Username { get; set; }
	}

	internal class NavigationPathBackup {
		public string FullName { get; set; }
		public string[] Pages { get; set; }
	}

	internal class SnippetBackup {
		public string Name { get; set; }
		public string Content { get; set; }
	}

	internal class UserBackup {
		public string Username { get; set; }
		public bool Active { get; set; }
		public DateTime DateTime { get; set; }
		public string DisplayName { get; set; }
		public string Email { get; set; }
		public string[] Groups { get; set; }
		public IDictionary<string, string> UserData { get; set; }
	}

	internal class UserGroupBackup {
		public string Name { get; set; }
		public string Description { get; set; }
	}

	internal class DirectoryBackup {
		public List<FileBackup> Files { get; set; }
		public List<DirectoryBackup> SubDirectories { get; set; }
		public string Name { get; set; }
	}

	internal class FileBackup {
		public string Name { get; set; }
		public long Size { get; set; }
		public DateTime LastModified { get; set; }
		public string DirectoryName { get; set; }
	}

	internal class VersionFile {
		public string BackupRestoreVersion { get; set; }
		public string WikiVersion { get; set; }
		public string BackupName { get; set; }
	}

	internal class AttachmentBackup {
		public string Name { get; set; }
		public string PageFullName { get; set; }
		public DateTime LastModified { get; set; }
		public long Size { get; set; }
	}

}
