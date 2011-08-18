<%@ Page Title="" Language="C#" MasterPageFile="~/Admin.master" AutoEventWireup="true" CodeBehind="AdminPages.aspx.cs" Inherits="ScrewTurn.Wiki.AdminPages" culture="auto" meta:resourcekey="PageResource2" uiculture="auto" %>

<%@ Register TagPrefix="st" TagName="PermissionsManager" Src="~/PermissionsManager.ascx" %>
<%@ Register TagPrefix="st" TagName="PageSelector" Src="~/PageSelector.ascx" %>
<%@ Register TagPrefix="st" TagName="ProviderSelector" Src="~/ProviderSelector.ascx" %>
<%@ Register TagPrefix="st" TagName="PageListBuilder" Src="~/PageListBuilder.ascx" %>

<asp:Content ID="ctnHead" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="ctnPages" ContentPlaceHolderID="cphAdmin" runat="server">
	<div class="leftaligned">
		<script type="text/javascript">
		<!--
			function __ShowDraftPreview() {
				document.getElementById("DraftPreviewDiv").style["display"] = "";
				return false;
			}
		// -->
		</script>

		<h2 class="sectiontitle"><asp:Literal ID="lblPages" runat="server" Text="Wiki Pages" EnableViewState="False" meta:resourcekey="lblPagesResource1" /></h2>
	
		<asp:Panel ID="pnlList" runat="server" meta:resourcekey="pnlListResource1" >
			<div id="NamespaceSelectorDiv">
				<asp:Literal ID="lblNamespace" runat="server" Text="Namespace" EnableViewState="False" meta:resourcekey="lblNamespaceResource1" /><br />
				<asp:DropDownList ID="lstNamespace" runat="server"  OnSelectedIndexChanged="lstNamespace_SelectedIndexChanged" Width="150px" meta:resourcekey="lstNamespaceResource1" />
			</div>
			<div id="FilterDiv">
				<asp:CheckBox ID="chkOrphansOnly" runat="server" Text="Display orphan pages only"  
					OnCheckedChanged="btnFilter_Click" meta:resourcekey="chkOrphansOnlyResource1" />
				&nbsp;&nbsp;&nbsp;&nbsp;
				<asp:TextBox ID="txtFilter" runat="server" CssClass="searchtextbox" ToolTip="Filter Pages by name" meta:resourcekey="txtFilterResource1" />
				<asp:ImageButton ID="btnFilter" runat="server" ToolTip="Apply Filter" ImageUrl="~/Images/Filter.png"
					EnableViewState="False" 
					CssClass="imagebutton" OnClick="btnFilter_Click" meta:resourcekey="btnFilterResource1" />
			</div>
		
			<div class="clear"></div>

			<div id="PageSelectorDiv">
				<st:PageSelector ID="pageSelector" runat="server" PageSize='<%# PageSize %>' OnSelectedPageChanged="pageSelector_SelectedPageChanged" />
			</div>
		
			<div id="PagesListContainerDiv">
				<asp:Repeater ID="rptPages" runat="server"
					OnDataBinding="rptPages_DataBinding" OnItemCommand="rptPages_ItemCommand">
					<HeaderTemplate>
						<table cellpadding="0" cellspacing="0" class="generic">
							<thead>
							<tr class="tableheader">
								<th><asp:Literal ID="lblName" runat="server" EnableViewState="False" meta:resourcekey="lblNameResource1" Text="Name" /></th>
								<th><asp:Literal ID="lblCurrentTitle" runat="server" EnableViewState="False" meta:resourcekey="lblCurrentTitleResource1" Text="Current Title" /></th>
								<th><asp:Literal ID="lblCreatedOn" runat="server" EnableViewState="False" meta:resourcekey="lblCreatedOnResource1" Text="Created On" /></th>
								<th><asp:Literal ID="lblCreatedBy" runat="server" EnableViewState="False" meta:resourcekey="lblCreatedByResource1" Text="Created By" /></th>
								<th><asp:Literal ID="lblLastModifiedOn" runat="server" EnableViewState="False" meta:resourcekey="lblLastModifiedOnResource1" Text="Modified On" /></th>
								<th><asp:Literal ID="lblLastModifiedBy" runat="server" EnableViewState="False" meta:resourcekey="lblLastModifiedByResource1" Text="Modified By" /></th>
								<th><asp:Literal ID="lblDiscussion" runat="server" EnableViewState="False" meta:resourcekey="lblDiscussionResource1" Text="Disc." /></th>
								<th><asp:Literal ID="lblRevisions" runat="server" EnableViewState="False" meta:resourcekey="lblRevisionsResource1" Text="Rev." /></th>
								<th><asp:Literal ID="lblOrphan" runat="server" EnableViewState="False" meta:resourcekey="lblOrphanResource1" Text="Orphan" /></th>
								<th><asp:Literal ID="lblProvider" runat="server" EnableViewState="False" meta:resourcekey="lblProviderResource1" Text="Provider" /></th>
								<th>&nbsp;</th>
							</tr>
							</thead>
							<tbody>
					</HeaderTemplate>
					<ItemTemplate>
						<tr class='tablerow<%# Eval("AdditionalClass") %>'>
							<td><a href='<%# Eval("FullName") %><%= ScrewTurn.Wiki.GlobalSettings.PageExtension %>' target="_blank" title='<%= ScrewTurn.Wiki.Properties.Messages.GoToPage %>'><%# ScrewTurn.Wiki.PluginFramework.NameTools.GetLocalName((string)Eval("FullName")) %></a></td>
							<td><%# Eval("Title") %></td>
							<td><%# Eval("CreatedOn") %></td>
							<td><%# Eval("CreatedBy") %></td>
							<td><%# Eval("LastModifiedOn") %></td>
							<td><%# Eval("LastModifiedBy") %></td>
							<td><a href='<%# Eval("FullName") %><%= ScrewTurn.Wiki.GlobalSettings.PageExtension %>?Discuss=1' target="_blank" title='<%= ScrewTurn.Wiki.Properties.Messages.GoToPageDiscussion %>'><%# Eval("Discussion") %></a></td>
							<td><a href='<%# Eval("FullName") %><%= ScrewTurn.Wiki.GlobalSettings.PageExtension %>?History=1' target="_blank" title='<%= ScrewTurn.Wiki.Properties.Messages.GoToPageHistory %>'><%# Eval("Revisions") %></a></td>
							<td><%# (bool)Eval("IsOrphan") ? "<span class=\"resulterror\">" + ScrewTurn.Wiki.Properties.Messages.Yes + "</span>" : ""%></td>
							<td><%# Eval("Provider") %></td>
							<td><a href='<%# Eval("FullName") %><%= ScrewTurn.Wiki.GlobalSettings.PageExtension %>?Edit=1' title='<%= ScrewTurn.Wiki.Properties.Messages.EditThisPage %>'><asp:Literal ID="lblEdit" runat="server" Text="Edit" EnableViewState="False" meta:resourcekey="lblEditResource1" /></a>
								&bull;
								<asp:LinkButton ID="btnSelect" runat="server" Visible='<%# (bool)Eval("CanSelect") %>' Text="Select" 
									ToolTip="Select this Page for administration" CommandName="Select" CommandArgument='<%# Eval("FullName") %>' meta:resourcekey="btnSelectResource1" />
								&bull;
								<asp:LinkButton ID="btnPermissions" runat="server" Visible='<%# (bool)Eval("CanSetPermissions") %>' Text="Permissions" 
									ToolTip="Manage Permissions for this Page" CommandName="Perms" CommandArgument='<%# Eval("FullName") %>' meta:resourcekey="btnPermissionsResource1" />
							</td>
						</tr>
					</ItemTemplate>
					<AlternatingItemTemplate>
						<tr class='tablerowalternate<%# Eval("AdditionalClass") %>'>
							<td><a href='<%# Eval("FullName") %><%= ScrewTurn.Wiki.GlobalSettings.PageExtension %>' target="_blank" title='<%= ScrewTurn.Wiki.Properties.Messages.GoToPage %>'><%# ScrewTurn.Wiki.PluginFramework.NameTools.GetLocalName((string)Eval("FullName")) %></a></td>
							<td><%# Eval("Title") %></td>
							<td><%# Eval("CreatedOn") %></td>
							<td><%# Eval("CreatedBy") %></td>
							<td><%# Eval("LastModifiedOn") %></td>
							<td><%# Eval("LastModifiedBy") %></td>
							<td><a href='<%# Eval("FullName") %><%= ScrewTurn.Wiki.GlobalSettings.PageExtension %>?Discuss=1' target="_blank" title='<%= ScrewTurn.Wiki.Properties.Messages.GoToPageDiscussion %>'><%# Eval("Discussion") %></a></td>
							<td><a href='<%# Eval("FullName") %><%= ScrewTurn.Wiki.GlobalSettings.PageExtension %>?History=1' target="_blank" title='<%= ScrewTurn.Wiki.Properties.Messages.GoToPageHistory %>'><%# Eval("Revisions") %></a></td>
							<td><%# (bool)Eval("IsOrphan") ? "<span class=\"resulterror\">" + ScrewTurn.Wiki.Properties.Messages.Yes + "</span>" : ""%></td>
							<td><%# Eval("Provider") %></td>
							<td>
								<a href='<%# Eval("FullName") %><%= ScrewTurn.Wiki.GlobalSettings.PageExtension %>?Edit=1' title='<%= ScrewTurn.Wiki.Properties.Messages.EditThisPage %>'><asp:Literal ID="lblEdit" runat="server" Text="Edit" EnableViewState="False" meta:resourcekey="lblEditResource2" /></a>
								&bull;
								<asp:LinkButton ID="btnSelect" runat="server" Visible='<%# (bool)Eval("CanSelect") %>' Text="Select" 
									ToolTip="Select this Page for administration" CommandName="Select" CommandArgument='<%# Eval("FullName") %>' meta:resourcekey="btnSelectResource2" />
								&bull;
								<asp:LinkButton ID="btnPermissions" runat="server" Visible='<%# (bool)Eval("CanSetPermissions") %>' Text="Permissions" 
									ToolTip="Manage Permissions for this Page" CommandName="Perms" CommandArgument='<%# Eval("FullName") %>' meta:resourcekey="btnPermissionsResource2" />
							</td>
						</tr>
					</AlternatingItemTemplate>
					<FooterTemplate>
						</tbody>
						</table>
					</FooterTemplate>
				</asp:Repeater>
			</div>
			<div class="ButtonsDiv">
				<asp:Button ID="btnNewPage" runat="server" Text="New Page" ToolTip="Create a new Page"
					OnClick="btnNewPage_Click" meta:resourcekey="btnNewPageResource1" />
				<br /><br /><br />
				<asp:Button ID="btnBulkMigrate" runat="server" Text="Bulk Migrate" ToolTip="Migrate many Pages to another namespace"
					OnClick="btnBulkMigrate_Click" meta:resourcekey="btnBulkMigrateResource1" />
			</div>
		</asp:Panel>
	
		<asp:Panel ID="pnlEditPage" runat="server" Visible="False" meta:resourcekey="pnlEditPageResource1" >
			<div id="EditPageDiv">
				<h2 class="separator"><asp:Literal ID="lblEditTitle" runat="server" Text="Page Operations" EnableViewState="False" meta:resourcekey="lblEditTitleResource1" />
				(<asp:Literal ID="lblCurrentPage" runat="server" meta:resourcekey="lblCurrentPageResource1" />)</h2>
				<br />
			
				<asp:Panel ID="pnlApproveRevision" runat="server" meta:resourcekey="pnlApproveRevisionResource1">
					<div class="pagefeaturecontainerapprove">
						<h3><asp:Literal ID="lblApproveTitle" runat="server" Text="Approve/Reject Draft" EnableViewState="False" meta:resourcekey="lblApproveTitleResource1" /></h3>
						<br />
						<asp:Literal ID="lblSavedOn" runat="server" Text="Saved on:" EnableViewState="False" meta:resourcekey="lblSavedOnResource1" />
						<b><asp:Literal ID="lblDateTime" runat="server" meta:resourcekey="lblDateTimeResource1" /></b><br />
						<asp:Literal ID="lblBy" runat="server" Text="By:" EnableViewState="False" meta:resourcekey="lblByResource1" />
						<b><asp:Literal ID="lblUser" runat="server" meta:resourcekey="lblUserResource1" /></b><br /><br />
						<a href="#" onclick="javascript:return __ShowDraftPreview();">Preview</a> &bull;
						<asp:HyperLink ID="lnkDiff" runat="server" Text="Diff" ToolTip="Show changes" Target="_blank" meta:resourcekey="lnkDiffResource1" /> &bull;
						<asp:HyperLink ID="lnkEdit" runat="server" Text="Edit" ToolTip="Edit this Draft" meta:resourcekey="lnkEditResource1" />
						<br /><br />
						<asp:Button ID="btnApprove" runat="server" Text="Approve" ToolTip="Approve this Revision"
							OnClick="btnApprove_Click" meta:resourcekey="btnApproveResource1" />
						<asp:Button ID="btnReject" runat="server" Text="Reject" ToolTip="Reject this Revision"
							OnClick="btnReject_Click" meta:resourcekey="btnRejectResource1" />
						<br />
						<asp:Label ID="lblApproveResult" runat="server" Auto meta:resourcekey="lblApproveResultResource1" />
					</div>
				</asp:Panel>
			
				<asp:Panel ID="pnlRename" runat="server" meta:resourcekey="pnlRenameResource1">
					<div class="pagefeaturecontainer">
						<h3><asp:Literal ID="lblRenamePageTitle" runat="server" Text="Rename Page" EnableViewState="False" meta:resourcekey="lblRenamePageTitleResource1" /></h3>
						<br />
						<asp:Literal ID="lblNewName" runat="server" Text="New Name" EnableViewState="False" meta:resourcekey="lblNewNameResource1" /><br />
						<asp:TextBox ID="txtNewName" runat="server" Width="200px" ValidationGroup="renpage" meta:resourcekey="txtNewNameResource1" AutoPostBack="true" /><br />
						<asp:CheckBox ID="chkShadowPage" runat="server" Text="Keep shadow Page" Checked="True"
							ToolTip="Keep the old Page and set it to auto-redirect to the new Page" meta:resourcekey="chkShadowPageResource1" />
						<br /><br />
						<asp:Button ID="btnRename" runat="server" Text="Rename" ToolTip="Rename the Page"
							OnClick="btnRename_Click" ValidationGroup="renpage" meta:resourcekey="btnRenameResource1" />
						<br />
						<asp:RequiredFieldValidator ID="rfvNewName" runat="server" Display="Dynamic" ValidationGroup="renpage"
							ControlToValidate="txtNewName" CssClass="resulterror" ErrorMessage="New Name is required" meta:resourcekey="rfvNewNameResource1" />
						<asp:CustomValidator ID="cvNewName" runat="server" Display="Dynamic" ValidationGroup="renpage"
							ControlToValidate="txtNewName" CssClass="resulterror" ErrorMessage="Invalid Page Name" OnServerValidate="cvNewName_ServerValidate" 
							meta:resourcekey="cvNewNameResource1" />
						<asp:Label ID="lblRenameResult" runat="server" meta:resourcekey="lblRenameResultResource1" />
					</div>
				</asp:Panel>
			
				<asp:Panel ID="pnlMigrate" runat="server" meta:resourcekey="pnlMigrateResource1">
					<div class="pagefeaturecontainer">
						<h3><asp:Literal ID="lblMigratePageTitle" runat="server" Text="Migrate Page" EnableViewState="false" meta:resourcekey="lblMigratePageTitleResource1" /></h3>
						<br />
						<asp:Literal ID="lblTargetNamespace" runat="server" Text="Target Namespace" EnableViewState="false" meta:resourcekey="lblTargetNamespaceResource1" />
						<br />
						<asp:DropDownList ID="lstTargetNamespace" runat="server" Width="200px" meta:resourcekey="lstTargetNamespaceResource1" AutoPostBack="true" />
						<br />
						<asp:CheckBox ID="chkCopyCategories" runat="server" Text="Copy Page Categories" ToolTip="Copy Page Categories to target Namespace" meta:resourcekey="chkCopyCategoriesResource1" />
						<br /><br />
						<asp:Button ID="btnMigrate" runat="server" Text="Migrate" ToolTip="Migrate the Page"
							OnClick="btnMigrate_Click" meta:resourcekey="btnMigrateResource1" />
						<asp:Label ID="lblMigrateResult" runat="server" meta:resourcekey="lblMigrateResource1" />
					</div>
				</asp:Panel>
			
				<asp:Panel ID="pnlRollback" runat="server" meta:resourcekey="pnlRollbackResource1" >
					<div class="pagefeaturecontainer">
						<h3><asp:Literal ID="lblRollbackPageTitle" runat="server" Text="Rollback Page" EnableViewState="False" meta:resourcekey="lblRollbackPageTitleResource1" /></h3>
						<br />
						<asp:Literal ID="lblRevision" runat="server" Text="Target Revision" EnableViewState="False" meta:resourcekey="lblRevisionResource1" /><br />
						<asp:DropDownList ID="lstRevision" runat="server" Width="200px" ToolTip="Available revisions, newer first" meta:resourcekey="lstRevisionResource1" />
						<br /><br />
						<asp:Button ID="btnRollback" runat="server" Text="Rollback" ToolTip="Rollback the Page"
							OnClick="btnRollback_Click" meta:resourcekey="btnRollbackResource1" />
						<br />
						<asp:Label ID="lblRollbackResult" runat="server" 
							Auto meta:resourcekey="lblRollbackResultResource1" />
					</div>
				</asp:Panel>
			
				<asp:Panel ID="pnlDeleteBackups" runat="server" meta:resourcekey="pnlDeleteBackupsResource1" >
					<div class="pagefeaturecontainer">
						<h3><asp:Literal ID="lblDeleteBackupsTitle" runat="server" Text="Delete Backups" EnableViewState="False" meta:resourcekey="lblDeleteBackupsTitleResource1" /></h3>
						<br />
						<asp:RadioButton ID="rdoAllBackups" runat="server" Text="Delete all Backups" GroupName="baks"
							 OnCheckedChanged="rdoBackup_CheckedChanged" meta:resourcekey="rdoAllBackupsResource1" /><br />
						<asp:RadioButton ID="rdoUpTo" runat="server" Text="Delete Backups older than and including"
							GroupName="baks"  OnCheckedChanged="rdoBackup_CheckedChanged" meta:resourcekey="rdoUpToResource1" /><br />
						<asp:DropDownList ID="lstBackup" runat="server" Width="200px" 
							Enabled="False" ToolTip="Available Backups, newer first" meta:resourcekey="lstBackupResource1" />
						<br /><br />
						<asp:Button ID="btnDeleteBackups" runat="server" Text="Delete" ToolTip="Delete the Page's Backups"
							OnClick="btnDeleteBackups_Click" meta:resourcekey="btnDeleteBackupsResource1" />
						<br />
						<asp:Label ID="lblBackupResult" runat="server" Auto meta:resourcekey="lblBackupResultResource1" />
					</div>
				</asp:Panel>
			
				<asp:Panel ID="pnlClearDiscussion" runat="server" meta:resourcekey="pnlClearDiscussionResource1">
					<div class="pagefeaturecontainer">
						<h3><asp:Literal ID="lblClearDiscussionTitle" runat="server" Text="Clear Page Discussion" EnableViewState="False" meta:resourcekey="lblClearDiscussionTitleResource1" /></h3>
						<br />
						<asp:Button ID="btnClearDiscussion" runat="server" Text="Clear Discussion" ToolTip="Delete all Messages in the Page's Discussion"
							OnClick="btnClearDiscussion_Click" meta:resourcekey="btnClearDiscussionResource1" />
						<br />
						<asp:Label ID="lblDiscussionResult" runat="server" Auto meta:resourcekey="lblDiscussionResultResource1" />
					</div>
				</asp:Panel>
			
				<asp:Panel ID="pnlDelete" runat="server" meta:resourcekey="pnlDeleteResource1" >
					<div class="pagefeaturecontainerwarning">
						<h3><asp:Literal ID="lblDeletePage" runat="server" Text="Delete Page" EnableViewState="False" meta:resourcekey="lblDeletePageResource1" /></h3>
						<br />
						<asp:Button ID="btnDeletePage" runat="server" Text="Delete Page" ToolTip="Delete this Page"
							OnClick="btnDeletePage_Click" meta:resourcekey="btnDeletePageResource1" />
						<br />
						<asp:Label ID="lblDeleteResult" runat="server" Auto meta:resourcekey="lblDeleteResultResource1" />
					</div>
				</asp:Panel>

				<div class="clear"></div>
			
				<div class="ButtonsDiv">
					<asp:Button ID="btnBack" runat="server" Text="Back" ToolTip="Back to the Page list" OnClick="btnBack_Click" CausesValidation="False" meta:resourcekey="btnBackResource1" />
				</div>
			</div>
		</asp:Panel>
	
		<asp:Panel ID="pnlPermissions" runat="server" Visible="False" meta:resourcekey="pnlPermissionsResource1">
			<h2 class="separator">
				<asp:Literal ID="lblPagePermissions" runat="server" Text="Page Permissions" EnableViewState="False" meta:resourcekey="lblPagePermissionsResource1" />
				(<asp:Literal ID="lblPageName" runat="server" meta:resourcekey="lblPageNameResource1" />)
			</h2>
		
			<div id="PermissionsTemplatesDiv">
				<a href="#" onclick="javascript:document.getElementById('PermissionsTemplatesContainerDiv').style['display'] = ''; return false;">
					<asp:Literal ID="lblPermissionsTemplates" runat="server" Text="Permissions Templates" EnableViewState="False" meta:resourcekey="lblPermissionsTemplatesResource1" />...
				</a>
				<div id="PermissionsTemplatesContainerDiv" style="display: none;">
					<br />
					<ul>
						<li>
							<asp:LinkButton ID="btnPublic" runat="server" Text="Public" 
								ToolTip="Use this Template" 
								OnClick="btnPublic_Click" meta:resourcekey="btnPublicResource1" /><br />
							<small>
								<asp:Literal ID="lblPublicInfo" runat="server" 
									Text="Anyone, including anonymous users, can <b>edit</b> this page." 
									EnableViewState="False" meta:resourcekey="lblPublicInfoResource1" />
							</small>
						</li>
						<li>
							<asp:LinkButton ID="btnAsNamespace" runat="server" Text="As Namespace" 
								ToolTip="Use this Template" 
								OnClick="btnAsNamespace_Click" meta:resourcekey="btnAsNamespaceResource1" /><br />
							<small>
								<asp:Literal ID="lblNormalInfo" runat="server" 
									Text="Inherit permissions from the parent namespace." EnableViewState="False" 
									meta:resourcekey="lblNormalInfoResource1" />
							</small>
						</li>
						<li>
							<asp:LinkButton ID="btnLocked" runat="server" Text="Locked" 
								ToolTip="Use this Template"
								OnClick="btnLocked_Click" meta:resourcekey="btnLockedResource1" /><br />
								<small>
									<asp:Literal ID="lblLockedInfo" runat="server" 
										Text="Only Administrators can <b>edit</b> this page." EnableViewState="False" 
										meta:resourcekey="lblLockedInfoResource1" />
								</small>
						</li>
					</ul>
					<br />
					<small>
						<asp:Literal ID="lblPermissionsTemplatesInfo" runat="server" 
							Text="<b>Warning</b>: setting a template will replace all existing permissions for the default user groups." 
							EnableViewState="False" meta:resourcekey="lblPermissionsTemplatesInfoResource1" />
					</small>
				</div>
			</div>
	
			<st:PermissionsManager ID="permissionsManager" runat="server" CurrentResourceType="Pages" />
		
			<div class="clear"></div>

			<div class="ButtonsDiv">
				<asp:Button ID="btnBack2" runat="server" Text="Back" ToolTip="Back to the Page list" OnClick="btnBack_Click" meta:resourcekey="btnBack2Resource1" />
			</div>
		</asp:Panel>
	
		<asp:Panel ID="pnlBulkMigrate" runat="server" Visible="false">
			<div id="PageBulkMigrateDiv">
				<div id="PageSelectionDiv">			
					<h3 class="separator"><asp:Literal ID="lblBulkStep1" runat="server" Text="1. Select Pages" EnableViewState="false" meta:resourcekey="lblBulkStep1Resource1" /></h3>
				
					<st:ProviderSelector ID="providerSelector" runat="server" ProviderType="Pages"
						OnSelectedProviderChanged="providerSelector_SelectedProviderChanged" />
					<br /><br />
				
					<st:PageListBuilder ID="pageListBuilder" runat="server" />
				</div>
			
				<div id="NamespaceSelectionDiv">
					<h3 class="separator"><asp:Literal ID="lblBulkStep2" runat="server" Text="2. Select Target Namespace" EnableViewState="false" meta:resourcekey="lblBulkStep2Resource1" /></h3>
				
					<asp:Literal ID="lblBulkMigarateInfo" runat="server" Text="<b>Note</b>: this list only displays namespaces in which you are allowed to manage all pages." EnableViewState="false"
						meta:resourcekey="lblBulkMigrateInfoResource1" />
					<br /><br />
				
					<asp:DropDownList ID="lstBulkMigrateTargetNamespace" runat="server" CssClass="dropdown" Auto />
				</div>
			
				<div id="BatchControlDiv">
					<h3 class="separator"><asp:Literal ID="lblBulkStep3" runat="server" Text="3. Perform Migration" EnableViewState="false" meta:resourcekey="lblBulkStep3Resource1" /></h3>
					<asp:CheckBox ID="chkBulkMigrateCopyCategories" runat="server" Text="Copy Page Categories" ToolTip="Copy Page Categories to target Namespace" meta:resourcekey="chkBulkMigrateCopyCategoriesResource1" />
					<br /><br />
				
					<asp:Literal ID="lblBulkMigrateInfo2" runat="server" Text="<b>Note</b>: the default page of the namespace cannot be migrated and is silently skipped." EnableViewState="false"
						meta:resourcekey="lblBulkMigrateInfo2Resource1" />
					<br /><br />
				
					<asp:Button ID="btnBulkMigratePages" runat="server" Text="Migrate" OnClientClick="javascript:return RequestConfirm();" OnClick="btnBulkMigratePages_Click"
						meta:resourcekey="btnBulkMigratePagesResource1" /><br />
					<asp:Label ID="lblBulkMigrateResult" runat="server" />
				</div>
			
				<div class="ButtonsDiv">
					<asp:Button ID="btnBulkMigrateBack" runat="server" Text="Back" ToolTip="Back to the Page list" OnClick="btnBulkMigrateBack_Click"
						CausesValidation="False" meta:resourcekey="btnBulkMigrateBackResource1" />
				</div>
			</div>
		</asp:Panel>
	
		<div id="DraftPreviewDiv" style="display: none;">
			<asp:Label ID="lblDraftPreview" runat="server" meta:resourcekey="lblDraftPreviewResource1" />
		</div>
	
		<asp:HiddenField ID="txtCurrentPage" runat="server" />
	</div>

	<div style="clear: both;"></div>

</asp:Content>
