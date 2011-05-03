
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;
using System.Threading;

namespace AzureTableStorageProviders {
	
	/// <summary>
	/// Query helper class.
	/// </summary>
	/// <typeparam name="T">The type of the data entity.</typeparam>
	public class QueryHelper<T> {

		private CloudTableQuery<T> _query;

		private List<T> _result;
		private ManualResetEvent _mainWaitHandle;

		/// <summary>
		/// Initializes a new instance of the <see cref="QueryHelper&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="query">The query.</param>
		public QueryHelper(CloudTableQuery<T> query) {
			if(query == null) throw new ArgumentNullException("query");

			_query = query;
		}

		private IList<T> ExecuteSegmented(bool firstOrDefaultMode) {
			_result = new List<T>();

			if(firstOrDefaultMode) _query.BeginExecuteSegmented(ResultAvailable_FirstOrDefault, null);
			else _query.BeginExecuteSegmented(ResultAvailable, null);

			_mainWaitHandle = new ManualResetEvent(false);
			_mainWaitHandle.WaitOne();

			return _result;
		}

		private void ResultAvailable(IAsyncResult asyncResult) {
			try {
				var result = _query.EndExecuteSegmented(asyncResult);

				_result.AddRange(result.Results);

				if(result.HasMoreResults) {
					_query.BeginExecuteSegmented(result.ContinuationToken, ResultAvailable, null);
				}
				else _mainWaitHandle.Set();
			}
			catch {
				_mainWaitHandle.Set();
				throw;
			}
		}

		private void ResultAvailable_FirstOrDefault(IAsyncResult asyncResult) {
			try {
				var result = _query.EndExecuteSegmented(asyncResult);

				_result.AddRange(result.Results);

				if(result.Results.Any()) _mainWaitHandle.Set();
				else {
					if(result.HasMoreResults) {
						_query.BeginExecuteSegmented(result.ContinuationToken, ResultAvailable_FirstOrDefault, null);
					}
					else _mainWaitHandle.Set();
				}
			}
			catch {
				_mainWaitHandle.Set();
				throw;
			}
		}

		/// <summary>
		/// Executes the query and returns the first element, if any.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <returns>The first element.</returns>
		public static T FirstOrDefault(CloudTableQuery<T> query) {
			QueryHelper<T> helper = new QueryHelper<T>(query);

			return helper.ExecuteSegmented(true).FirstOrDefault();
		}

		/// <summary>
		/// Executes the query.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <returns>The returned elements.</returns>
		public static IList<T> All(CloudTableQuery<T> query) {
			QueryHelper<T> helper = new QueryHelper<T>(query);

			return helper.ExecuteSegmented(false);
		}

	}
}