<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:fo="http://www.w3.org/1999/XSL/Format">
	<xsl:output method="html"/>
	<xsl:template match="/">
		<div id="UnfuddleTicketsDiv">
			<!-- Highest -->
			<xsl:if test="count(/root/ticket-report/groups/group/title[text() = 'Highest']/parent::group/tickets) &gt; 0">
			<h2 class="separator">Priority: Highest</h2>
			</xsl:if>
			<xsl:apply-templates select="/root/ticket-report/groups/group/title[text() = 'Highest']/parent::group/tickets"/>
			<!-- High -->
			<xsl:if test="count(/root/ticket-report/groups/group/title[text() = 'High']/parent::group/tickets) &gt; 0">
			<h2 class="separator">Priority: High</h2>
			</xsl:if>
			<xsl:apply-templates select="/root/ticket-report/groups/group/title[text() = 'High']/parent::group/tickets"/>
			<!-- Normal -->
			<xsl:if test="count(/root/ticket-report/groups/group/title[text() = 'Normal']/parent::group/tickets) &gt; 0">
			<h2 class="separator">Priority: Normal</h2>
			</xsl:if>
			<xsl:apply-templates select="/root/ticket-report/groups/group/title[text() = 'Normal']/parent::group/tickets"/>
			<!-- Low -->
			<xsl:if test="count(/root/ticket-report/groups/group/title[text() = 'Low']/parent::group/tickets) &gt; 0">
			<h2 class="separator">Priority: Low</h2>
			</xsl:if>
			<xsl:apply-templates select="/root/ticket-report/groups/group/title[text() = 'Low']/parent::group/tickets"/>
			<!-- Lowest -->
			<xsl:if test="count(/root/ticket-report/groups/group/title[text() = 'Lowest']/parent::group/tickets) &gt; 0">
			<h2 class="separator">Priority: Lowest</h2>
			</xsl:if>
			<xsl:apply-templates select="/root/ticket-report/groups/group/title[text() = 'Lowest']/parent::group/tickets"/>
		</div>
	</xsl:template>
	<xsl:template match="/root/ticket-report/groups/group/tickets" priority="5">
		<table style="width: 100%;" cellspacing="0" cellpadding="0">
			<thead>
				<tr class="tableheader">
					<th style="white-space: nowrap; text-align: left; width: 50px;">
                        #
                    </th>
					<th style="white-space: nowrap; text-align: left;">
                        Summary
                    </th>
					<th style="white-space: nowrap; text-align: left; width: 80px;">
                        Status
                    </th>
					<th style="white-space: nowrap;width: 70px;">
                        Priority
                    </th>
					<th style="white-space: nowrap; text-align: left;width: 120px;">
                        Milestone
                    </th>
				</tr>
			</thead>
			<tbody>
				<xsl:apply-templates select="ticket"/>
			</tbody>
		</table>
		<br />
		<br />
	</xsl:template>
	<xsl:template match="/root/ticket-report/groups/group/tickets/ticket">
		<tr>
			<xsl:if test="position() mod 2 != 1">
				<xsl:attribute name="class">priority_{priority} tablerowalternate</xsl:attribute>
			</xsl:if>
			<xsl:if test="position() mod 2 != 0">
				<xsl:attribute name="class">priority_{priority} tablerow</xsl:attribute>
			</xsl:if>
			<xsl:variable name="priority" select="priority"/>
			<td class='priority_{priority}' style="text-align: left;">
				<xsl:value-of select="number"/>
			</td>
			<td class='priority_{priority}'>
				<xsl:value-of select="summary" disable-output-escaping="yes"/>
			</td>
			<td class='priority_{priority}'>
				<xsl:variable name="c" select="substring(status,1,1)"/>
				<xsl:value-of select="translate($c,'abcdefghijklmnopqrst','ABCDEFGHIJKLMNOPQRST')"/>
				<xsl:value-of select="substring-after(status,$c)"/>
			</td>
			<td class='priority_{priority}' style="text-align: center;">
				<xsl:choose>
					<xsl:when test="priority = '5'">Highest</xsl:when>
					<xsl:when test="priority = '4'">High</xsl:when>
					<xsl:when test="priority = '3'">Normal</xsl:when>
					<xsl:when test="priority = '2'">Low</xsl:when>
					<xsl:when test="priority = '1'">Lowest</xsl:when>
				</xsl:choose>
			</td>
			<td class='priority_{priority}'>
				<xsl:variable name="milestoneID" select="milestone-id"/>
				<xsl:if test="$milestoneID = ''">
                    None
                </xsl:if>
				<xsl:if test="$milestoneID != ''">
					<xsl:value-of select="/root/milestones/milestone/id[text()=$milestoneID]/parent::milestone/title"/>
				</xsl:if>
			</td>
		</tr>
	</xsl:template>
</xsl:stylesheet>
