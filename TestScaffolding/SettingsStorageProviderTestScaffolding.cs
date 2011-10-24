
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;
using ScrewTurn.Wiki.AclEngine;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki.Tests {

	[TestFixture]
	public abstract class SettingsStorageProviderTestScaffolding {

		private MockRepository mocks = new MockRepository();
		private string testDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());

		private const int MaxLogSize = 8;
		private const int MaxRecentChanges = 20;

		protected IHostV40 MockHost() {
			if(!Directory.Exists(testDir)) Directory.CreateDirectory(testDir);
			//Console.WriteLine("Temp dir: " + testDir);

			IHostV40 host = mocks.DynamicMock<IHostV40>();
			Expect.Call(host.GetGlobalSettingValue(GlobalSettingName.PublicDirectory)).Return(testDir).Repeat.AtLeastOnce();

			Expect.Call(host.GetGlobalSettingValue(GlobalSettingName.LoggingLevel)).Return("3").Repeat.Any();
			Expect.Call(host.GetGlobalSettingValue(GlobalSettingName.MaxLogSize)).Return(MaxLogSize.ToString()).Repeat.Any();
			Expect.Call(host.GetSettingValue(SettingName.MaxRecentChanges)).Constraints(Rhino.Mocks.Constraints.Is.Equal(SettingName.MaxRecentChanges)).Return(MaxRecentChanges.ToString()).Repeat.Any();

			mocks.Replay(host);

			return host;
		}

		[TearDown]
		public void TearDown() {
			try {
				Directory.Delete(testDir, true);
			}
			catch {
				//Console.WriteLine("Test: could not delete temp directory");
			}
		}

		public abstract ISettingsStorageProviderV40 GetProvider();

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Init_NullHost() {
			ISettingsStorageProviderV40 prov = GetProvider();
			prov.Init(null, "", "");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Init_NullConfig() {
			ISettingsStorageProviderV40 prov = GetProvider();
			prov.Init(MockHost(), null, "");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void SetSetting_InvalidName(string n) {
			ISettingsStorageProviderV40 prov = GetProvider();

			prov.SetSetting(n, "blah");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void GetSetting_InvalidName(string n) {
			ISettingsStorageProviderV40 prov = GetProvider();

			prov.GetSetting(n);
		}

		[TestCase(null, "")]
		[TestCase("", "")]
		[TestCase("blah", "blah")]
		[TestCase("with\nnew\nline", "with\nnew\nline")]
		[TestCase("with|pipe", "with|pipe")]
		[TestCase("with<angbrack", "with<angbrack")]
		[TestCase("with>angbrack", "with>angbrack")]
		public void SetSetting_GetSetting(string c, string r) {
			ISettingsStorageProviderV40 prov = GetProvider();

			//Collectors.SettingsProvider = prov;

			Assert.IsTrue(prov.SetSetting("TS", c), "SetSetting should return true");
			Assert.AreEqual(r, prov.GetSetting("TS"), "Wrong return value");
		}

		[Test]
		public void SetSetting_GetAllSettings() {
			ISettingsStorageProviderV40 prov = GetProvider();

			//Collectors.SettingsProvider = prov;

			Assert.IsTrue(prov.SetSetting("TS1", "Value1"), "SetSetting should return true");
			Assert.IsTrue(prov.SetSetting("TS2", "Value2"), "SetSetting should return true");
			Assert.IsTrue(prov.SetSetting("TS3", "Value3"), "SetSetting should return true");

			IDictionary<string, string> settings = prov.GetAllSettings();
			Assert.AreEqual(3, settings.Count, "Wrong setting count");
			Assert.AreEqual("Value1", settings["TS1"], "Wrong setting value");
			Assert.AreEqual("Value2", settings["TS2"], "Wrong setting value");
			Assert.AreEqual("Value3", settings["TS3"], "Wrong setting value");
		}
		
		[TestCase(MetaDataItem.AccountActivationMessage, "Activation mod")]
		[TestCase(MetaDataItem.EditNotice, "Edit notice mod")]
		[TestCase(MetaDataItem.Footer, "Footer mod")]
		[TestCase(MetaDataItem.Header, "Header mod")]
		[TestCase(MetaDataItem.HtmlHead, "HTML head mod")]
		[TestCase(MetaDataItem.LoginNotice, "LN mod")]
		[TestCase(MetaDataItem.AccessDeniedNotice, "AD mod")]
		[TestCase(MetaDataItem.PageFooter, "PF mod")]
		[TestCase(MetaDataItem.PageHeader, "PH mod")]
		[TestCase(MetaDataItem.PasswordResetProcedureMessage, "Password reset mod")]
		[TestCase(MetaDataItem.Sidebar, "Sidebar mod")]
		[TestCase(MetaDataItem.PageChangeMessage, "Page change mod")]
		[TestCase(MetaDataItem.DiscussionChangeMessage, "Discussion change mod")]
		[TestCase(MetaDataItem.ApproveDraftMessage, "Approve draft mod")]
		[TestCase(MetaDataItem.RegisterNotice, "RN mod")]
		public void SetMetaDataItem_GetMetaDataItem(MetaDataItem item, string newContent) {
			ISettingsStorageProviderV40 prov = GetProvider();

			Assert.IsTrue(prov.SetMetaDataItem(item, null, newContent), "SetMetaDataItem should return true");
			Assert.AreEqual(newContent, prov.GetMetaDataItem(item, null), "Wrong content");

			Assert.IsTrue(prov.SetMetaDataItem(item, "Tag", newContent + "Mod"), "SetMetaDataItem should return true");
			Assert.AreEqual(newContent + "Mod", prov.GetMetaDataItem(item, "Tag"), "Wrong content");
		}

		[Test]
		public void SetMetaDataItem_NullContent() {
			ISettingsStorageProviderV40 prov = GetProvider();

			Assert.IsTrue(prov.SetMetaDataItem(MetaDataItem.Header, null, null), "SetMetaDataItem should return true");
			Assert.AreEqual("", prov.GetMetaDataItem(MetaDataItem.Header, null), "Wrong content");

			Assert.IsTrue(prov.SetMetaDataItem(MetaDataItem.Header, "Tag", null), "SetMetaDataItem should return true");
			Assert.AreEqual("", prov.GetMetaDataItem(MetaDataItem.Header, "Tag"), "Wrong content");
		}

		[Test]
		public void GetMetaDataItem_Inexistent() {
			ISettingsStorageProviderV40 prov = GetProvider();

			Assert.AreEqual("", prov.GetMetaDataItem(MetaDataItem.AccountActivationMessage, null), "GetMetaDataItem should return an empty string");
			Assert.AreEqual("", prov.GetMetaDataItem(MetaDataItem.AccountActivationMessage, "BLAH"), "GetMetaDataItem should return an empty string");
		}

		[Test]
		public void AddRecentChange_GetRecentChanges() {
			ISettingsStorageProviderV40 prov = GetProvider();

			//Collectors.SettingsProvider = prov;

			DateTime dt = DateTime.UtcNow;
			Assert.IsTrue(prov.AddRecentChange("MainPage", "Main Page", null, dt, Log.SystemUsername, ScrewTurn.Wiki.PluginFramework.Change.PageUpdated, ""), "AddRecentChange should return true");
			Assert.IsTrue(prov.AddRecentChange("MainPage", "Home Page", null, dt.AddHours(1), "admin", ScrewTurn.Wiki.PluginFramework.Change.PageUpdated, "Added info"), "AddRecentChange should return true");

			Assert.IsTrue(prov.AddRecentChange("MainPage", "Home Page", null, dt.AddHours(5), "admin", ScrewTurn.Wiki.PluginFramework.Change.PageRenamed, ""), "AddRecentChange should return true");
			Assert.IsTrue(prov.AddRecentChange("MainPage", "Main Page", null, dt.AddHours(6), "admin", ScrewTurn.Wiki.PluginFramework.Change.PageRolledBack, ""), "AddRecentChange should return true");
			Assert.IsTrue(prov.AddRecentChange("MainPage", "Main Page", null, dt.AddHours(7), "admin", ScrewTurn.Wiki.PluginFramework.Change.PageDeleted, ""), "AddRecentChange should return true");

			Assert.IsTrue(prov.AddRecentChange("MainPage", "Main Page", "Subject", dt.AddHours(2), "admin", ScrewTurn.Wiki.PluginFramework.Change.MessagePosted, ""), "AddRecentChange should return true");
			Assert.IsTrue(prov.AddRecentChange("MainPage", "Main Page", "Subject", dt.AddHours(3), "admin", ScrewTurn.Wiki.PluginFramework.Change.MessageEdited, ""), "AddRecentChange should return true");
			Assert.IsTrue(prov.AddRecentChange("MainPage", "Main Page", "Subject", dt.AddHours(4), "admin", ScrewTurn.Wiki.PluginFramework.Change.MessageDeleted, ""), "AddRecentChange should return true");

			RecentChange[] changes = prov.GetRecentChanges();

			Assert.AreEqual(8, changes.Length, "Wrong recent change count");

			Assert.AreEqual("MainPage", changes[0].Page, "Wrong page");
			Assert.AreEqual("Main Page", changes[0].Title, "Wrong title");
			Assert.AreEqual("", changes[0].MessageSubject, "Wrong message subject");
			Tools.AssertDateTimesAreEqual(dt, changes[0].DateTime);
			Assert.AreEqual(Log.SystemUsername, changes[0].User, "Wrong user");
			Assert.AreEqual(ScrewTurn.Wiki.PluginFramework.Change.PageUpdated, changes[0].Change, "Wrong change");
			Assert.AreEqual("", changes[0].Description, "Wrong description");

			Assert.AreEqual("MainPage", changes[1].Page, "Wrong page");
			Assert.AreEqual("Home Page", changes[1].Title, "Wrong title");
			Assert.AreEqual("", changes[1].MessageSubject, "Wrong message subject");
			Tools.AssertDateTimesAreEqual(dt.AddHours(1), changes[1].DateTime);
			Assert.AreEqual("admin", changes[1].User, "Wrong user");
			Assert.AreEqual(ScrewTurn.Wiki.PluginFramework.Change.PageUpdated, changes[1].Change, "Wrong change");
			Assert.AreEqual("Added info", changes[1].Description, "Wrong description");

			Assert.AreEqual("MainPage", changes[2].Page, "Wrong page");
			Assert.AreEqual("Main Page", changes[2].Title, "Wrong title");
			Assert.AreEqual("Subject", changes[2].MessageSubject, "Wrong message subject");
			Tools.AssertDateTimesAreEqual(dt.AddHours(2), changes[2].DateTime);
			Assert.AreEqual("admin", changes[2].User, "Wrong user");
			Assert.AreEqual(ScrewTurn.Wiki.PluginFramework.Change.MessagePosted, changes[2].Change, "Wrong change");
			Assert.AreEqual("", changes[2].Description, "Wrong description");

			Assert.AreEqual("MainPage", changes[3].Page, "Wrong page");
			Assert.AreEqual("Main Page", changes[3].Title, "Wrong title");
			Assert.AreEqual("Subject", changes[3].MessageSubject, "Wrong message subject");
			Tools.AssertDateTimesAreEqual(dt.AddHours(3), changes[3].DateTime);
			Assert.AreEqual("admin", changes[3].User, "Wrong user");
			Assert.AreEqual(ScrewTurn.Wiki.PluginFramework.Change.MessageEdited, changes[3].Change, "Wrong change");
			Assert.AreEqual("", changes[3].Description, "Wrong description");

			Assert.AreEqual("MainPage", changes[4].Page, "Wrong page");
			Assert.AreEqual("Main Page", changes[4].Title, "Wrong title");
			Assert.AreEqual("Subject", changes[4].MessageSubject, "Wrong message subject");
			Tools.AssertDateTimesAreEqual(dt.AddHours(4), changes[4].DateTime);
			Assert.AreEqual("admin", changes[4].User, "Wrong user");
			Assert.AreEqual(ScrewTurn.Wiki.PluginFramework.Change.MessageDeleted, changes[4].Change, "Wrong change");
			Assert.AreEqual("", changes[4].Description, "Wrong description");

			Assert.AreEqual("MainPage", changes[5].Page, "Wrong page");
			Assert.AreEqual("Home Page", changes[5].Title, "Wrong title");
			Assert.AreEqual("", changes[5].MessageSubject, "Wrong message subject");
			Tools.AssertDateTimesAreEqual(dt.AddHours(5), changes[5].DateTime);
			Assert.AreEqual("admin", changes[5].User, "Wrong user");
			Assert.AreEqual(ScrewTurn.Wiki.PluginFramework.Change.PageRenamed, changes[5].Change, "Wrong change");
			Assert.AreEqual("", changes[5].Description, "Wrong description");

			Assert.AreEqual("MainPage", changes[6].Page, "Wrong page");
			Assert.AreEqual("Main Page", changes[6].Title, "Wrong title");
			Assert.AreEqual("", changes[6].MessageSubject, "Wrong message subject");
			Tools.AssertDateTimesAreEqual(dt.AddHours(6), changes[6].DateTime);
			Assert.AreEqual("admin", changes[6].User, "Wrong user");
			Assert.AreEqual(ScrewTurn.Wiki.PluginFramework.Change.PageRolledBack, changes[6].Change, "Wrong change");
			Assert.AreEqual("", changes[6].Description, "Wrong description");

			Assert.AreEqual("MainPage", changes[7].Page, "Wrong page");
			Assert.AreEqual("Main Page", changes[7].Title, "Wrong title");
			Assert.AreEqual("", changes[7].MessageSubject, "Wrong message subject");
			Tools.AssertDateTimesAreEqual(dt.AddHours(7), changes[7].DateTime);
			Assert.AreEqual("admin", changes[7].User, "Wrong user");
			Assert.AreEqual(ScrewTurn.Wiki.PluginFramework.Change.PageDeleted, changes[7].Change, "Wrong change");
			Assert.AreEqual("", changes[7].Description, "Wrong description");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void AddRecentChange_InvalidPage(string p) {
			ISettingsStorageProviderV40 prov = GetProvider();

			//Collectors.SettingsProvider = prov;

			prov.AddRecentChange(p, "Title", null, DateTime.Now, "User", ScrewTurn.Wiki.PluginFramework.Change.PageDeleted, "Descr");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void AddRecentChange_InvalidTitle(string t) {
			ISettingsStorageProviderV40 prov = GetProvider();

			//Collectors.SettingsProvider = prov;

			prov.AddRecentChange("Page", t, null, DateTime.Now, "User", ScrewTurn.Wiki.PluginFramework.Change.PageDeleted, "Descr");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void AddRecentChange_InvalidUser(string u) {
			ISettingsStorageProviderV40 prov = GetProvider();

			//Collectors.SettingsProvider = prov;

			prov.AddRecentChange("Page", "Title", null, DateTime.Now, u, ScrewTurn.Wiki.PluginFramework.Change.PageDeleted, "Descr");
		}

		[Test]
		public void AddRecentChange_NullMessageSubject_NullDescription() {
			ISettingsStorageProviderV40 prov = GetProvider();

			//Collectors.SettingsProvider = prov;

			DateTime dt = DateTime.UtcNow;
			Assert.IsTrue(prov.AddRecentChange("Page", "Title", null, dt, "User", ScrewTurn.Wiki.PluginFramework.Change.PageUpdated, null), "AddRecentChange should return true");

			RecentChange c = prov.GetRecentChanges()[0];

			Assert.AreEqual("Page", c.Page, "Wrong page");
			Assert.AreEqual("Title", c.Title, "Wrong title");
			Assert.AreEqual("", c.MessageSubject, "Wrong message subject");
			Tools.AssertDateTimesAreEqual(dt, c.DateTime);
			Assert.AreEqual("User", c.User, "Wrong user");
			Assert.AreEqual(ScrewTurn.Wiki.PluginFramework.Change.PageUpdated, c.Change, "Wrong change");
			Assert.AreEqual("", c.Description, "Wrong description");
		}

		[Test]
		public void AddRecentChange_CutRecentChanges() {
			ISettingsStorageProviderV40 prov = GetProvider();

			//Collectors.SettingsProvider = prov;

			for(int i = 0; i < MaxRecentChanges + 8; i++) {
				Assert.IsTrue(prov.AddRecentChange("MainPage", "Main Page", null, DateTime.Now, Log.SystemUsername, ScrewTurn.Wiki.PluginFramework.Change.PageUpdated, ""), "AddRecentChange should return true");
			}

			RecentChange[] changes = prov.GetRecentChanges();

			Assert.IsTrue(changes.Length > 0 && changes.Length <= MaxRecentChanges, "Wrong recent change count (" + changes.Length.ToString() + ")");
		}

		[Test]
		public void AclManager_StoreEntry_RetrieveAllEntries_DeleteEntry() {
			ISettingsStorageProviderV40 prov = GetProvider();
			//Collectors.SettingsProvider = prov;

			Assert.IsTrue(prov.AclManager.StoreEntry("Res", "Action", "U.User", Value.Grant), "StoreEntry should return true");

			prov = null;
			prov = GetProvider();
			//Collectors.SettingsProvider = prov;

			AclEntry[] entries = prov.AclManager.RetrieveAllEntries();
			Assert.AreEqual(1, entries.Length, "Wrong entry count");

			Assert.AreEqual("Res", entries[0].Resource, "Wrong resource");
			Assert.AreEqual("Action", entries[0].Action, "Wrong action");
			Assert.AreEqual("U.User", entries[0].Subject, "Wrong subject");
			Assert.AreEqual(Value.Grant, entries[0].Value, "Wrong value");

			prov = null;
			prov = GetProvider();
			//Collectors.SettingsProvider = prov;

			Assert.IsTrue(prov.AclManager.RenameResource("Res", "NewName"), "RenameResource should return true");

			entries = prov.AclManager.RetrieveAllEntries();
			Assert.AreEqual(1, entries.Length, "Wrong entry count");

			Assert.AreEqual("NewName", entries[0].Resource, "Wrong resource");
			Assert.AreEqual("Action", entries[0].Action, "Wrong action");
			Assert.AreEqual("U.User", entries[0].Subject, "Wrong subject");
			Assert.AreEqual(Value.Grant, entries[0].Value, "Wrong value");

			prov = null;
			prov = GetProvider();
			//Collectors.SettingsProvider = prov;

			Assert.IsTrue(prov.AclManager.DeleteEntry("NewName", "Action", "U.User"), "DeleteEntry should return true");

			prov = null;
			prov = GetProvider();
			//Collectors.SettingsProvider = prov;

			Assert.AreEqual(0, prov.AclManager.RetrieveAllEntries().Length, "Wrong entry count");
		}

		[Test]
		public void StoreOutgoingLinks_GetOutgoingLinks() {
			ISettingsStorageProviderV40 prov = GetProvider();

			Assert.AreEqual(0, prov.GetOutgoingLinks("Page").Length, "Wrong initial link count");

			Assert.IsTrue(prov.StoreOutgoingLinks("Page", new string[] { "Page2", "Sub.Page", "Page3" }), "StoreOutgoingLinks should return true");

			string[] links = prov.GetOutgoingLinks("Page");

			Assert.AreEqual(3, links.Length, "Wrong link count");

			Array.Sort(links);
			Assert.AreEqual("Page2", links[0], "Wrong link");
			Assert.AreEqual("Page3", links[1], "Wrong link");
			Assert.AreEqual("Sub.Page", links[2], "Wrong link");
		}

		[Test]
		public void StoreOutgoingLinks_GetOutgoingLinks_Overwrite() {
			ISettingsStorageProviderV40 prov = GetProvider();

			Assert.AreEqual(0, prov.GetOutgoingLinks("Page").Length, "Wrong initial link count");

			Assert.IsTrue(prov.StoreOutgoingLinks("Page", new string[] { "Page1", "Sub.Page1", "Page5" }), "StoreOutgoingLinks should return true");
			Assert.IsTrue(prov.StoreOutgoingLinks("Page", new string[] { "Page2", "Sub.Page", "Page3" }), "StoreOutgoingLinks should return true");

			string[] links = prov.GetOutgoingLinks("Page");

			Assert.AreEqual(3, links.Length, "Wrong link count");

			Array.Sort(links);
			Assert.AreEqual("Page2", links[0], "Wrong link");
			Assert.AreEqual("Page3", links[1], "Wrong link");
			Assert.AreEqual("Sub.Page", links[2], "Wrong link");
		}

		[Test]
		public void StoreOutgoingLinks_GetOutgoingLinks_EmptyLinks() {
			ISettingsStorageProviderV40 prov = GetProvider();

			Assert.IsTrue(prov.StoreOutgoingLinks("Page", new string[0]), "StoreOutgoingLinks should return true even if outgoingLinks is empty");
			Assert.AreEqual(0, prov.GetOutgoingLinks("Page").Length, "Wrong link count");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void StoreOutgoingLinks_InvalidPage(string p) {
			ISettingsStorageProviderV40 prov = GetProvider();

			prov.StoreOutgoingLinks(p, new string[] { "P1", "P2" });
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void StoreOutgoingLinks_NullLinks() {
			ISettingsStorageProviderV40 prov = GetProvider();

			prov.StoreOutgoingLinks("Page", null);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void StoreOutgoingLinks_InvalidLinksEntry(string e) {
			ISettingsStorageProviderV40 prov = GetProvider();

			prov.StoreOutgoingLinks("Page", new string[] { "P1", e, "P3" });
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void GetOutgoingLinks_InvalidPage(string p) {
			ISettingsStorageProviderV40 prov = GetProvider();

			prov.GetOutgoingLinks(p);
		}

		[Test]
		public void GetAllOutgoingLinks() {
			ISettingsStorageProviderV40 prov = GetProvider();

			prov.StoreOutgoingLinks("Page1", new string[] { "Page2", "Page3" });
			prov.StoreOutgoingLinks("Page2", new string[] { "Page4", "Page5", "Page6" });
			prov.StoreOutgoingLinks("Page3", new string[] { "Page2", "Page5" });

			IDictionary<string, string[]> links = prov.GetAllOutgoingLinks();

			Assert.AreEqual(3, links.Count, "Wrong source page count");

			Assert.AreEqual(2, links["Page1"].Length, "Wrong link count");
			Array.Sort(links["Page1"]);
			Assert.AreEqual("Page2", links["Page1"][0], "Wrong link");
			Assert.AreEqual("Page3", links["Page1"][1], "Wrong link");

			Assert.AreEqual(3, links["Page2"].Length, "Wrong link count");
			Array.Sort(links["Page2"]);
			Assert.AreEqual("Page4", links["Page2"][0], "Wrong link");
			Assert.AreEqual("Page5", links["Page2"][1], "Wrong link");
			Assert.AreEqual("Page6", links["Page2"][2], "Wrong link");

			Assert.AreEqual(2, links["Page3"].Length, "Wrong link count");
			Array.Sort(links["Page3"]);
			Assert.AreEqual("Page2", links["Page3"][0], "Wrong link");
			Assert.AreEqual("Page5", links["Page3"][1], "Wrong link");
		}

		[Test]
		public void DeleteOutgoingLinks() {
			ISettingsStorageProviderV40 prov = GetProvider();

			prov.StoreOutgoingLinks("Page1", new string[] { "Page2", "Page3", "Page100" });
			prov.StoreOutgoingLinks("Page2", new string[] { "Page4", "Page5", "Page6" });
			prov.StoreOutgoingLinks("Page3", new string[] { "Page1", "Page6" });

			Assert.IsFalse(prov.DeleteOutgoingLinks("Page21"), "DeleteOutgoingLinks should return false");
			Assert.IsTrue(prov.DeleteOutgoingLinks("Page100"), "DeleteOutgoingLinks should return true");

			Assert.IsTrue(prov.DeleteOutgoingLinks("Page1"), "DeleteOutgoingLinks should return true");

			Assert.AreEqual(0, prov.GetOutgoingLinks("Page1").Length, "Links not deleted");

			Assert.AreEqual(3, prov.GetOutgoingLinks("Page2").Length, "Wrong link count");
			Assert.AreEqual(1, prov.GetOutgoingLinks("Page3").Length, "Wrong link count");

			IDictionary<string, string[]> links = prov.GetAllOutgoingLinks();
			Assert.AreEqual(2, links.Count, "Wrong source page count");
			
			Assert.AreEqual(3, links["Page2"].Length, "Wrong link count");
			Array.Sort(links["Page2"]);
			Assert.AreEqual("Page4", links["Page2"][0], "Wrong link");
			Assert.AreEqual("Page5", links["Page2"][1], "Wrong link");
			Assert.AreEqual("Page6", links["Page2"][2], "Wrong link");

			Assert.AreEqual(1, links["Page3"].Length, "Wrong link count");
			Assert.AreEqual("Page6", links["Page3"][0], "Wrong link");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void DeleteOutgoingLinks_InvalidPage(string p) {
			ISettingsStorageProviderV40 prov = GetProvider();

			prov.DeleteOutgoingLinks(p);
		}

		[Test]
		public void UpdateOutgoingLinksForRename() {
			ISettingsStorageProviderV40 prov = GetProvider();

			prov.StoreOutgoingLinks("Page1", new string[] { "Page2", "OldPage" });
			prov.StoreOutgoingLinks("Page2", new string[] { "Page4", "Page5", "Page6" });
			prov.StoreOutgoingLinks("OldPage", new string[] { "Page2", "Page5" });

			Assert.IsFalse(prov.UpdateOutgoingLinksForRename("Inexistent", "NewName"), "UpdateOutgoingLinksForRename should return false");

			Assert.IsTrue(prov.UpdateOutgoingLinksForRename("OldPage", "Page3"), "UpdateOutgoingLinksForRename should return true");

			IDictionary<string, string[]> links = prov.GetAllOutgoingLinks();

			Assert.AreEqual(3, links.Count, "Wrong source page count");

			Assert.AreEqual(2, links["Page1"].Length, "Wrong link count");
			Array.Sort(links["Page1"]);
			Assert.AreEqual("Page2", links["Page1"][0], "Wrong link");
			Assert.AreEqual("Page3", links["Page1"][1], "Wrong link");

			Assert.AreEqual(3, links["Page2"].Length, "Wrong link count");
			Array.Sort(links["Page2"]);
			Assert.AreEqual("Page4", links["Page2"][0], "Wrong link");
			Assert.AreEqual("Page5", links["Page2"][1], "Wrong link");
			Assert.AreEqual("Page6", links["Page2"][2], "Wrong link");

			Assert.AreEqual(2, links["Page3"].Length, "Wrong link count");
			Array.Sort(links["Page3"]);
			Assert.AreEqual("Page2", links["Page3"][0], "Wrong link");
			Assert.AreEqual("Page5", links["Page3"][1], "Wrong link");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void UpdateOutgoingLinksForRename_InvalidOldName(string n) {
			ISettingsStorageProviderV40 prov = GetProvider();

			prov.UpdateOutgoingLinksForRename(n, "NewName");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void UpdateOutgoingLinksForRename_InvalidNewName(string n) {
			ISettingsStorageProviderV40 prov = GetProvider();

			prov.UpdateOutgoingLinksForRename("OldName", n);
		}


		[Test]
		public void SetPluginStatus_RetrievePluginStatus() {
			ISettingsStorageProviderV40 prov = GetProvider();
			//Collectors.SettingsProvider = prov;

			Assert.IsTrue(prov.GetPluginStatus("My.Test.Plugin"), "GetPluginStatus should return true");

			Assert.IsTrue(prov.SetPluginStatus("My.Test.Plugin", true), "SetPluginStatus should return true");

			Assert.IsTrue(prov.GetPluginStatus("My.Test.Plugin"), "GetPluginStatus should return true");

			Assert.IsTrue(prov.SetPluginStatus("My.Test.Plugin", false), "SetPluginStatus should return true");

			Assert.IsFalse(prov.GetPluginStatus("My.Test.Plugin"), "GetPluginStatus should return false");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void SetPluginStatus_InvalidTypeName(string tn) {
			ISettingsStorageProviderV40 prov = GetProvider();
			//Collectors.SettingsProvider = prov;

			prov.SetPluginStatus(tn, false);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void GetPluginStatus_InvalidTypeName(string tn) {
			ISettingsStorageProviderV40 prov = GetProvider();
			//Collectors.SettingsProvider = prov;

			prov.GetPluginStatus(tn);
		}

		[Test]
		public void SetPluginConfiguration_GetPluginConfiguration() {
			ISettingsStorageProviderV40 prov = GetProvider();
			//Collectors.SettingsProvider = prov;

			Assert.IsEmpty(prov.GetPluginConfiguration("My.Test.Plugin"), "GetPluginConfiguration should return an empty string");

			Assert.IsTrue(prov.SetPluginConfiguration("My.Test.Plugin", "config"), "SetPluginConfiguration should return true");

			Assert.AreEqual("config", prov.GetPluginConfiguration("My.Test.Plugin"), "Wrong config");

			Assert.IsTrue(prov.SetPluginConfiguration("My.Test.Plugin", "config222"), "SetPluginConfiguration should return true");

			Assert.AreEqual("config222", prov.GetPluginConfiguration("My.Test.Plugin"), "Wrong config");

			Assert.IsTrue(prov.SetPluginConfiguration("My.Test.Plugin", ""), "SetPluginConfiguration should return true");

			Assert.AreEqual("", prov.GetPluginConfiguration("My.Test.Plugin"), "Wrong config");

			Assert.IsTrue(prov.SetPluginConfiguration("My.Test.Plugin", null), "SetPluginConfiguration should return true");

			Assert.AreEqual("", prov.GetPluginConfiguration("My.Test.Plugin"), "Wrong config");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void SetPluginConfiguration_InvalidTypeName(string tn) {
			ISettingsStorageProviderV40 prov = GetProvider();
			//Collectors.SettingsProvider = prov;

			prov.SetPluginConfiguration(tn, "config");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void GetPluginConfiguration_InvalidTypeName(string tn) {
			ISettingsStorageProviderV40 prov = GetProvider();
			//Collectors.SettingsProvider = prov;

			prov.GetPluginConfiguration(tn);
		}

	}

}
