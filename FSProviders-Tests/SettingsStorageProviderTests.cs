﻿
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using NUnit.Framework;
using Rhino.Mocks;

namespace ScrewTurn.Wiki.Tests {

	public class SettingsStorageProviderTests : SettingsStorageProviderTestScaffolding {

		public override ISettingsStorageProviderV40 GetProvider() {
			SettingsStorageProvider prov = new SettingsStorageProvider();
			prov.SetUp(MockHost(), "");
			prov.Init(MockHost(), "", "wiki1");
			return prov;
		}

		[Test]
		public void Init() {
			ISettingsStorageProviderV40 prov = GetProvider();
			prov.Init(MockHost(), "", null);

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}

	}

}
