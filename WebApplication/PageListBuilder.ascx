<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="PageListBuilder.ascx.cs" Inherits="ScrewTurn.Wiki.PageListBuilder" %>

<asp:TextBox ID="txtPageName" runat="server" CssClass="textbox"
	meta:resourcekey="txtPageNameResource1" />
<asp:Button ID="btnSearch" runat="server" Text="Search" ToolTip="Search for a Page"
	OnClick="btnSearch_Click" meta:resourcekey="btnSearchResource1" /><br />
<asp:DropDownList ID="lstAvailablePage" runat="server" CssClass="dropdown" 
	meta:resourcekey="lstAvailablePageResource1" />
<asp:Button ID="btnAddPage" runat="server" Text="Add" ToolTip="Add the selected Page to the list"
	Enabled="False" OnClick="btnAddPage_Click" 
	meta:resourcekey="btnAddPageResource1" />

<div id="PagesListDiv">
	<asp:ListBox ID="lstPages" runat="server" CssClass="listbox" 
		OnSelectedIndexChanged="lstPages_SelectedIndexChanged" meta:resourcekey="lstPagesResource1" />
</div>

<asp:Button ID="btnRemove" runat="server" Text="Remove" ToolTip="Remove the selected page from the list"
	Enabled="False" OnClick="btnRemove_Click" meta:resourcekey="btnRemoveResource1" />
