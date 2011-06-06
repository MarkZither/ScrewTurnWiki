
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;
using ScrewTurn.Wiki.SearchEngine;
using ScrewTurn.Wiki.SearchEngine.Tests;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki.Plugins.AzureStorage.Tests {

	[TestFixture]
	public class AzurePagesStorageProvider_AzureIndexTests : IndexBaseTests {

		private MockRepository mocks = new MockRepository();
		private string testDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());

		private delegate string ToStringDelegate(string wiki, PageInfo p, string input);

		protected IHostV40 MockHost() {
			if(!Directory.Exists(testDir)) Directory.CreateDirectory(testDir);

			IHostV40 host = mocks.DynamicMock<IHostV40>();
			Expect.Call(host.GetGlobalSettingValue(GlobalSettingName.PublicDirectory)).Return(testDir).Repeat.AtLeastOnce();
			Expect.Call(host.PrepareContentForIndexing(null, null, null)).IgnoreArguments().Do((ToStringDelegate)delegate(string wiki, PageInfo p, string input) { return input; }).Repeat.Any();
			Expect.Call(host.PrepareTitleForIndexing(null, null, null)).IgnoreArguments().Do((ToStringDelegate)delegate(string wiki, PageInfo p, string input) { return input; }).Repeat.Any();

			mocks.Replay(host);

			return host;
		}

		public IPagesStorageProviderV40 GetProvider() {
			AzurePagesStorageProvider prov = new AzurePagesStorageProvider();
			prov.SetUp(MockHost(), "DefaultEndpointsProtocol=http;AccountName=unittestonazurestorage;AccountKey=YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==");
			prov.Init(MockHost(), "DefaultEndpointsProtocol=http;AccountName=unittestonazurestorage;AccountKey=YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", "");

			return prov;
		}
		
		[TearDown]
		public void TearDown() {
			TableStorage.TruncateTable("DefaultEndpointsProtocol=http;AccountName=unittestonazurestorage;AccountKey=YJYFEAfNT88YBhYnneUNAO8EqYUcPHU6ito1xKHI5g9wHB0dxEiostlZJIz2BjUY0wICXusR0A7QB5P7toK9eg==", AzurePagesStorageProvider.IndexWordMappingTable);
		}

		/// <summary>
		/// Gets the instance of the index to test.
		/// </summary>
		/// <returns>The instance of the index.</returns>
		protected override IIndex GetIndex() {
			AzurePagesStorageProvider prov = GetProvider() as AzurePagesStorageProvider;
			prov.SetFlags(true);
			return prov.Index;
		}

	}

}
