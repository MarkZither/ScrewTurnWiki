<%@ Page Title="Admin Theme" Language="C#" MasterPageFile="~/Admin.master" AutoEventWireup="true" CodeBehind="AdminTheme.aspx.cs" Inherits="ScrewTurn.Wiki.AdminTheme" culture="auto" meta:resourcekey="PageResource1" uiculture="auto" %>

<asp:Content ID="ctnHead" ContentPlaceHolderID="head" runat="server">
</asp:Content>


<asp:Content ID="ctnThemes" ContentPlaceHolderID="cphAdmin" runat="server">

	<h2 class="sectiontitle"><asp:Literal ID="lblThemes" runat="server" Text="Themes Management" EnableViewState="False" /></h2>
	<br />
	
	<h2 class="separator"><asp:Literal ID="lblUploadThemeTitle" runat="server" Text="Upload New Themes" EnableViewState="False" /></h2>
	<asp:Literal ID="lblSelectProvider" runat="server" Text="Destination provider" EnableViewState="false" /><br />
	<asp:DropDownList ID="lstProvThemeSelectorUpload" runat="server" CssClass="storageproviderselector" AutoPostBack="true"/>
	<br /><br />
	
	<asp:Literal ID="lblThemeZip" runat="server" Text="Theme ZIP file" EnableViewState="false" /><br />
	<asp:FileUpload ID="upTheme" runat="server" />
	<asp:Button ID="btnTheme" runat="server" Text="Upload" OnClick="btnTheme_Click" />
	<br />
	<asp:Label ID="lblUploadThemeResult" runat="server" />
	
	<br /><br />
	<h2 class="separator"><asp:Literal ID="lblDeletThemeTitle" runat="server" Text="Delete Existing Themes" EnableViewState="False" /></h2>
	<asp:Literal ID="lblSelectProvider2" runat="server" Text="Select the provider and the theme to delete" EnableViewState="false" /><br />
	<asp:DropDownList ID="provThemeSelector" runat="server" OnSelectedIndexChanged="providerThemeSelector_SelectedIndexChanged" CssClass="storageproviderselector" AutoPostBack="true"/>
	
	<asp:DropDownList ID="lstThemes" runat="server" />
	<asp:Button ID="btnDeleteTheme" runat="server" Text="Delete" OnClick="btnDeleteTheme_Click" />
	<br />
	<asp:Label ID="lblThemeResult" runat="server" />
	
	<div style="clear: both;"></div>
</asp:Content>