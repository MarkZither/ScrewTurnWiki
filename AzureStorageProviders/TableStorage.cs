
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure;
using System.Data.Services.Client;

namespace ScrewTurn.Wiki.Plugins.AzureStorage {

	/// <summary>
	/// 
	/// </summary>
	public static class TableStorage {

		/// <summary>
		/// Get the storage account.
		/// </summary>
		/// <param name="accountName">The name of the account.</param>
		/// <param name="key">The key for the given account.</param>
		public static CloudStorageAccount StorageAccount(string accountName, string key) {
			return new CloudStorageAccount(new StorageCredentialsAccountAndKey(accountName, key), false);
		}

		private static CloudTableClient TableClient(string accountName, string key) {
			return StorageAccount(accountName, key).CreateCloudTableClient();
		}

		/// <summary>
		/// Creates a table if it does not exist.
		/// </summary>
		/// <param name="accountName">Name of the account.</param>
		/// <param name="key">The key.</param>
		/// <param name="tableName">The name of the table.</param>
		public static void CreateTable(string accountName, string key, string tableName) {
			TableClient(accountName, key).CreateTableIfNotExist(tableName);
		}

		/// <summary>
		/// Removes all data from a table.
		/// </summary>
		/// <param name="tableName">The table name.</param>
		public static void TruncateTable(string accountName, string key, string tableName) {
			var client = TableClient(accountName, key);
			if(!client.DoesTableExist(tableName)) return;

			var context = client.GetDataServiceContext();
			var entities =
				from e in context.CreateQuery<DummyEntity>(tableName)
				select e;

			foreach(var e in entities) {
				context.DeleteObject(e);
				context.SaveChangesStandard();
			}
		}

		/// <summary>
		/// Gets the context.
		/// </summary>
		public static TableServiceContext GetContext(string accountName, string key) {
			return TableClient(accountName, key).GetDataServiceContext();
		}

		/// <summary>
		/// Saves the changes.
		/// </summary>
		/// <param name="context">The context.</param>
		public static void SaveChangesStandard(this TableServiceContext context) {
			context.SaveChangesWithRetries(SaveChangesOptions.Batch | SaveChangesOptions.ReplaceOnUpdate);
		}

		private class DummyEntity : TableServiceEntity {
		}

		#region blobs

		/// <summary>
		/// Gets the default retry policy.
		/// </summary>
		public static RetryPolicy GetDefaultRetryPolicy() {
			return RetryPolicies.RetryExponential(10, TimeSpan.FromSeconds(0.5));
		}

		/// <summary>
		/// Deletes all blobs in all containers.
		/// Used only in tests tear-down method
		/// </summary>
		/// <param name="accountName">Name of the account.</param>
		/// <param name="key">The key.</param>
		public static void DeleteAllBlobs(string accountName, string key) {
			CloudBlobClient _client = TableStorage.StorageAccount(accountName, key).CreateCloudBlobClient();
			_client.RetryPolicy = GetDefaultRetryPolicy();

			foreach(CloudBlobContainer containerRef in _client.ListContainers()) {
				BlobRequestOptions options = new BlobRequestOptions();
				options.UseFlatBlobListing = true;
				IEnumerable<IListBlobItem> blobs = containerRef.ListBlobs(options);
				foreach(IListBlobItem blob in blobs) {
					var blobRef = _client.GetBlobReference(blob.Uri.AbsoluteUri);
					blobRef.DeleteIfExists();
				}
			}
		}

		#endregion

	}
}
