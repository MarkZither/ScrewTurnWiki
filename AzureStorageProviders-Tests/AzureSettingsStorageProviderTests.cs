﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki.Tests;

namespace ScrewTurn.Wiki.Plugins.AzureStorage.Tests {

	[TestFixture]
	public class AzureSettingsStorageProviderTests : SettingsStorageProviderTestScaffolding {

		public override ISettingsStorageProviderV30 GetProvider() {
			AzureSettingsStorageProvider settingsProvider = new AzureSettingsStorageProvider();
			settingsProvider.SetUp(MockHost(), "testonazurestorage|GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==");
			settingsProvider.Init(MockHost(), "testonazurestorage|GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", "");

			return settingsProvider;
		}

		[TearDown]
		public new void TearDown() {
			base.TearDown();

			TableStorage.TruncateTable("testonazurestorage", "GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", AzureSettingsStorageProvider.SettingsTable);
			TableStorage.TruncateTable("testonazurestorage", "GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", AzureSettingsStorageProvider.MetadataTable);
			TableStorage.TruncateTable("testonazurestorage", "GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", AzureSettingsStorageProvider.OutgoingLinksTable);
			TableStorage.TruncateTable("testonazurestorage", "GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", AzureSettingsStorageProvider.RecentChangesTable);
			TableStorage.TruncateTable("testonazurestorage", "GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", AzureSettingsStorageProvider.AclEntriesTable);
		}

		[Test]
		public void Init() {
			ISettingsStorageProviderV30 prov = GetProvider();
			prov.Init(MockHost(), "testonazurestorage|GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==", "");

			Assert.IsNotNull(prov.Information, "Information should not be null");
		}

		[TestCase("", ExpectedException = typeof(InvalidConfigurationException))]
		[TestCase("blah", ExpectedException = typeof(InvalidConfigurationException))]
		[TestCase("testonazurestorage|GxMxHoP/2yNh+I9PVXWZStP+qTX7rWagK09EWW5gYPmb8+qnpdC5dCjEDid+7Q6KSS8fMWZFeK4cD+UYoB38AA==|123", ExpectedException = typeof(InvalidConfigurationException))]
		public void Init_InvalidConnString(string c) {
			ISettingsStorageProviderV30 prov = GetProvider();
			prov.Init(MockHost(), c, null);
		}
	}
}
