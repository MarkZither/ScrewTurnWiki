
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki;

namespace ScrewTurn.Wiki {

	public partial class AdminTheme : System.Web.UI.Page {
		protected void Page_Load(object sender, EventArgs e) {
			AdminMaster.RedirectToLoginIfNeeded();
			if(!AdminMaster.CanManageProviders(SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames())) UrlTools.Redirect("AccessDenied.aspx");

			if(!Page.IsPostBack) {
				// Load themes and related data
				LoadThemes();

			}
			
			//string name = (new ProviderRow.Name);
			//if(stProviderSelector.SelectedProvider.ToString() == "Local Pages Provider") {
			//    lstThemes.Enabled = false;
			//}

		 }

		private void UploadTheme() {
			
		}

		# region Themes
		private void LoadThemes() {
			//ThemeProviderSelectorDelete.Reload();
			//List<string> dir = Themes.ListThemes(ThemeProviderSelectorDelete.SelectedProvider);
			//lstThemes.Items.Clear();
			//lstThemes.Items.Add(new ListItem("- " + Properties.Messages.SelectAndDelete + " -", ""));
			//foreach(string theme in dir) {
			//    lstThemes.Items.Add(new ListItem(theme, theme));
			//}
		}

		protected void lstThemeProviders_SelectedIndexChanged() {

			lstThemes.Items.Clear();
			List<string> dir = Themes.ListThemes(ThemeProviderSelector.SelectedProvider);
			lstThemes.Items.Add(new ListItem("- " + Properties.Messages.SelectAndDelete + " -", ""));
			foreach(string theme in dir) {
				lstThemes.Items.Add(new ListItem(theme, theme));
			}

			//if(ThemeProviderSelector.IsDefault()) {
			//    lstThemes.Enabled = false;
			//    btnDeleteTheme.Enabled = false;
			//    upTheme.Enabled = false;
			//    btnTheme.Enabled = false;
			//}
			//else {
			//    lstThemes.Enabled = true;
			//    btnDeleteTheme.Enabled = true;
			//    upTheme.Enabled = true;
			//    btnTheme.Enabled = true;

			//}
		}

		protected void lstThemes_SelectedIndexChanged(object sender, EventArgs e) {
			btnDeleteTheme.Enabled = lstThemes.SelectedIndex >= 0;
		}
		
		protected void btnTheme_Click(object sender, EventArgs e) {
			string file = upTheme.FileName;

			string ext = System.IO.Path.GetExtension(file);
			if(ext != null) ext = ext.ToLowerInvariant();
			if(ext != ".zip") {
				lblUploadThemeResult.CssClass = "resulterror";
				lblUploadThemeResult.Text = Properties.Messages.VoidOrInvalidFile;
				return;
			}

			Log.LogEntry("Theme upload requested " + upTheme.FileName, EntryType.General, SessionFacade.CurrentUsername);
			List<string> themes = Themes.ListThemes(ThemeProviderSelector.SelectedProvider);
			bool exist = false;
			foreach(string th in themes) {
				if (th == file) exist = true;
			}
			if (exist){
				// Theme already exists
				lblUploadThemeResult.CssClass = "resulterror";
				lblUploadThemeResult.Text = Properties.Messages.ThemeAlreadyExists;
				return;
			}
			else {
				Themes.StoreTheme(System.IO.Path.GetFileNameWithoutExtension(file), upTheme.FileBytes);

				lblUploadThemeResult.CssClass = "resultok";
				lblUploadThemeResult.Text = Properties.Messages.LoadedThemes;
				upTheme.Attributes.Add("value", "");

				LoadThemes();
			}
		}

		protected void btnDeleteTheme_Click(object sender, EventArgs e) {
			if(Themes.DeleteTheme(lstThemes.SelectedValue)) {
				LoadThemes();
				lstThemes_SelectedIndexChanged(sender, e);
				lblThemeResult.CssClass = "resultok";
				lblThemeResult.Text = Properties.Messages.ThemeDeleted;
			}
			else {
				lblThemeResult.CssClass = "resulterror";
				lblThemeResult.Text = Properties.Messages.CouldNotDeleteTheme;
			}
		}
		#endregion;
	}
}
