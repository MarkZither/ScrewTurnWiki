
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki.AclEngine;

namespace ScrewTurn.Wiki.Tests {

	[TestFixture]
	public class AuthWriterTests {

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

		protected IHostV30 MockHost() {
			if(!Directory.Exists(testDir)) Directory.CreateDirectory(testDir);

			IHostV30 host = mocks.DynamicMock<IHostV30>();
			Expect.Call(host.GetGlobalSettingValue(GlobalSettingName.PublicDirectory)).Return(testDir).Repeat.Any();

			mocks.Replay(host);

			return host;
		}

		private ISettingsStorageProviderV30 MockProvider(List<AclEntry> entries) {
			ISettingsStorageProviderV30 provider = mocks.DynamicMock<ISettingsStorageProviderV30>();
			provider.Init(MockHost(), "", null);
			LastCall.On(provider).Repeat.Any();

			AclManagerBase aclManager = new StandardAclManager();
			Expect.Call(provider.AclManager).Return(aclManager).Repeat.Any();

			mocks.Replay(provider);

			foreach(AclEntry entry in entries) {
				aclManager.StoreEntry(entry.Resource, entry.Action, entry.Subject, entry.Value);
			}

			return provider;
		}

		private ISettingsStorageProviderV30 MockProvider() {
			return MockProvider(new List<AclEntry>());
		}

		private IFilesStorageProviderV30 MockFilesProvider() {
			IFilesStorageProviderV30 prov = mocks.DynamicMock<IFilesStorageProviderV30>();
			mocks.Replay(prov);
			return prov;
		}

