
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Allow access to the search engine.
	/// </summary>
	public static class SearchClass {

		/// <summary>
		/// Searches the specified phrase in the specified search fields.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="searchFields">The search fields.</param>
		/// <param name="phrase">The phrase to search.</param>
		/// <param name="searchOption">The search options.</param>
		/// <returns>A list of <see cref="SearchResult"/> items.</returns>
		public static List<SearchResult> Search(string wiki, SearchField[] searchFields, string phrase, SearchOptions searchOption) {
			IIndexDirectoryProviderV40 indexDirectoryProvider = Collectors.CollectorsBox.GetIndexDirectoryProvider(wiki);

			IndexSearcher searcher = new IndexSearcher(indexDirectoryProvider.GetDirectory(), false);

			string[] searchFieldsAsString = (from f in searchFields select f.AsString()).ToArray();
			MultiFieldQueryParser queryParser = new MultiFieldQueryParser(Lucene.Net.Util.Version.LUCENE_29, searchFieldsAsString, new SimpleAnalyzer());
			
			if(searchOption == SearchOptions.AllWords) queryParser.SetDefaultOperator(QueryParser.Operator.AND);
			if(searchOption == SearchOptions.AtLeastOneWord) queryParser.SetDefaultOperator(QueryParser.Operator.OR);
			
			TopDocs topDocs = searcher.Search(queryParser.Parse(phrase), 100);

			List<SearchResult> searchResults = new List<SearchResult>(topDocs.totalHits);
			for(int i = 0; i < topDocs.totalHits; i++) {
				Document doc = searcher.Doc(topDocs.scoreDocs[i].doc);
				searchResults.Add(new SearchResult() {
					DocumentType = DocumentTypeFromString(doc.GetField(SearchField.DocumentType.AsString()).StringValue()),
					Wiki = doc.GetField(SearchField.Wiki.AsString()).StringValue(),
					PageFullName = doc.GetField(SearchField.PageFullName.AsString()).StringValue(),
					Title = doc.GetField(SearchField.PageTitle.AsString()).StringValue(),
					Content = doc.GetField(SearchField.PageContent.AsString()).StringValue()
				});
			}
			searcher.Close();
			return searchResults;
		}

		/// <summary>
		/// Indexes the page.
		/// </summary>
		/// <param name="page">The page page to be intexed.</param>
		/// <returns><c>true</c> if the page has been indexed succesfully, <c>false</c> otherwise.</returns>
		public static bool IndexPage(PageContent page) {
			IIndexDirectoryProviderV40 indexDirectoryProvider = Collectors.CollectorsBox.GetIndexDirectoryProvider(page.Provider.CurrentWiki);

			Analyzer analyzer = new SimpleAnalyzer();
			IndexWriter writer = new IndexWriter(indexDirectoryProvider.GetDirectory(), analyzer, IndexWriter.MaxFieldLength.UNLIMITED);

			Document doc = new Document();
			doc.Add(new Field(SearchField.DocumentType.AsString(), DocumentTypeToString(DocumentType.Page), Field.Store.YES, Field.Index.NO));
			doc.Add(new Field(SearchField.Wiki.AsString(), page.Provider.CurrentWiki, Field.Store.YES, Field.Index.NOT_ANALYZED));
			doc.Add(new Field(SearchField.PageFullName.AsString(), page.FullName, Field.Store.YES, Field.Index.NOT_ANALYZED));
			doc.Add(new Field(SearchField.PageTitle.AsString(), page.Title, Field.Store.YES, Field.Index.ANALYZED));
			doc.Add(new Field(SearchField.PageContent.AsString(), page.Content, Field.Store.YES, Field.Index.ANALYZED));
			writer.AddDocument(doc);
			writer.Commit();
			writer.Close();
			return true;
		}

		/// <summary>
		/// Indexes the message.
		/// </summary>
		/// <param name="directory">The Lucene.NET directory to be used.</param>
		/// <param name="message">The message to be indexed.</param>
		/// <returns><c>true</c> if the message has been indexed succesfully, <c>false</c> otherwise.</returns>
		public static bool IndexMessage(Directory directory, Message message, PageContent page) {
			IIndexDirectoryProviderV40 indexDirectoryProvider = Collectors.CollectorsBox.GetIndexDirectoryProvider(page.Provider.CurrentWiki);

			Analyzer analyzer = new SimpleAnalyzer();
			IndexWriter writer = new IndexWriter(indexDirectoryProvider.GetDirectory(), analyzer, IndexWriter.MaxFieldLength.UNLIMITED);

			Document doc = new Document();
			doc.Add(new Field(SearchField.DocumentType.AsString(), DocumentTypeToString(DocumentType.Message), Field.Store.YES, Field.Index.NO));
			doc.Add(new Field(SearchField.Wiki.AsString(), page.Provider.CurrentWiki, Field.Store.YES, Field.Index.NOT_ANALYZED));
			doc.Add(new Field(SearchField.PageFullName.AsString(), page.FullName, Field.Store.YES, Field.Index.NOT_ANALYZED));
			doc.Add(new Field(SearchField.MessageId.AsString(), message.ID.ToString(), Field.Store.NO, Field.Index.NOT_ANALYZED);
			doc.Add(new Field(SearchField.MessageSubject.AsString(), message.Subject, Field.Store.YES, Field.Index.ANALYZED));
			doc.Add(new Field(SearchField.MessageBody.AsString(), message.Body, Field.Store.YES, Field.Index.ANALYZED));
			writer.AddDocument(doc);
			writer.Commit();
			writer.Close();
			return true;
		}

		/// <summary>
		/// Unindexes the page.
		/// </summary>
		/// <param name="page">The page to be unindexed.</param>
		/// <returns><c>true</c> if the page has been unindexed succesfully, <c>false</c> otherwise.</returns>
		public static bool UnindexPage(PageContent page) {
			IIndexDirectoryProviderV40 indexDirectoryProvider = Collectors.CollectorsBox.GetIndexDirectoryProvider(page.Provider.CurrentWiki);

			IndexWriter writer = new IndexWriter(indexDirectoryProvider.GetDirectory(), new KeywordAnalyzer(), IndexWriter.MaxFieldLength.UNLIMITED);

			Query query = MultiFieldQueryParser.Parse(Lucene.Net.Util.Version.LUCENE_29, new string[] { page.FullName }, new string[] { SearchField.PageFullName.AsString()}, new BooleanClause.Occur[] { BooleanClause.Occur.MUST }, new KeywordAnalyzer());

			writer.DeleteDocuments(query);
			writer.Commit();
			writer.Close();
			return true;
		}

		/// <summary>
		/// Unindexes the message.
		/// </summary>
		/// <param name="directory">The Lucene.NET directory to be used.</param>
		/// <param name="message">The message to be unindexed.</param>
		/// <returns><c>true</c> if the message has been unindexed succesfully, <c>false</c> otherwise.</returns>
		public static bool UnindexMessage(Directory directory, Message message, PageContent page) {
			IIndexDirectoryProviderV40 indexDirectoryProvider = Collectors.CollectorsBox.GetIndexDirectoryProvider(page.Provider.CurrentWiki);

			IndexWriter writer = new IndexWriter(indexDirectoryProvider.GetDirectory(), new KeywordAnalyzer(), IndexWriter.MaxFieldLength.UNLIMITED);

			Query query = MultiFieldQueryParser.Parse(Lucene.Net.Util.Version.LUCENE_29, new string[] { page.FullName, message.ID.ToString() }, new string[] { SearchField.PageFullName.AsString(), SearchField.MessageId.AsString() }, new BooleanClause.Occur[] { BooleanClause.Occur.MUST, BooleanClause.Occur.MUST }, new KeywordAnalyzer());

			writer.DeleteDocuments(query);
			writer.Commit();
			writer.Close();
			return true;
		}

		private static DocumentType DocumentTypeFromString(string documentType) {
			switch(documentType) {
				case "page":
					return DocumentType.Page;
				case "message":
					return DocumentType.Message;
				case "attachment":
					return DocumentType.Attachment;
				case "file":
					return DocumentType.File;
				default:
					throw new ArgumentException("The given string is not a valid DocumentType.");
			}
		}

		private static string DocumentTypeToString(DocumentType documentType) {
			switch(documentType) {
				case DocumentType.Page:
					return "page";
				case DocumentType.Message:
					return "message";
				case DocumentType.Attachment:
					return "attachment";
				case DocumentType.File:
					return "file";
				default:
					throw new ArgumentException("The given document type is not valid");
			}
		}

	}

	/// <summary>
	/// Lists legal values for Search Options
	/// </summary>
	public enum SearchOptions {
		/// <summary>
		/// Search return results that contains at least one of the given words.
		/// </summary>
		AtLeastOneWord,
		/// <summary>
		/// Search return results that contains all the given words.
		/// </summary>
		AllWords
	}

	/// <summary>
	/// Lists legal values for DocumentType.
	/// </summary>
	public enum DocumentType {
		/// <summary>
		/// The document returned is a page.
		/// </summary>
		Page,
		/// <summary>
		/// The document returned is a message.
		/// </summary>
		Message,
		/// <summary>
		/// The document returned is a page attachment.
		/// </summary>
		Attachment,
		/// <summary>
		/// The document returned is a file.
		/// </summary>
		File
	}
}
