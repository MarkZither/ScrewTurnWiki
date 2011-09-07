
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

		/// <summary>
		/// Backups the specified settings provider.
		/// </summary>
		/// <param name="settingsStorageProvider">The source settings provider.</param>
		/// <param name="knownNamespaces">The currently known page namespaces.</param>
		/// <param name="knownPlugins">The currently known plugins.</param>		
		/// <returns>The json backup file.</returns>
		public static byte[] BackupSettingsStorageProvider(ISettingsStorageProviderV40 settingsStorageProvider, string[] knownNamespaces, string[] knownPlugins) {
			SettingsBackup settingsBackup = new SettingsBackup();

			// Settings
			settingsBackup.Settings = (Dictionary<string, string>)settingsStorageProvider.GetAllSettings();

			// Plugins Status and Configuration
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
			settingsBackup.AclEntries = settingsStorageProvider.AclManager.RetrieveAllEntries();

			JavaScriptSerializer javascriptSerializer = new JavaScriptSerializer();
			javascriptSerializer.MaxJsonLength = javascriptSerializer.MaxJsonLength * 10;

			ZipFile settingsBackupZipFile = new ZipFile();
			settingsBackupZipFile.AddEntry("Settings.json", Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(settingsBackup)));

			settingsBackupZipFile.AddEntry("Version.json", Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(generateVersionFile("Settings"))));

			byte[] buffer;
			using(MemoryStream stream = new MemoryStream()) {
				settingsBackupZipFile.Save(stream);
				stream.Seek(0, SeekOrigin.Begin);
				buffer = new byte[stream.Length];
				stream.Read(buffer, 0, (int)stream.Length);
			}

			return buffer;
		}

		/// <summary>
		/// Restores the settings from a json file.
		/// </summary>
		/// <param name="backupFile">The json backup file.</param>
		/// <param name="settingsStorageProvider">The destination settings storage provider.</param>
		/// <returns><c>true</c> if the restore is succesful <c>false</c> otherwise.</returns>
		public static bool RestoreSettingsStorageProvider(byte[] backupFile, ISettingsStorageProviderV40 settingsStorageProvider) {
			JavaScriptSerializer javascriptSerializer = new JavaScriptSerializer();
			javascriptSerializer.MaxJsonLength = javascriptSerializer.MaxJsonLength * 10;
			
			string temp = Encoding.Unicode.GetString(backupFile);

			SettingsBackup settingsBackup = javascriptSerializer.Deserialize<SettingsBackup>(temp);

			// Settings
			settingsStorageProvider.BeginBulkUpdate();
			foreach(var pair in settingsBackup.Settings) {
				settingsStorageProvider.SetSetting(pair.Key, pair.Value);
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
			foreach(AclEntry entry in settingsBackup.AclEntries) {
				settingsStorageProvider.AclManager.StoreEntry(entry.Resource, entry.Action, entry.Subject, entry.Value);
			}

			return true;
		}

		/// <summary>
		/// Backups the pages storage provider.
		/// </summary>
		/// <param name="pagesStorageProvider">The pages storage provider.</param>
		/// <returns>The zip backup file.</returns>
		public static byte[] BackupPagesStorageProvider(IPagesStorageProviderV40 pagesStorageProvider) {
			JavaScriptSerializer javascriptSerializer = new JavaScriptSerializer();
			javascriptSerializer.MaxJsonLength = javascriptSerializer.MaxJsonLength * 10;

			ZipFile pagesBackupZipFile = new ZipFile();

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
				pagesBackupZipFile.AddEntry("Categories.json", Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(categoriesBackup)));
				
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
					Name = nspace == null ? "<root>" : nspace.Name,
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

					// Backup the 100 most recent versions of the page
					List<PageRevisionBackup> pageContentBackupList = new List<PageRevisionBackup>();
					int[] revisions = pagesStorageProvider.GetBackups(page.FullName);
					for(int i = 0; i < Math.Min(revisions.Length, 100); i++) {
						PageContent pageRevision = pagesStorageProvider.GetBackupContent(page.FullName, revisions[i]);
						PageRevisionBackup pageContentBackup = new PageRevisionBackup() {
							Content = pageRevision.Content,
							Comment = pageRevision.Comment,
							Description = pageRevision.Description,
							Keywords = pageRevision.Keywords,
							Title = pageRevision.Title,
							User = pageRevision.User
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
							User = draft.User
						};
					}

					// Backup all messages of the page
					List<MessageBackup> messageBackupList = new List<MessageBackup>();
					foreach(Message message in pagesStorageProvider.GetMessages(page.FullName)) {
						messageBackupList.Add(BackupMessage(message));
					}
					pageBackup.Messages = messageBackupList;

					pagesBackupZipFile.AddEntry(page.FullName + ".json", Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(pageBackup)));
				}
			}
			pagesBackupZipFile.AddEntry("Namespaces.json", Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(namespaceBackupList)));

			// Backup content templates
			ContentTemplate[] contentTemplates = pagesStorageProvider.GetContentTemplates();
			List<ContentTemplateBackup> contentTemplatesBackup = new List<ContentTemplateBackup>(contentTemplates.Length);
			foreach(ContentTemplate contentTemplate in contentTemplates) {
				contentTemplatesBackup.Add(new ContentTemplateBackup() {
					Name = contentTemplate.Name,
					Content = contentTemplate.Content
				});
			}
			pagesBackupZipFile.AddEntry("ContentTemplates.json", Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(contentTemplatesBackup)));

			// Backup Snippets
			Snippet[] snippets = pagesStorageProvider.GetSnippets();
			List<SnippetBackup> snippetsBackup = new List<SnippetBackup>(snippets.Length);
			foreach(Snippet snippet in snippets) {
				snippetsBackup.Add(new SnippetBackup() {
					Name = snippet.Name,
					Content = snippet.Content
				});
			}
			pagesBackupZipFile.AddEntry("Snippets.json", Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(snippetsBackup)));

			pagesBackupZipFile.AddEntry("Version.json", Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(generateVersionFile("Pages"))));

			byte[] buffer;
			using(MemoryStream stream = new MemoryStream()) {
				pagesBackupZipFile.Save(stream);
				stream.Seek(0, SeekOrigin.Begin);
				buffer = new byte[stream.Length];
				stream.Read(buffer, 0, (int)stream.Length);
			}

			return buffer;
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
		/// Backups the users storage provider.
		/// </summary>
		/// <param name="usersStorageProvider">The users storage provider.</param>
		/// <returns>The zip backup file.</returns>
		public static byte[] BackupUsersStorageProvider(IUsersStorageProviderV40 usersStorageProvider) {
			JavaScriptSerializer javascriptSerializer = new JavaScriptSerializer();
			javascriptSerializer.MaxJsonLength = javascriptSerializer.MaxJsonLength * 10;

			ZipFile usersBackupZipFile = new ZipFile();

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
			usersBackupZipFile.AddEntry("Users.json", Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(usersBackup)));

			// Backup UserGroups
			UserGroup[] userGroups = usersStorageProvider.GetUserGroups();
			List<UserGroupBackup> userGroupsBackup = new List<UserGroupBackup>(userGroups.Length);
			foreach(UserGroup userGroup in userGroups) {
				userGroupsBackup.Add(new UserGroupBackup() {
					Name = userGroup.Name,
					Description = userGroup.Description,
					Users = userGroup.Users
				});
			}
			usersBackupZipFile.AddEntry("Groups.json", Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(userGroupsBackup)));

			usersBackupZipFile.AddEntry("Version.json", Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(generateVersionFile("Users"))));

			byte[] buffer;
			using(MemoryStream stream = new MemoryStream()) {
				usersBackupZipFile.Save(stream);
				stream.Seek(0, SeekOrigin.Begin);
				buffer = new byte[stream.Length];
				stream.Read(buffer, 0, (int)stream.Length);
			}

			return buffer;
		}

		/// <summary>
		/// Backups the files storage provider.
		/// </summary>
		/// <param name="filesStorageProvider">The files storage provider.</param>
		/// <returns>The zip backup file.</returns>
		public static byte[] BackupFilesStorageProvider(IFilesStorageProviderV40 filesStorageProvider) {
			JavaScriptSerializer javascriptSerializer = new JavaScriptSerializer();
			javascriptSerializer.MaxJsonLength = javascriptSerializer.MaxJsonLength * 10;

			ZipFile filesBackupZipFile = new ZipFile();

			DirectoryBackup directoriesBackup = BackupDirectory(filesStorageProvider, filesBackupZipFile, null);

			filesBackupZipFile.AddEntry("Files.json", Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(directoriesBackup)));

			// Backup Pages Attachments
			string[] pagesWithAttachment = filesStorageProvider.GetPagesWithAttachments();
			foreach(string pageWithAttachment in pagesWithAttachment) {
				string[] attachments = filesStorageProvider.ListPageAttachments(pageWithAttachment);
				List<FileBackup> attachmentsBackup = new List<FileBackup>(attachments.Length);
				foreach(string attachment in attachments) {
					FileDetails attachmentDetails = filesStorageProvider.GetPageAttachmentDetails(pageWithAttachment, attachment);
					attachmentsBackup.Add(new FileBackup() {
						Name = attachment,
						LastModified = attachmentDetails.LastModified,
						Size = attachmentDetails.Size
					});
					using(MemoryStream stream = new MemoryStream()) {
						filesStorageProvider.RetrievePageAttachment(pageWithAttachment, attachment, stream);
						byte[] tempBuffer = new byte[stream.Length];
						stream.Read(tempBuffer, 0, (int)stream.Length);
						filesBackupZipFile.AddEntry(Path.Combine("__attachments", pageWithAttachment, attachment), tempBuffer);
					}
				}
				filesBackupZipFile.AddEntry(Path.Combine("__attachments", pageWithAttachment, "attachments.json"), Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(attachmentsBackup)));
			}

			filesBackupZipFile.AddEntry("Version.json", Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(generateVersionFile("Files"))));

			byte[] buffer;
			using(MemoryStream stream = new MemoryStream()) {
				filesBackupZipFile.Save(stream);
				stream.Seek(0, SeekOrigin.Begin);
				buffer = new byte[stream.Length];
				stream.Read(buffer, 0, (int)stream.Length);
			}

			return buffer;

		}

		private static DirectoryBackup BackupDirectory(IFilesStorageProviderV40 filesStorageProvider, ZipFile filesBackupZipFile, string directory) {
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
				using(MemoryStream stream = new MemoryStream()) {
					filesStorageProvider.RetrieveFile(file, stream);
					byte[] buffer = new byte[stream.Length];
					stream.Read(buffer, 0, (int)stream.Length);
					filesBackupZipFile.AddEntry(file, buffer);
				}
			}
			directoryBackup.Files = filesBackup;

			string[] directories = filesStorageProvider.ListDirectories(directory);
			List<DirectoryBackup> subdirectoriesBackup = new List<DirectoryBackup>(directories.Length);
			foreach(string d in directories) {
				subdirectoriesBackup.Add(BackupDirectory(filesStorageProvider, filesBackupZipFile, d));
			}
			directoryBackup.SubDirectories = subdirectoriesBackup;

			return directoryBackup;
		}

		/// <summary>
		/// Backups the themes storage provider.
		/// </summary>
		/// <param name="themesStorageProvider">The themes storage provider.</param>
		/// <returns>The zip backup file.</returns>
		public static byte[] BackupThemesStorageProvider(IThemesStorageProviderV40 themesStorageProvider) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Backups the specified global settings storage provider.
		/// </summary>
		/// <param name="globalSettingsStorageProvider">The global settings storage provider.</param>
		/// <returns>The zip backup file.</returns>
		public static byte[] BackupGlobalSettingsStorageProvider(IGlobalSettingsStorageProviderV40 globalSettingsStorageProvider) {
			GlobalSettingsBackup globalSettingsBackup = new GlobalSettingsBackup();

			// Settings
			globalSettingsBackup.Settings = (Dictionary<string, string>)globalSettingsStorageProvider.GetAllSettings();

			List<string> plugins = globalSettingsStorageProvider.ListPluginAssemblies().ToList();
			ZipFile globalSettingsBackupZipFile = new ZipFile();
			foreach(string pluginFileName in plugins) {
				globalSettingsBackupZipFile.AddEntry(pluginFileName, globalSettingsStorageProvider.RetrievePluginAssembly(pluginFileName));
			}

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
			using(ZipFile globalSettingsBackupZipFile = ZipFile.Read(backupFile)) {
				foreach(ZipEntry zipEntry in globalSettingsBackupZipFile) {
					if(zipEntry.FileName == "GlobalSettings.json") {
						using(MemoryStream stream = new MemoryStream()) {
							zipEntry.Extract(stream);
							stream.Seek(0, SeekOrigin.Begin);
							byte[] buffer = new byte[stream.Length];
							stream.Read(buffer, 0, (int)stream.Length);
							DeserializeGlobalSettingsBackup(Encoding.UTF8.GetString(buffer), globalSettingsStorageProvider);
						}
					}
					else {
						using(MemoryStream stream = new MemoryStream()) {
							zipEntry.Extract(stream);
							stream.Seek(0, SeekOrigin.Begin);
							byte[] buffer = new byte[stream.Length];
							stream.Read(buffer, 0, (int)stream.Length);
							globalSettingsStorageProvider.StorePluginAssembly(zipEntry.FileName, buffer);
						}
					}
				}
			}
			return true;
		}

		private static void DeserializeGlobalSettingsBackup(string json, IGlobalSettingsStorageProviderV40 globalSettingsStorageProvider) {
			JavaScriptSerializer serializer = new JavaScriptSerializer();
			serializer.MaxJsonLength = serializer.MaxJsonLength * 10;

			GlobalSettingsBackup globalSettingsBackup = serializer.Deserialize<GlobalSettingsBackup>(json);

			foreach(var pair in globalSettingsBackup.Settings) {
				globalSettingsStorageProvider.SetSetting(pair.Key, pair.Value);
			}
		}

	}

	internal class SettingsBackup {
		public Dictionary<string, string> Settings { get; set; }
		public Dictionary<string, bool> PluginsStatus { get; set; }
		public Dictionary<string, string> PluginsConfiguration { get; set; }
		public List<MetaData> Metadata { get; set; }
		public List<RecentChange> RecentChanges { get; set; }
		public Dictionary<string, string[]> OutgoingLinks { get; set; }
		public AclEntry[] AclEntries { get; set; }
	}

	internal class MetaData {
		public MetaDataItem Item {get; set;}
		public string Tag {get; set;}
		public string Content {get; set;}
	}

	internal class GlobalSettingsBackup {
		public Dictionary<string, string> Settings { get; set; }
		public List<string> pluginsFileNames { get; set; }
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
	}

	internal class PageRevisionBackup {
		public string Content { get; set; }
		public string Comment { get; set; }
		public string Description { get; set; }
		public string[] Keywords { get; set; }
		public string Title { get; set; }
		public string User { get; set; }
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
		public string[] Users { get; set; }
	}

	internal class DirectoryBackup {
		public List<FileBackup> Files { get; set; }
		public List<DirectoryBackup> SubDirectories { get; set; }
	}

	internal class FileBackup {
		public string Name { get; set; }
		public long Size { get; set; }
		public DateTime LastModified { get; set; }
	}

	internal class VersionFile {
		public string BackupRestoreVersion { get; set; }
		public string WikiVersion { get; set; }
		public string BackupName { get; set; }
	}
}
