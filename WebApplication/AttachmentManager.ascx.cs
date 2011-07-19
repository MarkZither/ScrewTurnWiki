
using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	public partial class AttachmentManager : System.Web.UI.UserControl {

		private IFilesStorageProviderV40 provider;

		private bool canDownload = false;
		private bool canUpload = false;
		private bool canDelete = false;
		private bool isAdmin = false;

		protected void Page_Load(object sender, EventArgs e) {
			string currentWiki = Tools.DetectCurrentWiki();

			if(!Page.IsPostBack) {
				// Localized strings for JavaScript
				StringBuilder sb = new StringBuilder();
				sb.Append(@"<script type=""text/javascript"">" + "\r\n<!--\n");
				sb.Append("var ConfirmMessage = '");
				sb.Append(Properties.Messages.ConfirmOperation);
				sb.Append("';\r\n");
				sb.AppendFormat("var UploadControl = '{0}';\r\n", fileUpload.ClientID);
				//sb.AppendFormat("var RefreshCommandParameter = '{0}';\r\n", btnRefresh.UniqueID);
				sb.AppendFormat("var OverwriteControl = '{0}';\r\n", chkOverwrite.ClientID);
				sb.Append("// -->\n</script>\n");
				lblStrings.Text = sb.ToString();

				// Setup upload information (max file size, allowed file types)
				lblUploadFilesInfo.Text = lblUploadFilesInfo.Text.Replace("$1", Tools.BytesToString(GlobalSettings.MaxFileSize * 1024));
				sb = new StringBuilder();
				string[] aft = Settings.GetAllowedFileTypes(currentWiki);
				for(int i = 0; i < aft.Length; i++) {
					sb.Append(aft[i].ToUpper());
					if(i != aft.Length - 1) sb.Append(", ");
				}
				lblUploadFilesInfo.Text = lblUploadFilesInfo.Text.Replace("$2", sb.ToString());

				// Load Providers
				foreach(IFilesStorageProviderV40 prov in Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(currentWiki)) {
					ListItem item = new ListItem(prov.Information.Name, prov.GetType().FullName);
					if(item.Value == GlobalSettings.DefaultFilesProvider) {
						item.Selected = true;
					}
					lstProviders.Items.Add(item);
				}

				if(CurrentPage == null) btnUpload.Enabled = false;
			}

			// Set provider
			provider = Collectors.CollectorsBox.FilesProviderCollector.GetProvider(lstProviders.SelectedValue, currentWiki);

			if(!Page.IsPostBack) {
				rptItems.DataBind();
			}

			DetectPermissions();
			SetupControls();
		}

		/// <summary>
		/// Detects the permissions of the current user.
		/// </summary>
		private void DetectPermissions() {
			if(CurrentPage != null) {
				string currentWiki = Tools.DetectCurrentWiki();
				string currentUser = SessionFacade.GetCurrentUsername();
				string[] currentGroups = SessionFacade.GetCurrentGroupNames(currentWiki);
				AuthChecker authChecker = new AuthChecker(Collectors.CollectorsBox.GetSettingsProvider(currentWiki));
				canDownload = authChecker.CheckActionForPage(CurrentPage.FullName, Actions.ForPages.DownloadAttachments, currentUser, currentGroups);
				canUpload = authChecker.CheckActionForPage(CurrentPage.FullName, Actions.ForPages.UploadAttachments, currentUser, currentGroups);
				canDelete = authChecker.CheckActionForPage(CurrentPage.FullName, Actions.ForPages.DeleteAttachments, currentUser, currentGroups);
				isAdmin = Array.Find(currentGroups, delegate(string g) { return g == Settings.GetAdministratorsGroup(currentWiki); }) != null;
			}
			else {
				canDownload = false;
				canUpload = false;
				canDelete = false;
				isAdmin = false;
			}
			lstProviders.Visible = isAdmin;
		}

		/// <summary>
		/// Sets up buttons and controls using the permissions.
		/// </summary>
		private void SetupControls() {
			if(!canUpload) {
				btnUpload.Enabled = false;
			}
			if(!canDelete) {
				chkOverwrite.Enabled = false;
			}
		}

		/// <summary>
		/// Gets or sets the PageInfo object.
		/// </summary>
		/// <remarks>This property must be set at page load.</remarks>
		public PageContent CurrentPage {
			get { return Pages.FindPage(Tools.DetectCurrentWiki(), ViewState["CP"] as string); }
			set {
				if(value == null) ViewState["CP"] = null;
				else ViewState["CP"] = value.FullName;
				btnUpload.Enabled = value != null;
				lblNoUpload.Visible = !btnUpload.Enabled;
				DetectPermissions();
				SetupControls();
				rptItems.DataBind();
			}
		}

		protected void rptItems_DataBinding(object sender, EventArgs e) {
			provider = Collectors.CollectorsBox.FilesProviderCollector.GetProvider(lstProviders.SelectedValue, Tools.DetectCurrentWiki());

			if(provider == null || CurrentPage == null) {
				return;
			}

			// Build a DataTable containing the proper information
			DataTable table = new DataTable("Items");

			table.Columns.Add("Name");
			table.Columns.Add("Size");
			table.Columns.Add("Editable", typeof(bool));
			table.Columns.Add("Page");
			table.Columns.Add("Link");
			table.Columns.Add("CanDelete", typeof(bool));
			table.Columns.Add("CanDownload", typeof(bool));

			string[] attachments = provider.ListPageAttachments(CurrentPage.FullName);
			foreach(string s in attachments) {
				FileDetails details = provider.GetPageAttachmentDetails(CurrentPage.FullName, s);

				DataRow row = table.NewRow();
				string ext = Path.GetExtension(s).ToLowerInvariant();
				row["Name"] = s;
				row["Size"] = Tools.BytesToString(details.Size);
				row["Editable"] = canUpload && canDelete && (ext == ".jpg" || ext == ".jpeg" || ext == ".png");
				row["Page"] = CurrentPage.FullName;
				if(canDownload) {
					row["Link"] = "GetFile.aspx?File=" + Tools.UrlEncode(s).Replace("'", "&#39;") + "&amp;AsStreamAttachment=1&amp;Provider=" +
						provider.GetType().FullName + "&amp;IsPageAttachment=1&amp;Page=" +
						Tools.UrlEncode(CurrentPage.FullName) + "&amp;NoHit=1";
				}
				else {
					row["Link"] = "";
				}
				row["CanDelete"] = canDelete;
				row["CanDownload"] = canDownload;
				table.Rows.Add(row);
			}

			rptItems.DataSource = table;
		}

		protected void btnRefresh_Click(object sender, EventArgs e) {
			rptItems.DataBind();
		}

		protected void btnUpload_Click(object sender, EventArgs e) {
			if(canUpload) {
				lblUploadResult.Text = "";				
				if(fileUpload.HasFile) {
					if(fileUpload.FileBytes.Length > GlobalSettings.MaxFileSize * 1024) {
						lblUploadResult.Text = Properties.Messages.FileTooBig;
						lblUploadResult.CssClass = "resulterror";
					}
					else {
						// Check file extension
						string[] aft = Settings.GetAllowedFileTypes(Tools.DetectCurrentWiki());
						bool allowed = false;

						if(aft.Length > 0 && aft[0] == "*") allowed = true;
						else {
							string ext = Path.GetExtension(fileUpload.FileName);
							if(ext == null) ext = "";
							if(ext.StartsWith(".")) ext = ext.Substring(1).ToLowerInvariant();
							foreach(string ft in aft) {
								if(ft == ext) {
									allowed = true;
									break;
								}
							}
						}

						if(!allowed) {
							lblUploadResult.Text = Properties.Messages.InvalidFileType;
							lblUploadResult.CssClass = "resulterror";
						}
						else {
							// Store attachment
							bool done = provider.StorePageAttachment(CurrentPage.FullName, fileUpload.FileName, fileUpload.FileContent, chkOverwrite.Checked);
							if(!done) {
								lblUploadResult.Text = Properties.Messages.CannotStoreFile;
								lblUploadResult.CssClass = "resulterror";

								// Index the attached file
								string tempDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());
								string tempFile = Path.Combine(tempDir, fileUpload.FileName);
								FileStream writer = File.OpenWrite(tempFile);
								writer.Write(fileUpload.FileBytes, 0, fileUpload.FileBytes.Length);
								SearchClass.IndexPageAttachment(fileUpload.FileName, tempFile, CurrentPage);
								Directory.Delete(tempDir, true);
							}
							else {
								Host.Instance.OnAttachmentActivity(Tools.DetectCurrentWiki(), provider.GetType().FullName,
									fileUpload.FileName, CurrentPage.FullName, null, FileActivity.AttachmentUploaded);
							}
							rptItems.DataBind();
						}
					}
				}
				else {
					lblUploadResult.Text = Properties.Messages.FileVoid;
					lblUploadResult.CssClass = "resulterror";
				}
			}
		}

		protected void lstProviders_SelectedIndexChanged(object sender, EventArgs e) {
			provider = Collectors.CollectorsBox.FilesProviderCollector.GetProvider(lstProviders.SelectedValue, Tools.DetectCurrentWiki());
			rptItems.DataBind();
		}

		protected void rptItems_ItemCommand(object sender, RepeaterCommandEventArgs e) {
			// Raised when a ButtonField is clicked

			switch(e.CommandName) {
				case "Rename":
					if(canDelete) {
						pnlRename.Visible = true;
						lblItem.Text = (string)e.CommandArgument;
						txtNewName.Text = (string)e.CommandArgument;
						rptItems.Visible = false;
					}
					break;
				case "Delete":
					if(canDelete) {
						// Delete Attachment
						bool d = provider.DeletePageAttachment(CurrentPage.FullName, (string)e.CommandArgument);

						if(d) {
							Host.Instance.OnAttachmentActivity(Tools.DetectCurrentWiki(), provider.GetType().FullName,
								(string)e.CommandArgument, CurrentPage.FullName, null, FileActivity.AttachmentDeleted);
						}

						rptItems.DataBind();
					}
					break;
			}
		}

		protected void btnRename_Click(object sender, EventArgs e) {
			if(canDelete) {
				lblRenameResult.Text = "";

				txtNewName.Text = txtNewName.Text.Trim();

				// Ensure that the extension is not changed (security)
				string previousExtension = Path.GetExtension(lblItem.Text);
				string newExtension = Path.GetExtension(txtNewName.Text);
				if(string.IsNullOrEmpty(newExtension)) {
					newExtension = previousExtension;
					txtNewName.Text += previousExtension;
				}

				if(newExtension.ToLowerInvariant() != previousExtension.ToLowerInvariant()) {
					txtNewName.Text += previousExtension;
				}

				txtNewName.Text = txtNewName.Text.Trim();

				bool done = true;
				if(txtNewName.Text.ToLowerInvariant() != lblItem.Text.ToLowerInvariant()) {
					done = provider.RenamePageAttachment(CurrentPage.FullName, lblItem.Text, txtNewName.Text);
				}

				if(done) {
					pnlRename.Visible = false;
					rptItems.Visible = true;
					rptItems.DataBind();

					Host.Instance.OnAttachmentActivity(Tools.DetectCurrentWiki(), provider.GetType().FullName,
						txtNewName.Text, CurrentPage.FullName, lblItem.Text, FileActivity.AttachmentRenamed);

					// Fix index according to file renaming
					SearchClass.RenamePageAttachment(CurrentPage, lblItem.Text, txtNewName.Text);

				}
				else {
					lblRenameResult.Text = Properties.Messages.CannotRenameItem;
					lblRenameResult.CssClass = "resulterror";
				}
			}
		}

		protected void btnCancel_Click(object sender, EventArgs e) {
			pnlRename.Visible = false;
			rptItems.Visible = true;
			lblRenameResult.Text = "";
		}

	}

}
