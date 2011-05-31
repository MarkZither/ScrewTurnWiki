
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

		/// <summary>
		/// Backups the specified settings provider.
		/// </summary>
		/// <param name="settingsProvider">The settings provider.</param>
		/// <param name="knownNamespaces">The currently known page namespaces.</param>
		/// <param name="knownPlugins">The currently known plugins.</param>		
		/// <returns>The backup file.</returns>
		public static byte[] BackupSettingsStorageProvider(ISettingsStorageProviderV30 settingsStorageProvider, string[] knownNamespaces, string[] knownPlugins) {
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
			settingsBackup.PluginsConfiguratio = pluginsConfiguration;

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

			string temp = javascriptSerializer.Serialize(settingsBackup);

			return Encoding.UTF8.GetBytes(temp);
		}

		/// <summary>
		/// Restores the settings provider.
		/// </summary>
		/// <param name="backupFile">The backup file.</param>
		/// <param name="settingsStorageProvider">The settings storage provider.</param>
		/// <returns><c>true</c> if the restore is succesful <c>false</c> otherwise.</returns>
		public static bool RestoreSettingsStorageProvider(byte[] backupFile, ISettingsStorageProviderV30 settingsStorageProvider) {
			JavaScriptSerializer javascriptSerializer = new JavaScriptSerializer();
			javascriptSerializer.MaxJsonLength = javascriptSerializer.MaxJsonLength * 10;
			
			string temp = Encoding.UTF8.GetString(backupFile);

			SettingsBackup settingsBackup = javascriptSerializer.Deserialize<SettingsBackup>(temp);

			// Settings
			settingsStorageProvider.BeginBulkUpdate();
			foreach(var pair in settingsBackup.Settings) {
				settingsStorageProvider.SetSetting(pair.Key, pair.Value);
			}
			settingsStorageProvider.EndBulkUpdate();

			// Plugins Status
			foreach(var pair in settingsBackup.PluginsStatus) {
				settingsStorageProvider.SetPluginStatus(pair.Key, (bool)pair.Value);
			}

			// Plugins Configuration
			foreach(var pair in settingsBackup.PluginsConfiguratio) {
				settingsStorageProvider.SetPluginConfiguration(pair.Key, (string)pair.Value);
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
		/// Backups the specified global settings storage provider.
		/// </summary>
		/// <param name="globalSettingsStorageProvider">The global settings storage provider.</param>
		/// <returns>The backup file.</returns>
		public static byte[] BackupGlobalSettingsStorageProvider(IGlobalSettingsStorageProviderV30 globalSettingsStorageProvider) {
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

			globalSettingsBackupZipFile.AddEntry("GlobalSettings.dat", Encoding.UTF8.GetBytes(serializer.Serialize(globalSettingsBackup)));

			byte[] buffer;
			using(MemoryStream stream = new MemoryStream()) {
				globalSettingsBackupZipFile.Save(stream);
				stream.Seek(0, SeekOrigin.Begin);
				buffer = new byte[stream.Length];
				stream.Read(buffer, 0, (int)stream.Length);
			}

			return buffer;
		}

		public static bool RestoreGlobalSettingsStorageProvider(byte[] backupFile, IGlobalSettingsStorageProviderV30 globalSettingsStorageProvider) {
			using(ZipFile globalSettingsBackupZipFile = ZipFile.Read(backupFile)) {
				foreach(ZipEntry zipEntry in globalSettingsBackupZipFile) {
					if(zipEntry.FileName == "GlobalSettings.dat") {
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

		private static void DeserializeGlobalSettingsBackup(string json, IGlobalSettingsStorageProviderV30 globalSettingsStorageProvider) {
			JavaScriptSerializer serializer = new JavaScriptSerializer();
			serializer.MaxJsonLength = serializer.MaxJsonLength * 10;

			GlobalSettingsBackup globalSettingsBackup = serializer.Deserialize<GlobalSettingsBackup>(json);

			foreach(var pair in globalSettingsBackup.Settings) {
				globalSettingsStorageProvider.SetSetting(pair.Key, pair.Value);
			}
		}

	}

	internal class SettingsBackup {
		public Dictionary<string, string> Settings;
		public Dictionary<string, bool> PluginsStatus;
		public Dictionary<string, string> PluginsConfiguratio;
		public List<MetaData> Metadata;
		public List<RecentChange> RecentChanges;
		public Dictionary<string, string[]> OutgoingLinks;
		public AclEntry[] AclEntries;
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
}
