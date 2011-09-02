
using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using ScrewTurn.Wiki.PluginFramework;
using System.Text;

namespace ScrewTurn.Wiki {

	public partial class Print : BasePage {

		PageContent page = null;
		private string currentWiki = null;

		protected void Page_Load(object sender, EventArgs e) {
			currentWiki = DetectWiki();
			
			page = Pages.FindPage(currentWiki, Request["Page"]);
			if(page == null) UrlTools.RedirectHome(currentWiki);

			// Check permissions
			bool canView = false;
			AuthChecker authChecker = new AuthChecker(Collectors.CollectorsBox.GetSettingsProvider(currentWiki));
			if(Request["Discuss"] == null) {
				canView = authChecker.CheckActionForPage(page.FullName, Actions.ForPages.ReadPage,
					SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames(currentWiki));
			}
			else {
				canView = authChecker.CheckActionForPage(page.FullName, Actions.ForPages.ReadDiscussion,
					SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames(currentWiki));
			}
			if(!canView) UrlTools.Redirect("AccessDenied.aspx");

			Page.Title = FormattingPipeline.PrepareTitle(currentWiki, page.Title, false, FormattingContext.PageContent, page.FullName) + " - " + Settings.GetWikiTitle(currentWiki);

			Literal canonical = new Literal();
			canonical.Text = Tools.GetCanonicalUrlTag(Request.Url.ToString(), page, Pages.FindNamespace(NameTools.GetNamespace(page.FullName)));
			Page.Header.Controls.Add(canonical);

			PrintContent();
		}

		/// <summary>
		/// Prints the content.
		/// </summary>
		public void PrintContent() {
			StringBuilder sb = new StringBuilder(5000);
			string title = FormattingPipeline.PrepareTitle(currentWiki, page.Title, false, FormattingContext.PageContent, page.FullName);

			if(Request["Discuss"] == null) {
				string[] categories =
					(from c in Pages.GetCategoriesForPage(page)
					 select NameTools.GetLocalName(c.FullName)).ToArray();

				UserInfo user = Users.FindUser(currentWiki, page.User);

				sb.Append(@"<h1 class=""pagetitle"">");
				sb.Append(title);
				sb.Append("</h1>");
				sb.AppendFormat("<small>{0} {1} {2} {3} &mdash; {4}: {5}</small><br /><br />",
					Properties.Messages.ModifiedOn,
					Preferences.AlignWithTimezone(currentWiki, page.LastModified).ToString(Settings.GetDateTimeFormat(currentWiki)),
					Properties.Messages.By,
					user != null ? Users.GetDisplayName(user) : page.User,
					Properties.Messages.CategorizedAs,
					categories.Length == 0 ? Properties.Messages.Uncategorized : string.Join(", ", categories));
				sb.Append(FormattedContent.GetFormattedPageContent(currentWiki, page));
			}
			else {
				sb.Append(@"<h1 class=""pagetitle"">");
				sb.Append(title);
				sb.Append(" - Discussion</h1>");
				PrintDiscussion(sb);
			}
			lblContent.Text = sb.ToString();
		}

		/// <summary>
		/// Prints a discussion.
		/// </summary>
		/// <param name="sb">The output <see cref="T:StringBuilder" />.</param>
		private void PrintDiscussion(StringBuilder sb) {
			List<Message> messages = new List<Message>(Pages.GetPageMessages(page));
			if(messages.Count == 0) {
				sb.Append("<i>");
				sb.Append(Properties.Messages.NoMessages);
				sb.Append("</i>");
			}
			else PrintSubtree(messages, null, sb);
		}

		/// <summary>
		/// Prints a subtree of Messages depth-first.
		/// </summary>
		/// <param name="messages">The Messages.</param>
		private void PrintSubtree(IEnumerable<Message> messages, Message parent, StringBuilder sb) {
			foreach(Message msg in messages) {
				sb.Append(@"<div" + (parent != null ? @" class=""messagecontainer""" : @" class=""rootmessagecontainer""") + ">");
				PrintMessage(msg, parent, sb);
				PrintSubtree(msg.Replies, msg, sb);
				sb.Append("</div>");
			}
		}

		/// <summary>
		/// Prints a message.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <param name="parent">The parent message.</param>
		/// <param name="sb">The output <see cref="T:StringBuilder" />.</param>
		private void PrintMessage(Message message, Message parent, StringBuilder sb) {
			// Header
			sb.Append(@"<div class=""messageheader"">");
			sb.Append(Preferences.AlignWithTimezone(currentWiki, message.DateTime).ToString(Settings.GetDateTimeFormat(currentWiki)));
			sb.Append(" ");
			sb.Append(Properties.Messages.By);
			sb.Append(" ");
			sb.Append(Users.UserLink(currentWiki, message.Username));

			// Subject
			if(message.Subject.Length > 0) {
				sb.Append(@"<br /><span class=""messagesubject"">");
				sb.Append(FormattingPipeline.PrepareTitle(currentWiki, message.Subject, false, FormattingContext.MessageBody, page.FullName));
				sb.Append("</span>");
			}

			sb.Append("</div>");

			// Body
			sb.Append(@"<div class=""messagebody"">");
			sb.Append(FormattingPipeline.FormatWithPhase3(currentWiki, FormattingPipeline.FormatWithPhase1And2(currentWiki, message.Body, false, FormattingContext.MessageBody, page.FullName),
				FormattingContext.MessageBody, page.FullName));
			sb.Append("</div>");
		}

	}

}
