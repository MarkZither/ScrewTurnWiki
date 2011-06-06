
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki.Tests {

	[TestFixture]
	public class ProviderLoaderTests {

		[TestCase(null, typeof(GlobalSettingsStorageProvider))]
		[TestCase("", typeof(GlobalSettingsStorageProvider))]
		[TestCase("default", typeof(GlobalSettingsStorageProvider))]
		[TestCase("DEfaulT", typeof(GlobalSettingsStorageProvider))]
		[TestCase("ScrewTurn.Wiki.GlobalSettingsStorageProvider, ScrewTurn.Wiki.Core", typeof(GlobalSettingsStorageProvider))]
		[TestCase("ScrewTurn.Wiki.GlobalSettingsStorageProvider, ScrewTurn.Wiki.Core.dll", typeof(GlobalSettingsStorageProvider))]
		[TestCase("ScrewTurn.Wiki.Tests.TestGlobalSettingsStorageProvider, ScrewTurn.Wiki.Core.Tests.dll", typeof(TestGlobalSettingsStorageProvider))]
		[TestCase("glglglglglglg, gfgfgfgfggf.dll", typeof(string), ExpectedException = typeof(ArgumentException))]
		public void Static_LoadSettingsStorageProvider(string p, Type type) {
			IGlobalSettingsStorageProviderV40 prov = ProviderLoader.LoadGlobalSettingsStorageProvider(p);
			Assert.IsNotNull(prov, "Provider should not be null");
			// type == prov.GetType() seems to fail due to reflection
			Assert.AreEqual(type.ToString(), prov.GetType().FullName, "Wrong return type");
		}

	}

}
