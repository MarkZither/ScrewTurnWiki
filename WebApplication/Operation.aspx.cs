
using System;
using System.Data;
using System.Configuration;
using System.Collections.Generic;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using ScrewTurn.Wiki.PluginFramework;
using System.Text;

namespace ScrewTurn.Wiki {

	public partial class Operation : BasePage {

		private string currentWiki = null;

		string op = "";

		protected void Page_Load(object sender, EventArgs e) {
			currentWiki = DetectWiki();

			Page.Title = Properties.Messages.OperationTitle + " - " + Settings.GetWikiTitle(currentWiki);

			if(Request["Operation"] == null) UrlTools.RedirectHome(currentWiki);

			op = Request["Operation"].ToLowerInvariant();

			switch(op) {
				case "deletemessage":
					Page.Title = "Delete Message - " + Settings.GetWikiTitle(currentWiki);
					mlwOperation.ActiveViewIndex = 0;
					PrepareDeleteMessage();
					break;
			}

		}

		#region Operation: DeleteMessage

		/// <summary>
		/// Prepares the message deletion GUI.
		/// </summary>
		private void PrepareDeleteMessage() {
			string ms = Request["Message"];
			string pg = Request["Page"];
			if(ms == null || ms.Length == 0 || pg == null || pg.Length == 0) UrlTools.RedirectHome(currentWiki);

			PageInfo page = Pages.FindPage(currentWiki, pg);
			if(page == null) UrlTools.RedirectHome(currentWiki);
			if(page.Provider.ReadOnly) UrlTools.Redirect(UrlTools.BuildUrl(currentWiki, page.FullName, GlobalSettings.PageExtension));

			AuthChecker authChecker = new AuthChecker(Collectors.CollectorsBox.GetSettingsProvider(currentWiki));
			bool canManageDiscussion = authChecker.CheckActionForPage(page, Actions.ForPages.ManageDiscussion,
				SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames(currentWiki));
			if(!canManageDiscussion) UrlTools.Redirect("AccessDenied.aspx");

			int id = -1;
			try {
				id = int.Parse(ms);
			}
			catch {
				UrlTools.RedirectHome(currentWiki);
			}

			Message message = Pages.FindMessage(Pages.GetPageMessages(page), id);
			if(message == null) UrlTools.RedirectHome(currentWiki);

			StringBuilder sb = new StringBuilder(500);
			sb.Append("<b>");
			sb.Append(FormattingPipeline.PrepareTitle(currentWiki, message.Subject, false, FormattingContext.MessageBody, page));
			sb.Append("</b><br /><small>");
			sb.Append(Properties.Messages.Posted);
			sb.Append(" ");
			sb.Append(Preferences.AlignWithTimezone(currentWiki, message.DateTime).ToString(Settings.GetDateTimeFormat(currentWiki)));
			sb.Append(" ");
			sb.Append(Properties.Messages.By);
			sb.Append(" ");
			sb.Append(Users.UserLink(currentWiki, message.Username));
			sb.Append("</small><br /><br />");
			sb.Append(FormattingPipeline.FormatWithPhase3(currentWiki, FormattingPipeline.FormatWithPhase1And2(currentWiki, message.Body, false, FormattingContext.MessageBody, page),
				FormattingContext.MessageBody, page));

			lblDeleteMessageContent.Text = sb.ToString();
		}

		protected void btnDeleteMessage_Click(object sender, EventArgs e) {
			int id = int.Parse(Request["Message"]);
			PageInfo page = Pages.FindPage(currentWiki, Request["Page"]);
			Log.LogEntry("Message deletion requested for " + page.FullName + "." + id.ToString(), EntryType.General, SessionFacade.GetCurrentUsername(), currentWiki);
			bool done = Pages.RemoveMessage(page, id, chkDeleteMessageReplies.Checked);
			UrlTools.Redirect(UrlTools.BuildUrl(currentWiki, Request["Page"], GlobalSettings.PageExtension + "?Discuss=1"));
		}

		protected void btnCancelDeleteMessage_Click(object sender, EventArgs e) {
			UrlTools.Redirect(UrlTools.BuildUrl(currentWiki, Request["Page"], GlobalSettings.PageExtension, "?Discuss=1"));
		}

		#endregion

	}

}
