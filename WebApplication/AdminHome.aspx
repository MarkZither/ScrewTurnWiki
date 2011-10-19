<%@ Page Title="" Language="C#" MasterPageFile="~/Admin.master" AutoEventWireup="true" CodeBehind="AdminHome.aspx.cs" Inherits="ScrewTurn.Wiki.AdminHome" culture="auto" meta:resourcekey="PageResource2" uiculture="auto" %>

<asp:Content ID="ctnHead" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="ctnAdminHome" ContentPlaceHolderID="cphAdmin" runat="server">
	<h2 class="sectiontitle"><asp:Literal ID="lblAdminHome" runat="server" Text="Administration" EnableViewState="False" meta:resourcekey="lblAdminHomeResource1" /></h2>
	
	<h2 class="separator"><asp:Literal ID="lblMissingPages" runat="server" Text="Missing Pages" EnableViewState="False" meta:resourcekey="lblMissingPagesResource1" /></h2>
	<asp:Repeater ID="rptPages" runat="server" OnDataBinding="rptPages_DataBinding">
		<HeaderTemplate>
			<table class="generic" cellpadding="0" cellspacing="0">
				<thead>
				<tr class="tableheader">
					<th><asp:Literal ID="lblNamespace" runat="server" Text="Namespace" EnableViewState="False" meta:resourcekey="lblNamespaceResource1" /></th>
					<th><asp:Literal ID="lblPage" runat="server" Text="Page Name" EnableViewState="False" meta:resourcekey="lblPageResource1" /></th>
					<th><asp:Literal ID="lblLinkedIn" runat="server" Text="Linked in" EnableViewState="False" meta:resourcekey="lblLinkedInResource1" /></th>
				</tr>
				</thead>
				<tbody>
		</HeaderTemplate>
		<ItemTemplate>
			<tr class="tablerow">
				<td><a href='<%# Eval("NspacePrefix") %>Default.aspx' title='<%= ScrewTurn.Wiki.Properties.Messages.GoToMainPage %>' target="_blank"><%# Eval("Nspace") %></a></td>
				<td><a href='<%# Eval("NspacePrefix") %>Edit.aspx?Page=<%# ScrewTurn.Wiki.Tools.UrlEncode(Eval("Name") as string) %>' title='<%= ScrewTurn.Wiki.Properties.Messages.CreateThisPage %>' target="_blank"><%# ScrewTurn.Wiki.PluginFramework.NameTools.GetLocalName(Eval("Name") as string) %></a></td>
				<td><%# Eval("LinkingPages") %></td>
			</tr>
		</ItemTemplate>
		<AlternatingItemTemplate>
			<tr class="tablerowalternate">
				<td><a href='<%# Eval("NspacePrefix") %>Default.aspx' title='<%= ScrewTurn.Wiki.Properties.Messages.GoToMainPage %>' target="_blank"><%# Eval("Nspace") %></a></td>
				<td><a href='<%# Eval("NspacePrefix") %>Edit.aspx?Page=<%# ScrewTurn.Wiki.Tools.UrlEncode(Eval("Name") as string) %>' title='<%= ScrewTurn.Wiki.Properties.Messages.CreateThisPage %>' target="_blank"><%# ScrewTurn.Wiki.PluginFramework.NameTools.GetLocalName(Eval("Name") as string) %></a></td>
				<td><%# Eval("LinkingPages") %></td>
			</tr>
		</AlternatingItemTemplate>
		<FooterTemplate>
			</tbody>
			</table>
		</FooterTemplate>
	</asp:Repeater>
	<br /><br />
	
	<h2 class="separator"><asp:Literal ID="lblOrphanPages" runat="server" Text="Orphan Pages" EnableViewState="false" meta:resourcekey="lblOrphanPagesResource1" /></h2>
	<asp:Literal ID="lblOrphanPagesInfoPre" runat="server" Text="There seem to be " EnableViewState="false" meta:resourcekey="lblOrphanPagesInfoPreResource1" />
	<b><asp:Label ID="lblOrphanPagesCount" runat="server" Text="0" /></b>
	<asp:Literal ID="lblOrphanPagesInfoPost" runat="server" Text=" orphan pages in the wiki" EnableViewState="false" meta:resourcekey="lblOrphanPagesInfoPostResource1" />
	<small>(<asp:HyperLink ID="lnkPages" runat="server" Text="see Pages" ToolTip="Go to the Pages administration tab" NavigateUrl="~/AdminPages.aspx" meta:resourcekey="lnkPagesResource1" />)</small>.
	<br />
	<small><asp:Literal ID="lblOrphanPagesInfo" runat="server" Text="<b>Note</b>: a page is considered an <i>orphan</i> when it has no incoming links from other pages."
		EnableViewState="false" meta:resourcekey="lblOrphanPagesInfoResource1" /></small>
	<br /><br />
	
	<asp:Button ID="btnRebuildPageLinks" runat="server" Text="Rebuild Page Links" ToolTip="Rebuild the links structure"
		meta:resourcekey="btnRebuildPageLinksResource1"
		OnClick="btnRebuildPageLinks_Click" />
	<span id="OrphansProgressSpan" style="display: none;">
		<img src="Images/Wait.gif" alt="Rebuilding..." />
		<img src="Images/Wait.gif" alt="Rebuilding..." />
		<img src="Images/Wait.gif" alt="Rebuilding..." />
	</span>
	<br /><br />
	
	<small><asp:Literal ID="lblRebuildPageLinksInfo" runat="server" meta:resourcekey="lblRebuildPageLinksInfoResource1"
		Text="<b>Warning</b>: rebuilding page links might take some time. Please do not close this screen while the links are being rebuilt. If you experience timeouts, increase the request execution timeout in the web.config." 
		EnableViewState="False" /></small>
	<br /><br />
	
	<h2 class="separator"><asp:Literal ID="lblIndexStatus" runat="server" Text="Search Index Status" EnableViewState="False" meta:resourcekey="lblIndexStatusResource1" /></h2>
	<asp:Repeater ID="rptIndex" runat="server" OnDataBinding="rptIndex_DataBinding" OnItemCommand="rptIndex_ItemCommand" >
		<HeaderTemplate>
			<table class="generic" cellpadding="0" cellspacing="0">
				<thead>
				<tr class="tableheader">
					<th><asp:Literal ID="lblProvider" runat="server" Text="Provider" EnableViewState="False" meta:resourcekey="lblProviderResource1" /></th>
					<th>&nbsp;</th>
				</tr>
				</thead>
				<tbody>
		</HeaderTemplate>
		<ItemTemplate>
			<tr class="tablerow">
				<td><%# Eval("Provider") %></td>
				<td><asp:LinkButton ID="btnRebuild" runat="server" Text="Rebuild" ToolTip="Rebuild this index" CommandName='<%# Eval("Command") %>' CommandArgument='<%# Eval("ProviderType") %>'
					meta:resourcekey="btnRebuildResource1" /></td>
			</tr>
		</ItemTemplate>
		<AlternatingItemTemplate>
			<tr class="tablerowalternate">
				<td><%# Eval("Provider") %></td>
				<td><asp:LinkButton ID="btnRebuild" runat="server" Text="Rebuild" ToolTip="Rebuild this index" CommandName='<%# Eval("Command") %>' CommandArgument='<%# Eval("ProviderType") %>'
					meta:resourcekey="btnRebuildResource2" /></td>
			</tr>
		</AlternatingItemTemplate>
		<FooterTemplate>
			</tbody>
			</table>
		</FooterTemplate>
	</asp:Repeater>
	<br />
	<span id="ProgressSpan" style="display: none;">
		<img src="Images/Wait.gif" alt="Rebuilding..." />
		<img src="Images/Wait.gif" alt="Rebuilding..." />
		<img src="Images/Wait.gif" alt="Rebuilding..." />
	</span>
	<small>
		<asp:Literal ID="lblRebuildIndexInfo" runat="server" 
			Text="<b>Warning</b>: rebuilding a search index might take some time. Please do not close this screen while the index is being rebuilt. If you experience timeouts, increase the request execution timeout in the web.config." 
			EnableViewState="False" meta:resourcekey="lblRebuildIndexInfoResource1" />
	</small>

	<div id="BulkEmailDiv">
		<h2 class="separator"><asp:Literal ID="lblBulkEmail" runat="server" Text="Mass Email" EnableViewState="false" meta:resourcekey="lblBulkEmailResource1" /></h2>
		<asp:Literal ID="lblBulkEmailInfo" runat="server" Text="You can send an email message to all users of one or more groups." EnableViewState="false" meta:resourcekey="lblBulkEmailInfoResource1" />
		<br /><br />
		<asp:CheckBoxList ID="lstGroups" runat="server" CellSpacing="3" RepeatDirection="Horizontal" RepeatLayout="Table" RepeatColumns="2" />
		<br />
		<asp:Literal ID="lblSubject" runat="server" Text="Subject" EnableViewState="false" meta:resourcekey="lblSubjectResource1" /><br />
		<asp:TextBox ID="txtSubject" runat="server" CssClass="textbox" /><br />
		<asp:TextBox ID="txtBody" runat="server" TextMode="MultiLine" CssClass="body" />
		<br /><br />
		<asp:Button ID="btnSendBulkEmail" runat="server" Text="Send Mass Email" ValidationGroup="email" OnClick="btnSendBulkEmail_Click" meta:resourcekey="btnSendBulkEmailResource1" />
		<asp:RequiredFieldValidator ID="rfvSubject" runat="server" Display="Dynamic" CssClass="resulterror"
			ControlToValidate="txtSubject" ErrorMessage="Subject is required" ValidationGroup="email" meta:resourcekey="rfvSubjectResource1" />
		<asp:RequiredFieldValidator ID="rfvBody" runat="server" Display="Dynamic" CssClass="resulterror"
			ControlToValidate="txtBody" ErrorMessage="Body is required" ValidationGroup="email" meta:resourcekey="rfvBodyResource1" />
		<asp:CustomValidator ID="cvGroups" runat="server" Display="Dynamic" CssClass="resulterror"
			ErrorMessage="You must select at least one group" ValidationGroup="email" meta:resourcekey="cvGroupsResource1"
			OnServerValidate="cvGroups_ServerValidate" />
		<asp:Label ID="lblEmailResult" runat="server" />
	</div>
	
	<div style="clear: both;"></div>
</asp:Content>
