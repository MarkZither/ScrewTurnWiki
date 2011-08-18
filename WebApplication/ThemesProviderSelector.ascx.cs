
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki;

namespace ScrewTurn.Wiki {
	public partial class ThemesProviderSelector : System.Web.UI.UserControl {

		private ProviderType providerType = ProviderType.Themes;
		private bool excludeReadOnly = false;
		private UsersProviderIntendedUse usersProviderIntendedUse = UsersProviderIntendedUse.AccountsManagement;
		private string currentWiki = null;

		protected void Page_Load(object sender, EventArgs e) {
			currentWiki = Tools.DetectCurrentWiki();
			
			object t = ViewState["ProviderType"];
			if(t != null) providerType = (ProviderType)t;
			t = ViewState["ExcludeReadOnly"];
			if(t != null) excludeReadOnly = (bool)t;
			t = ViewState["UsersProviderIntendedUse"];
			if(t != null) usersProviderIntendedUse = (UsersProviderIntendedUse)t;
			if(!Page.IsPostBack) {
				Reload();
				FillThemes(SelectedProvider);
			}
		}

		/// <summary>
		/// Reloads this instance.
		/// </summary>
		public void Reload() {
			IProviderV40[] allProviders = null;

			allProviders = Collectors.CollectorsBox.ThemesProviderCollector.GetAllProviders(currentWiki);
			lstThemesProviders.Items.Clear();

			lstThemesProviders.Items.Add(new ListItem("standard", "standard"));
			foreach(IProviderV40 prov in allProviders) {
				string typeName = prov.GetType().FullName;
				ListItem item = new ListItem(prov.Information.Name, typeName);
				if(typeName == Settings.GetTheme(currentWiki, null).Split(new char[] { '|' })[0]) item.Selected = true;
				lstThemesProviders.Items.Add(item);
			}
		}

		/// <summary>
		/// Gets or sets the selected provider.
		/// </summary>
		public string SelectedProvider {
			get { return lstThemesProviders.SelectedValue; }
			set {
				lstThemesProviders.SelectedIndex = -1;
				foreach(ListItem itm in lstThemesProviders.Items) {
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
		public string SelectedThemes {
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

		/// <summary>
		/// Determines whether this instance is default.
		/// </summary>
		public bool IsDefault() {
			bool result = false;
			if(lstThemesProviders.SelectedValue == "standard") result = true;
			return result;
		}

		/// <summary>
		/// Gets or sets a value indicating whether the control is enabled.
		/// </summary>
		public bool Enabled {
			get { return lstThemesProviders.Enabled; }
			set { lstThemesProviders.Enabled = value; }
		}

		private void FillThemes(string provider) {
			lstThemes.Items.Clear();
			lstThemes.Items.Add(new ListItem(Properties.Messages.SelectTheme, ""));
			foreach(string th in Themes.ListThemes(currentWiki, provider)) {
				ListItem item = new ListItem(th, th);
				if(th == Settings.GetTheme(currentWiki, null).Split(new char[] { '|' })[1]) item.Selected = true;
				lstThemes.Items.Add(item);
			}
		}

		/// <summary>
		/// Event fired when the selected provider changes.
		/// </summary>
		public event EventHandler<EventArgs> SelectedThemesProviderChanged;

		protected void lstProviders_SelectedIndexChanged(object sender, EventArgs e) {
			if(SelectedThemesProviderChanged != null) SelectedThemesProviderChanged(sender, e);
			else FillThemes(SelectedProvider);
		}
	}
}