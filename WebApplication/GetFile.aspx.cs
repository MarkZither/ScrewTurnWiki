
using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Globalization;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	// No BasePage because compression/language selection are not needed
	public partial class GetFile : Page {

		protected void Page_Load(object sender, EventArgs e) {
			string filename = Request["File"];

			if(filename == null) {
				Response.StatusCode = 404;
				Response.Write(Properties.Messages.FileNotFound);
				return;
			}

			string currentWiki = Tools.DetectCurrentWiki();

			// Remove ".." sequences that might be a security issue
			filename = filename.Replace("..", "");

			bool isPageAttachment = !string.IsNullOrEmpty(Request["Page"]);
			PageContent pageContent = isPageAttachment ? Pages.FindPage(currentWiki, Request["Page"]) : null;
			if(isPageAttachment && pageContent == null) {
				Response.StatusCode = 404;
				Response.Write(Properties.Messages.FileNotFound);
				return;
			}

			IFilesStorageProviderV40 provider;

			if(!string.IsNullOrEmpty(Request["Provider"])) provider = Collectors.CollectorsBox.FilesProviderCollector.GetProvider(Request["Provider"], currentWiki);
			else {
				if(isPageAttachment) provider = FilesAndAttachments.FindPageAttachmentProvider(currentWiki, pageContent.FullName, filename);
				else provider = FilesAndAttachments.FindFileProvider(currentWiki, filename);
			}

			if(provider == null) {
				Response.StatusCode = 404;
				Response.Write("File not found.");
				return;
			}

			// Use canonical path format (leading with /)
			if(!isPageAttachment) {
				if(!filename.StartsWith("/")) filename = "/" + filename;
				filename = filename.Replace("\\", "/");
			}

			// Verify permissions
			bool canDownload = false;

			AuthChecker authChecker = new AuthChecker(Collectors.CollectorsBox.GetSettingsProvider(currentWiki));

			if(isPageAttachment) {
				canDownload = authChecker.CheckActionForPage(pageContent.FullName, Actions.ForPages.DownloadAttachments,
					SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames(currentWiki));
			}
			else {
				string dir = Tools.GetDirectoryName(filename);
				canDownload = authChecker.CheckActionForDirectory(provider, dir,
					 Actions.ForDirectories.DownloadFiles, SessionFacade.GetCurrentUsername(),
					 SessionFacade.GetCurrentGroupNames(currentWiki));
			}
			if(!canDownload) {
				Response.StatusCode = 401;
				return;
			}

			long size = -1;

			FileDetails details = null;
			if(isPageAttachment) details = provider.GetPageAttachmentDetails(pageContent.FullName, filename);
			else details = provider.GetFileDetails(filename);

			if(details != null) size = details.Size;
			else {
				Log.LogEntry("Attempted to download an inexistent file/attachment (" + (pageContent != null ? pageContent.FullName + "/" : "") + filename + ")", EntryType.Warning, Log.SystemUsername, currentWiki);
				Response.StatusCode = 404;
				Response.Write("File not found.");
				return;
			}

			string mime = "";
			try {
				string ext = Path.GetExtension(filename);
				if(ext.StartsWith(".")) ext = ext.Substring(1).ToLowerInvariant(); // Remove trailing dot
				mime = GetMimeType(ext);
			}
			catch {
				// ext is null -> no mime type -> abort
				Response.Write(filename + "<br />");
				Response.StatusCode = 404;
				Response.Write("File not found.");
				//mime = "application/octet-stream";
				return;
			}

			// Prepare response
			Response.Clear();
			Response.AddHeader("content-type", mime);
			if(Request["AsStreamAttachment"] != null) {
				Response.AddHeader("content-disposition", "attachment;filename=\"" + Path.GetFileName(filename) + "\"");
			}
			else {
				Response.AddHeader("content-disposition", "inline;filename=\"" + Path.GetFileName(filename) + "\"");
			}
			Response.AddHeader("content-length", size.ToString());

			bool retrieved = false;
			if(isPageAttachment) {
				try {
					retrieved = provider.RetrievePageAttachment(pageContent.FullName, filename, Response.OutputStream);
				}
				catch(ArgumentException ex) {
					Log.LogEntry("Attempted to download an inexistent attachment (" + pageContent.FullName + "/" + filename + ")\n" + ex.ToString(), EntryType.Warning, Log.SystemUsername, currentWiki);
				}
			}
			else {
				try {
					retrieved = provider.RetrieveFile(filename, Response.OutputStream);
				}
				catch(ArgumentException ex) {
					Log.LogEntry("Attempted to download an inexistent file/attachment (" + filename + ")\n" + ex.ToString(), EntryType.Warning, Log.SystemUsername, currentWiki);
				}
			}

			if(!retrieved) {
				Response.StatusCode = 404;
				Response.Write("File not found.");
				return;
			}

			// Set the cache duration accordingly to the file date/time
			//Response.AddFileDependency(filename);
			//Response.Cache.SetETagFromFileDependencies();
			//Response.Cache.SetLastModifiedFromFileDependencies();
			Response.Cache.SetETag(filename.GetHashCode().ToString() + "-" + size.ToString());
			Response.Cache.SetCacheability(HttpCacheability.Public);
			Response.Cache.SetSlidingExpiration(true);
			Response.Cache.SetValidUntilExpires(true);
			Response.Cache.VaryByParams["File"] = true;
			Response.Cache.VaryByParams["Provider"] = true;
			Response.Cache.VaryByParams["Page"] = true;
			Response.Cache.VaryByParams["IsPageAttachment"] = true;
		}

		private string GetMimeType(string ext) {
			string mime = "";
			if(MimeTypes.Types.TryGetValue(ext, out mime)) return mime;
			else return "application/octet-stream";
		}

	}

}
