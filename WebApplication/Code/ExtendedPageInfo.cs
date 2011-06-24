
using System;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Contains extended information about a Page.
	/// </summary>
	public class ExtendedPageInfo {

		private PageContent pageContent;
		private string title, creator, lastAuthor;
		private int messageCount;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:ExtendedPageInfo" /> class.
		/// </summary>
		/// <param name="pageContent">The <see cref="T:PageContent" /> object.</param>
		/// <param name="creator">The creator.</param>
		/// <param name="lastAuthor">The last author.</param>
		public ExtendedPageInfo(PageContent pageContent, string creator, string lastAuthor) {
			this.pageContent = pageContent;
			this.title = FormattingPipeline.PrepareTitle(Tools.DetectCurrentWiki(), pageContent.Title, false, FormattingContext.PageContent, pageContent.FullName);
			this.creator = creator;
			this.lastAuthor = lastAuthor;
			this.messageCount = Pages.GetMessageCount(pageContent);
		}

		/// <summary>
		/// Gets the PageContent object.
		/// </summary>
		public PageContent PageContent {
			get { return pageContent; }
		}

		/// <summary>
		/// Gets the title of the page.
		/// </summary>
		public string Title {
			get { return title; }
		}

		/// <summary>
		/// Gets the creation date/time.
		/// </summary>
		public DateTime CreationDateTime {
			get { return pageContent.CreationDateTime; }
		}

		/// <summary>
		/// Gets the creator.
		/// </summary>
		public string Creator {
			get { return creator; }
		}

		/// <summary>
		/// Gets the last author.
		/// </summary>
		public string LastAuthor {
			get { return lastAuthor; }
		}

		/// <summary>
		/// Gets the number of messages.
		/// </summary>
		public int MessageCount {
			get { return messageCount; }
		}

	}

}
