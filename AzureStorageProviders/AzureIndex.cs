
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.SearchEngine;

namespace ScrewTurn.Wiki.Plugins.AzureStorage {

	public class AzureIndex :IIndex {

		#region IIndex Members

		public string[] StopWords {
			get {
				throw new NotImplementedException();
			}
			set {
				throw new NotImplementedException();
			}
		}

		public int TotalWords {
			get { throw new NotImplementedException(); }
		}

		public int TotalDocuments {
			get { throw new NotImplementedException(); }
		}

		public int TotalOccurrences {
			get { throw new NotImplementedException(); }
		}

		public void Clear(object state) {
			throw new NotImplementedException();
		}

		public int StoreDocument(IDocument document, string[] keywords, string content, object state) {
			throw new NotImplementedException();
		}

		public void RemoveDocument(IDocument document, object state) {
			throw new NotImplementedException();
		}

		public SearchResultCollection Search(SearchParameters parameters) {
			throw new NotImplementedException();
		}

		#endregion
	}
}
