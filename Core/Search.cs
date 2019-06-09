using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Search.Highlight;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki
{
	/// <summary>
	/// Allow access to the search engine.
	/// </summary>
	public static class SearchClass
	{

		/// <summary>
		/// Searches the specified phrase in the specified search fields.
		/// </summary>
		/// <param name="searchFields">The search fields.</param>
		/// <param name="phrase">The phrase to search.</param>
		/// <param name="searchOption">The search options.</param>
		/// <returns>A list of <see cref="SearchResult"/> items.</returns>
		public static List<SearchResult> Search(SearchField[] searchFields, string phrase, SearchOptions searchOption)
		{
			IIndexDirectoryProviderV30 indexDirectoryProvider = Collectors.IndexDirectoryProvider;
			Analyzer analyzer = new SimpleAnalyzer();

			using(IndexSearcher searcher = new IndexSearcher(indexDirectoryProvider.GetDirectory(), false))
			{
				string[] searchFieldsAsString = (from f in searchFields select f.AsString()).ToArray();
				MultiFieldQueryParser queryParser = new MultiFieldQueryParser(Lucene.Net.Util.Version.LUCENE_29, searchFieldsAsString, analyzer);

				if(searchOption == SearchOptions.AllWords)
				{
					queryParser.DefaultOperator = QueryParser.Operator.AND;
				}

				if(searchOption == SearchOptions.AtLeastOneWord)
				{
					queryParser.DefaultOperator = QueryParser.Operator.OR;
				}

				try
				{
					Query query = queryParser.Parse(phrase);
					TopDocs topDocs = searcher.Search(query, 100);

					Highlighter highlighter = new Highlighter(new SimpleHTMLFormatter("<b class=\"searchkeyword\">", "</b>"), new QueryScorer(query));

					List<SearchResult> searchResults = new List<SearchResult>(topDocs.TotalHits);
					for(int i = 0; i < Math.Min(100, topDocs.TotalHits); i++)
					{
						Document doc = searcher.Doc(topDocs.ScoreDocs[i].Doc);

						SearchResult result = new SearchResult();
						result.DocumentType = DocumentTypeFromString(doc.GetField(SearchField.DocumentType.AsString()).StringValue);
						result.Relevance = topDocs.ScoreDocs[i].Score * 100;
						switch(result.DocumentType)
						{
							case DocumentType.Page:
								PageDocument page = new PageDocument();
								page.PageFullName = doc.GetField(SearchField.PageFullName.AsString()).StringValue;
								page.Title = doc.GetField(SearchField.Title.AsString()).StringValue;

								TokenStream tokenStream1 = analyzer.TokenStream(SearchField.Title.AsString(), new StringReader(page.Title));
								page.HighlightedTitle = highlighter.GetBestFragments(tokenStream1, page.Title, 3, " [...] ");

								page.Content = doc.GetField(SearchField.Content.AsString()).StringValue;

								tokenStream1 = analyzer.TokenStream(SearchField.Content.AsString(), new StringReader(page.Content));
								page.HighlightedContent = highlighter.GetBestFragments(tokenStream1, page.Content, 3, " [...] ");

								result.Document = page;
								break;
							case DocumentType.Message:
								MessageDocument message = new MessageDocument();
								message.PageFullName = doc.GetField(SearchField.PageFullName.AsString()).StringValue;
								message.DateTime = DateTime.Parse(doc.GetField(SearchField.MessageDateTime.AsString()).StringValue);
								message.Subject = doc.GetField(SearchField.Title.AsString()).StringValue;
								message.Body = doc.GetField(SearchField.Content.AsString()).StringValue;

								TokenStream tokenStream2 = analyzer.TokenStream(SearchField.Content.AsString(), new StringReader(message.Body));
								message.HighlightedBody = highlighter.GetBestFragments(tokenStream2, message.Body, 3, " [...] ");

								result.Document = message;
								break;
							case DocumentType.Attachment:
								PageAttachmentDocument attachment = new PageAttachmentDocument();
								attachment.PageFullName = doc.GetField(SearchField.PageFullName.AsString()).StringValue;
								attachment.FileName = doc.GetField(SearchField.FileName.AsString()).StringValue;
								attachment.FileContent = doc.GetField(SearchField.FileContent.AsString()).StringValue;

								TokenStream tokenStream3 = analyzer.TokenStream(SearchField.Content.AsString(), new StringReader(attachment.FileContent));
								attachment.HighlightedFileContent = highlighter.GetBestFragments(tokenStream3, attachment.FileContent, 3, " [...] ");

								result.Document = attachment;
								break;
							case DocumentType.File:
								FileDocument file = new FileDocument();
								file.FileName = doc.GetField(SearchField.FileName.AsString()).StringValue;
								file.FileContent = doc.GetField(SearchField.FileContent.AsString()).StringValue;

								TokenStream tokenStream4 = analyzer.TokenStream(SearchField.Content.AsString(), new StringReader(file.FileContent));
								file.HighlightedFileContent = highlighter.GetBestFragments(tokenStream4, file.FileContent, 3, " [...]");

								result.Document = file;
								break;
							case DocumentType.SourceControlFile:
								FileDocument scfile = new FileDocument();
								scfile.FileName = doc.GetField(SearchField.FileName.AsString()).StringValue;
								scfile.FileContent = doc.GetField(SearchField.FileContent.AsString()).StringValue;

								TokenStream tokenStream5 = analyzer.TokenStream(SearchField.Content.AsString(), new StringReader(scfile.FileContent));
								scfile.HighlightedFileContent = highlighter.GetBestFragments(tokenStream5, scfile.FileContent, 3, " [...]");

								result.Document = scfile;
								break;
						}

						searchResults.Add(result);
					}
					return searchResults;
				}
				catch(ParseException)
				{
					return new List<SearchResult>(0);
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="docCount"></param>
		/// <param name="wordCount"></param>
		/// <param name="matchCount"></param>
		/// <param name="size"></param>
		public static void GetIndexStats(out int docCount, out long wordCount, out int matchCount, out long size)
		{
			docCount = 0;
			wordCount = 0;
			matchCount = 0;
			size = 0;

			IIndexDirectoryProviderV30 indexDirectoryProvider = Collectors.IndexDirectoryProvider;
			using(IndexReader reader = IndexReader.Open(indexDirectoryProvider.GetDirectory(), true))
			{
				docCount = reader.NumDocs();
				wordCount = reader.UniqueTermCount;
			}
			size = (from strFile in indexDirectoryProvider.GetDirectory().ListAll() select indexDirectoryProvider.GetDirectory().FileLength(strFile)).Sum();
		}

		/// <summary>
		/// Indexes the page.
		/// </summary>
		/// <param name="page">The page page to be intexed.</param>
		/// <returns><c>true</c> if the page has been indexed succesfully, <c>false</c> otherwise.</returns>
		public static bool IndexPage(PageContent page)
		{
			IIndexDirectoryProviderV30 indexDirectoryProvider = Collectors.IndexDirectoryProvider;

			Analyzer analyzer = new SimpleAnalyzer();
			using(IndexWriter writer = new IndexWriter(indexDirectoryProvider.GetDirectory(), analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
			{
				Document doc = new Document();
				doc.Add(new Field(SearchField.Key.AsString(), (DocumentTypeToString(DocumentType.Page) + "|" + page.FullName).Replace(" ", ""), Field.Store.NO, Field.Index.NOT_ANALYZED_NO_NORMS));
				doc.Add(new Field(SearchField.DocumentType.AsString(), DocumentTypeToString(DocumentType.Page), Field.Store.YES, Field.Index.NOT_ANALYZED));
				doc.Add(new Field(SearchField.PageFullName.AsString(), page.FullName, Field.Store.YES, Field.Index.ANALYZED));
				doc.Add(new Field(SearchField.Title.AsString(), page.Title, Field.Store.YES, Field.Index.ANALYZED));
				doc.Add(new Field(SearchField.Content.AsString(), page.Content, Field.Store.YES, Field.Index.ANALYZED));
				writer.AddDocument(doc);
				writer.Commit();
			}
			return true;
		}

		/// <summary>
		/// Indexes the message.
		/// </summary>
		/// <param name="message">The message to be indexed.</param>
		/// <param name="page">The page the message belongs to.</param>
		/// <returns><c>true</c> if the message has been indexed succesfully, <c>false</c> otherwise.</returns>
		public static bool IndexMessage(Message message, PageContent page)
		{
			IIndexDirectoryProviderV30 indexDirectoryProvider = Collectors.IndexDirectoryProvider;

			Analyzer analyzer = new SimpleAnalyzer();
			
			using(IndexWriter writer = new IndexWriter(indexDirectoryProvider.GetDirectory(), analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
			{
				Document doc = new Document();
				doc.Add(new Field(SearchField.DocumentType.AsString(), DocumentTypeToString(DocumentType.Message), Field.Store.YES, Field.Index.NOT_ANALYZED));
				doc.Add(new Field(SearchField.PageFullName.AsString(), page.PageInfo.FullName, Field.Store.YES, Field.Index.NOT_ANALYZED));
				doc.Add(new Field(SearchField.MessageId.AsString(), message.ID.ToString(), Field.Store.NO, Field.Index.NOT_ANALYZED));
				doc.Add(new Field(SearchField.Title.AsString(), message.Subject, Field.Store.YES, Field.Index.ANALYZED));
				doc.Add(new Field(SearchField.Content.AsString(), message.Body, Field.Store.YES, Field.Index.ANALYZED));
				doc.Add(new Field(SearchField.MessageDateTime.AsString(), message.DateTime.ToString("yyyy/MM/dd HH:mm:ss"), Field.Store.YES, Field.Index.NO));
				writer.AddDocument(doc);
				writer.Commit();
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
		public static bool IndexPageAttachment(string fileName, string filePath, PageContent page)
		{
			IIndexDirectoryProviderV30 indexDirectoryProvider = Collectors.IndexDirectoryProvider;

			Analyzer analyzer = new SimpleAnalyzer();
			using(IndexWriter writer = new IndexWriter(indexDirectoryProvider.GetDirectory(), analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
			{
				try
				{
					Document doc = new Document();
					doc.Add(new Field(SearchField.Key.AsString(), (DocumentTypeToString(DocumentType.Attachment) + "|" + page.FullName + "|" + fileName).Replace(" ", ""), Field.Store.NO, Field.Index.NOT_ANALYZED_NO_NORMS));
					doc.Add(new Field(SearchField.DocumentType.AsString(), DocumentTypeToString(DocumentType.Attachment), Field.Store.YES, Field.Index.ANALYZED));
					doc.Add(new Field(SearchField.PageFullName.AsString(), page.FullName, Field.Store.YES, Field.Index.ANALYZED));
					doc.Add(new Field(SearchField.FileName.AsString(), fileName, Field.Store.YES, Field.Index.ANALYZED));
					string fileContent = SearchEngine.Parser.Parse(filePath);
					doc.Add(new Field(SearchField.FileContent.AsString(), fileContent, Field.Store.YES, Field.Index.ANALYZED));
					writer.AddDocument(doc);
					writer.Commit();
				}
				catch(System.Runtime.InteropServices.COMException ex)
				{
					Log.LogEntry(ex.Message, EntryType.Warning, Log.SystemUsername);
				}
			}

			return true;
		}

		/// <summary>
		/// Indexes a file.
		/// </summary>
		/// <param name="fileName">The name of the file to be indexed.</param>
		/// <param name="filePath">The path of the file to be indexed.</param>
		/// <returns><c>true</c> if the message has been indexed succesfully, <c>false</c> otherwise.</returns>
		public static bool IndexFile(string fileName, string filePath)
		{
			IIndexDirectoryProviderV30 indexDirectoryProvider = Collectors.IndexDirectoryProvider;

			Analyzer analyzer = new SimpleAnalyzer();
			using(IndexWriter writer = new IndexWriter(indexDirectoryProvider.GetDirectory(), analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
			{
				try
				{
					Document doc = new Document();
					doc.Add(new Field(SearchField.Key.AsString(), (DocumentTypeToString(DocumentType.File) + "|" + fileName).Replace(" ", ""), Field.Store.NO, Field.Index.NOT_ANALYZED_NO_NORMS));
					doc.Add(new Field(SearchField.DocumentType.AsString(), DocumentTypeToString(DocumentType.File), Field.Store.YES, Field.Index.ANALYZED));
					doc.Add(new Field(SearchField.FileName.AsString(), fileName, Field.Store.YES, Field.Index.ANALYZED));
					string fileContent = SearchEngine.Parser.Parse(filePath);
					doc.Add(new Field(SearchField.FileContent.AsString(), fileContent, Field.Store.YES, Field.Index.ANALYZED));
					writer.AddDocument(doc);
					writer.Commit();
				}
				catch(System.Runtime.InteropServices.COMException ex)
				{
					Log.LogEntry(ex.Message, EntryType.Warning, Log.SystemUsername);
				}
			}
			return true;
		}

		/// <summary>
		/// Unindexes the page.
		/// </summary>
		/// <param name="page">The page to be unindexed.</param>
		/// <returns><c>true</c> if the page has been unindexed succesfully, <c>false</c> otherwise.</returns>
		public static bool UnindexPage(PageContent page)
		{
			IIndexDirectoryProviderV30 indexDirectoryProvider = Collectors.IndexDirectoryProvider;

			using(IndexWriter writer = new IndexWriter(indexDirectoryProvider.GetDirectory(), new KeywordAnalyzer(), IndexWriter.MaxFieldLength.UNLIMITED))
			{
				writer.DeleteDocuments(new Term(SearchField.Key.AsString(), (DocumentTypeToString(DocumentType.Page) + "|" + page.FullName).Replace(" ", "")));
				writer.Commit();
			}
			return true;
		}

		/// <summary>
		/// Unindexes the message.
		/// </summary>
		/// <param name="messageId">The id of the message to be unindexed.</param>
		/// <param name="page">The page the message belongs to.</param>
		/// <returns><c>true</c> if the message has been unindexed succesfully, <c>false</c> otherwise.</returns>
		public static bool UnindexMessage(int messageId, PageContent page)
		{
			IIndexDirectoryProviderV30 indexDirectoryProvider = Collectors.IndexDirectoryProvider;

			using(IndexWriter writer = new IndexWriter(indexDirectoryProvider.GetDirectory(), new KeywordAnalyzer(), IndexWriter.MaxFieldLength.UNLIMITED))
			{
				Query query = MultiFieldQueryParser.Parse(Lucene.Net.Util.Version.LUCENE_30
				, new string[] { DocumentTypeToString(DocumentType.Message), page.PageInfo.FullName, messageId.ToString() }
				, new string[] { SearchField.DocumentType.AsString(), SearchField.PageFullName.AsString(), SearchField.MessageId.AsString() }
				, new Occur[] { Occur.MUST, Occur.MUST, Occur.MUST }, new KeywordAnalyzer());
				writer.DeleteDocuments(query);
				writer.Commit();
			}

			return true;
		}

		/// <summary>
		/// Unindexes the page attachment.
		/// </summary>
		/// <param name="fileName">The name of the attachment.</param>
		/// <param name="page">The page the attachment belongs to.</param>
		/// <returns><c>true</c> if the attachment has been unindexed succesfully, <c>false</c> otherwise.</returns>
		public static bool UnindexPageAttachment(string fileName, PageContent page)
		{
			IIndexDirectoryProviderV30 indexDirectoryProvider = Collectors.IndexDirectoryProvider;

			using(IndexWriter writer = new IndexWriter(indexDirectoryProvider.GetDirectory(), new SimpleAnalyzer(), IndexWriter.MaxFieldLength.UNLIMITED))
			{
				writer.DeleteDocuments(new Term(SearchField.Key.AsString()
					, (DocumentTypeToString(DocumentType.Attachment) + "|" + page.PageInfo.FullName + "|" + fileName).Replace(" ", "")));
				writer.Commit();
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
		public static bool RenamePageAttachment(PageContent page, string oldName, string newName)
		{
			IIndexDirectoryProviderV30 indexDirectoryProvider = Collectors.IndexDirectoryProvider;

			Analyzer analyzer = new SimpleAnalyzer();
			Term term = new Term(SearchField.Key.AsString(), (DocumentTypeToString(DocumentType.Attachment) + "|" + page.PageInfo.FullName + "|" + oldName).Replace(" ", ""));

			
			
			Query query = new TermQuery(term);
			using(IndexWriter writer = new IndexWriter(indexDirectoryProvider.GetDirectory(), analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
			using(IndexSearcher searcher = new IndexSearcher(indexDirectoryProvider.GetDirectory(), false))
			{
				TopDocs topDocs = searcher.Search(query, 100);
				if(topDocs.ScoreDocs.Length == 0)
				{
					return true;
				}

				Document doc = searcher.Doc(topDocs.ScoreDocs[0].Doc);

				Document newDoc = new Document();
				newDoc.Add(new Field(SearchField.Key.AsString(), (DocumentTypeToString(DocumentType.Attachment) + "|" + page.PageInfo.FullName + "|" + newName).Replace(" ", ""), Field.Store.NO, Field.Index.NOT_ANALYZED_NO_NORMS));
				newDoc.Add(new Field(SearchField.DocumentType.AsString(), DocumentTypeToString(DocumentType.Attachment), Field.Store.YES, Field.Index.NO));
				newDoc.Add(new Field(SearchField.PageFullName.AsString(), page.PageInfo.FullName, Field.Store.YES, Field.Index.ANALYZED));
				newDoc.Add(new Field(SearchField.FileName.AsString(), newName, Field.Store.YES, Field.Index.ANALYZED));
				newDoc.Add(new Field(SearchField.FileContent.AsString(), doc.GetField(SearchField.FileContent.AsString()).StringValue, Field.Store.YES, Field.Index.ANALYZED));
				writer.UpdateDocument(term, newDoc);
				writer.Commit();
			}
			return true;
		}

		/// <summary>
		/// Unindexes the file.
		/// </summary>
		/// <param name="fileName">The name of the attachment.</param>
		/// <returns><c>true</c> if the file has been unindexed succesfully, <c>false</c> otherwise.</returns>
		public static bool UnindexFile(string fileName)
		{
			IIndexDirectoryProviderV30 indexDirectoryProvider = Collectors.IndexDirectoryProvider;

			using(IndexWriter writer = new IndexWriter(indexDirectoryProvider.GetDirectory(), new SimpleAnalyzer(), IndexWriter.MaxFieldLength.UNLIMITED))
			{
				writer.DeleteDocuments(new Term(SearchField.Key.AsString(), (DocumentTypeToString(DocumentType.File) + "|" + fileName).Replace(" ", "")));
				writer.Commit();
			}
			return true;
		}

		/// <summary>
		/// Rebuilds the index
		/// </summary>
		/// <returns><c>true</c> if the file has been unindexed succesfully, <c>false</c> otherwise.</returns>
		public static bool RebuildIndex()
		{
			if((true))//DateTime.Now - Settings.LastPageIndexing).TotalDays > 7 || Collectors.IndexDirectoryProvider.GetDirectory().ListAll().Length == 0)
			{
				Settings.LastPageIndexing = DateTime.Now;
				System.Threading.Thread.Sleep(10000);
				foreach(var provider in Collectors.PagesProviderCollector.AllProviders)
				{
					if(!provider.ReadOnly)
					{
						Log.LogEntry("Starting automatic rebuilding index for provider: " + provider.Information.Name, EntryType.General, Log.SystemUsername);
						foreach(var nspace in provider.GetNamespaces())
						{
							PageInfo[] pages = provider.GetPages(nspace);
							foreach(var item in pages)
							{
								var pageContent = Content.GetPageContent(item, false);
								UnindexPage(pageContent);
								IndexPage(pageContent);

								foreach(var message in item.Provider.GetMessages(item))
								{
									UnindexMessage(message.ID, pageContent);
									IndexMessage(message, pageContent);
								}
							}
						}

						PageInfo[] nonspages = provider.GetPages(null);
						foreach(var item in nonspages)
						{
							var pageContent = Content.GetPageContent(item, false);
							UnindexPage(pageContent);
							IndexPage(pageContent);

							foreach(var message in item.Provider.GetMessages(item))
							{
								UnindexMessage(message.ID, pageContent);
								IndexMessage(message, pageContent);
							}
						}
						Log.LogEntry("Finished automatic rebuilding index for provider: " + provider.Information.Name, EntryType.General, Log.SystemUsername);
					}
				}
				foreach(var prov in Collectors.VersionedFilesProviderCollector.AllProviders)
				{
					Log.LogEntry("Starting automatic rebuilding index for provider: " + prov.Information.Name, EntryType.General, Log.SystemUsername);
					var files = prov.ListFiles("\\");
					//var allDirs = prov.ListDirectories(null).ToList();
					//allDirs.Insert(0, "/");
					foreach(var file in files)
					{
					}
					Log.LogEntry("Finished automatic rebuilding files index for provider: " + prov.Information.Name, EntryType.General, Log.SystemUsername);
				}
				foreach(var prov in Collectors.FilesProviderCollector.AllProviders)
				{
					Log.LogEntry("Starting automatic rebuilding index for provider: " + prov.Information.Name, EntryType.General, Log.SystemUsername);
					var allDirs = prov.ListDirectories(null).ToList();
					allDirs.Insert(0, "/");
					foreach(var dir in allDirs)
					{
						var allFiles = prov.ListFiles(dir);
						foreach(var file in allFiles)
						{
							FileDetails fileDetails = prov.GetFileDetails(file);
							FileInfo fileInfo = new FileInfo(file);

							// Index the attached file
							string tempDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());
							if(!System.IO.Directory.Exists(tempDir))
							{
								System.IO.Directory.CreateDirectory(tempDir);
							}
							string tempFile = Path.Combine(tempDir, fileInfo.Name);
							using(MemoryStream ms = new MemoryStream(1048576))
							{
								prov.RetrieveFile(file, ms, false);
								ms.Seek(0, SeekOrigin.Begin);
								using(FileStream temp = File.Create(tempFile))
								{
									ms.CopyTo(temp);
								}
								IndexFile(prov.GetType().FullName + "|" + fileInfo.Name, tempFile);
							}
							System.IO.Directory.Delete(tempDir, true);
						}
					}
					string[] pagesWithAttachments = prov.GetPagesWithAttachments();
					foreach(string pageWithAttachments in pagesWithAttachments)
					{
						var pageInfo = Pages.FindPage(pageWithAttachments);
						var pageContent = Content.GetPageContent(pageInfo, false);
						string[] attachments = prov.ListPageAttachments(pageInfo);
						foreach(var attachment in attachments)
						{
							var fileDetails = prov.GetPageAttachmentDetails(Pages.FindPage(pageWithAttachments), attachment);

							string name = attachment;
							// Index the attached file
							string tempDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());
							if(!System.IO.Directory.Exists(tempDir))
							{
								System.IO.Directory.CreateDirectory(tempDir);
							}

							string tempFile = Path.Combine(tempDir, name);
							using(MemoryStream ms = new MemoryStream(1048576))
							{
								prov.RetrievePageAttachment(pageInfo, attachment, ms, false);
								ms.Seek(0, SeekOrigin.Begin);
								using(FileStream temp = File.Create(tempFile))
								{
									ms.CopyTo(temp);
								}
								IndexPageAttachment(name, tempFile, pageContent);
							}
							System.IO.Directory.Delete(tempDir, true);
						}
					}
					Log.LogEntry("Finished automatic rebuilding files and attachments index for provider: " + prov.Information.Name, EntryType.General, Log.SystemUsername);
				}
			}
			return true;
		}

		/// <summary>
		/// Renames a file in the index.
		/// </summary>
		/// <param name="oldName">The old attachment name.</param>
		/// <param name="newName">The new attachment name.</param>
		public static bool RenameFile(string oldName, string newName)
		{
			IIndexDirectoryProviderV30 indexDirectoryProvider = Collectors.IndexDirectoryProvider;

			Analyzer analyzer = new SimpleAnalyzer();
			Term term = new Term(SearchField.Key.AsString(), (DocumentTypeToString(DocumentType.File) + "|" + oldName).Replace(" ", ""));

			Query query = new TermQuery(term);
			using(IndexWriter writer = new IndexWriter(indexDirectoryProvider.GetDirectory(), analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
			using(IndexSearcher searcher = new IndexSearcher(indexDirectoryProvider.GetDirectory(), false))
			{
				TopDocs topDocs = searcher.Search(query, 100);
				if(topDocs.ScoreDocs.Length == 0)
				{
					return true;
				}

				Document doc = searcher.Doc(topDocs.ScoreDocs[0].Doc);

				Document newDoc = new Document();
				newDoc.Add(new Field(SearchField.Key.AsString(), (DocumentTypeToString(DocumentType.File) + "|" + newName).Replace(" ", ""), Field.Store.NO, Field.Index.NOT_ANALYZED_NO_NORMS));
				newDoc.Add(new Field(SearchField.DocumentType.AsString(), DocumentTypeToString(DocumentType.File), Field.Store.YES, Field.Index.ANALYZED));
				newDoc.Add(new Field(SearchField.FileName.AsString(), newName, Field.Store.YES, Field.Index.ANALYZED));
				newDoc.Add(new Field(SearchField.FileContent.AsString(), doc.GetField(SearchField.FileContent.AsString()).StringValue, Field.Store.YES, Field.Index.ANALYZED));
				writer.UpdateDocument(term, newDoc);
				writer.Commit();
			}
			return true;
		}

		/// <summary>
		/// Clears the index (files excluded).
		/// </summary>
		public static void ClearIndex()
		{
			IIndexDirectoryProviderV30 indexDirectoryProvider = Collectors.IndexDirectoryProvider;

			Analyzer analyzer = new SimpleAnalyzer();
			using(IndexWriter writer = new IndexWriter(indexDirectoryProvider.GetDirectory(), analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
			{
				ClearMessagesIndex(writer);
				ClearPagesIndex(writer);

				writer.Commit();
			}
		}

		private static void ClearAttachmentsIndex(IndexWriter writer)
		{
			writer.DeleteDocuments(new Term(SearchField.DocumentType.AsString(), DocumentTypeToString(DocumentType.Attachment)));
		}

		private static void ClearMessagesIndex(IndexWriter writer)
		{
			writer.DeleteDocuments(new Term(SearchField.DocumentType.AsString(), DocumentTypeToString(DocumentType.Message)));
		}

		private static void ClearPagesIndex(IndexWriter writer)
		{
			writer.DeleteDocuments(new Term(SearchField.DocumentType.AsString(), DocumentTypeToString(DocumentType.Page)));
		}

		/// <summary>
		/// Clears the files index.
		/// </summary>
		public static void ClearFilesIndex()
		{
			IIndexDirectoryProviderV30 indexDirectoryProvider = Collectors.IndexDirectoryProvider;

			Analyzer analyzer = new SimpleAnalyzer();
			using(IndexWriter writer = new IndexWriter(indexDirectoryProvider.GetDirectory(), analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
			{
				ClearFilesIndex(writer);
				ClearAttachmentsIndex(writer);

				writer.Commit();
			}
		}

		private static void ClearFilesIndex(IndexWriter writer)
		{
			writer.DeleteDocuments(new Term(SearchField.DocumentType.AsString(), DocumentTypeToString(DocumentType.File)));
		}

		private static DocumentType DocumentTypeFromString(string documentType)
		{
			switch(documentType)
			{
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

		private static string DocumentTypeToString(DocumentType documentType)
		{
			switch(documentType)
			{
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
	public enum SearchOptions
	{
		/// <summary>
		/// Search return results that contains at least one of the given words.
		/// </summary>
		AtLeastOneWord,
		/// <summary>
		/// Search return results that contains all the given words.
		/// </summary>
		AllWords,
		/// <summary>
		/// Search return results that contains the exact given phrase.
		/// </summary>
		ExactPhrase
	}

	/// <summary>
	/// Lists legal values for DocumentType.
	/// </summary>
	public enum DocumentType
	{
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
		File,
		/// <summary>
		/// The document returned is a file.
		/// </summary>
		SourceControlFile
	}
}
