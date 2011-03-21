
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
		}

		/// <summary>
		/// Event fired when the selected provider changes.
		/// </summary>
		public event EventHandler<EventArgs> SelectedProviderThemesChanged;

		protected void providerThemeSelector_SelectedIndexChanged(object sender, EventArgs e) {
			if(SelectedProviderThemesChanged != null) SelectedProviderThemesChanged(sender, e);
			else fillThemeList(SelectedProviderThemeDelete);
		}

		# region Themes
		private void LoadThemes() {
			lstProvThemeSelectorUpload.Items.Clear();
			foreach(IProviderV30 themesProv in Collectors.ThemeProviderCollector.AllProviders) {
				lstProvThemeSelectorUpload.Items.Add(new ListItem(themesProv.Information.Name, themesProv.ToString()));
			}

			provThemeSelector.Items.Clear();
			foreach(IProviderV30 themesProvider in Collectors.ThemeProviderCollector.AllProviders) {
				provThemeSelector.Items.Add(new ListItem(themesProvider.Information.Name, themesProvider.ToString()));
			}
			fillThemeList(SelectedProviderThemeDelete);

		}

		private void fillThemeList(string provider) {
			lstThemes.Enabled = true;
			lstThemes.Items.Clear();
			if(provThemeSelector.SelectedIndex != -1) {
				lstThemes.Items.Add(new ListItem(Properties.Messages.SelectAndDelete, Properties.Messages.SelectAndDelete));
				foreach(string theme in Themes.ListThemes(provider)) {
					lstThemes.Items.Add(new ListItem(theme, theme));
				}
			}
			else {
				lstThemes.Items.Add(new ListItem(Properties.Messages.SelectAndDelete, Properties.Messages.SelectAndDelete));
				lstThemes.Enabled = false;
			}
		}

		/// <summary>
		/// Gets or sets the selected provider.
		/// </summary>
		public string SelectedProviderUpload {
			get { return lstProvThemeSelectorUpload.SelectedValue; }
			set {
				lstProvThemeSelectorUpload.SelectedIndex = -1;
				foreach(ListItem itm in lstProvThemeSelectorUpload.Items) {
					if(itm.Value == value) {
						itm.Selected = true;
						break;
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets the selected provider.
		/// </summary>
		public string SelectedProviderThemeDelete {
			get { return provThemeSelector.SelectedValue; }
			set {
				provThemeSelector.SelectedIndex = -1;
				foreach(ListItem itm in provThemeSelector.Items) {
					if(itm.Value == value) {
						itm.Selected = true;
						break;
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets the selected themes.
		/// </summary>
		/// <value>
		/// The selected themes.
		/// </value>
		public string SelectedThemesToDelete {
			get { return lstThemes.SelectedValue; }
			set {
				lstThemes.SelectedIndex = -1;
				foreach(ListItem itm in lstThemes.Items) {
					if(itm.Value == value) {
						itm.Selected = true;
						break;
					}
				}
			}
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
			List<string> themes = Themes.ListThemes(SelectedProviderUpload);
			bool exist = false;
			foreach(string th in themes) {

				if(th.Replace(".zip", "") == file.Replace(".zip", "")) exist = true;
			}
			if(exist) {
				// Theme already exists
				lblUploadThemeResult.CssClass = "resulterror";
				lblUploadThemeResult.Text = Properties.Messages.ThemeAlreadyExists;
				return;
			}
			else {
				Themes.StoreTheme(lstProvThemeSelectorUpload.SelectedValue + "|" + System.IO.Path.GetFileNameWithoutExtension(file), upTheme.FileBytes);

				lblUploadThemeResult.CssClass = "resultok";
				lblUploadThemeResult.Text = Properties.Messages.LoadedThemes;
				upTheme.Attributes.Add("value", "");

				LoadThemes();
			}
		}


		protected void lstThemes_SelectedIndexChanged(object sender, EventArgs e) {
		}

		protected void btnDeleteTheme_Click(object sender, EventArgs e) {
			if(lstThemes.SelectedIndex != 0) {
				if(Themes.DeleteTheme(provThemeSelector.SelectedValue + "|" + lstThemes.SelectedValue)) {
					LoadThemes();
					lblThemeResult.CssClass = "resultok";
					lblThemeResult.Text = Properties.Messages.ThemeDeleted;
				}
				else {
					lblThemeResult.CssClass = "resulterror";
					lblThemeResult.Text = Properties.Messages.CouldNotDeleteTheme;
				}
			}
			else {
				lblThemeResult.CssClass = "resulterror";
				lblThemeResult.Text = Properties.Messages.CouldNotDeleteTheme;
			}
		}
		#endregion;
	}
}
