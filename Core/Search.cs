
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
using System.IO;
using Lucene.Net.Highlight;

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
			Analyzer analyzer = new SimpleAnalyzer();

			IndexSearcher searcher = new IndexSearcher(indexDirectoryProvider.GetDirectory(), false);

			string[] searchFieldsAsString = (from f in searchFields select f.AsString()).ToArray();
			MultiFieldQueryParser queryParser = new MultiFieldQueryParser(Lucene.Net.Util.Version.LUCENE_29, searchFieldsAsString, analyzer);
			
			if(searchOption == SearchOptions.AllWords) queryParser.SetDefaultOperator(QueryParser.Operator.AND);
			if(searchOption == SearchOptions.AtLeastOneWord) queryParser.SetDefaultOperator(QueryParser.Operator.OR);
			
			Query query = queryParser.Parse(phrase);
			TopDocs topDocs = searcher.Search(query, 100);

			Highlighter highlighter = new Highlighter(new SimpleHTMLFormatter("<b class=\"searchkeyword\">", "</b>"), new QueryScorer(query));

			List<SearchResult> searchResults = new List<SearchResult>(topDocs.totalHits);
			for(int i = 0; i < topDocs.totalHits; i++) {
				Document doc = searcher.Doc(topDocs.scoreDocs[i].doc);

				SearchResult result = new SearchResult();
				result.DocumentType = DocumentTypeFromString(doc.GetField(SearchField.DocumentType.AsString()).StringValue());
				result.Relevance = topDocs.scoreDocs[i].score * 100;
				switch(result.DocumentType) {
					case DocumentType.Page:
						PageDocument page = new PageDocument();
						page.Wiki = doc.GetField(SearchField.Wiki.AsString()).StringValue();
						page.PageFullName = doc.GetField(SearchField.PageFullName.AsString()).StringValue();
						page.Title = doc.GetField(SearchField.Title.AsString()).StringValue();

						TokenStream tokenStream1 = analyzer.TokenStream(SearchField.Title.AsString(), new StringReader(page.Title));
						page.HighlightedTitle = highlighter.GetBestFragments(tokenStream1, page.Title, 3, " [...] ");

						page.Content = doc.GetField(SearchField.Content.AsString()).StringValue();

						tokenStream1 = analyzer.TokenStream(SearchField.Content.AsString(), new StringReader(page.Content));
						page.HighlightedContent = highlighter.GetBestFragments(tokenStream1, page.Content, 3, " [...] ");

						result.Document = page;
						break;
					case DocumentType.Message:
						MessageDocument message = new MessageDocument();
						message.Wiki = doc.GetField(SearchField.Wiki.AsString()).StringValue();
						message.PageFullName = doc.GetField(SearchField.PageFullName.AsString()).StringValue();
						message.DateTime = DateTime.Parse(doc.GetField(SearchField.MessageDateTime.AsString()).StringValue());
						message.Subject = doc.GetField(SearchField.Title.AsString()).StringValue();
						message.Body = doc.GetField(SearchField.Content.AsString()).StringValue();

						TokenStream tokenStream2 = analyzer.TokenStream(SearchField.Content.AsString(), new StringReader(message.Body));
						message.HighlightedBody = highlighter.GetBestFragments(tokenStream2, message.Body, 3, " [...] ");

						result.Document = message;
						break;
					case DocumentType.Attachment:
						PageAttachmentDocument attachment = new PageAttachmentDocument();
						attachment.Wiki = doc.GetField(SearchField.Wiki.AsString()).StringValue();
						attachment.PageFullName = doc.GetField(SearchField.PageFullName.AsString()).StringValue();
						attachment.FileName = doc.GetField(SearchField.Title.AsString()).StringValue();
						attachment.FileContent = doc.GetField(SearchField.Content.AsString()).StringValue();

						TokenStream tokenStream3 = analyzer.TokenStream(SearchField.Content.AsString(), new StringReader(attachment.FileContent));
						attachment.HighlightedFileContent = highlighter.GetBestFragments(tokenStream3, attachment.FileContent, 3, " [...] ");

						result.Document = attachment;
						break;
				}

				searchResults.Add(result);
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
			try {
				Document doc = new Document();
				doc.Add(new Field(SearchField.DocumentType.AsString(), DocumentTypeToString(DocumentType.Page), Field.Store.YES, Field.Index.NOT_ANALYZED));
				doc.Add(new Field(SearchField.Wiki.AsString(), page.Provider.CurrentWiki, Field.Store.YES, Field.Index.NOT_ANALYZED));
				doc.Add(new Field(SearchField.PageFullName.AsString(), page.FullName, Field.Store.YES, Field.Index.NOT_ANALYZED));
				doc.Add(new Field(SearchField.Title.AsString(), page.Title, Field.Store.YES, Field.Index.ANALYZED));
				doc.Add(new Field(SearchField.Content.AsString(), page.Content, Field.Store.YES, Field.Index.ANALYZED));
				writer.AddDocument(doc);
				writer.Commit();
			}
			catch { throw; }
			finally {
				writer.Close();
			}
			return true;
		}

		/// <summary>
		/// Indexes the message.
		/// </summary>
		/// <param name="message">The message to be indexed.</param>
		/// <param name="page">The page the message belongs to.</param>
		/// <returns><c>true</c> if the message has been indexed succesfully, <c>false</c> otherwise.</returns>
		public static bool IndexMessage(Message message, PageContent page) {
			IIndexDirectoryProviderV40 indexDirectoryProvider = Collectors.CollectorsBox.GetIndexDirectoryProvider(page.Provider.CurrentWiki);

			Analyzer analyzer = new SimpleAnalyzer();
			IndexWriter writer = new IndexWriter(indexDirectoryProvider.GetDirectory(), analyzer, IndexWriter.MaxFieldLength.UNLIMITED);

			try {
				Document doc = new Document();
				doc.Add(new Field(SearchField.DocumentType.AsString(), DocumentTypeToString(DocumentType.Message), Field.Store.YES, Field.Index.NOT_ANALYZED));
				doc.Add(new Field(SearchField.Wiki.AsString(), page.Provider.CurrentWiki, Field.Store.YES, Field.Index.NOT_ANALYZED));
				doc.Add(new Field(SearchField.PageFullName.AsString(), page.FullName, Field.Store.YES, Field.Index.NOT_ANALYZED));
				doc.Add(new Field(SearchField.MessageId.AsString(), message.ID.ToString(), Field.Store.NO, Field.Index.NOT_ANALYZED));
				doc.Add(new Field(SearchField.Title.AsString(), message.Subject, Field.Store.YES, Field.Index.ANALYZED));
				doc.Add(new Field(SearchField.Content.AsString(), message.Body, Field.Store.YES, Field.Index.ANALYZED));
				doc.Add(new Field(SearchField.MessageDateTime.AsString(), message.DateTime.ToString(), Field.Store.YES, Field.Index.NO));
				writer.AddDocument(doc);
				writer.Commit();
			}
			catch { throw; }
			finally {
				writer.Close();
			}
			return true;
		}

		/// <summary>
		/// Indexes a page attachment.
		/// </summary>
		/// <param name="fileName">The name of the attachment to be indexed.</param>
		/// <param name="filePath">The path of the file to be indexed.</param>
		/// <param name="page">The page the file is attached to.</param>
		/// <returns><c>true</c> if the message has been indexed succesfully, <c>false</c> otherwise.</returns>
		public static bool IndexPageAttachment(string fileName, string filePath, PageContent page) {
			IIndexDirectoryProviderV40 indexDirectoryProvider = Collectors.CollectorsBox.GetIndexDirectoryProvider(page.Provider.CurrentWiki);

			Analyzer analyzer = new SimpleAnalyzer();
			IndexWriter writer = new IndexWriter(indexDirectoryProvider.GetDirectory(), analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
			try {
				Document doc = new Document();
				doc.Add(new Field(SearchField.Key.AsString(), (DocumentTypeToString(DocumentType.Attachment) + "|" + page.Provider.CurrentWiki + "|" + page.FullName + "|" + fileName).Replace(" ", ""), Field.Store.NO, Field.Index.NOT_ANALYZED_NO_NORMS));
				doc.Add(new Field(SearchField.DocumentType.AsString(), DocumentTypeToString(DocumentType.Attachment), Field.Store.YES, Field.Index.ANALYZED));
				doc.Add(new Field(SearchField.Wiki.AsString(), page.Provider.CurrentWiki, Field.Store.YES, Field.Index.ANALYZED));
				doc.Add(new Field(SearchField.PageFullName.AsString(), page.FullName, Field.Store.YES, Field.Index.ANALYZED));
				doc.Add(new Field(SearchField.Title.AsString(), fileName, Field.Store.YES, Field.Index.ANALYZED));
				string fileContent = ScrewTurn.Wiki.SearchEngine.Parser.Parse(filePath);
				doc.Add(new Field(SearchField.Content.AsString(), fileContent, Field.Store.YES, Field.Index.ANALYZED));
				writer.AddDocument(doc);
				writer.Commit();
			}
			catch { throw; }
			finally {
				writer.Close();
			}
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

			try {
				Query query = MultiFieldQueryParser.Parse(Lucene.Net.Util.Version.LUCENE_29, new string[] { DocumentTypeToString(DocumentType.Page), page.FullName }, new string[] { SearchField.DocumentType.AsString(), SearchField.PageFullName.AsString() }, new BooleanClause.Occur[] { BooleanClause.Occur.MUST, BooleanClause.Occur.MUST }, new KeywordAnalyzer());
				writer.DeleteDocuments(query);
				writer.Commit();
			}
			catch { throw; }
			finally {
				writer.Close();
			}
			return true;
		}

		/// <summary>
		/// Unindexes the message.
		/// </summary>
		/// <param name="messageId">The id of the message to be unindexed.</param>
		/// <param name="page">The page the message belongs to.</param>
		/// <returns><c>true</c> if the message has been unindexed succesfully, <c>false</c> otherwise.</returns>
		public static bool UnindexMessage(int messageId, PageContent page) {
			IIndexDirectoryProviderV40 indexDirectoryProvider = Collectors.CollectorsBox.GetIndexDirectoryProvider(page.Provider.CurrentWiki);

			IndexWriter writer = new IndexWriter(indexDirectoryProvider.GetDirectory(), new KeywordAnalyzer(), IndexWriter.MaxFieldLength.UNLIMITED);
			try {
				Query query = MultiFieldQueryParser.Parse(Lucene.Net.Util.Version.LUCENE_29, new string[] { page.Provider.CurrentWiki, DocumentTypeToString(DocumentType.Message), page.FullName, messageId.ToString() }, new string[] { SearchField.Wiki.AsString(), SearchField.DocumentType.AsString(), SearchField.PageFullName.AsString(), SearchField.MessageId.AsString() }, new BooleanClause.Occur[] { BooleanClause.Occur.MUST, BooleanClause.Occur.MUST, BooleanClause.Occur.MUST, BooleanClause.Occur.MUST }, new KeywordAnalyzer());
				writer.DeleteDocuments(query);
				writer.Commit();
			}
			catch { throw; }
			finally {
				writer.Close();
			}
			return true;
		}

		/// <summary>
		/// Unindexes the message.
		/// </summary>
		/// <param name="fileName">The name of the attachment.</param>
		/// <param name="page">The page the attachment belongs to.</param>
		/// <returns><c>true</c> if the attachment has been unindexed succesfully, <c>false</c> otherwise.</returns>
		public static bool UnindexPageAttachment(string fileName, PageContent page) {
			IIndexDirectoryProviderV40 indexDirectoryProvider = Collectors.CollectorsBox.GetIndexDirectoryProvider(page.Provider.CurrentWiki);

			IndexWriter writer = new IndexWriter(indexDirectoryProvider.GetDirectory(), new SimpleAnalyzer(), IndexWriter.MaxFieldLength.UNLIMITED);
			try {
				writer.DeleteDocuments(new Term(SearchField.Key.AsString(), (DocumentTypeToString(DocumentType.Attachment) + "|" + page.Provider.CurrentWiki + "|" + page.FullName + "|" + fileName).Replace(" ", "")));
				writer.Commit();
			}
			catch { throw; }
			finally {
				writer.Close();
			}
			return true;
		}

		/// <summary>
		/// Renames a page attachment in the index.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <param name="oldName">The old attachment name.</param>
		/// <param name="newName">The new attachment name.</param>
		/// <returns></returns>
		public static bool RenamePageAttachment(PageContent page, string oldName, string newName) {
			IIndexDirectoryProviderV40 indexDirectoryProvider = Collectors.CollectorsBox.GetIndexDirectoryProvider(page.Provider.CurrentWiki);
			
			Analyzer analyzer = new SimpleAnalyzer();
			Term term = new Term(SearchField.Key.AsString(), (DocumentTypeToString(DocumentType.Attachment) + "|" + page.Provider.CurrentWiki + "|" + page.FullName + "|" + oldName).Replace(" ", ""));

			IndexWriter writer = new IndexWriter(indexDirectoryProvider.GetDirectory(), analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
			IndexSearcher searcher = new IndexSearcher(indexDirectoryProvider.GetDirectory(), false);
			Query query = new TermQuery(term);
			try {
				TopDocs topDocs = searcher.Search(query, 100);
				Document doc = searcher.Doc(topDocs.scoreDocs[0].doc);

				Document newDoc = new Document();
				newDoc.Add(new Field(SearchField.Key.AsString(), (DocumentTypeToString(DocumentType.Attachment) + "|" + page.Provider.CurrentWiki + "|" + page.FullName + "|" + newName).Replace(" ", ""), Field.Store.NO, Field.Index.NOT_ANALYZED_NO_NORMS));
				newDoc.Add(new Field(SearchField.DocumentType.AsString(), DocumentTypeToString(DocumentType.Attachment), Field.Store.YES, Field.Index.NO));
				newDoc.Add(new Field(SearchField.Wiki.AsString(), page.Provider.CurrentWiki, Field.Store.YES, Field.Index.ANALYZED));
				newDoc.Add(new Field(SearchField.PageFullName.AsString(), page.FullName, Field.Store.YES, Field.Index.ANALYZED));
				newDoc.Add(new Field(SearchField.Title.AsString(), newName, Field.Store.YES, Field.Index.ANALYZED));
				newDoc.Add(new Field(SearchField.Content.AsString(), doc.GetField(SearchField.Content.AsString()).StringValue(), Field.Store.YES, Field.Index.ANALYZED));
				writer.UpdateDocument(term, newDoc);
				writer.Commit();
			}
			catch { throw; }
			finally {
				searcher.Close();
				writer.Close();
			}
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