		[Test]
		public void SetPermissionForGlobals_Group_Grant() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.StoreEntry(Actions.ForGlobals.ResourceMasterPrefix,
				Actions.ForGlobals.ManageAccounts, "G.Group", Value.Grant)).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.SetPermissionForGlobals(AuthStatus.Grant, Actions.ForGlobals.ManageAccounts,
				new UserGroup("Group", "Descr", null)), "SetPermissionForGlobals should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		public void SetPermissionForGlobals_Group_Deny() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.StoreEntry(Actions.ForGlobals.ResourceMasterPrefix,
				Actions.ForGlobals.ManageProviders, "G.Group", Value.Deny)).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.SetPermissionForGlobals(AuthStatus.Deny, Actions.ForGlobals.ManageProviders,
				new UserGroup("Group", "Descr", null)), "SetPermissionForGlobals should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		public void SetPermissionForGlobals_Group_Delete() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.DeleteEntry(Actions.ForGlobals.ResourceMasterPrefix,
				Actions.ForGlobals.ManageMetaFiles, "G.Group")).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.SetPermissionForGlobals(AuthStatus.Delete, Actions.ForGlobals.ManageMetaFiles,
				new UserGroup("Group", "Descr", null)), "SetPermissionForGlobals should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		[TestCase("Blah", ExpectedException = typeof(ArgumentException))]
		[TestCase("*")]
		public void SetPermissionForGlobals_Group_InvalidAction(string a) {
			AuthWriter authWriter = new AuthWriter(MockProvider());
			authWriter.SetPermissionForGlobals(AuthStatus.Grant, a, new UserGroup("Group", "Desc", null));
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void SetPermissionForGlobals_Group_NullGroup() {
			AuthWriter authWriter = new AuthWriter(MockProvider());
			authWriter.SetPermissionForGlobals(AuthStatus.Grant, Actions.ForGlobals.ManageAccounts, null as UserGroup);
		}

		[Test]
		public void SetPermissionForGlobals_User_Grant() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.StoreEntry(Actions.ForGlobals.ResourceMasterPrefix,
				Actions.ForGlobals.ManageAccounts, "U.User", Value.Grant)).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.SetPermissionForGlobals(AuthStatus.Grant, Actions.ForGlobals.ManageAccounts,
				new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null)),
				"SetPermissionForGlobals should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		public void SetPermissionForGlobals_User_Deny() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.StoreEntry(Actions.ForGlobals.ResourceMasterPrefix,
				Actions.ForGlobals.ManageProviders, "U.User", Value.Deny)).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.SetPermissionForGlobals(AuthStatus.Deny, Actions.ForGlobals.ManageProviders,
				new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null)),
				"SetPermissionForGlobals should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		public void SetPermissionForGlobals_User_Delete() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.DeleteEntry(Actions.ForGlobals.ResourceMasterPrefix,
				Actions.ForGlobals.ManageMetaFiles, "U.User")).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.SetPermissionForGlobals(AuthStatus.Delete, Actions.ForGlobals.ManageMetaFiles,
				new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null)),
				"SetPermissionForGlobals should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		[TestCase("Blah", ExpectedException = typeof(ArgumentException))]
		[TestCase("*")]
		public void SetPermissionForGlobals_User_InvalidAction(string a) {
			ISettingsStorageProviderV30 prov = MockProvider();

			AuthWriter authWriter = new AuthWriter(prov);
			authWriter.SetPermissionForGlobals(AuthStatus.Grant, a,
				new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null));
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void SetPermissionForGlobals_User_NullUser() {
			ISettingsStorageProviderV30 prov = MockProvider();

			AuthWriter authWriter = new AuthWriter(prov);
			authWriter.SetPermissionForGlobals(AuthStatus.Grant, Actions.ForGlobals.ManageAccounts, null as UserInfo);
		}

		[Test]
		public void SetPermissionForNamespace_Group_Grant() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.StoreEntry(Actions.ForNamespaces.ResourceMasterPrefix,
				Actions.ForNamespaces.CreatePages, "G.Group", Value.Grant)).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.SetPermissionForNamespace(AuthStatus.Grant, null,
				Actions.ForNamespaces.CreatePages, new UserGroup("Group", "Descr", null)),
				"SetPermissionForNamespace should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		public void SetPermissionForNamespace_Group_Deny() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.StoreEntry(Actions.ForNamespaces.ResourceMasterPrefix,
				Actions.ForNamespaces.ModifyPages, "G.Group", Value.Deny)).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.SetPermissionForNamespace(AuthStatus.Deny, null,
				Actions.ForNamespaces.ModifyPages, new UserGroup("Group", "Descr", null)),
				"SetPermissionForNamespace should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		public void SetPermissionForNamespace_Group_Delete() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.DeleteEntry(Actions.ForNamespaces.ResourceMasterPrefix + "NS",
				Actions.ForNamespaces.ReadPages, "G.Group")).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.SetPermissionForNamespace(AuthStatus.Delete, new NamespaceInfo("NS", null, null),
				Actions.ForNamespaces.ReadPages, new UserGroup("Group", "Descr", null)),
				"SetPermissionForNamespace should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		[TestCase("Blah", ExpectedException = typeof(ArgumentException))]
		[TestCase("*")]
		public void SetPermissionForNamespace_Group_InvalidAction(string a) {
			ISettingsStorageProviderV30 prov = MockProvider();

			AuthWriter authWriter = new AuthWriter(prov);
			authWriter.SetPermissionForNamespace(AuthStatus.Grant, null, a, new UserGroup("Group", "Descr", null));
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void SetPermissionForNamespace_Group_NullGroup() {
			ISettingsStorageProviderV30 prov = MockProvider();

			AuthWriter authWriter = new AuthWriter(prov);
			authWriter.SetPermissionForNamespace(AuthStatus.Grant, null, Actions.ForNamespaces.ModifyPages, null as UserGroup);
		}

		[Test]
		public void SetPermissionForNamespace_User_Grant() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.StoreEntry(Actions.ForNamespaces.ResourceMasterPrefix,
				Actions.ForNamespaces.CreatePages, "U.User", Value.Grant)).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.SetPermissionForNamespace(AuthStatus.Grant, null,
				Actions.ForNamespaces.CreatePages, new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null)),
				"SetPermissionForNamespace should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		public void SetPermissionForNamespace_User_Deny() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.StoreEntry(Actions.ForNamespaces.ResourceMasterPrefix,
				Actions.ForNamespaces.ModifyPages, "U.User", Value.Deny)).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.SetPermissionForNamespace(AuthStatus.Deny, null,
				Actions.ForNamespaces.ModifyPages, new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null)),
				"SetPermissionForNamespace should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		public void SetPermissionForNamespace_User_Delete() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.DeleteEntry(Actions.ForNamespaces.ResourceMasterPrefix + "NS",
				Actions.ForNamespaces.ReadPages, "U.User")).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.SetPermissionForNamespace(AuthStatus.Delete, new NamespaceInfo("NS", null, null),
				Actions.ForNamespaces.ReadPages, new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null)),
				"SetPermissionForNamespace should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		[TestCase("Blah", ExpectedException = typeof(ArgumentException))]
		[TestCase("*")]
		public void SetPermissionForNamespace_User_InvalidAction(string a) {
			ISettingsStorageProviderV30 prov = MockProvider();

			AuthWriter authWriter = new AuthWriter(prov);
			authWriter.SetPermissionForNamespace(AuthStatus.Grant, null, a,
				new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null));
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void SetPermissionForNamespace_User_NullUser() {
			ISettingsStorageProviderV30 prov = MockProvider();

			AuthWriter authWriter = new AuthWriter(prov);
			authWriter.SetPermissionForNamespace(AuthStatus.Grant, null, Actions.ForNamespaces.ModifyPages, null as UserInfo);
		}

		[Test]
		public void SetPermissionForDirectory_Group_Grant() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IFilesStorageProviderV30 filesProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.StoreEntry(
				Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filesProv, "/"),
				Actions.ForDirectories.DownloadFiles, "G.Group", Value.Grant)).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.SetPermissionForDirectory(AuthStatus.Grant, filesProv, "/", Actions.ForDirectories.DownloadFiles,
				new UserGroup("Group", "Group", null)), "SetPermissionForDirectory should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		public void SetPermissionForDirectory_Group_Deny() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IFilesStorageProviderV30 filesProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.StoreEntry(
				Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filesProv, "/"),
				Actions.ForDirectories.CreateDirectories, "G.Group", Value.Deny)).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.SetPermissionForDirectory(AuthStatus.Deny, filesProv, "/", Actions.ForDirectories.CreateDirectories,
				new UserGroup("Group", "Group", null)), "SetPermissionForDirectory should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		public void SetPermissionForDirectory_Group_Delete() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IFilesStorageProviderV30 filesProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.DeleteEntry(Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filesProv, "/Sub/"),
				Actions.ForDirectories.List, "G.Group")).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.SetPermissionForDirectory(AuthStatus.Delete, filesProv, "/Sub/", Actions.ForDirectories.List,
				new UserGroup("Group", "Group", null)), "SetPermissionForDirectory should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void SetPermissionForDirectory_Group_NullProvider() {
			ISettingsStorageProviderV30 prov = MockProvider();

			AuthWriter authWriter = new AuthWriter(prov);
			authWriter.SetPermissionForDirectory(AuthStatus.Grant, null, "/", Actions.ForDirectories.DeleteFiles, new UserGroup("Group", "Group", null));
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void SetPermissionForDirectory_Group_InvalidDirectory(string d) {
			ISettingsStorageProviderV30 prov = MockProvider();
			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			AuthWriter authWriter = new AuthWriter(prov);
			authWriter.SetPermissionForDirectory(AuthStatus.Grant, filesProv, d, Actions.ForDirectories.DownloadFiles, new UserGroup("Group", "Group", null));
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		[TestCase("Blah", ExpectedException = typeof(ArgumentException))]
		[TestCase("*")]
		public void SetPermissionForDirectory_Group_InvalidAction(string a) {
			ISettingsStorageProviderV30 prov = MockProvider();
			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			AuthWriter authWriter = new AuthWriter(prov);
			authWriter.SetPermissionForDirectory(AuthStatus.Grant, filesProv, "/", a, new UserGroup("Group", "Group", null));
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void SetPermissionForDirectory_Group_NullGroup() {
			ISettingsStorageProviderV30 prov = MockProvider();
			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			AuthWriter authWriter = new AuthWriter(prov);
			authWriter.SetPermissionForDirectory(AuthStatus.Grant, filesProv, "/", Actions.ForDirectories.DownloadFiles, null as UserGroup);
		}

		[Test]
		public void SetPermissionForDirectory_User_Grant() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IFilesStorageProviderV30 filesProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.StoreEntry(
				Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filesProv, "/"),
				Actions.ForDirectories.DownloadFiles, "U.User", Value.Grant)).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.SetPermissionForDirectory(AuthStatus.Grant, filesProv, "/", Actions.ForDirectories.DownloadFiles,
				new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null)),
				"SetPermissionForDirectory should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		public void SetPermissionForDirectory_User_Deny() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IFilesStorageProviderV30 filesProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.StoreEntry(
				Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filesProv, "/"),
				Actions.ForDirectories.CreateDirectories, "U.User", Value.Deny)).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.SetPermissionForDirectory(AuthStatus.Deny, filesProv, "/", Actions.ForDirectories.CreateDirectories,
				new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null)),
				"SetPermissionForDirectory should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		public void SetPermissionForDirectory_User_Delete() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IFilesStorageProviderV30 filesProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.DeleteEntry(Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filesProv, "/Sub/"),
				Actions.ForDirectories.List, "U.User")).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.SetPermissionForDirectory(AuthStatus.Delete, filesProv, "/Sub/", Actions.ForDirectories.List,
				new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null)),
				"SetPermissionForDirectory should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void SetPermissionForDirectory_User_NullProvider() {
			ISettingsStorageProviderV30 prov = MockProvider();

			AuthWriter authWriter = new AuthWriter(prov);
			authWriter.SetPermissionForDirectory(AuthStatus.Grant, null, "/", Actions.ForDirectories.DeleteFiles,
				new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null));
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void SetPermissionForDirectory_User_InvalidDirectory(string d) {
			ISettingsStorageProviderV30 prov = MockProvider();
			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			AuthWriter authWriter = new AuthWriter(prov);
			authWriter.SetPermissionForDirectory(AuthStatus.Grant, filesProv, d, Actions.ForDirectories.DownloadFiles,
				new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null));
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		[TestCase("Blah", ExpectedException = typeof(ArgumentException))]
		[TestCase("*")]
		public void SetPermissionForDirectory_User_InvalidAction(string a) {
			ISettingsStorageProviderV30 prov = MockProvider();
			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			AuthWriter authWriter = new AuthWriter(prov);
			authWriter.SetPermissionForDirectory(AuthStatus.Grant, filesProv, "/", a,
				new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null));
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void SetPermissionForDirectory_User_NullUser() {
			ISettingsStorageProviderV30 prov = MockProvider();
			IFilesStorageProviderV30 filesProv = MockFilesProvider();

			AuthWriter authWriter = new AuthWriter(prov);
			authWriter.SetPermissionForDirectory(AuthStatus.Grant, filesProv, "/", Actions.ForDirectories.DownloadFiles, null as UserInfo);
		}

		[Test]
		public void SetPermissionForPage_Group_Grant() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.StoreEntry(Actions.ForPages.ResourceMasterPrefix + "Page",
				Actions.ForPages.ModifyPage, "G.Group", Value.Grant)).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.SetPermissionForPage(AuthStatus.Grant, new PageInfo("Page", null, DateTime.Now),
				Actions.ForPages.ModifyPage, new UserGroup("Group", "Group", null)), "SetPermissionForPage should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		public void SetPermissionForPage_Group_Deny() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.StoreEntry(Actions.ForPages.ResourceMasterPrefix + "Page",
				Actions.ForPages.ManageCategories, "G.Group", Value.Deny)).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.SetPermissionForPage(AuthStatus.Deny, new PageInfo("Page", null, DateTime.Now),
				Actions.ForPages.ManageCategories, new UserGroup("Group", "Group", null)), "SetPermissionForPage should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		public void SetPermissionForPage_Group_Delete() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.DeleteEntry(Actions.ForPages.ResourceMasterPrefix + "NS.Page",
				Actions.ForPages.UploadAttachments, "G.Group")).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.SetPermissionForPage(AuthStatus.Delete, new PageInfo("NS.Page", null, DateTime.Now),
				Actions.ForPages.UploadAttachments, new UserGroup("Group", "Group", null)), "SetPermissionForPage should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void SetPermissionForPage_Group_NullPage() {
			ISettingsStorageProviderV30 prov = MockProvider();

			AuthWriter authWriter = new AuthWriter(prov);
			authWriter.SetPermissionForPage(AuthStatus.Grant, null, Actions.ForPages.ModifyPage, new UserGroup("Group", "Group", null));
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		[TestCase("Blah", ExpectedException = typeof(ArgumentException))]
		[TestCase("*")]
		public void SetPermissionForPage_Group_InvalidAction(string a) {
			ISettingsStorageProviderV30 prov = MockProvider();

			AuthWriter authWriter = new AuthWriter(prov);
			authWriter.SetPermissionForPage(AuthStatus.Grant, new PageInfo("Page", null, DateTime.Now),
				a, new UserGroup("Group", "Group", null));
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void SetPermissionForPage_Group_NullGroup() {
			ISettingsStorageProviderV30 prov = MockProvider();

			AuthWriter authWriter = new AuthWriter(prov);
			authWriter.SetPermissionForPage(AuthStatus.Grant, new PageInfo("Page", null, DateTime.Now),
				Actions.ForPages.ModifyPage, null as UserGroup);
		}

		[Test]
		public void SetPermissionForPage_User_Grant() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.StoreEntry(Actions.ForPages.ResourceMasterPrefix + "Page",
				Actions.ForPages.ModifyPage, "U.User", Value.Grant)).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.SetPermissionForPage(AuthStatus.Grant, new PageInfo("Page", null, DateTime.Now),
				Actions.ForPages.ModifyPage, new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null)),
				"SetPermissionForPage should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		public void SetPermissionForPage_User_Deny() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.StoreEntry(Actions.ForPages.ResourceMasterPrefix + "Page",
				Actions.ForPages.ManageCategories, "U.User", Value.Deny)).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.SetPermissionForPage(AuthStatus.Deny, new PageInfo("Page", null, DateTime.Now),
				Actions.ForPages.ManageCategories, new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null)),
				"SetPermissionForPage should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		public void SetPermissionForPage_User_Delete() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.DeleteEntry(Actions.ForPages.ResourceMasterPrefix + "NS.Page",
				Actions.ForPages.UploadAttachments, "U.User")).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.SetPermissionForPage(AuthStatus.Delete, new PageInfo("NS.Page", null, DateTime.Now),
				Actions.ForPages.UploadAttachments, new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null)),
				"SetPermissionForPage should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void SetPermissionForPage_User_NullPage() {
			ISettingsStorageProviderV30 prov = MockProvider();

			AuthWriter authWriter = new AuthWriter(prov);
			authWriter.SetPermissionForPage(AuthStatus.Grant, null, Actions.ForPages.ModifyPage,
				new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null));
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		[TestCase("Blah", ExpectedException = typeof(ArgumentException))]
		[TestCase("*")]
		public void SetPermissionForPage_User_InvalidAction(string a) {
			ISettingsStorageProviderV30 prov = MockProvider();

			AuthWriter authWriter = new AuthWriter(prov);
			authWriter.SetPermissionForPage(AuthStatus.Grant, new PageInfo("Page", null, DateTime.Now),
				a, new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null));
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void SetPermissionForPage_User_NullUser() {
			ISettingsStorageProviderV30 prov = MockProvider();

			AuthWriter authWriter = new AuthWriter(prov);
			authWriter.SetPermissionForPage(AuthStatus.Grant, new PageInfo("Page", null, DateTime.Now),
				Actions.ForPages.ModifyPage, null as UserInfo);
		}

		[Test]
		public void RemoveEntriesForGlobals_Group() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.RetrieveEntriesForSubject("G.Group")).Return(
				new AclEntry[] {
					new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageNamespaces, "G.Group", Value.Grant),
					new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageGroups, "G.Group", Value.Grant),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManagePages, "G.Group", Value.Grant) });

			Expect.Call(aclManager.DeleteEntry(Actions.ForGlobals.ResourceMasterPrefix,
				Actions.ForGlobals.ManageNamespaces, "G.Group")).Return(true);
			Expect.Call(aclManager.DeleteEntry(Actions.ForGlobals.ResourceMasterPrefix,
				Actions.ForGlobals.ManageGroups, "G.Group")).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.RemoveEntriesForGlobals(new UserGroup("Group", "Group", null)), "RemoveEntriesForGlobals should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RemoveEntriesForGlobals_Group_NullGroup() {
			AuthWriter authWriter = new AuthWriter(MockProvider());
			authWriter.RemoveEntriesForGlobals(null as UserGroup);
		}

		[Test]
		public void RemoveEntriesForGlobals_User() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.RetrieveEntriesForSubject("U.User")).Return(
				new AclEntry[] {
					new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageNamespaces, "U.User", Value.Grant),
					new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageGroups, "U.User", Value.Grant),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManagePages, "U.User", Value.Grant) });

			Expect.Call(aclManager.DeleteEntry(Actions.ForGlobals.ResourceMasterPrefix,
				Actions.ForGlobals.ManageNamespaces, "U.User")).Return(true);
			Expect.Call(aclManager.DeleteEntry(Actions.ForGlobals.ResourceMasterPrefix,
				Actions.ForGlobals.ManageGroups, "U.User")).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.RemoveEntriesForGlobals(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null)), "RemoveEntriesForGlobals should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RemoveEntriesForGlobals_User_NullUser() {
			AuthWriter authWriter = new AuthWriter(MockProvider());
			authWriter.RemoveEntriesForGlobals(null as UserInfo);
		}

		[Test]
		public void RemoveEntriesForNamespace_Group_Root() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.RetrieveEntriesForSubject("G.Group")).Return(
				new AclEntry[] {
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManagePages, "G.Group", Value.Grant),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManageCategories, "G.Group", Value.Grant),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub", Actions.ForNamespaces.ModifyPages, "G.Group", Value.Grant),
					new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageNavigationPaths, "G.Group", Value.Grant) });

			Expect.Call(aclManager.DeleteEntry(Actions.ForNamespaces.ResourceMasterPrefix,
				Actions.ForNamespaces.ManagePages, "G.Group")).Return(true);
			Expect.Call(aclManager.DeleteEntry(Actions.ForNamespaces.ResourceMasterPrefix,
				Actions.ForNamespaces.ManageCategories, "G.Group")).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.RemoveEntriesForNamespace(new UserGroup("Group", "Group", null), null), "RemoveEntriesForNamespace should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		public void RemoveEntriesForNamespace_Group_Sub() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.RetrieveEntriesForSubject("G.Group")).Return(
				new AclEntry[] {
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub", Actions.ForNamespaces.ManagePages, "G.Group", Value.Grant),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub", Actions.ForNamespaces.ManageCategories, "G.Group", Value.Grant),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ModifyPages, "G.Group", Value.Grant),
					new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageNavigationPaths, "G.Group", Value.Grant) });

			Expect.Call(aclManager.DeleteEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub",
				Actions.ForNamespaces.ManagePages, "G.Group")).Return(true);
			Expect.Call(aclManager.DeleteEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub",
				Actions.ForNamespaces.ManageCategories, "G.Group")).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.RemoveEntriesForNamespace(new UserGroup("Group", "Group", null), new NamespaceInfo("Sub", null, null)), "RemoveEntriesForNamespace should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RemoveEntriesForNamespace_Group_NullGroup() {
			AuthWriter authWriter = new AuthWriter(MockProvider());
			authWriter.RemoveEntriesForNamespace(null as UserGroup, new NamespaceInfo("Sub", null, null));
		}

		[Test]
		public void RemoveEntriesForNamespace_User_Root() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.RetrieveEntriesForSubject("U.User")).Return(
				new AclEntry[] {
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManagePages, "U.User", Value.Grant),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ManageCategories, "U.User", Value.Grant),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub", Actions.ForNamespaces.ModifyPages, "U.User", Value.Grant),
					new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageNavigationPaths, "U.User", Value.Grant) });

			Expect.Call(aclManager.DeleteEntry(Actions.ForNamespaces.ResourceMasterPrefix,
				Actions.ForNamespaces.ManagePages, "U.User")).Return(true);
			Expect.Call(aclManager.DeleteEntry(Actions.ForNamespaces.ResourceMasterPrefix,
				Actions.ForNamespaces.ManageCategories, "U.User")).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.RemoveEntriesForNamespace(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null), null),
				"RemoveEntriesForNamespace should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		public void RemoveEntriesForNamespace_User_Sub() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.RetrieveEntriesForSubject("U.User")).Return(
				new AclEntry[] {
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub", Actions.ForNamespaces.ManagePages, "U.User", Value.Grant),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub", Actions.ForNamespaces.ManageCategories, "U.User", Value.Grant),
					new AclEntry(Actions.ForNamespaces.ResourceMasterPrefix, Actions.ForNamespaces.ModifyPages, "U.User", Value.Grant),
					new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageNavigationPaths, "U.User", Value.Grant) });

			Expect.Call(aclManager.DeleteEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub",
				Actions.ForNamespaces.ManagePages, "U.User")).Return(true);
			Expect.Call(aclManager.DeleteEntry(Actions.ForNamespaces.ResourceMasterPrefix + "Sub",
				Actions.ForNamespaces.ManageCategories, "U.User")).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.RemoveEntriesForNamespace(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null),
				new NamespaceInfo("Sub", null, null)), "RemoveEntriesForNamespace should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RemoveEntriesForNamespace_User_NullUser() {
			AuthWriter authWriter = new AuthWriter(MockProvider());
			authWriter.RemoveEntriesForNamespace(null as UserInfo, new NamespaceInfo("Sub", null, null));
		}

		[Test]
		public void RemoveEntriesForPage_Group() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.RetrieveEntriesForSubject("G.Group")).Return(
				new AclEntry[] {
					new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ManagePage, "G.Group", Value.Grant),
					new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ManageCategories, "G.Group", Value.Deny),
					new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Sub.Page", Actions.ForPages.ManagePage, "G.Group", Value.Grant),
					new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageNavigationPaths, "G.Group", Value.Grant) });

			Expect.Call(aclManager.DeleteEntry(Actions.ForPages.ResourceMasterPrefix + "Page",
				Actions.ForPages.ManagePage, "G.Group")).Return(true);
			Expect.Call(aclManager.DeleteEntry(Actions.ForPages.ResourceMasterPrefix + "Page",
				Actions.ForPages.ManageCategories, "G.Group")).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.RemoveEntriesForPage(new UserGroup("Group", "Group", null),
				new PageInfo("Page", null, DateTime.Now)), "RemoveEntriesForPage should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RemoveEntriesForPage_Group_NullGroup() {
			AuthWriter authWriter = new AuthWriter(MockProvider());
			authWriter.RemoveEntriesForPage(null as UserGroup, new PageInfo("Page", null, DateTime.Now));
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RemoveEntriesForPage_Group_NullPage() {
			AuthWriter authWriter = new AuthWriter(MockProvider());
			authWriter.RemoveEntriesForPage(new UserGroup("Group", "Group", null), null);
		}

		[Test]
		public void RemoveEntriesForPage_User() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.RetrieveEntriesForSubject("U.User")).Return(
				new AclEntry[] {
					new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ManagePage, "U.User", Value.Grant),
					new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Page", Actions.ForPages.ManageCategories, "U.User", Value.Deny),
					new AclEntry(Actions.ForPages.ResourceMasterPrefix + "Sub.Page", Actions.ForPages.ManagePage, "U.User", Value.Grant),
					new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageNavigationPaths, "U.User", Value.Grant) });

			Expect.Call(aclManager.DeleteEntry(Actions.ForPages.ResourceMasterPrefix + "Page",
				Actions.ForPages.ManagePage, "U.User")).Return(true);
			Expect.Call(aclManager.DeleteEntry(Actions.ForPages.ResourceMasterPrefix + "Page",
				Actions.ForPages.ManageCategories, "U.User")).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.RemoveEntriesForPage(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null),
				new PageInfo("Page", null, DateTime.Now)), "RemoveEntriesForPage should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RemoveEntriesForPage_User_NullUser() {
			AuthWriter authWriter = new AuthWriter(MockProvider());
			authWriter.RemoveEntriesForPage(null as UserInfo, new PageInfo("Page", null, DateTime.Now));
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RemoveEntriesForPage_User_NullPage() {
			AuthWriter authWriter = new AuthWriter(MockProvider());
			authWriter.RemoveEntriesForPage(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null), null);
		}

		[Test]
		public void RemoveEntriesForDirectory_Root_Group() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IFilesStorageProviderV30 filedProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			string dirName = Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filedProv, "/");
			Expect.Call(aclManager.RetrieveEntriesForSubject("G.Group")).Return(
				new AclEntry[] {
					new AclEntry(dirName, Actions.ForDirectories.List, "G.Group", Value.Grant),
					new AclEntry(dirName, Actions.ForDirectories.UploadFiles, "G.Group", Value.Deny),
					new AclEntry("D." + AuthTools.GetDirectoryName(filedProv, "/Other/"), Actions.ForDirectories.UploadFiles, "G.Group", Value.Grant),
					new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageNavigationPaths, "G.Group", Value.Grant) });

			Expect.Call(aclManager.DeleteEntry(dirName, Actions.ForDirectories.List, "G.Group")).Return(true);
			Expect.Call(aclManager.DeleteEntry(dirName, Actions.ForDirectories.UploadFiles, "G.Group")).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.RemoveEntriesForDirectory(new UserGroup("Group", "Group", null),
				filedProv, "/"), "RemoveEntriesForPage should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		public void RemoveEntriesForDirectory_Sub_Group() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IFilesStorageProviderV30 filedProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			string dirName = Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filedProv, "/Dir/Sub/");
			Expect.Call(aclManager.RetrieveEntriesForSubject("G.Group")).Return(
				new AclEntry[] {
					new AclEntry(dirName, Actions.ForDirectories.List, "G.Group", Value.Grant),
					new AclEntry(dirName, Actions.ForDirectories.UploadFiles, "G.Group", Value.Deny),
					new AclEntry("D." + AuthTools.GetDirectoryName(filedProv, "/"), Actions.ForDirectories.UploadFiles, "G.Group", Value.Grant),
					new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageNavigationPaths, "G.Group", Value.Grant) });

			Expect.Call(aclManager.DeleteEntry(dirName, Actions.ForDirectories.List, "G.Group")).Return(true);
			Expect.Call(aclManager.DeleteEntry(dirName, Actions.ForDirectories.UploadFiles, "G.Group")).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.RemoveEntriesForDirectory(new UserGroup("Group", "Group", null),
				filedProv, "/Dir/Sub/"), "RemoveEntriesForPage should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RemoveEntriesForDirectory_Group_NullGroup() {
			AuthWriter authWriter = new AuthWriter(MockProvider());
			IFilesStorageProviderV30 fProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			mocks.Replay(fProv);
			authWriter.RemoveEntriesForDirectory(null as UserGroup, fProv, "/");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RemoveEntriesForDirectory_Group_NullProvider() {
			AuthWriter authWriter = new AuthWriter(MockProvider());
			authWriter.RemoveEntriesForDirectory(new UserGroup("Group", "Group", null), null, "/");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RemoveEntriesForDirectory_Group_InvalidDirectory(string d) {
			AuthWriter authWriter = new AuthWriter(MockProvider());

			IFilesStorageProviderV30 fProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			mocks.Replay(fProv);
			authWriter.RemoveEntriesForDirectory(new UserGroup("Group", "Group", null), fProv, d);
		}

		[Test]
		public void RemoveEntriesForDirectory_Root_User() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IFilesStorageProviderV30 filesProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			string dirName = Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filesProv, "/");
			Expect.Call(aclManager.RetrieveEntriesForSubject("U.User")).Return(
				new AclEntry[] {
					new AclEntry(dirName, Actions.ForDirectories.List, "U.User", Value.Grant),
					new AclEntry(dirName, Actions.ForDirectories.UploadFiles, "U.User", Value.Deny),
					new AclEntry("D." + AuthTools.GetDirectoryName(filesProv, "/Other/"), Actions.ForDirectories.UploadFiles, "U.User", Value.Grant),
					new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageNavigationPaths, "U.User", Value.Grant) });

			Expect.Call(aclManager.DeleteEntry(dirName, Actions.ForDirectories.List, "U.User")).Return(true);
			Expect.Call(aclManager.DeleteEntry(dirName, Actions.ForDirectories.UploadFiles, "U.User")).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.RemoveEntriesForDirectory(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null),
				filesProv, "/"), "RemoveEntriesForPage should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		public void RemoveEntriesForDirectory_Sub_User() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IFilesStorageProviderV30 filesProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			string dirName = Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filesProv, "/Dir/Sub/");
			Expect.Call(aclManager.RetrieveEntriesForSubject("U.User")).Return(
				new AclEntry[] {
					new AclEntry(dirName, Actions.ForDirectories.List, "U.User", Value.Grant),
					new AclEntry(dirName, Actions.ForDirectories.UploadFiles, "U.User", Value.Deny),
					new AclEntry("D." + AuthTools.GetDirectoryName(filesProv, "/"), Actions.ForDirectories.UploadFiles, "U.User", Value.Grant),
					new AclEntry(Actions.ForGlobals.ResourceMasterPrefix, Actions.ForGlobals.ManageNavigationPaths, "U.User", Value.Grant) });

			Expect.Call(aclManager.DeleteEntry(dirName, Actions.ForDirectories.List, "U.User")).Return(true);
			Expect.Call(aclManager.DeleteEntry(dirName, Actions.ForDirectories.UploadFiles, "U.User")).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.RemoveEntriesForDirectory(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null),
				filesProv, "/Dir/Sub/"), "RemoveEntriesForPage should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RemoveEntriesForDirectory_User_NullUser() {
			AuthWriter authWriter = new AuthWriter(MockProvider());

			IFilesStorageProviderV30 fProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			mocks.Replay(fProv);
			authWriter.RemoveEntriesForDirectory(null as UserInfo, fProv, "/");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RemoveEntriesForDirectory_User_NullProvider() {
			AuthWriter authWriter = new AuthWriter(MockProvider());
			authWriter.RemoveEntriesForDirectory(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null), null, "/");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RemoveEntriesForDirectory_User_InvalidDirectory(string d) {
			AuthWriter authWriter = new AuthWriter(MockProvider());

			IFilesStorageProviderV30 fProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			mocks.Replay(fProv);
			authWriter.RemoveEntriesForDirectory(new UserInfo("User", "User", "user@users.com", true, DateTime.Now, null), fProv, d);
		}

		[Test]
		public void ClearEntriesForDirectory() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IFilesStorageProviderV30 filesProv = mocks.DynamicMock<IFilesStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			string dirName = Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filesProv, "/Dir/Sub/");
			Expect.Call(aclManager.DeleteEntriesForResource(dirName)).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			authWriter.ClearEntriesForDirectory(filesProv, "/Dir/Sub/");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ClearEntriesForDirectory_NullProvider() {
			AuthWriter authWriter = new AuthWriter(MockProvider());

			authWriter.ClearEntriesForDirectory(null, "/dir/");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void ClearEntriesForDirectory_InvalidDirectory(string d) {
			AuthWriter authWriter = new AuthWriter(MockProvider());
			authWriter.ClearEntriesForDirectory(MockFilesProvider(), d);
		}

		[Test]
		public void ClearEntriesForNamespace() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.DeleteEntriesForResource(Actions.ForNamespaces.ResourceMasterPrefix + "NS")).Return(true);
			Expect.Call(aclManager.DeleteEntriesForResource(Actions.ForPages.ResourceMasterPrefix + "NS.Page1")).Return(true);
			Expect.Call(aclManager.DeleteEntriesForResource(Actions.ForPages.ResourceMasterPrefix + "NS.Page2")).Return(true);
			Expect.Call(aclManager.DeleteEntriesForResource(Actions.ForPages.ResourceMasterPrefix + "NS.Page3")).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			authWriter.ClearEntriesForNamespace("NS", new List<string>() { "Page1", "Page2", "Page3" });

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void ClearEntriesForNamespace_InvalidNamespace(string n) {
			AuthWriter authWriter = new AuthWriter(MockProvider());
			authWriter.ClearEntriesForNamespace(n, new List<string>());
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ClearEntriesForNamespace_NullPages() {
			AuthWriter authWriter = new AuthWriter(MockProvider());
			authWriter.ClearEntriesForNamespace("NS", null);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void ClearEntriesForNamespace_InvalidPageElement(string p) {
			AuthWriter authWriter = new AuthWriter(MockProvider());
			authWriter.ClearEntriesForNamespace("NS", new List<string>() { "Page", p, "Page3" });
		}

		[Test]
		public void ClearEntriesForPage() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.DeleteEntriesForResource(Actions.ForPages.ResourceMasterPrefix + "Page")).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			authWriter.ClearEntriesForPage("Page");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void ClearEntriesForPage_InvalidPage(string p) {
			AuthWriter authWriter = new AuthWriter(MockProvider());
			authWriter.ClearEntriesForPage(p);
		}

		[Test]
		public void ProcessDirectoryRenaming() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IFilesStorageProviderV30 filesProv = MockFilesProvider();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.RenameResource(
				Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filesProv, "/Dir/"),
				Actions.ForDirectories.ResourceMasterPrefix + AuthTools.GetDirectoryName(filesProv, "/Dir2/"))).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.ProcessDirectoryRenaming(filesProv, "/Dir/", "/Dir2/"), "ProcessDirectoryRenaming should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ProcessDirectoryRenaming_NullProvider() {
			AuthWriter authWriter = new AuthWriter(MockProvider());
			authWriter.ProcessDirectoryRenaming(null, "/Dir/", "/Dir2/");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void ProcessDirectoryRenaming_InvalidOldName(string n) {
			AuthWriter authWriter = new AuthWriter(MockProvider());
			authWriter.ProcessDirectoryRenaming(MockFilesProvider(), n, "/Dir/");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void ProcessDirectoryRenaming_InvalidNewName(string n) {
			AuthWriter authWriter = new AuthWriter(MockProvider());
			authWriter.ProcessDirectoryRenaming(MockFilesProvider(), "/Dir/", n);
		}

		[Test]
		public void ProcessNamespaceRenaming() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.RenameResource(Actions.ForNamespaces.ResourceMasterPrefix + "NS", Actions.ForNamespaces.ResourceMasterPrefix + "NS_Renamed")).Return(true);
			Expect.Call(aclManager.RenameResource(Actions.ForPages.ResourceMasterPrefix + "NS.Page1", Actions.ForPages.ResourceMasterPrefix + "NS_Renamed.Page1")).Return(true);
			Expect.Call(aclManager.RenameResource(Actions.ForPages.ResourceMasterPrefix + "NS.Page2", Actions.ForPages.ResourceMasterPrefix + "NS_Renamed.Page2")).Return(true);
			Expect.Call(aclManager.RenameResource(Actions.ForPages.ResourceMasterPrefix + "NS.Page3", Actions.ForPages.ResourceMasterPrefix + "NS_Renamed.Page3")).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.ProcessNamespaceRenaming("NS", new List<string>() { "Page1", "Page2", "Page3" }, "NS_Renamed"));

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void ProcessNamespaceRenaming_InvalidOldName(string n) {
			AuthWriter authWriter = new AuthWriter(MockProvider());
			authWriter.ProcessNamespaceRenaming(n, new List<string>(), "NS_Renamed");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ProcessNamespaceRenaming_NullPages() {
			AuthWriter authWriter = new AuthWriter(MockProvider());
			authWriter.ProcessNamespaceRenaming("NS", null, "NS_Renamed");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void ProcessNamespaceRenaming_InvalidPageElement(string p) {
			AuthWriter authWriter = new AuthWriter(MockProvider());
			authWriter.ProcessNamespaceRenaming("NS", new List<string>() { "Page", p, "Page3" }, "NS_Renamed");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void ProcessNamespaceRenaming_InvalidNewName(string n) {
			AuthWriter authWriter = new AuthWriter(MockProvider());
			authWriter.ProcessNamespaceRenaming("NS", new List<string>(), n);
		}

		[Test]
		public void ProcessPageRenaming() {
			MockRepository mocks = new MockRepository();
			ISettingsStorageProviderV30 prov = mocks.DynamicMock<ISettingsStorageProviderV30>();
			IAclManager aclManager = mocks.DynamicMock<IAclManager>();

			Expect.Call(prov.AclManager).Return(aclManager).Repeat.Any();

			Expect.Call(aclManager.RenameResource(Actions.ForPages.ResourceMasterPrefix + "NS.Page", Actions.ForPages.ResourceMasterPrefix + "NS.Renamed")).Return(true);

			mocks.Replay(prov);
			mocks.Replay(aclManager);

			AuthWriter authWriter = new AuthWriter(prov);
			Assert.IsTrue(authWriter.ProcessPageRenaming("NS.Page", "NS.Renamed"), "ProcessPageRenaming should return true");

			mocks.Verify(prov);
			mocks.Verify(aclManager);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void ProcessPageRenaming_InvalidOldName(string n) {
			AuthWriter authWriter = new AuthWriter(MockProvider());
			authWriter.ProcessPageRenaming(n, "NS.Renamed");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void ProcessPageRenaming_InvalidNewName(string n) {
			AuthWriter authWriter = new AuthWriter(MockProvider());
			authWriter.ProcessPageRenaming("NS.Original", n);
		}

	}

}
