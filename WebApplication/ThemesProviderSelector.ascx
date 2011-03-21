<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ThemesProviderSelector.ascx.cs" Inherits="ScrewTurn.Wiki.ThemesProviderSelector" %>
<asp:DropDownList ID="lstThemesProviders" runat="server" OnSelectedIndexChanged="lstProviders_SelectedIndexChanged" CssClass="storageproviderselector" AutoPostBack="true" meta:resourcekey="lstThemesProvidersResource1"/> <br />
<asp:DropDownList ID="lstThemes" runat="server" CssClass="dropdown" meta:resourcekey="lstThemeResource1" AutoPostBack="true" /><br />
