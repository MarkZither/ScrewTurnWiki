
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using Microsoft.WindowsAzure.StorageClient;
using System.Data.Services.Client;
using ScrewTurn.Wiki.AclEngine;

namespace ScrewTurn.Wiki.Plugins.AzureStorage {

	/// <summary>
	/// Implements a Azure Table Storage settings storage provider.
	/// </summary>
	public class AzureTableStorageSettingsProvider : ISettingsStorageProviderV30 {

		private IHostV30 _host;
		private string _wiki;

		private TableServiceContext _context;

		private bool isFirstWikiStart;

		private IAclManager _aclManager;

		/// <summary>
		/// Retrieves the value of a Setting.
		/// </summary>
		/// <param name="name">The name of the Setting.</param>
		/// <returns>The value of the Setting, or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty.</exception>
		public string GetSetting(string name) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("name");
			try {
				var query = (from e in _context.CreateQuery<SettingsEntity>(SettingsTable).AsTableServiceQuery()
							 where e.PartitionKey.Equals(_wiki) && e.RowKey.Equals(name)
							 select e).AsTableServiceQuery();
				var entity = QueryHelper<SettingsEntity>.FirstOrDefault(query);
				if(entity == null) return null;
				return string.IsNullOrEmpty(entity.Value) ? "" : entity.Value;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Stores the value of a Setting.
		/// </summary>
		/// <param name="name">The name of the Setting.</param>
		/// <param name="value">The value of the Setting. Value cannot contain CR and LF characters, which will be removed.</param>
		/// <returns>True if the Setting is stored, false otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty.</exception>
		public bool SetSetting(string name, string value) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("name");

			try {
				SettingsEntity settingsEntity = new SettingsEntity() {
					PartitionKey = _wiki,
					RowKey = name,
					Value = value
				};
				_context.AddObject(SettingsTable, settingsEntity);
				_context.SaveChangesStandard();
				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Gets the all the setting values.
		/// </summary>
		/// <returns>All the settings.</returns>
		public IDictionary<string, string> GetAllSettings() {
			try {
				var query = (from e in _context.CreateQuery<SettingsEntity>(SettingsTable).AsTableServiceQuery()
							 where e.PartitionKey.Equals(_wiki)
							 select e).AsTableServiceQuery();
				IList<SettingsEntity> settingsEntities = QueryHelper<SettingsEntity>.All(query);

				Dictionary<string, string> settings = new Dictionary<string, string>(settingsEntities.Count);
				foreach(SettingsEntity entity in settingsEntities) {
					settings.Add(entity.RowKey, entity.Value);
				}
				return settings;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Starts a Bulk update of the Settings so that a bulk of settings can be set before storing them.
		/// </summary>
		public void BeginBulkUpdate() {
			// Nothing todo
		}

		/// <summary>
		/// Ends a Bulk update of the Settings and stores the settings.
		/// </summary>
		public void EndBulkUpdate() {
			// Nothing todo
		}

		/// <summary>
		/// Gets a meta-data item's content.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="tag">The tag that specifies the context (usually the namespace).</param>
		/// <returns>The content.</returns>
		public string GetMetaDataItem(MetaDataItem item, string tag) {
			try {
				var query = (from e in _context.CreateQuery<MetadataEntity>(MetadataTable).AsTableServiceQuery()
							 where e.PartitionKey.Equals(_wiki) && e.RowKey.Equals(item + "|" + tag)
							 select e).AsTableServiceQuery();
				var entity = QueryHelper<MetadataEntity>.FirstOrDefault(query);
				if(entity == null) return "";
				return string.IsNullOrEmpty(entity.Content) ? "" : entity.Content;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Sets a meta-data items' content.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="tag">The tag that specifies the context (usually the namespace).</param>
		/// <param name="content">The content.</param>
		/// <returns><c>true</c> if the content is set, <c>false</c> otherwise.</returns>
		public bool SetMetaDataItem(MetaDataItem item, string tag, string content) {
			try {
				MetadataEntity metadataEntity = new MetadataEntity() {
					PartitionKey = _wiki,
					RowKey = item + "|" + tag,
					Content = content
				};
				_context.AddObject(MetadataTable, metadataEntity);
				_context.SaveChangesStandard();
				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Gets the change from a string.
		/// </summary>
		/// <param name="change">The string.</param>
		/// <returns>The change.</returns>
		private static ScrewTurn.Wiki.PluginFramework.Change GetChange(string change) {
			switch(change.ToUpperInvariant()) {
				case "U":
					return ScrewTurn.Wiki.PluginFramework.Change.PageUpdated;
				case "D":
					return ScrewTurn.Wiki.PluginFramework.Change.PageDeleted;
				case "R":
					return ScrewTurn.Wiki.PluginFramework.Change.PageRolledBack;
				case "N":
					return ScrewTurn.Wiki.PluginFramework.Change.PageRenamed;
				case "MP":
					return ScrewTurn.Wiki.PluginFramework.Change.MessagePosted;
				case "ME":
					return ScrewTurn.Wiki.PluginFramework.Change.MessageEdited;
				case "MD":
					return ScrewTurn.Wiki.PluginFramework.Change.MessageDeleted;
				default:
					throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Gets the change string for a change.
		/// </summary>
		/// <param name="change">The change.</param>
		/// <returns>The change string.</returns>
		private static string GetChangeString(ScrewTurn.Wiki.PluginFramework.Change change) {
			switch(change) {
				case ScrewTurn.Wiki.PluginFramework.Change.PageUpdated:
					return "U";
				case ScrewTurn.Wiki.PluginFramework.Change.PageDeleted:
					return "D";
				case ScrewTurn.Wiki.PluginFramework.Change.PageRolledBack:
					return "R";
				case ScrewTurn.Wiki.PluginFramework.Change.PageRenamed:
					return "N";
				case ScrewTurn.Wiki.PluginFramework.Change.MessagePosted:
					return "MP";
				case ScrewTurn.Wiki.PluginFramework.Change.MessageEdited:
					return "ME";
				case ScrewTurn.Wiki.PluginFramework.Change.MessageDeleted:
					return "MD";
				default:
					throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Gets the recent changes of the Wiki.
		/// </summary>
		/// <returns>The recent Changes, oldest to newest.</returns>
		public RecentChange[] GetRecentChanges() {
			try {
				var query = (from e in _context.CreateQuery<RecentChangesEntity>(RecentChangesTable).AsTableServiceQuery()
							 where e.PartitionKey.Equals(_wiki)
							 select e).Take((int)(int.Parse(_host.GetSettingValue(_wiki, SettingName.MaxRecentChanges)) * 0.90)).AsTableServiceQuery();
				IList<RecentChangesEntity> recentChangesEntities = QueryHelper<RecentChangesEntity>.All(query);

				List<RecentChange> recentChanges = new List<RecentChange>(recentChangesEntities.Count);
				foreach(RecentChangesEntity entity in recentChangesEntities) {
					recentChanges.Add(new RecentChange(entity.Page, entity.Title, entity.MessageSubject + "", entity.DateTime.ToLocalTime(), entity.User, GetChange(entity.Change), entity.Description + ""));
				}
				return recentChanges.ToArray();
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Adds a new change.
		/// </summary>
		/// <param name="page">The page name.</param>
		/// <param name="title">The page title.</param>
		/// <param name="messageSubject">The message subject (or <c>null</c>).</param>
		/// <param name="dateTime">The date/time.</param>
		/// <param name="user">The user.</param>
		/// <param name="change">The change.</param>
		/// <param name="descr">The description (optional).</param>
		/// <returns><c>true</c> if the change is saved, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/>, <paramref name="title"/> or <paramref name="user"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="page"/>, <paramref name="title"/> or <paramref name="user"/> are empty.</exception>
		public bool AddRecentChange(string page, string title, string messageSubject, DateTime dateTime, string user, ScrewTurn.Wiki.PluginFramework.Change change, string descr) {
			if(page == null) throw new ArgumentNullException("page");
			if(title == null) throw new ArgumentNullException("title");
			if(user == null) throw new ArgumentNullException("user"); 
			if(page.Length == 0) throw new ArgumentException("page");
			if(title.Length == 0) throw new ArgumentException("title");
			if(user.Length == 0) throw new ArgumentException("user");

			try {
				RecentChangesEntity recentChangesEntity = new RecentChangesEntity() {
					PartitionKey = _wiki,
					RowKey = dateTime.Ticks + "-" + Guid.NewGuid().ToString("N"),
					Page = page,
					Title = title,
					MessageSubject = messageSubject,
					DateTime = dateTime.ToUniversalTime(),
					User = user,
					Change = GetChangeString(change),
					Description = descr
				};
				_context.AddObject(RecentChangesTable, recentChangesEntity);
				_context.SaveChangesStandard();
				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Gets the ACL Manager instance.
		/// </summary>
		public ScrewTurn.Wiki.AclEngine.IAclManager AclManager {
			get { return _aclManager; }
		}

		/// <summary>
		/// Stores the outgoing links of a page, overwriting existing data.
		/// </summary>
		/// <param name="page">The full name of the page.</param>
		/// <param name="outgoingLinks">The full names of the pages that <b>page</b> links to.</param>
		/// <returns><c>true</c> if the outgoing links are stored, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> or <paramref name="outgoingLinks"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="page"/> or <paramref name="outgoingLinks"/> are empty.</exception>
		public bool StoreOutgoingLinks(string page, string[] outgoingLinks) {
			if(page == null) throw new ArgumentNullException("page");
			if(outgoingLinks == null) throw new ArgumentNullException("outgoingLinks");
			if(page.Length == 0) throw new ArgumentException("page");

			try {
				var query = (from e in _context.CreateQuery<OutgoingLinkEntity>(OutgoingLinksTable).AsTableServiceQuery()
							 where e.PartitionKey.Equals(_wiki) && e.SourcePage.Equals(page)
							 select e).AsTableServiceQuery();
				var entities = QueryHelper<OutgoingLinkEntity>.All(query);
				foreach(var entity in entities) {
					_context.DeleteObject(entity);
				}
				_context.SaveChangesStandard();

				foreach(string outgoingLink in outgoingLinks) {
					if(outgoingLink == null) throw new ArgumentNullException("outgoingLinks", "Null element in outgoing links array");
					if(outgoingLink.Length == 0) throw new ArgumentException("Elements in outgoing links cannot be empty", "outgoingLinks");

					OutgoingLinkEntity outgoingLinksEntity = new OutgoingLinkEntity() {
						PartitionKey = _wiki,
						RowKey = Guid.NewGuid().ToString("N"),
						SourcePage = page,
						DestinationPage = outgoingLink
					};
					_context.AddObject(OutgoingLinksTable, outgoingLinksEntity);
				}
				_context.SaveChangesStandard();
				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Gets the outgoing links of a page.
		/// </summary>
		/// <param name="page">The full name of the page.</param>
		/// <returns>The outgoing links.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="page"/> is empty.</exception>
		public string[] GetOutgoingLinks(string page) {
			if(page == null) throw new ArgumentNullException("page");
			if(page.Length == 0) throw new ArgumentException("page");

			try {
				var query = (from e in _context.CreateQuery<OutgoingLinkEntity>(OutgoingLinksTable).AsTableServiceQuery()
							 where e.PartitionKey.Equals(_wiki) && e.SourcePage.Equals(page)
							 select e).AsTableServiceQuery();
				var entities = QueryHelper<OutgoingLinkEntity>.All(query);
				List<string> outgoingLinks = new List<string>(entities.Count);
				foreach(var entity in entities) {
					outgoingLinks.Add(entity.DestinationPage);
				}
				return outgoingLinks.ToArray();
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Gets all the outgoing links stored.
		/// </summary>
		/// <returns>The outgoing links, in a dictionary in the form page-&gt;outgoing_links.</returns>
		public IDictionary<string, string[]> GetAllOutgoingLinks() {
			try {
				var query = (from e in _context.CreateQuery<OutgoingLinkEntity>(OutgoingLinksTable).AsTableServiceQuery()
							 where e.PartitionKey.Equals(_wiki)
							 select e).AsTableServiceQuery();
				IList<OutgoingLinkEntity> outgoingLinksEntities = QueryHelper<OutgoingLinkEntity>.All(query);

				Dictionary<string, List<string>> outgoingLinksWithList = new Dictionary<string, List<string>>(outgoingLinksEntities.Count);
				foreach(OutgoingLinkEntity entity in outgoingLinksEntities) {
					if(!outgoingLinksWithList.ContainsKey(entity.SourcePage)) outgoingLinksWithList[entity.SourcePage] = new List<string>();
					outgoingLinksWithList[entity.SourcePage].Add(entity.DestinationPage);
				}
				Dictionary<string, string[]> outgoingLinks = new Dictionary<string, string[]>(outgoingLinksWithList.Count);
				foreach(var outgoingLink in outgoingLinksWithList) {
					outgoingLinks.Add(outgoingLink.Key, outgoingLink.Value.ToArray());
				}
				return outgoingLinks;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Deletes the outgoing links of a page and all the target links that include the page.
		/// </summary>
		/// <param name="page">The full name of the page.</param>
		/// <returns><c>true</c> if the links are deleted, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="page"/> is empty.</exception>
		public bool DeleteOutgoingLinks(string page) {
			if(page == null) throw new ArgumentNullException("page");
			if(page.Length == 0) throw new ArgumentException("page");

			try {
				var query = (from e in _context.CreateQuery<OutgoingLinkEntity>(OutgoingLinksTable).AsTableServiceQuery()
							where e.PartitionKey.Equals(_wiki) && (e.SourcePage.Equals(page) || e.DestinationPage.Equals(page))
							select e).AsTableServiceQuery();
				var entities = QueryHelper<OutgoingLinkEntity>.All(query);
				if(entities == null || entities.Count == 0) return false;
				foreach(var entity in entities) {
					_context.DeleteObject(entity);
				}
				_context.SaveChangesStandard();

				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Updates all outgoing links data for a page rename.
		/// </summary>
		/// <param name="oldName">The old page name.</param>
		/// <param name="newName">The new page name.</param>
		/// <returns><c>true</c> if the data is updated, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="oldName"/> or <paramref name="newName"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="oldName"/> or <paramref name="newName"/> are empty.</exception>
		public bool UpdateOutgoingLinksForRename(string oldName, string newName) {
			if(oldName == null) throw new ArgumentNullException("oldName");
			if(newName == null) throw new ArgumentNullException("newName");
			if(oldName.Length == 0) throw new ArgumentException("oldName");
			if(newName.Length == 0) throw new ArgumentException("newName");

			try {
				bool updateExecuted = false;
				var query = (from e in _context.CreateQuery<OutgoingLinkEntity>(OutgoingLinksTable).AsTableServiceQuery()
							 where e.PartitionKey.Equals(_wiki) && e.SourcePage.Equals(oldName)
							 select e).AsTableServiceQuery();
				var entities = QueryHelper<OutgoingLinkEntity>.All(query);
				if(entities != null && entities.Count > 0) {
					updateExecuted = true;
					foreach(var entity in entities) {
						entity.SourcePage = newName;
						_context.UpdateObject(entity);
					}
					_context.SaveChangesStandard();
				}

				query = (from e in _context.CreateQuery<OutgoingLinkEntity>(OutgoingLinksTable).AsTableServiceQuery()
						 where e.PartitionKey.Equals(_wiki) && e.DestinationPage.Equals(oldName)
						 select e).AsTableServiceQuery();
				entities = QueryHelper<OutgoingLinkEntity>.All(query);
				if(entities != null && entities.Count > 0) {
					updateExecuted = true;
					foreach(var entity in entities) {
						entity.DestinationPage= newName;
						_context.UpdateObject(entity);
					}
					_context.SaveChangesStandard();
				}

				return updateExecuted;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Determines whether the wiki was started for the first time.
		/// </summary>
		/// <returns><c>true</c> if the wiki was started for the first time, <c>false</c> otherwise.</returns>
		public bool IsFirstApplicationStart() {
			return isFirstWikiStart;
		}
		
		/// <summary>
		/// The settings table name.
		/// </summary>
		public static readonly string SettingsTable = "Settings";

		/// <summary>
		/// The metadata table name.
		/// </summary>
		public static readonly string MetadataTable = "Metadata";

		/// <summary>
		/// The recent changes table name.
		/// </summary>
		public static readonly string RecentChangesTable = "RecentChanges";

		/// <summary>
		/// The outgoing links table name.
		/// </summary>
		public static readonly string OutgoingLinksTable = "OutgoingLinks";

		/// <summary>
		/// The outgoing links table name.
		/// </summary>
		public static readonly string AclEntriesTable = "AclEntries";

		/// <summary>
		/// Initializes the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <param name="wiki">The wiki.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="host"/> or <paramref name="config"/> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <paramref name="config"/> is not valid or is incorrect.</exception>
		public void Init(IHostV30 host, string config, string wiki) {
			if(host == null) throw new ArgumentNullException("host");
			if(config == null) throw new ArgumentNullException("config");

			_host = host;
			_wiki = string.IsNullOrEmpty(wiki) ? "root" : wiki.ToLowerInvariant();
			string[] connectionStrings = config.Split(new char[] { '|' });
			if(connectionStrings == null || connectionStrings.Length != 2) throw new InvalidConfigurationException("The given connections string is invalid.");

			_context = TableStorage.GetContext(connectionStrings[0], connectionStrings[1]);

			_aclManager = new AzureTableStorageAclManager(StoreEntry, DeleteEntries, RenameAclResource, RetrieveAllAclEntries, RetrieveAclEntriesForResource, RetrieveAclEntriesForSubject);

			//string firstStart = GetSetting("FirstStart");
			//isFirstWikiStart = string.IsNullOrEmpty(firstStart);
			//if(isFirstWikiStart) SetSetting("FirstStart", "true");
		}

		/// <summary>
		/// Sets up the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="host"/> or <paramref name="config"/> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <paramref name="config"/> is not valid or is incorrect.</exception>
		public void SetUp(IHostV30 host, string config) {
			if(host == null) throw new ArgumentNullException("host");
			if(config == null) throw new ArgumentNullException("config");

			string[] connectionStrings = config.Split(new char[] { '|' });
			if(connectionStrings == null || connectionStrings.Length != 2) throw new InvalidConfigurationException("The given connections string is invalid.");

			TableStorage.CreateTable(connectionStrings[0], connectionStrings[1], SettingsTable);
			TableStorage.CreateTable(connectionStrings[0], connectionStrings[1], MetadataTable);
			TableStorage.CreateTable(connectionStrings[0], connectionStrings[1], RecentChangesTable);
			TableStorage.CreateTable(connectionStrings[0], connectionStrings[1], OutgoingLinksTable);
			TableStorage.CreateTable(connectionStrings[0], connectionStrings[1], AclEntriesTable);
		}

		/// <summary>
		/// Gets the Information about the Provider.
		/// </summary>
		public ComponentInformation Information {
			get { return new ComponentInformation("AzureTableStorage Settings Provider", "Threeplicate Srl", "0.1", "", ""); }
		}

		/// <summary>
		/// Gets a brief summary of the configuration string format, in HTML. Returns <c>null</c> if no configuration is needed.
		/// </summary>
		public string ConfigHelpHtml {
			get { return ""; }
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose() {
			// Nothing todo
		}

		#region AclManager backend methods

		/// <summary>
		/// Converts a <see cref="T:Value" /> to its corresponding string representation.
		/// </summary>
		/// <param name="value">The <see cref="T:Value" />.</param>
		/// <returns>The character representation.</returns>
		private static string AclEntryValueToString(Value value) {
			switch(value) {
				case Value.Grant:
					return "G";
				case Value.Deny:
					return "D";
				default:
					throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Converts a string representation of a <see cref="T:Value" /> back to the enum value.
		/// </summary>
		/// <param name="c">The character representation.</param>
		/// <returns>The <see cref="T:Value" />.</returns>
		private static Value AclEntryValueFromString(string c) {
			switch(c.ToUpperInvariant()) {
				case "G":
					return Value.Grant;
				case "D":
					return Value.Deny;
				default:
					throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Retrieves all ACL entries.
		/// </summary>
		/// <returns>The ACL entries.</returns>
		private AclEntry[] RetrieveAllAclEntries() {
			try {
				var query = (from e in _context.CreateQuery<AclEntriesEntity>(AclEntriesTable).AsTableServiceQuery()
							 where e.PartitionKey.Equals(_wiki)
							 select e).AsTableServiceQuery();
				IList<AclEntriesEntity> aclEntriesEntities = QueryHelper<AclEntriesEntity>.All(query);

				if(aclEntriesEntities == null) return new AclEntry[0];

				List<AclEntry> aclEntries = new List<AclEntry>(aclEntriesEntities.Count);
				foreach(AclEntriesEntity entity in aclEntriesEntities) {
					aclEntries.Add(new AclEntry(entity.Resource, entity.Action, entity.Subject, AclEntryValueFromString(entity.Value)));
				}
				return aclEntries.ToArray();
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Retrieves all ACL entries for a resource.
		/// </summary>
		/// <param name="resource">The resource.</param>
		/// <returns>The ACL entries for the resource.</returns>
		private AclEntry[] RetrieveAclEntriesForResource(string resource) {
			try {
				var query = (from e in _context.CreateQuery<AclEntriesEntity>(AclEntriesTable).AsTableServiceQuery()
							 where e.PartitionKey.Equals(_wiki) && e.Resource.Equals(resource)
							 select e).AsTableServiceQuery();
				IList<AclEntriesEntity> aclEntriesEntities = QueryHelper<AclEntriesEntity>.All(query);

				if(aclEntriesEntities == null) return new AclEntry[0];

				List<AclEntry> aclEntries = new List<AclEntry>(aclEntriesEntities.Count);
				foreach(AclEntriesEntity entity in aclEntriesEntities) {
					aclEntries.Add(new AclEntry(entity.Resource, entity.Action, entity.Subject, AclEntryValueFromString(entity.Value)));
				}
				return aclEntries.ToArray();
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Retrieves all ACL entries for a subject.
		/// </summary>
		/// <param name="subject">The subject.</param>
		/// <returns>The ACL entries for the subject.</returns>
		private AclEntry[] RetrieveAclEntriesForSubject(string subject) {
			try {
				var query = (from e in _context.CreateQuery<AclEntriesEntity>(AclEntriesTable).AsTableServiceQuery()
							 where e.PartitionKey.Equals(_wiki) && e.Subject.Equals(subject)
							 select e).AsTableServiceQuery();
				IList<AclEntriesEntity> aclEntriesEntities = QueryHelper<AclEntriesEntity>.All(query);

				if(aclEntriesEntities == null) return new AclEntry[0];

				List<AclEntry> aclEntries = new List<AclEntry>(aclEntriesEntities.Count);
				foreach(AclEntriesEntity entity in aclEntriesEntities) {
					aclEntries.Add(new AclEntry(entity.Resource, entity.Action, entity.Subject, AclEntryValueFromString(entity.Value)));
				}
				return aclEntries.ToArray();
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Deletes some ACL entries.
		/// </summary>
		/// <param name="entries">The entries to delete.</param>
		/// <returns><c>true</c> if one or more entries were deleted, <c>false</c> otherwise.</returns>
		private bool DeleteEntries(AclEntry[] entries) {
			try {
				var query = (from e in _context.CreateQuery<AclEntriesEntity>(AclEntriesTable).AsTableServiceQuery()
							 where e.PartitionKey.Equals(_wiki)
							 select e).AsTableServiceQuery();
				IList<AclEntriesEntity> aclEntriesEntities = QueryHelper<AclEntriesEntity>.All(query);

				if(aclEntriesEntities == null || aclEntriesEntities.Count == 0) return false;

				bool deleteExecuted = false;
				foreach(AclEntry aclEntry in entries) {
					foreach(AclEntriesEntity entity in aclEntriesEntities) {
						if(aclEntry.Resource == entity.Resource &&
						   aclEntry.Subject == entity.Subject &&
						   aclEntry.Action == entity.Action) {
								deleteExecuted = true;
								_context.DeleteObject(entity);
						}
					}
				}
				if(deleteExecuted) _context.SaveChangesStandard();
				return deleteExecuted;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Stores a ACL entry.
		/// </summary>
		/// <param name="entry">The entry to store.</param>
		/// <returns><c>true</c> if the entry was stored, <c>false</c> otherwise.</returns>
		private bool StoreEntry(AclEntry entry) {
			try {
				AclEntriesEntity aclEntriesEntity = new AclEntriesEntity() {
					PartitionKey = _wiki,
					RowKey = Guid.NewGuid().ToString("N"),
					Resource = entry.Resource,
					Subject = entry.Subject,
					Action = entry.Action,
					Value = AclEntryValueToString(entry.Value)
				};
				_context.AddObject(AclEntriesTable, aclEntriesEntity);
				_context.SaveChangesStandard();
				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Renames a ACL resource.
		/// </summary>
		/// <param name="resource">The resource to rename.</param>
		/// <param name="newName">The new name of the resource.</param>
		/// <returns><c>true</c> if one or more entries weere updated, <c>false</c> otherwise.</returns>
		private bool RenameAclResource(string resource, string newName) {
			try {
				var query = (from e in _context.CreateQuery<AclEntriesEntity>(AclEntriesTable).AsTableServiceQuery()
							 where e.PartitionKey.Equals(_wiki) && e.Resource.Equals(resource)
							 select e).AsTableServiceQuery();
				IList<AclEntriesEntity> aclEntriesEntities = QueryHelper<AclEntriesEntity>.All(query);

				if(aclEntriesEntities == null || aclEntriesEntities.Count == 0) return false;

				foreach(AclEntriesEntity entity in aclEntriesEntities) {
					entity.Resource = newName;
					_context.UpdateObject(entity);
				}
				_context.SaveChangesStandard();
				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		#endregion
	}

	internal class SettingsEntity : TableServiceEntity {
		// PartitionKey = wiki
		// RowKey = settingKey

		public string Value { get; set; }
	}

	internal class MetadataEntity : TableServiceEntity {
		// PartitionKey = wiki
		// RowKey = item|tag

		public string Content { get; set; }
	}

	internal class RecentChangesEntity : TableServiceEntity {
		// PartitionKey = wiki
		// RowKey = remainder (seconds) + salt

		public string Page { get; set; }
		public string Title { get; set; }
		public string MessageSubject { get; set; }
		public DateTime DateTime { get; set; }
		public string User { get; set; }
		public string Change { get; set; }
		public string Description { get; set; }
	}

	internal class OutgoingLinkEntity : TableServiceEntity {
		// PartitionKey = wiki
		// RowKey = GUID

		public string SourcePage { get; set; }
		public string DestinationPage { get; set; }
	}

	internal class AclEntriesEntity : TableServiceEntity {
		// PartitionKey = wiki
		// RowKey = GUID

		public string Resource { get; set; }
		public string Subject { get; set; }
		public string Action { get; set; }
		public string Value { get; set; }
	}
}
