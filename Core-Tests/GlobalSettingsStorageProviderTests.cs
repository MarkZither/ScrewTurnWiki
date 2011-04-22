
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using NUnit.Framework;
using Rhino.Mocks;

namespace ScrewTurn.Wiki.Tests {

	public class GlobalSettingsStorageProviderTests : GlobalSettingsStorageProviderTestScaffolding {

		public override IGlobalSettingsStorageProviderV30 GetProvider() {
			GlobalSettingsStorageProvider prov = new GlobalSettingsStorageProvider();
			prov.SetUp(MockHost(), "");
			return prov;
		}

		[Test]
		public void Init() {
			IGlobalSettingsStorageProviderV30 prov = GetProvider();
			prov.Init(MockHost(), "", null);

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}

	}

}
