<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ProviderSelector.ascx.cs" Inherits="ScrewTurn.Wiki.ProviderSelector" %>
<asp:DropDownList ID="lstProviders" runat="server"
	OnSelectedIndexChanged="lstProviders_SelectedIndexChanged" CssClass="storageproviderselector" />
