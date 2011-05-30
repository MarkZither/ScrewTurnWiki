
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using ScrewTurn.Wiki.PluginFramework;
using System.Globalization;


namespace ScrewTurn.Wiki {
	
	/// <summary>
	/// Allows access to current user's preferences.
	/// </summary>
	public static class Preferences {

		/// <summary>
		/// Loads the language from a cookie.
		/// </summary>
		/// <returns>The language, or <c>null</c>.</returns>
		public static string LoadLanguageFromCookie() {
			HttpCookie cookie = HttpContext.Current.Request.Cookies[GlobalSettings.CultureCookieName];
			if(cookie != null) {
				string culture = cookie["C"];
				return culture;
			}
			else return null;
		}

		/// <summary>
		/// Loads the language from the current user's data.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <returns>The language, or <c>null</c>.</returns>
		public static string LoadLanguageFromUserData(string wiki) {
			UserInfo currentUser = SessionFacade.GetCurrentUser(wiki);
			if(currentUser != null) {
				string culture = Users.GetUserData(currentUser, "Culture");
				return culture;
			}
			else return null;
		}

		/// <summary>
		/// Loads the timezone from a cookie.
		/// </summary>
		/// <returns>The timezone, or <c>null</c>.</returns>
		public static int? LoadTimezoneFromCookie() {
			HttpCookie cookie = HttpContext.Current.Request.Cookies[GlobalSettings.CultureCookieName];
			if(cookie != null) {
				string timezone = cookie["T"];
				int res = 0;
				if(int.TryParse(timezone, NumberStyles.Any, CultureInfo.InvariantCulture, out res)) return res;
			}
			
			return null;
		}

		/// <summary>
		/// Loads the timezone from the current user's data.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <returns>The timezone, or <c>null</c>.</returns>
		public static int? LoadTimezoneFromUserData(string wiki) {
			UserInfo currentUser = SessionFacade.GetCurrentUser(wiki);
			if(currentUser != null) {
				string timezone = Users.GetUserData(currentUser, "Timezone");
				if(timezone != null) {
					int res = 0;
					if(int.TryParse(timezone, NumberStyles.Any, CultureInfo.InvariantCulture, out res)) return res;
				}
			}
			
			return null;
		}

		/// <summary>
		/// Saves language and timezone preferences into a cookie.
		/// </summary>
		/// <param name="culture">The culture.</param>
		/// <param name="timezone">The timezone.</param>
		public static void SavePreferencesInCookie(string culture, int timezone) {
			HttpCookie cookie = new HttpCookie(GlobalSettings.CultureCookieName);
			cookie.Expires = DateTime.Now.AddYears(10);
			cookie.Path = GlobalSettings.CookiePath;
			cookie.Values.Add("C", culture);
			cookie.Values.Add("T", timezone.ToString(CultureInfo.InvariantCulture));
			HttpContext.Current.Response.Cookies.Add(cookie);
		}

		/// <summary>
		/// Deletes the language and timezone preferences cookie.
		/// </summary>
		public static void DeletePreferencesCookie() {
			HttpCookie cookie = new HttpCookie(GlobalSettings.CultureCookieName);
			cookie.Expires = DateTime.Now.AddYears(-1);
			cookie.Path = GlobalSettings.CookiePath;
			cookie.Values.Add("C", null);
			cookie.Values.Add("T", null);
			HttpContext.Current.Request.Cookies.Add(cookie);
		}

		/// <summary>
		/// Saves language and timezone preferences into the current user's data.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="culture">The culture.</param>
		/// <param name="timezone">The timezone.</param>
		/// <returns><c>true</c> if the data is stored, <c>false</c> otherwise.</returns>
		public static bool SavePreferencesInUserData(string wiki, string culture, int timezone) {
			UserInfo user = SessionFacade.GetCurrentUser(wiki);
			if(user != null && !user.Provider.UsersDataReadOnly) {
				Users.SetUserData(user, "Culture", culture);
				Users.SetUserData(user, "Timezone", timezone.ToString(CultureInfo.InvariantCulture));

				return true;
			}
			else {
				if(user == null) {
					Log.LogEntry("Attempt to save user data when no user has logged in", EntryType.Warning, Log.SystemUsername, wiki);
				}
				return false;
			}
		}

		/// <summary>
		/// Aligns a date/time with the User's preferences (if any).
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="dateTime">The date/time to align.</param>
		/// <returns>The aligned date/time.</returns>
		public static DateTime AlignWithTimezone(string wiki, DateTime dateTime) {
			// First, look for hard-stored user's preferences
			// If they are not available, look at the cookie

			int? tempShift = LoadTimezoneFromUserData(wiki);
			if(!tempShift.HasValue) tempShift = LoadTimezoneFromCookie();

			int shift = tempShift.HasValue ? tempShift.Value : Settings.GetDefaultTimezone(wiki);
			return dateTime.ToUniversalTime().AddMinutes(shift + (dateTime.IsDaylightSavingTime() ? 60 : 0));
		}

		/// <summary>
		/// Aligns a date/time with the default timezone.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="dateTime">The date/time to align.</param>
		/// <returns>The aligned date/time.</returns>
		public static DateTime AlignWithServerTimezone(string wiki, DateTime dateTime) {
			return dateTime.ToUniversalTime().AddMinutes(Settings.GetDefaultTimezone(wiki) + (dateTime.IsDaylightSavingTime() ? 60 : 0));
		}

	}

}
