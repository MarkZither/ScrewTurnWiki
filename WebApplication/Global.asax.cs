
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Text;
using System.IO;

namespace ScrewTurn.Wiki {

	public class Global : System.Web.HttpApplication {

		protected void Application_Start(object sender, EventArgs e) {
			// Nothing to do (see Application_BeginRequest).
		}

		protected void Session_Start(object sender, EventArgs e) { }

		protected void Application_BeginRequest(object sender, EventArgs e) {
			if(Application["StartupOK"] == null) {
				Application.Lock();
				if(Application["StartupOK"] == null) {
					// Setup Resource Exchanger
					ScrewTurn.Wiki.Exchanger.ResourceExchanger = new ScrewTurn.Wiki.ResourceExchanger();
					ScrewTurn.Wiki.StartupTools.Startup();

					// All is OK, proceed with normal startup operations
					Application["StartupOK"] = "OK";
				}
				Application.UnLock();
			}

			string physicalPath = null;

			try {
				physicalPath = HttpContext.Current.Request.PhysicalPath;
			}
			catch(ArgumentException) {
				// Illegal characters in path
				HttpContext.Current.Response.Redirect("~/PageNotFound.aspx");
				return;
			}

			// Extract the physical page name, e.g. MainPage, Edit or Category
			string pageName = Path.GetFileNameWithoutExtension(physicalPath);
			// Exctract the extension, e.g. .ashx or .aspx
			string ext = Path.GetExtension(HttpContext.Current.Request.PhysicalPath).ToLowerInvariant();
			// Remove trailing dot, .ashx -> ashx
			if(ext.Length > 0) ext = ext.Substring(1);

			// IIS7+Integrated Pipeline handles all requests through the ASP.NET engine
			// All non-interesting files are not processed, such as GIF, CSS, etc.
			if(ext == "ashx" || ext == "aspx") {
				if(!Request.PhysicalPath.ToLowerInvariant().Contains("createmasterpassword.aspx")) {
					string currentWiki = Tools.DetectCurrentWiki();
					if(Application["MasterPasswordOk"] == null || !((List<string>)Application["MasterPasswordOk"]).Contains(currentWiki)) {
						Application.Lock();
						if(Application["MasterPasswordOk"] == null || !((List<string>)Application["MasterPasswordOk"]).Contains(currentWiki)) {
							//Setup Master Password
							if(!String.IsNullOrEmpty(Settings.GetMasterPassword(currentWiki))) {
								if(Application["MasterPasswordOk"] == null) Application["MasterPasswordOk"] = new List<string>();
								((List<string>)Application["MasterPasswordOk"]).Add(currentWiki);
							}
						}
						Application.UnLock();
					}

					if(Application["MasterPasswordOk"] == null || !((List<string>)Application["MasterPasswordOk"]).Contains(currentWiki)) {
						ScrewTurn.Wiki.UrlTools.Redirect("CreateMasterPassword.aspx");
					}
				}
			}
			ScrewTurn.Wiki.UrlTools.RouteCurrentRequest();
		}

		protected void Application_AcquireRequestState(object sender, EventArgs e) {
			if(HttpContext.Current.Session != null) {
				// Try to automatically login the user through the cookie
				ScrewTurn.Wiki.LoginTools.TryAutoLogin(Tools.DetectCurrentWiki());
			}
		}

		protected void Application_AuthenticateRequest(object sender, EventArgs e) {
			// Nothing to do
		}

		protected void Application_EndRequest(object sender, EventArgs e) {
			if(Collectors.CollectorsBoxUsed) {
				Collectors.CollectorsBox.Dispose();
				Collectors.CollectorsBox = null;
				Collectors.CollectorsBoxUsed = false;
			}
		}

		/// <summary>
		/// Logs an error.
		/// </summary>
		/// <param name="ex">The error.</param>
		private void LogError(Exception ex) {
			//if(ex.InnerException != null) ex = ex.InnerException;
			try {
				ScrewTurn.Wiki.Log.LogEntry(Tools.GetCurrentUrlFixed() + "\n" +
					ex.Source + " thrown " + ex.GetType().FullName + "\n" + ex.Message + "\n" + ex.StackTrace,
					ScrewTurn.Wiki.PluginFramework.EntryType.Error, ScrewTurn.Wiki.Log.SystemUsername, null);
			}
			catch { }
		}

		protected void Application_Error(object sender, EventArgs e) {
			// Retrieve last error and log it, redirecting to Error.aspx (avoiding infinite loops)

			Exception ex = Server.GetLastError();

			HttpException httpEx = ex as HttpException;
			if(httpEx != null) {
				// Try to redirect an inexistent .aspx page to a probably existing .ashx page
				if(httpEx.GetHttpCode() == 404) {
					string page = System.IO.Path.GetFileNameWithoutExtension(Request.PhysicalPath);
					ScrewTurn.Wiki.UrlTools.Redirect(page + ScrewTurn.Wiki.GlobalSettings.PageExtension);
					return;
				}
			}

			LogError(ex);
			string url = "";
			try {
				url = Tools.GetCurrentUrlFixed();
			}
			catch { }
			EmailTools.NotifyError(ex, url);
			if(!Request.PhysicalPath.ToLowerInvariant().Contains("error.aspx")) ScrewTurn.Wiki.UrlTools.Redirect("Error.aspx");
		}

		protected void Session_End(object sender, EventArgs e) { }

		protected void Application_End(object sender, EventArgs e) {
			// Try to cleanly shutdown the application and providers
			ScrewTurn.Wiki.StartupTools.Shutdown();
		}

	}

}
