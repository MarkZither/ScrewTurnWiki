<%@ Page Title="Admin Theme" Language="C#" MasterPageFile="~/Admin.master" AutoEventWireup="true" CodeBehind="AdminTheme.aspx.cs" Inherits="ScrewTurn.Wiki.AdminTheme" culture="auto" meta:resourcekey="PageResource1" uiculture="auto" %>

<%@ Register TagPrefix="st" TagName="ProviderSelector" Src="~/ProviderSelector.ascx" %>

<asp:Content ID="ctnHead" ContentPlaceHolderID="head" runat="server">
</asp:Content>


<asp:Content ID="ctnThemes" ContentPlaceHolderID="cphAdmin" runat="server">

	<h2 class="sectiontitle"><asp:Literal ID="lblThemes" runat="server" Text="Themes" EnableViewState="False" meta:resourcekey="lblProvidersResource1" /></h2>
	<br />
	<h2 class="separator"><asp:Literal ID="lblUploadThemeTitle" runat="server" Text="Themes Upload" EnableViewState="False" meta:resourcekey="lblUploadThemeTitleResource1" /></h2>
	<h4><asp:Literal ID="lblSelectProvider" runat="server" Text="Select the provider" EnableViewState="false" meta:resourcekey="lblSelectProviderResource1" /></h4>
	<st:ProviderSelector ID="ProviderSelector" runat="server" ExcludeReadOnly="true" ProviderType="Themes" /><br />
	<br />
	<h4><asp:Literal ID="lblUploadNewTheme" runat="server" Text="Upload new Theme" EnableViewState="False" meta:resourcekey="lblUploadNewThemeResource1" /></h4>
	<asp:FileUpload ID="upTheme" runat="server" meta:resourcekey="upThemeResource1" />
	<asp:Button ID="btnTheme" runat="server" Text="Upload" OnClick="btnTheme_Click"
		meta:resourcekey="btnThemeResource1" />
	<br />
	<br />
	<asp:Label ID="lblUploadThemeResult" runat="server" meta:resourcekey="lblUploadThemeResultResource1" />
	<br />
	<h2 class="separator"><asp:Literal ID="lblDeletThemeTitle" runat="server" Text="Themes Delete" EnableViewState="False" meta:resourcekey="lblDeleteThemeTitleResource1" /></h2>
	<h4><asp:Literal ID="lblSelectProvider2" runat="server" Text="Select the provider" EnableViewState="false" meta:resourcekey="lblSelectProviderResource1" /></h4>
	<st:ProviderSelector ID="ThemeProviderSelector" runat="server" ExludeReadOnly="true" ProviderType="Themes" /><br />
	<br />
	<div id="ThemesListContainerDiv">
		<h4><asp:Literal ID="lblDeleteTheme" runat="server" Text="Delete old theme" EnableViewState="false" meta:resourcekey="lblDeleteThemeResource1" /></h4>
		<asp:DropDownList ID="lstThemes" runat="server" OnSelectedIndexChanged="lstThemes_SelectedIndexChanged" meta:resourcekey="lstDllsResource1" />
		<asp:Button ID="btnDeleteTheme" runat="server" Text="Delete" OnClick="btnDeleteTheme_Click" meta:resourcekey="btnDeleteThemeResource1" />
		<br />
		<asp:Label ID="lblThemeResult" runat="server" meta:resourcekey="lblThemeResultResource1" />
	</div>
	<div style="clear: both;"></div>
</asp:Content>