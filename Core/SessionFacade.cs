
using System;
using System.Collections.Generic;
using System.Text;
using System.Web.SessionState;
using System.Web;
using ScrewTurn.Wiki.PluginFramework;
using System.Security.Cryptography;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Exposes in a strongly-typed fashion the Session variables.
	/// </summary>
	public static class SessionFacade {

		private const string CookieName = "ScrewTurnWiki1";
		private const string LoginKeyCookieName = "LK";
		private const string UserNameKey = "UN";
		private const string WikiKey = "WK";
		private const string IsLoggingOutKey = "ILO";
		private const string CaptchaKey = "CK";

		private static HttpCookie Cookie {
			get {
				if(HttpContext.Current == null) return null;
				if(HttpContext.Current.Request == null) return null;

				return HttpContext.Current.Request.Cookies[CookieName];
			}
		}

		/// <summary>
		/// Setups the session.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="user">The user.</param>
		public static void SetupSession(string wiki, UserInfo user) {
			if(HttpContext.Current != null) {
				HttpCookie cookie = new HttpCookie(CookieName);
				cookie.Values[UserNameKey] = user.Username;
				cookie.Values[LoginKeyCookieName] = Users.ComputeLoginKey(wiki, user.Username, user.Email, user.DateTime);
				cookie.Values[WikiKey] = wiki;

				HttpContext.Current.Response.Cookies.Add(cookie);
			}
		}

		/// <summary>
		/// Gets or sets the Login Key.
		/// </summary>
		public static string LoginKey {
			get {
				if(Cookie != null) {
					return Cookie[LoginKeyCookieName];
				}
				return null;
			}
		}

		/// <summary>
		/// Gets or sets the current username, or <c>null</c>.
		/// </summary>
		public static string CurrentUsername {
			get {
				if(Cookie != null) {
					return Cookie[UserNameKey];
				}
				return null;
			}
		}

		/// <summary>
		/// Gets the current user, if any.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <returns>The current user, or <c>null</c>.</returns>
		public static UserInfo GetCurrentUser(string wiki) {
			string un = CurrentUsername;
			if(string.IsNullOrEmpty(un)) return null;
			else if(un == AnonymousUsername) return Users.GetAnonymousAccount(wiki);
			else {
				UserInfo current = Users.FindUser(wiki, un);
				if(current != null && Cookie[LoginKeyCookieName] == Users.ComputeLoginKey(wiki, current.Username, current.Email, current.DateTime)) {
					return current;
				}
				else {
					// Username or cookie is invalid
					Clear();
					return null;
				}
			}
		}

		/// <summary>
		/// The username for anonymous users.
		/// </summary>
		public const string AnonymousUsername = "|";

		/// <summary>
		/// Gets the current username for security checks purposes only.
		/// </summary>
		/// <returns>The current username, or <b>AnonymousUsername</b> if anonymous.</returns>
		public static string GetCurrentUsername() {
			string un = CurrentUsername;
			if(string.IsNullOrEmpty(un)) return AnonymousUsername;
			else return un;
		}

		/// <summary>
		/// Gets the current user groups.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static UserGroup[] GetCurrentGroups(string wiki) {
			UserGroup[] groups;
			UserInfo current = GetCurrentUser(wiki);
			if(current != null) {
				// This check is necessary because after group deletion the session might contain outdated data
				List<UserGroup> temp = new List<UserGroup>();
				foreach(string groupName in current.Groups) {
					UserGroup tempGroup = Users.FindUserGroup(wiki, groupName);
					if(tempGroup != null) temp.Add(tempGroup);
				}
				groups = temp.ToArray();
			}
			else {
				groups = new UserGroup[] { Users.FindUserGroup(wiki, Settings.GetAnonymousGroup(wiki)) };
			}

			return groups;
		}

		/// <summary>
		/// Gets the current group names.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <returns>The group names.</returns>
		public static string[] GetCurrentGroupNames(string wiki) {
			return Array.ConvertAll(GetCurrentGroups(wiki), delegate(UserGroup g) { return g.Name; });
		}

		/// <summary>
		/// Gets the Breadcrumbs Manager.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static BreadcrumbsManager Breadcrumbs(string wiki) {
			return new BreadcrumbsManager(wiki);
		}

		/// <summary>
		/// Clears cookies after a logout.
		/// </summary>
		public static void Clear() {
			HttpCookie cookie = new HttpCookie(CookieName);
			cookie.Expires = DateTime.Now.ToUniversalTime().AddDays(-3000);

			HttpContext.Current.Response.Cookies.Add(cookie);
		}


		/// <summary>
		/// Gets or sets a value indicating whether the user is logging out.
		/// </summary>
		/// <value><c>true</c> if this use is logging out; otherwise, <c>false</c>.</value>
		public static bool IsLoggingOut {
			get {
				if(Cookie != null) {
					return Cookie[IsLoggingOutKey] == "0" ? false : true;
				}
				return false;
			}
			set {
				HttpCookie cookie = Cookie;
				if(cookie == null) cookie = new HttpCookie(CookieName);

				cookie.Values[IsLoggingOutKey] = value ? "1" : "0";
				HttpContext.Current.Response.Cookies.Add(cookie);
			}
		}

		/// <summary>
		/// Gets the captcha.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <returns>The captcha strings.</returns>
		public static string GetCaptcha(string wiki) {
			if(Cookie != null) {
				TripleDESCryptoServiceProvider provider = new TripleDESCryptoServiceProvider();
				provider.Key = Settings.GetMasterPasswordBytes(wiki);
				provider.IV = new byte[] { 2, 5, 23, 21, 3, 8, 5, 38 };
				return DecryptBytes(provider, Convert.FromBase64String(Cookie[CaptchaKey]));
			}
			return null;
		}

		/// <summary>
		/// Sets the captcha.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="captcha">The captcha string.</param>
		public static void SetCaptcha(string wiki, string captcha) {
			HttpCookie cookie = Cookie;
			if(cookie == null) cookie = new HttpCookie(CookieName);
			TripleDESCryptoServiceProvider provider = new TripleDESCryptoServiceProvider();
			provider.Key = Settings.GetMasterPasswordBytes(wiki);
			provider.IV = new byte[] { 2, 5, 23, 21, 3, 8, 5, 38 };
			cookie.Values[CaptchaKey] = EncryptString(provider, captcha);
			HttpContext.Current.Response.Cookies.Add(cookie);
		}

		private static string EncryptString(SymmetricAlgorithm symAlg, string inString) {
			byte[] inBlock = UnicodeEncoding.Unicode.GetBytes(inString);
			ICryptoTransform xfrm = symAlg.CreateEncryptor();
			byte[] outBlock = xfrm.TransformFinalBlock(inBlock, 0, inBlock.Length);

			string temp = Convert.ToBase64String(outBlock);
			return temp;
		}

		private static string DecryptBytes(SymmetricAlgorithm symAlg, byte[] inBytes) {
			ICryptoTransform xfrm = symAlg.CreateDecryptor();
			byte[] outBlock = xfrm.TransformFinalBlock(inBytes, 0, inBytes.Length);

			string temp = UnicodeEncoding.Unicode.GetString(outBlock);
			return temp;
		}
	}

}
