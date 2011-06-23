
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
	public class AuthReaderTests {

		private MockRepository mocks = null;
		private string testDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());

		[SetUp]
		public void SetUp() {
			mocks = new MockRepository();
			// TODO: Verify if this is really needed
			//Collectors.SettingsProvider = MockProvider();
		}

		[TearDown]
		public void TearDown() {
			try {
				Directory.Delete(testDir, true);
			}
			catch {
				//Console.WriteLine("Test: could not delete temp directory");
			}
			mocks.VerifyAll();
		}

		protected IHostV40 MockHost() {
			if(!Directory.Exists(testDir)) Directory.CreateDirectory(testDir);

			IHostV40 host = mocks.DynamicMock<IHostV40>();
			Expect.Call(host.GetGlobalSettingValue(GlobalSettingName.PublicDirectory)).Return(testDir).Repeat.Any();

			mocks.Replay(host);

			return host;
		}

		private ISettingsStorageProviderV40 MockProvider(List<AclEntry> entries, IAclManager aclManager) {
			DummySettingsStorageProvider settingsProvider = new DummySettingsStorageProvider();
			settingsProvider.SetUp(MockHost(), "");

			settingsProvider.AclManager = aclManager;
			
			foreach(AclEntry entry in entries) {
				aclManager.StoreEntry(entry.Resource, entry.Action, entry.Subject, entry.Value);
			}

			return settingsProvider;
		}

		private ISettingsStorageProviderV40 MockProvider(IAclManager aclManager) {
			return MockProvider(new List<AclEntry>(), aclManager);
		}

		private ISettingsStorageProviderV40 MockProvider() {
			return MockProvider(new List<AclEntry>(), new StandardAclManager());
		}

		[Test]
		public void RetrieveGrantsForGlobals_Group() {
			MockRepository mocks = new MockRepository();

			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			ISettingsStorageProviderV40 settingsProvider = MockProvider(aclManager);
			
			Expect.Call(aclManager.RetrieveEntriesForSubject("G.Group")).Return(
				new AclEntry[] {
					new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageFiles, "G.Group", Value.Deny),
					new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageAccounts, "G.Group", Value.Grant),
					new AclEntry(Actions.ForDirectories.ResourceMasterPrefix + "/", Actions.ForDirectories.UploadFiles, "G.Group", Value.Grant) });

			mocks.Replay(aclManager);

			AuthReader authReader = new AuthReader(settingsProvider);

			string[] grants = authReader.RetrieveGrantsForGlobals(new UserGroup("Group", "Group", null));
			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForGlobals.ManageAccounts, grants[0], "Wrong grant");
			
			mocks.Verify(aclManager);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveGrantsForGlobals_Group_NullGroup() {
			ISettingsStorageProviderV40 settingsProvider = MockProvider();

			AuthReader authReader = new AuthReader(settingsProvider);
			authReader.RetrieveGrantsForGlobals(null as UserGroup);
		}

		[Test]
		public void RetrieveGrantsForGlobals_User() {
			MockRepository mocks = new MockRepository();

			IAclManager aclManager = mocks.DynamicMock<IAclManager>();
			ISettingsStorageProviderV40 settingsProvider = MockProvider(aclManager);

			Expect.Call(aclManager.RetrieveEntriesForSubject("U.User")).Return(
				new AclEntry[] {
		            new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageFiles, "U.User", Value.Deny),
		            new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageAccounts, "U.User", Value.Grant),
		            new AclEntry(Actions.ForDirectories.ResourceMasterPrefix + "/", Actions.ForDirectories.UploadFiles, "U.User", Value.Grant) });

			mocks.Replay(aclManager);

			AuthReader authReader = new AuthReader(settingsProvider);
			string[] grants = authReader.RetrieveGrantsForGlobals(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null));
			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForGlobals.ManageAccounts, grants[0], "Wrong grant");

			mocks.Verify(aclManager);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveGrantsForGlobals_User_NullUser() {
			ISettingsStorageProviderV40 settingsProvider = MockProvider();

			AuthReader authReader = new AuthReader(settingsProvider);
			authReader.RetrieveGrantsForGlobals(null as UserInfo);
		}

		[Test]
		public void RetrieveDenialsForGlobals_Group() {
			MockRepository mocks = new MockRepository();

			IAclManager aclManager = mocks.DynamicMock<IAclManager>();
			ISettingsStorageProviderV40 settingsProvider = MockProvider(aclManager);

			Expect.Call(aclManager.RetrieveEntriesForSubject("G.Group")).Return(
				new AclEntry[] {
		            new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageFiles, "G.Group", Value.Deny),
		            new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageConfiguration, "G.Group", Value.Grant),
		            new AclEntry(Actions.ForDirectories.ResourceMasterPrefix + "/", Actions.ForDirectories.UploadFiles, "G.Group", Value.Deny) });

			mocks.Replay(aclManager);

			AuthReader authReader = new AuthReader(settingsProvider);
			string[] grants = authReader.RetrieveDenialsForGlobals(new UserGroup("Group", "Group", null));
			Assert.AreEqual(1, grants.Length, "Wrong denial count");
			Assert.AreEqual(Actions.ForGlobals.ManageFiles, grants[0], "Wrong denial");

			mocks.Verify(aclManager);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveDenialsForGlobals_Group_NullGroup() {
			ISettingsStorageProviderV40 settingsProvider = MockProvider();

			AuthReader authReader = new AuthReader(settingsProvider);
			authReader.RetrieveDenialsForGlobals(null as UserGroup);
		}

		[Test]
		public void RetrieveDenialsForGlobals_User() {
			MockRepository mocks = new MockRepository();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();
			ISettingsStorageProviderV40 settingsProvider = MockProvider(aclManager);

			Expect.Call(aclManager.RetrieveEntriesForSubject("U.User")).Return(
				new AclEntry[] {
		            new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageFiles, "U.User", Value.Deny),
		            new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageAccounts, "U.User", Value.Grant),
		            new AclEntry(Actions.ForDirectories.ResourceMasterPrefix + "/", Actions.ForDirectories.UploadFiles, "U.User", Value.Deny) });

			mocks.Replay(aclManager);

			AuthReader authReader = new AuthReader(settingsProvider);
			string[] grants = authReader.RetrieveDenialsForGlobals(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null));
			Assert.AreEqual(1, grants.Length, "Wrong denial count");
			Assert.AreEqual(Actions.ForGlobals.ManageFiles, grants[0], "Wrong denial");

			mocks.Verify(aclManager);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveDenialsForGlobals_User_NullUser() {
			AuthReader authReader = new AuthReader(MockProvider());
			authReader.RetrieveDenialsForGlobals(null as UserInfo);
		}

		[Test]
		public void RetrieveSubjectsForNamespace_Root() {
			MockRepository mocks = new MockRepository();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();
			ISettingsStorageProviderV40 settingsProvider = MockProvider(aclManager);

			Expect.Call(aclManager.RetrieveEntriesForResource("N.")).Return(
				new AclEntry[] {
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.DeletePages, "U.User1", Value.Grant),
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ReadPages, "U.User", Value.Grant),
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManagePages, "U.User", Value.Grant),
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManagePages, "G.Group", Value.Deny) });

			mocks.Replay(aclManager);

			AuthReader authReader = new AuthReader(settingsProvider);
			SubjectInfo[] infos = authReader.RetrieveSubjectsForNamespace(null);

			Assert.AreEqual(3, infos.Length, "Wrong info count");

			Array.Sort(infos, delegate(SubjectInfo x, SubjectInfo y) { return x.Name.CompareTo(y.Name); });
			Assert.AreEqual("Group", infos[0].Name, "Wrong subject name");
			Assert.AreEqual(SubjectType.Group, infos[0].Type, "Wrong subject type");
			Assert.AreEqual("User", infos[1].Name, "Wrong subject name");
			Assert.AreEqual(SubjectType.User, infos[1].Type, "Wrong subject type");
			Assert.AreEqual("User1", infos[2].Name, "Wrong subject name");
			Assert.AreEqual(SubjectType.User, infos[2].Type, "Wrong subject type");

			mocks.Verify(aclManager);
		}

		[Test]
		public void RetrieveSubjectsForNamespace_Sub() {
			MockRepository mocks = new MockRepository();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();
			ISettingsStorageProviderV40 settingsProvider = MockProvider(aclManager);

			Expect.Call(aclManager.RetrieveEntriesForResource("N.Sub")).Return(
				new AclEntry[] {
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.DeletePages, "U.User1", Value.Grant),
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ReadPages, "U.User", Value.Grant),
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManagePages, "U.User", Value.Grant),
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManagePages, "G.Group", Value.Deny) });

			mocks.Replay(aclManager);

			AuthReader authReader = new AuthReader(settingsProvider);
			SubjectInfo[] infos = authReader.RetrieveSubjectsForNamespace(new NamespaceInfo("Sub", null, null));

			Assert.AreEqual(3, infos.Length, "Wrong info count");

			Array.Sort(infos, delegate(SubjectInfo x, SubjectInfo y) { return x.Name.CompareTo(y.Name); });
			Assert.AreEqual("Group", infos[0].Name, "Wrong subject name");
			Assert.AreEqual(SubjectType.Group, infos[0].Type, "Wrong subject type");
			Assert.AreEqual("User", infos[1].Name, "Wrong subject name");
			Assert.AreEqual(SubjectType.User, infos[1].Type, "Wrong subject type");
			Assert.AreEqual("User1", infos[2].Name, "Wrong subject name");
			Assert.AreEqual(SubjectType.User, infos[2].Type, "Wrong subject type");

			mocks.Verify(aclManager);
		}

		[Test]
		public void RetrieveGrantsForNamespace_Group_Root() {
			MockRepository mocks = new MockRepository();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();
			ISettingsStorageProviderV40 settingsProvider = MockProvider(aclManager);

			Expect.Call(aclManager.RetrieveEntriesForSubject("G.Group")).Return(
				new AclEntry[] {
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManagePages, "G.Group", Value.Grant),
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManageCategories, "G.Group", Value.Deny),
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub", Actions.ForNamespaces.ManagePages, "G.Group", Value.Grant) });

			mocks.Replay(aclManager);

			AuthReader authReader = new AuthReader(settingsProvider);
			string[] grants = authReader.RetrieveGrantsForNamespace(new UserGroup("Group", "Group", null), null);

			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForNamespaces.ManagePages, grants[0], "Wrong grant");
		}

		[Test]
		public void RetrieveGrantsForNamespace_Group_Sub() {
			MockRepository mocks = new MockRepository();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();
			ISettingsStorageProviderV40 settingProvider = MockProvider(aclManager);

			Expect.Call(aclManager.RetrieveEntriesForSubject("G.Group")).Return(
				new AclEntry[] {
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub", Actions.ForNamespaces.ManagePages, "G.Group", Value.Grant),
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub", Actions.ForNamespaces.ManageCategories, "G.Group", Value.Deny),
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManagePages, "G.Group", Value.Grant) });

			mocks.Replay(aclManager);

			AuthReader authReader = new AuthReader(settingProvider);
			string[] grants = authReader.RetrieveGrantsForNamespace(new UserGroup("Group", "Group", null), new NamespaceInfo("Sub", null, null));

			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForNamespaces.ManagePages, grants[0], "Wrong grant");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveGrantsForNamespace_Group_NullGroup() {
			AuthReader authReader = new AuthReader(MockProvider());
			authReader.RetrieveGrantsForNamespace(null as UserGroup, new NamespaceInfo("Sub", null, null));
		}

		[Test]
		public void RetrieveGrantsForNamespace_User_Root() {
			MockRepository mocks = new MockRepository();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();
			ISettingsStorageProviderV40 prov = MockProvider(aclManager);

			Expect.Call(aclManager.RetrieveEntriesForSubject("U.User")).Return(
				new AclEntry[] {
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManagePages, "U.User", Value.Grant),
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManageCategories, "U.User", Value.Deny),
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub", Actions.ForNamespaces.ManagePages, "U.User", Value.Grant) });

			mocks.Replay(aclManager);

			AuthReader authReader = new AuthReader(prov);
			string[] grants = authReader.RetrieveGrantsForNamespace(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null), null);

			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForNamespaces.ManagePages, grants[0], "Wrong grant");
		}

		[Test]
		public void RetrieveGrantsForNamespace_User_Sub() {
			MockRepository mocks = new MockRepository();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();
			ISettingsStorageProviderV40 prov = MockProvider(aclManager);

			Expect.Call(aclManager.RetrieveEntriesForSubject("U.User")).Return(
				new AclEntry[] {
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub", Actions.ForNamespaces.ManagePages, "U.User", Value.Grant),
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub", Actions.ForNamespaces.ManageCategories, "U.User", Value.Deny),
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManagePages, "U.User", Value.Grant) });

			mocks.Replay(aclManager);

			AuthReader authReader = new AuthReader(prov);
			string[] grants = authReader.RetrieveGrantsForNamespace(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null),
				new NamespaceInfo("Sub", null, null));

			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForNamespaces.ManagePages, grants[0], "Wrong grant");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveGrantsForNamespace_User_NullUser() {
			AuthReader authReader = new AuthReader(MockProvider());
			authReader.RetrieveGrantsForNamespace(null as UserInfo, new NamespaceInfo("Sub", null, null));
		}

		[Test]
		public void RetrieveDenialsForNamespace_Group_Root() {
			MockRepository mocks = new MockRepository();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();
			ISettingsStorageProviderV40 settingsProvider = MockProvider(aclManager);

			Expect.Call(aclManager.RetrieveEntriesForSubject("G.Group")).Return(
				new AclEntry[] {
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManagePages, "G.Group", Value.Deny),
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManageCategories, "G.Group", Value.Grant),
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub", Actions.ForNamespaces.ManagePages, "G.Group", Value.Deny) });

			mocks.Replay(aclManager);

			AuthReader authReader = new AuthReader(settingsProvider);
			string[] grants = authReader.RetrieveDenialsForNamespace(new UserGroup("Group", "Group", null), null);

			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForNamespaces.ManagePages, grants[0], "Wrong grant");
		}

		[Test]
		public void RetrieveDenialsForNamespace_Group_Sub() {
			MockRepository mocks = new MockRepository();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();
			ISettingsStorageProviderV40 settingsProvider = MockProvider(aclManager);

			Expect.Call(aclManager.RetrieveEntriesForSubject("G.Group")).Return(
				new AclEntry[] {
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub", Actions.ForNamespaces.ManagePages, "G.Group", Value.Deny),
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub", Actions.ForNamespaces.ManageCategories, "G.Group", Value.Grant),
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManagePages, "G.Group", Value.Deny) });

			mocks.Replay(aclManager);

			AuthReader authReader = new AuthReader(settingsProvider);
			string[] grants = authReader.RetrieveDenialsForNamespace(new UserGroup("Group", "Group", null), new NamespaceInfo("Sub", null, null));

			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForNamespaces.ManagePages, grants[0], "Wrong grant");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveDenialsForNamespace_Group_NullGroup() {
			AuthReader authReader = new AuthReader(MockProvider());
			authReader.RetrieveDenialsForNamespace(null as UserGroup, new NamespaceInfo("Sub", null, null));
		}

		[Test]
		public void RetrieveDenialsForNamespace_User_Root() {
			MockRepository mocks = new MockRepository();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();
			ISettingsStorageProviderV40 settingsProvider = MockProvider(aclManager);

			Expect.Call(aclManager.RetrieveEntriesForSubject("U.User")).Return(
				new AclEntry[] {
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManagePages, "U.User", Value.Deny),
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManageCategories, "U.User", Value.Grant),
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub", Actions.ForNamespaces.ManagePages, "U.User", Value.Deny) });

			mocks.Replay(aclManager);

			AuthReader authReader = new AuthReader(settingsProvider);
			string[] grants = authReader.RetrieveDenialsForNamespace(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null), null);

			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForNamespaces.ManagePages, grants[0], "Wrong grant");
		}

		[Test]
		public void RetrieveDenialsForNamespace_User_Sub() {
			MockRepository mocks = new MockRepository();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();
			ISettingsStorageProviderV40 settingsProvider = MockProvider(aclManager);

			Expect.Call(aclManager.RetrieveEntriesForSubject("U.User")).Return(
				new AclEntry[] {
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub", Actions.ForNamespaces.ManagePages, "U.User", Value.Deny),
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub", Actions.ForNamespaces.ManageCategories, "U.User", Value.Grant),
		            new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManagePages, "U.User", Value.Deny) });

			mocks.Replay(aclManager);

			AuthReader authReader = new AuthReader(settingsProvider);
			string[] grants = authReader.RetrieveDenialsForNamespace(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null),
				new NamespaceInfo("Sub", null, null));

			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForNamespaces.ManagePages, grants[0], "Wrong grant");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveDenialsForNamespace_User_NullUser() {
			AuthReader authReader = new AuthReader(MockProvider());
			authReader.RetrieveDenialsForNamespace(null as UserInfo, new NamespaceInfo("Sub", null, null));
		}

		[Test]
		public void RetrieveSubjectsForPage() {
			MockRepository mocks = new MockRepository();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();
			ISettingsStorageProviderV40 prov = MockProvider(aclManager);

			Expect.Call(aclManager.RetrieveEntriesForResource("P.NS.Page")).Return(
				new AclEntry[] {
		            new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ModifyPage, "U.User1", Value.Grant),
		            new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ManageCategories, "U.User", Value.Grant),
		            new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.DeleteAttachments, "G.Group", Value.Grant),
		            new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.DeleteAttachments, "U.User", Value.Deny) });

			mocks.Replay(aclManager);

			AuthReader authReader = new AuthReader(prov);
			SubjectInfo[] infos = authReader.RetrieveSubjectsForPage(NameTools.GetFullName(null, "NS.Page"));

			Assert.AreEqual(3, infos.Length, "Wrong info count");

			Array.Sort(infos, delegate(SubjectInfo x, SubjectInfo y) { return x.Name.CompareTo(y.Name); });
			Assert.AreEqual("Group", infos[0].Name, "Wrong subject name");
			Assert.AreEqual(SubjectType.Group, infos[0].Type, "Wrong subject type");
			Assert.AreEqual("User", infos[1].Name, "Wrong subject name");
			Assert.AreEqual(SubjectType.User, infos[1].Type, "Wrong subject type");
			Assert.AreEqual("User1", infos[2].Name, "Wrong subject name");
			Assert.AreEqual(SubjectType.User, infos[2].Type, "Wrong subject type");

			mocks.Verify(aclManager);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveSubjectsForPage_NullPage() {
			AuthReader authReader = new AuthReader(MockProvider());
			authReader.RetrieveSubjectsForPage(null);
		}

		[Test]
		public void RetrieveGrantsForPage_Group() {
			MockRepository mocks = new MockRepository();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();
			ISettingsStorageProviderV40 settingsProvider = MockProvider(aclManager);

			Expect.Call(aclManager.RetrieveEntriesForSubject("G.Group")).Return(
				new AclEntry[] {
		            new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ModifyPage, "G.Group", Value.Grant),
		            new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.FullControl, "G.Group", Value.Deny),
		            new AclEntry(Actions.ForPages.ResourceMasterPrefix + "NS.Blah", Actions.ForPages.ManagePage, "G.Group", Value.Grant)
		        });

			mocks.Replay(aclManager);

			AuthReader authReader = new AuthReader(settingsProvider);
			string[] grants = authReader.RetrieveGrantsForPage(new UserGroup("Group", "Group", null),
				NameTools.GetFullName(null, "Page"));

			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForPages.ModifyPage, grants[0], "Wrong grant");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveGrantsForPage_Group_NullGroup() {
			AuthReader authReader = new AuthReader(MockProvider());
			authReader.RetrieveGrantsForPage(null as UserGroup, NameTools.GetFullName(null, "Page"));
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveGrantsForPage_Group_NullPage() {
			AuthReader authReader = new AuthReader(MockProvider());
			authReader.RetrieveGrantsForPage(new UserGroup("Group", "Group", null), null);
		}

		[Test]
		public void RetrieveGrantsForPage_User() {
			MockRepository mocks = new MockRepository();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();
			ISettingsStorageProviderV40 settingsProvider = MockProvider(aclManager);
			
			Expect.Call(aclManager.RetrieveEntriesForSubject("U.User")).Return(
				new AclEntry[] {
		            new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ModifyPage, "U.User", Value.Grant),
		            new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.FullControl, "U.User", Value.Deny),
		            new AclEntry(Actions.ForPages.ResourceMasterPrefix + "NS.Blah", Actions.ForPages.ManagePage, "U.User", Value.Grant)
		        });

			mocks.Replay(aclManager);

			AuthReader authReader = new AuthReader(settingsProvider);
			string[] grants = authReader.RetrieveGrantsForPage(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null),
				NameTools.GetFullName(null, "Page"));

			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForPages.ModifyPage, grants[0], "Wrong grant");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveGrantsForPage_User_NullUser() {
			AuthReader authReader = new AuthReader(MockProvider());
			authReader.RetrieveGrantsForPage(null as UserInfo, NameTools.GetFullName(null, "Page"));
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveGrantsForPage_User_NullPage() {
			AuthReader authReader = new AuthReader(MockProvider());
			authReader.RetrieveGrantsForPage(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null), null);
		}

		[Test]
		public void RetrieveDenialsForPage_Group() {
			MockRepository mocks = new MockRepository();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();
			ISettingsStorageProviderV40 prov = MockProvider(aclManager);

			Expect.Call(aclManager.RetrieveEntriesForSubject("G.Group")).Return(
				new AclEntry[] {
		            new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ModifyPage, "G.Group", Value.Deny),
		            new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.FullControl, "G.Group", Value.Grant),
		            new AclEntry(Actions.ForPages.ResourceMasterPrefix + "NS.Blah", Actions.ForPages.ManagePage, "G.Group", Value.Deny)
		        });

			mocks.Replay(aclManager);

			AuthReader authReader = new AuthReader(prov);
			string[] grants = authReader.RetrieveDenialsForPage(new UserGroup("Group", "Group", null),
				NameTools.GetFullName(null, "Page"));

			Assert.AreEqual(1, grants.Length, "Wrong denial count");
			Assert.AreEqual(Actions.ForPages.ModifyPage, grants[0], "Wrong denial");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveDenialsForPage_Group_NullGroup() {
			AuthReader authReader = new AuthReader(MockProvider());
			authReader.RetrieveDenialsForPage(null as UserGroup, NameTools.GetFullName(null, "Page"));
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveDenialsForPage_Group_NullPage() {
			AuthReader authReader = new AuthReader(MockProvider());
			authReader.RetrieveDenialsForPage(new UserGroup("Group", "Group", null), null);
		}

		[Test]
		public void RetrieveDenialsForPage_User() {
			MockRepository mocks = new MockRepository();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();
			ISettingsStorageProviderV40 settingsProvider = MockProvider(aclManager);
			
			Expect.Call(aclManager.RetrieveEntriesForSubject("U.User")).Return(
				new AclEntry[] {
		            new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ModifyPage, "U.User", Value.Deny),
		            new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.FullControl, "U.User", Value.Grant),
		            new AclEntry(Actions.ForPages.ResourceMasterPrefix + "NS.Blah", Actions.ForPages.ManagePage, "U.User", Value.Deny)
		        });

			mocks.Replay(aclManager);

			AuthReader authReader = new AuthReader(settingsProvider);
			string[] grants = authReader.RetrieveDenialsForPage(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null),
				NameTools.GetFullName(null, "Page"));

			Assert.AreEqual(1, grants.Length, "Wrong denial count");
			Assert.AreEqual(Actions.ForPages.ModifyPage, grants[0], "Wrong denial");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveDenialsForPage_User_NullUser() {
			AuthReader authReader = new AuthReader(MockProvider());
			authReader.RetrieveDenialsForPage(null as UserInfo, NameTools.GetFullName(null, "Page"));
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveDenialsForPage_User_NullPage() {
			AuthReader authReader = new AuthReader(MockProvider());
			authReader.RetrieveDenialsForPage(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null), null);
		}

		[Test]
		public void RetrieveSubjectsForDirectory_Root() {
			MockRepository mocks = new MockRepository();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();
			ISettingsStorageProviderV40 prov = MockProvider(aclManager);
			IFilesStorageProviderV40 filesProv = mocks.DynamicMock<IFilesStorageProviderV40>();

			string dirName = Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filesProv, "/");
			Expect.Call(aclManager.RetrieveEntriesForResource(dirName)).Return(
				new AclEntry[] {
		            new AclEntry(dirName, Actions.ForDirectories.List, "U.User1", Value.Grant),
		            new AclEntry(dirName, Actions.ForDirectories.UploadFiles, "U.User", Value.Grant),
		            new AclEntry(dirName, Actions.ForDirectories.DownloadFiles, "G.Group", Value.Grant),
		            new AclEntry(dirName, Actions.ForDirectories.CreateDirectories, "U.User", Value.Deny) });

			mocks.Replay(aclManager);

			AuthReader authReader = new AuthReader(prov);
			SubjectInfo[] infos = authReader.RetrieveSubjectsForDirectory(filesProv, "/");

			Assert.AreEqual(3, infos.Length, "Wrong info count");

			Array.Sort(infos, delegate(SubjectInfo x, SubjectInfo y) { return x.Name.CompareTo(y.Name); });
			Assert.AreEqual("Group", infos[0].Name, "Wrong subject name");
			Assert.AreEqual(SubjectType.Group, infos[0].Type, "Wrong subject type");
			Assert.AreEqual("User", infos[1].Name, "Wrong subject name");
			Assert.AreEqual(SubjectType.User, infos[1].Type, "Wrong subject type");
			Assert.AreEqual("User1", infos[2].Name, "Wrong subject name");
			Assert.AreEqual(SubjectType.User, infos[2].Type, "Wrong subject type");

			mocks.Verify(aclManager);
		}

		[Test]
		public void RetrieveSubjectsForDirectory_Sub() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV40 prov = mocks.DynamicMock<ISettingsStorageProviderV40>();
			IFilesStorageProviderV40 filesProv = mocks.DynamicMock<IFilesStorageProviderV40>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			string dirName = Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filesProv, "/Dir/Sub/");
			Expect.Call(aclManager.RetrieveEntriesForResource(dirName)).Return(
				new AclEntry[] {
		            new AclEntry(dirName, Actions.ForDirectories.List, "U.User1", Value.Grant),
		            new AclEntry(dirName, Actions.ForDirectories.UploadFiles, "U.User", Value.Grant),
		            new AclEntry(dirName, Actions.ForDirectories.DownloadFiles, "G.Group", Value.Grant),
		            new AclEntry(dirName, Actions.ForDirectories.CreateDirectories, "U.User", Value.Deny) });

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthReader authReader = new AuthReader(prov);
			SubjectInfo[] infos = authReader.RetrieveSubjectsForDirectory(filesProv, "/Dir/Sub/");

			Assert.AreEqual(3, infos.Length, "Wrong info count");

			Array.Sort(infos, delegate(SubjectInfo x, SubjectInfo y) { return x.Name.CompareTo(y.Name); });
			Assert.AreEqual("Group", infos[0].Name, "Wrong subject name");
			Assert.AreEqual(SubjectType.Group, infos[0].Type, "Wrong subject type");
			Assert.AreEqual("User", infos[1].Name, "Wrong subject name");
			Assert.AreEqual(SubjectType.User, infos[1].Type, "Wrong subject type");
			Assert.AreEqual("User1", infos[2].Name, "Wrong subject name");
			Assert.AreEqual(SubjectType.User, infos[2].Type, "Wrong subject type");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveSubjectsForDirectory_NullProvider() {
			AuthReader authReader = new AuthReader(MockProvider());
			authReader.RetrieveSubjectsForDirectory(null, "/");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RetrieveSubjectsForDirectory_InvalidDirectory(string d) {
			AuthReader authReader = new AuthReader(MockProvider());

			IFilesStorageProviderV40 fProv = mocks.DynamicMock<IFilesStorageProviderV40>();
			mocks.Replay(fProv);
			authReader.RetrieveSubjectsForDirectory(fProv, d);
		}

		[Test]
		public void RetrieveGrantsForDirectory_Root_Group() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV40 prov = mocks.DynamicMock<ISettingsStorageProviderV40>();
			IFilesStorageProviderV40 filesProv = mocks.DynamicMock<IFilesStorageProviderV40>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			string dirName = Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filesProv, "/");
			Expect.Call(aclManager.RetrieveEntriesForSubject("G.Group")).Return(
				new AclEntry[] {
		            new AclEntry(dirName, Actions.ForDirectories.List, "G.Group", Value.Grant),
		            new AclEntry(dirName, Actions.FullControl, "G.Group", Value.Deny),
		            new AclEntry("D." + AuthTools.GetDirectoryName(filesProv, "/Other/"), Actions.ForDirectories.UploadFiles, "G.Group", Value.Grant)
		        });

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthReader authReader = new AuthReader(prov);
			string[] grants = authReader.RetrieveGrantsForDirectory(new UserGroup("Group", "Group", null),
				filesProv, "/");

			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForDirectories.List, grants[0], "Wrong grant");
		}

		[Test]
		public void RetrieveGrantsForDirectory_Sub_Group() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV40 prov = mocks.DynamicMock<ISettingsStorageProviderV40>();
			IFilesStorageProviderV40 filesProv = mocks.DynamicMock<IFilesStorageProviderV40>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			string dirName = Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filesProv, "/Dir/Sub/");
			Expect.Call(aclManager.RetrieveEntriesForSubject("G.Group")).Return(
				new AclEntry[] {
		            new AclEntry(dirName, Actions.ForDirectories.List, "G.Group", Value.Grant),
		            new AclEntry(dirName, Actions.FullControl, "G.Group", Value.Deny),
		            new AclEntry("D." + AuthTools.GetDirectoryName(filesProv, "/"), Actions.ForDirectories.UploadFiles, "G.Group", Value.Grant)
		        });

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthReader authReader = new AuthReader(prov);
			string[] grants = authReader.RetrieveGrantsForDirectory(new UserGroup("Group", "Group", null),
				filesProv, "/Dir/Sub/");

			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForDirectories.List, grants[0], "Wrong grant");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveGrantsForDirectory_Group_NullGroup() {
			AuthReader authReader = new AuthReader(MockProvider());

			IFilesStorageProviderV40 fProv = mocks.DynamicMock<IFilesStorageProviderV40>();
			mocks.Replay(fProv);
			authReader.RetrieveGrantsForDirectory(null as UserGroup, fProv, "/");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveGrantsForDirectory_Group_NullProvider() {
			AuthReader authReader = new AuthReader(MockProvider());

			authReader.RetrieveGrantsForDirectory(new UserGroup("Group", "Group", null), null, "/");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RetrieveGrantsForDirectory_Group_InvalidDirectory(string d) {
			AuthReader authReader = new AuthReader(MockProvider());

			IFilesStorageProviderV40 fProv = mocks.DynamicMock<IFilesStorageProviderV40>();
			mocks.Replay(fProv);
			authReader.RetrieveGrantsForDirectory(new UserGroup("Group", "Group", null), fProv, d);
		}

		[Test]
		public void RetrieveGrantsForDirectory_Root_User() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV40 prov = mocks.DynamicMock<ISettingsStorageProviderV40>();
			IFilesStorageProviderV40 filesProv = mocks.DynamicMock<IFilesStorageProviderV40>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			string dirName = Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filesProv, "/");
			Expect.Call(aclManager.RetrieveEntriesForSubject("U.User")).Return(
				new AclEntry[] {
		            new AclEntry(dirName, Actions.ForDirectories.UploadFiles, "U.User", Value.Grant),
		            new AclEntry(dirName, Actions.FullControl, "U.User", Value.Deny),
		            new AclEntry("D." + AuthTools.GetDirectoryName(filesProv, "/Other/"), Actions.ForDirectories.List, "U.User", Value.Grant)
		        });

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthReader authReader = new AuthReader(prov);
			string[] grants = authReader.RetrieveGrantsForDirectory(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null),
				filesProv, "/");

			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForDirectories.UploadFiles, grants[0], "Wrong grant");
		}

		[Test]
		public void RetrieveGrantsForDirectory_Sub_User() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV40 prov = mocks.DynamicMock<ISettingsStorageProviderV40>();
			IFilesStorageProviderV40 filesProv = mocks.DynamicMock<IFilesStorageProviderV40>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			string dirName = Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filesProv, "/Dir/Sub/");
			Expect.Call(aclManager.RetrieveEntriesForSubject("U.User")).Return(
				new AclEntry[] {
		            new AclEntry(dirName, Actions.ForDirectories.UploadFiles, "U.User", Value.Grant),
		            new AclEntry(dirName, Actions.FullControl, "U.User", Value.Deny),
		            new AclEntry("D." + AuthTools.GetDirectoryName(filesProv, "/"), Actions.ForDirectories.List, "U.User", Value.Grant)
		        });

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthReader authReader = new AuthReader(prov);
			string[] grants = authReader.RetrieveGrantsForDirectory(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null),
				filesProv, "/Dir/Sub/");

			Assert.AreEqual(1, grants.Length, "Wrong grant count");
			Assert.AreEqual(Actions.ForDirectories.UploadFiles, grants[0], "Wrong grant");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveGrantsForDirectory_User_NullUser() {
			AuthReader authReader = new AuthReader(MockProvider());

			IFilesStorageProviderV40 fProv = mocks.DynamicMock<IFilesStorageProviderV40>();
			mocks.Replay(fProv);
			authReader.RetrieveGrantsForDirectory(null as UserInfo, fProv, "/");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveGrantsForDirectory_User_NullProvider() {
			AuthReader authReader = new AuthReader(MockProvider());

			authReader.RetrieveGrantsForDirectory(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null),
				null, "/");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RetrieveGrantsForDirectory_User_InvalidDirectory(string d) {
			AuthReader authReader = new AuthReader(MockProvider());

			IFilesStorageProviderV40 fProv = mocks.DynamicMock<IFilesStorageProviderV40>();
			mocks.Replay(fProv);
			authReader.RetrieveGrantsForDirectory(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null), fProv, d);
		}

		[Test]
		public void RetrieveDenialsForDirectory_Root_Group() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV40 prov = mocks.DynamicMock<ISettingsStorageProviderV40>();
			IFilesStorageProviderV40 filesProv = mocks.DynamicMock<IFilesStorageProviderV40>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			string dirName = Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filesProv, "/");
			Expect.Call(aclManager.RetrieveEntriesForSubject("G.Group")).Return(
				new AclEntry[] {
		            new AclEntry(dirName, Actions.ForDirectories.List, "G.Group", Value.Deny),
		            new AclEntry(dirName, Actions.FullControl, "G.Group", Value.Grant),
		            new AclEntry("D." + AuthTools.GetDirectoryName(filesProv, "/Other/"), Actions.ForDirectories.UploadFiles, "G.Group", Value.Deny)
		        });

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthReader authReader = new AuthReader(prov);
			string[] grants = authReader.RetrieveDenialsForDirectory(new UserGroup("Group", "Group", null),
				filesProv, "/");

			Assert.AreEqual(1, grants.Length, "Wrong denial count");
			Assert.AreEqual(Actions.ForDirectories.List, grants[0], "Wrong denial");
		}

		[Test]
		public void RetrieveDenialsForDirectory_Sub_Group() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV40 prov = mocks.DynamicMock<ISettingsStorageProviderV40>();
			IFilesStorageProviderV40 filesProv = mocks.DynamicMock<IFilesStorageProviderV40>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			string dirName = Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filesProv, "/Dir/Sub/");
			Expect.Call(aclManager.RetrieveEntriesForSubject("G.Group")).Return(
				new AclEntry[] {
		            new AclEntry(dirName, Actions.ForDirectories.List, "G.Group", Value.Deny),
		            new AclEntry(dirName, Actions.FullControl, "G.Group", Value.Grant),
		            new AclEntry("D." + AuthTools.GetDirectoryName(filesProv, "/"), Actions.ForDirectories.UploadFiles, "G.Group", Value.Deny)
		        });

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthReader authReader = new AuthReader(prov);
			string[] grants = authReader.RetrieveDenialsForDirectory(new UserGroup("Group", "Group", null),
				filesProv, "/Dir/Sub/");

			Assert.AreEqual(1, grants.Length, "Wrong denial count");
			Assert.AreEqual(Actions.ForDirectories.List, grants[0], "Wrong denial");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveDenialsForDirectory_Group_NullGroup() {
			AuthReader authReader = new AuthReader(MockProvider());

			IFilesStorageProviderV40 fProv = mocks.DynamicMock<IFilesStorageProviderV40>();
			mocks.Replay(fProv);
			authReader.RetrieveDenialsForDirectory(null as UserGroup, fProv, "/");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveDenialsForDirectory_Group_NullProvider() {
			AuthReader authReader = new AuthReader(MockProvider());
			authReader.RetrieveDenialsForDirectory(new UserGroup("Group", "Group", null), null, "/");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RetrieveDenialsForDirectory_Group_InvalidDirectory(string d) {
			AuthReader authReader = new AuthReader(MockProvider());

			IFilesStorageProviderV40 fProv = mocks.DynamicMock<IFilesStorageProviderV40>();
			mocks.Replay(fProv);
			authReader.RetrieveDenialsForDirectory(new UserGroup("Group", "Group", null), fProv, d);
		}

		[Test]
		public void RetrieveDenialsForDirectory_Root_User() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV40 prov = mocks.DynamicMock<ISettingsStorageProviderV40>();
			IFilesStorageProviderV40 filesProv = mocks.DynamicMock<IFilesStorageProviderV40>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			string dirName = Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filesProv, "/");
			Expect.Call(aclManager.RetrieveEntriesForSubject("U.User")).Return(
				new AclEntry[] {
		            new AclEntry(dirName, Actions.ForDirectories.UploadFiles, "U.User", Value.Deny),
		            new AclEntry(dirName, Actions.FullControl, "U.User", Value.Grant),
		            new AclEntry("D." + AuthTools.GetDirectoryName(filesProv, "/Other/"), Actions.ForDirectories.List, "U.User", Value.Deny)
		        });

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthReader authReader = new AuthReader(prov);
			string[] grants = authReader.RetrieveDenialsForDirectory(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null),
				filesProv, "/");

			Assert.AreEqual(1, grants.Length, "Wrong denial count");
			Assert.AreEqual(Actions.ForDirectories.UploadFiles, grants[0], "Wrong denial");
		}

		[Test]
		public void RetrieveDenialsForDirectory_Sub_User() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV40 prov = mocks.DynamicMock<ISettingsStorageProviderV40>();
			IFilesStorageProviderV40 filesProv = mocks.DynamicMock<IFilesStorageProviderV40>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			string dirName = Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filesProv, "/Dir/Sub/");
			Expect.Call(aclManager.RetrieveEntriesForSubject("U.User")).Return(
				new AclEntry[] {
		            new AclEntry(dirName, Actions.ForDirectories.UploadFiles, "U.User", Value.Deny),
		            new AclEntry(dirName, Actions.FullControl, "U.User", Value.Grant),
		            new AclEntry("D." + AuthTools.GetDirectoryName(filesProv, "/"), Actions.ForDirectories.List, "U.User", Value.Deny)
		        });

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthReader authReader = new AuthReader(prov);
			string[] grants = authReader.RetrieveDenialsForDirectory(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null),
				filesProv, "/Dir/Sub/");

			Assert.AreEqual(1, grants.Length, "Wrong denial count");
			Assert.AreEqual(Actions.ForDirectories.UploadFiles, grants[0], "Wrong denial");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveDenialsForDirectory_User_NullUser() {
			AuthReader authReader = new AuthReader(MockProvider());

			IFilesStorageProviderV40 fProv = mocks.DynamicMock<IFilesStorageProviderV40>();
			mocks.Replay(fProv);
			authReader.RetrieveDenialsForDirectory(null as UserInfo, fProv, "/");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveDenialsForDirectory_User_NullProvider() {
			AuthReader authReader = new AuthReader(MockProvider());
			authReader.RetrieveDenialsForDirectory(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null),
				null, "/");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RetrieveDenialsForDirectory_User_InvalidDirectory(string d) {
			AuthReader authReader = new AuthReader(MockProvider());

			IFilesStorageProviderV40 fProv = mocks.DynamicMock<IFilesStorageProviderV40>();
			mocks.Replay(fProv);
			authReader.RetrieveDenialsForDirectory(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null), fProv, d);
		}

		private class DummySettingsStorageProvider : ISettingsStorageProviderV40 {

			#region ISettingsStorageProviderV40 Members

			public string GetSetting(string name) {
				throw new NotImplementedException();
			}

			public bool SetSetting(string name, string value) {
				throw new NotImplementedException();
			}

			public IDictionary<string, string> GetAllSettings() {
				throw new NotImplementedException();
			}

			public void BeginBulkUpdate() {
				throw new NotImplementedException();
			}

			public void EndBulkUpdate() {
				throw new NotImplementedException();
			}

			public void LogEntry(string message, EntryType entryType, string user) {
				throw new NotImplementedException();
			}

			public LogEntry[] GetLogEntries() {
				throw new NotImplementedException();
			}

			public void ClearLog() {
				throw new NotImplementedException();
			}

			public int LogSize {
				get { throw new NotImplementedException(); }
			}

			public string GetMetaDataItem(MetaDataItem item, string tag) {
				throw new NotImplementedException();
			}

			public bool SetMetaDataItem(MetaDataItem item, string tag, string content) {
				throw new NotImplementedException();
			}

			public RecentChange[] GetRecentChanges() {
				throw new NotImplementedException();
			}

			public bool AddRecentChange(string page, string title, string messageSubject, DateTime dateTime, string user, ScrewTurn.Wiki.PluginFramework.Change change, string descr) {
				throw new NotImplementedException();
			}

			public string[] ListPluginAssemblies() {
				throw new NotImplementedException();
			}

			public bool StorePluginAssembly(string filename, byte[] assembly) {
				throw new NotImplementedException();
			}

			public byte[] RetrievePluginAssembly(string filename) {
				throw new NotImplementedException();
			}

			public bool DeletePluginAssembly(string filename) {
				throw new NotImplementedException();
			}

			public bool SetPluginStatus(string typeName, bool enabled) {
				throw new NotImplementedException();
			}

			public bool GetPluginStatus(string typeName) {
				throw new NotImplementedException();
			}

			public bool SetPluginConfiguration(string typeName, string config) {
				throw new NotImplementedException();
			}

			public string GetPluginConfiguration(string typeName) {
				throw new NotImplementedException();
			}

			private IAclManager aclManager;

			public AclEngine.IAclManager AclManager {
				get { return aclManager; }
				set { aclManager = value; }
			}

			public bool StoreOutgoingLinks(string page, string[] outgoingLinks) {
				throw new NotImplementedException();
			}

			public string[] GetOutgoingLinks(string page) {
				throw new NotImplementedException();
			}

			public IDictionary<string, string[]> GetAllOutgoingLinks() {
				throw new NotImplementedException();
			}

			public bool DeleteOutgoingLinks(string page) {
				throw new NotImplementedException();
			}

			public bool UpdateOutgoingLinksForRename(string oldName, string newName) {
				throw new NotImplementedException();
			}

			public bool IsFirstApplicationStart() {
				throw new NotImplementedException();
			}

			#endregion

			#region IProviderV40 Members

			public string CurrentWiki {
				get { throw new NotImplementedException(); }
			}

			public void Init(IHostV40 host, string config, string wiki) {
				// Nothing TO-DO
			}

			public void SetUp(IHostV40 host, string config) {
				// Nothing TO-DO
			}

			public ComponentInformation Information {
				get { throw new NotImplementedException(); }
			}

			public string ConfigHelpHtml {
				get { throw new NotImplementedException(); }
			}

			#endregion

			#region IDisposable Members

			public void Dispose() {
				throw new NotImplementedException();
			}

			#endregion
		}

	}

}
