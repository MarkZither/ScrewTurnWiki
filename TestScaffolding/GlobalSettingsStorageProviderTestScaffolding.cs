
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
	public abstract class GlobalSettingsStorageProviderTestScaffolding {

		private MockRepository mocks = new MockRepository();
		private string testDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());

		private const int MaxLogSize = 8;
		private const int MaxRecentChanges = 20;

		protected IHostV30 MockHost() {
			if(!Directory.Exists(testDir)) Directory.CreateDirectory(testDir);
			//Console.WriteLine("Temp dir: " + testDir);

			IHostV30 host = mocks.DynamicMock<IHostV30>();
			Expect.Call(host.GetGlobalSettingValue(GlobalSettingName.PublicDirectory)).Return(testDir).Repeat.AtLeastOnce();

			Expect.Call(host.GetGlobalSettingValue(GlobalSettingName.LoggingLevel)).Return("3").Repeat.Any();
			Expect.Call(host.GetGlobalSettingValue(GlobalSettingName.MaxLogSize)).Return(MaxLogSize.ToString()).Repeat.Any();
			Expect.Call(host.GetSettingValue(null, SettingName.MaxRecentChanges)).Return(MaxRecentChanges.ToString()).Repeat.Any();

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

		public abstract IGlobalSettingsStorageProviderV30 GetProvider();

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Init_NullHost() {
			IGlobalSettingsStorageProviderV30 prov = GetProvider();
			prov.Init(null, "", null);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Init_NullConfig() {
			IGlobalSettingsStorageProviderV30 prov = GetProvider();
			prov.Init(MockHost(), null, null);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void SetSetting_InvalidName(string n) {
			IGlobalSettingsStorageProviderV30 prov = GetProvider();

			prov.SetSetting(n, "blah");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void GetSetting_InvalidName(string n) {
			IGlobalSettingsStorageProviderV30 prov = GetProvider();

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
			IGlobalSettingsStorageProviderV30 prov = GetProvider();

			//Collectors.SettingsProvider = prov;

			Assert.IsTrue(prov.SetSetting("TS", c), "SetSetting should return true");
			Assert.AreEqual(r, prov.GetSetting("TS"), "Wrong return value");
		}

		[Test]
		public void SetSetting_GetAllSettings() {
			IGlobalSettingsStorageProviderV30 prov = GetProvider();

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

		[TestCase("Message", EntryType.General, "User")]
		[TestCase("Message\nblah", EntryType.Error, "User\nggg")]
		[TestCase("Message|ppp", EntryType.Warning, "User|ghghgh")]
		public void LogEntry_GetLogEntries(string m, EntryType t, string u) {
			IGlobalSettingsStorageProviderV30 prov = GetProvider();

			//Collectors.SettingsProvider = prov;

			prov.LogEntry(m, t, u);

			LogEntry[] entries = prov.GetLogEntries();
			Assert.AreEqual(m, entries[entries.Length - 1].Message, "Wrong message");
			Assert.AreEqual(t, entries[entries.Length - 1].EntryType, "Wrong entry type");
			Assert.AreEqual(u, entries[entries.Length - 1].User, "Wrong user");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void LogEntry_InvalidMessage(string m) {
			IGlobalSettingsStorageProviderV30 prov = GetProvider();

			//Collectors.SettingsProvider = prov;

			prov.LogEntry(m, EntryType.General, "NUnit");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void LogEntry_InvalidUser(string u) {
			IGlobalSettingsStorageProviderV30 prov = GetProvider();

			//Collectors.SettingsProvider = prov;

			prov.LogEntry("Test", EntryType.General, u);
		}

		[Test]
		public void ClearLog() {
			IGlobalSettingsStorageProviderV30 prov = GetProvider();

			//Collectors.SettingsProvider = prov;

			prov.LogEntry("Test", EntryType.General, "User");
			prov.LogEntry("Test", EntryType.Error, "User");
			prov.LogEntry("Test", EntryType.Warning, "User");

			Assert.AreEqual(3, prov.GetLogEntries().Length, "Wrong log entry count");

			prov.ClearLog();

			Assert.AreEqual(0, prov.GetLogEntries().Length, "Wrong log entry count");
		}

		[Test]
		public void CutLog_LogSize() {
			IGlobalSettingsStorageProviderV30 prov = GetProvider();

			//Collectors.SettingsProvider = prov;

			for(int i = 0; i < 100; i++) {
				prov.LogEntry("Test", EntryType.General, "User");
				prov.LogEntry("Test", EntryType.Error, "User");
				prov.LogEntry("Test", EntryType.Warning, "User");
			}

			Assert.IsTrue(prov.LogSize > 0 && prov.LogSize < MaxLogSize, "Wrong size");

			Assert.IsTrue(prov.GetLogEntries().Length < 300, "Wrong log entry count");
		}

		[Test]
		public void StorePluginAssembly_RetrievePluginAssembly_ListPluginAssemblies() {
			IGlobalSettingsStorageProviderV30 prov = GetProvider();
			//Collectors.SettingsProvider = prov;

			byte[] stuff = new byte[50];
			for(int i = 0; i < stuff.Length; i++) stuff[i] = (byte)i;

			Assert.AreEqual(0, prov.ListPluginAssemblies().Length, "Wrong length");

			Assert.IsTrue(prov.StorePluginAssembly("Plugin.dll", stuff), "StorePluginAssembly should return true");

			string[] asms = prov.ListPluginAssemblies();
			Assert.AreEqual(1, asms.Length, "Wrong length");
			Assert.AreEqual("Plugin.dll", asms[0], "Wrong assembly name");

			byte[] output = prov.RetrievePluginAssembly("Plugin.dll");
			Assert.AreEqual(stuff.Length, output.Length, "Wrong content length");
			for(int i = 0; i < stuff.Length; i++) Assert.AreEqual(stuff[i], output[i], "Wrong content");

			stuff = new byte[30];
			for(int i = stuff.Length - 1; i >= 0; i--) stuff[i] = (byte)i;

			Assert.IsTrue(prov.StorePluginAssembly("Plugin.dll", stuff), "StorePluginAssembly should return true");

			output = prov.RetrievePluginAssembly("Plugin.dll");
			Assert.AreEqual(stuff.Length, output.Length, "Wrong content length");
			for(int i = 0; i < stuff.Length; i++) Assert.AreEqual(stuff[i], output[i], "Wrong content");
		}

		[Test]
		public void DeletePluginAssembly() {
			IGlobalSettingsStorageProviderV30 prov = GetProvider();
			//Collectors.SettingsProvider = prov;

			Assert.IsFalse(prov.DeletePluginAssembly("Assembly.dll"), "DeletePluginAssembly should return false");

			byte[] stuff = new byte[50];
			for(int i = 0; i < stuff.Length; i++) stuff[i] = (byte)i;

			prov.StorePluginAssembly("Plugin.dll", stuff);
			prov.StorePluginAssembly("Assembly.dll", stuff);

			Assert.IsTrue(prov.DeletePluginAssembly("Assembly.dll"), "DeletePluginAssembly should return true");

			string[] asms = prov.ListPluginAssemblies();

			Assert.AreEqual(1, asms.Length, "Wrong length");
			Assert.AreEqual("Plugin.dll", asms[0], "Wrong assembly");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void DeletePluginAssembly_InvalidName(string n) {
			IGlobalSettingsStorageProviderV30 prov = GetProvider();
			//Collectors.SettingsProvider = prov;

			prov.DeletePluginAssembly(n);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void StorePluginAssembly_InvalidFilename(string fn) {
			IGlobalSettingsStorageProviderV30 prov = GetProvider();
			//Collectors.SettingsProvider = prov;

			prov.StorePluginAssembly(fn, new byte[10]);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void StorePluginAssembly_NullAssembly() {
			IGlobalSettingsStorageProviderV30 prov = GetProvider();
			//Collectors.SettingsProvider = prov;

			prov.StorePluginAssembly("Test.dll", null);
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void StorePluginAssembly_EmptyAssembly() {
			IGlobalSettingsStorageProviderV30 prov = GetProvider();
			//Collectors.SettingsProvider = prov;

			prov.StorePluginAssembly("Test.dll", new byte[0]);
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RetrievePluginAssembly_InvalidFilename(string fn) {
			IGlobalSettingsStorageProviderV30 prov = GetProvider();
			//Collectors.SettingsProvider = prov;

			prov.RetrievePluginAssembly(fn);
		}

		[Test]
		public void RetrievePluginAssembly_InexistentFilename() {
			IGlobalSettingsStorageProviderV30 prov = GetProvider();
			//Collectors.SettingsProvider = prov;

			Assert.IsNull(prov.RetrievePluginAssembly("Inexistent.dll"), "RetrievePluginAssembly should return null");
		}

	}

}
