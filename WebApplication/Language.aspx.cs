
using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Threading;
using System.Globalization;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

namespace ScrewTurn.Wiki {

	public partial class Language : BasePage {

		protected void Page_Load(object sender, EventArgs e) {
			string currentWiki = DetectWiki();

			Page.Title = "Language/Time Zone - " + Settings.GetWikiTitle(currentWiki);

			if(SessionFacade.LoginKey != null && SessionFacade.GetCurrentUsername() != "admin") UrlTools.Redirect("Profile.aspx");

			if(!Page.IsPostBack) {
				// Load values stored in cookie
				HttpCookie cookie = Request.Cookies[GlobalSettings.CultureCookieName];

				languageSelector.LoadLanguages();
				languageSelector.LoadTimezones();

				string culture = null;
				if(cookie != null) culture = cookie["C"];
				else culture = Settings.GetDefaultLanguage(currentWiki);
				languageSelector.SelectedLanguage = culture;

				string timezone = null;
				if(cookie != null) timezone = cookie["T"];
				else timezone = Settings.GetDefaultTimezone(currentWiki).ToString();
				languageSelector.SelectedTimezone = timezone;

				if(!string.IsNullOrEmpty(Request["Language"])) {
					string lang = Request["Language"];

					SavePreferences(lang, languageSelector.SelectedTimezone);
					languageSelector.SelectedLanguage = lang;

					if(Request["Redirect"] != null) UrlTools.Redirect(UrlTools.BuildUrl(currentWiki, Request["Redirect"]));
					else if(Request.UrlReferrer != null && !string.IsNullOrEmpty(Request.UrlReferrer.ToString())) UrlTools.Redirect(UrlTools.BuildUrl(currentWiki, Request.UrlReferrer.FixHost().ToString()));
				}
			}

		}

		protected void btnSet_Click(object sender, EventArgs e) {
			string culture = languageSelector.SelectedLanguage;
			string timezone = languageSelector.SelectedTimezone;
			SavePreferences(culture, timezone);
		}

		/// <summary>
		/// Saves the preferences into a ookie.
		/// </summary>
		/// <param name="culture">The culture.</param>
		/// <param name="timezone">The timezone.</param>
		private void SavePreferences(string culture, string timezone) {
			Preferences.SavePreferencesInCookie(culture, timezone);
			Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
			Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
		}

	}

}
