
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
		/// <param name="connectionString">The connection string.</param>
		public static CloudStorageAccount StorageAccount(string connectionString) {
			return CloudStorageAccount.Parse(connectionString);
		}

		private static CloudTableClient TableClient(string connectionString) {
			return StorageAccount(connectionString).CreateCloudTableClient();
		}

		/// <summary>
		/// Creates a table if it does not exist.
		/// </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <param name="tableName">The name of the table.</param>
		public static void CreateTable(string connectionString, string tableName) {
			TableClient(connectionString).CreateTableIfNotExist(tableName);
		}

		/// <summary>
		/// Removes all data from a table.
		/// </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <param name="tableName">The table name.</param>
		public static void TruncateTable(string connectionString, string tableName) {
			var client = TableClient(connectionString);
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

		private class DummyEntity : TableServiceEntity {
		}

		/// <summary>
		/// Gets the context.
		/// </summary>
		/// <param name="connectionString">The connection string.</param>
		public static TableServiceContext GetContext(string connectionString) {
			TableServiceContext context = TableClient(connectionString).GetDataServiceContext();
			context.RetryPolicy = GetDefaultRetryPolicy();
			return context;
		}

		/// <summary>
		/// Saves the changes.
		/// </summary>
		/// <param name="context">The context.</param>
		public static void SaveChangesStandard(this TableServiceContext context) {
			context.SaveChangesWithRetries(SaveChangesOptions.Batch | SaveChangesOptions.ReplaceOnUpdate);
		}

		#region blobs

		/// <summary>
		/// Deletes all blobs in all containers.
		/// Used only in tests tear-down method
		/// </summary>
		/// <param name="connectionString">The connection string.</param>
		public static void DeleteAllBlobs(string connectionString) {
			CloudBlobClient _client = TableStorage.StorageAccount(connectionString).CreateCloudBlobClient();
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

		/// <summary>
		/// Gets the default retry policy.
		/// </summary>
		public static RetryPolicy GetDefaultRetryPolicy() {
			return RetryPolicies.RetryExponential(10, TimeSpan.FromSeconds(0.5));
		}
	}
}
