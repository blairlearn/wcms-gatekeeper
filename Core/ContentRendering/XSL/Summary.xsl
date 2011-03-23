<?xml version='1.0'?>	
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
							  xmlns:msxsl="urn:schemas-microsoft-com:xslt"
							  xmlns:scripts="urn:local-scripts">

	<xsl:include href="Common/CommonElements.xsl"/>
	<xsl:include href="Common/CommonScripts.xsl"/>
	<xsl:include href="Common/CustomTemplates.xsl"/>
	<xsl:include href="Common/CommonTableFormatter.xsl"/>
	
	<xsl:output method="xml"/>

	<xsl:template match="/">
		<Summary>
			<xsl:apply-templates/>
		</Summary>
	</xsl:template>
	
	<xsl:template match="*">
		<!-- suppress defaults -->	
	</xsl:template>
	
	<!-- ****************************** TOC Section ***************************** -->
	
	<xsl:template match="Summary">
		<xsl:apply-templates/>
	</xsl:template>

	<xsl:template match="SummaryTitle">
		<Span Class="Summary-Title"><xsl:apply-templates/></Span>
			<ul>
			<xsl:for-each select="//SummarySection"> <!-- Only select a valid title element  -->
				<xsl:if test="./Title[node()] = true()"> <!-- Only select top 3 levels of title for TOC -->
					<xsl:if test="count(ancestor::SummarySection) &lt; 3">
						<xsl:choose>
							<xsl:when test="count(ancestor::SummarySection) = 0">
								<li Class="Summary-SummaryTitle-Level1">
									<xsl:element name="a"><xsl:attribute name="href">#Section<xsl:value-of select="@id"/></xsl:attribute>
										<xsl:apply-templates select="Title"/>
									</xsl:element>
								</li>
							</xsl:when>
							<xsl:when test="count(ancestor::SummarySection) = 1">
								<ul>
									<li Class="Summary-SummaryTitle-Level2">
										<xsl:element name="a"><xsl:attribute name="href">#Section<xsl:apply-templates select="@id"/></xsl:attribute>
											<xsl:apply-templates select="Title"/>
										</xsl:element>
									</li>
								</ul>			
							</xsl:when>
							<xsl:when test="count(ancestor::SummarySection) = 2">
								<ul>
									<ul>
										<li Class="Summary-SummaryTitle-Level3">
											<xsl:element name="a"><xsl:attribute name="href">#Section<xsl:apply-templates select="@id"/></xsl:attribute>
												<xsl:apply-templates select="Title"/>
											</xsl:element>
										</li>
									</ul>
								</ul>
							</xsl:when>
						</xsl:choose>
					</xsl:if>
				</xsl:if>		
			</xsl:for-each>
			</ul>
		<!--xsl:copy-of select="$ReturnToTopBar"/-->	
	
	</xsl:template>

	<!-- *************************** End TOC Section **************************** -->
	
	
	<!-- *********************** Summary Section Section ************************ -->

    <xsl:template match="SummarySection">
		<xsl:choose>
			<xsl:when test="count(ancestor::SummarySection) = 0">
				<xsl:element name="a">
					<xsl:attribute name="name">Section<xsl:value-of select="@id"/></xsl:attribute>
					<xsl:call-template name="SectionDetails"/>
				</xsl:element>
			</xsl:when>
			<xsl:otherwise>
				<xsl:element name="a">
					<xsl:attribute name="name">Section<xsl:value-of select="@id"/></xsl:attribute>
				</xsl:element>
				<xsl:call-template name="SectionDetails"/>
				<xsl:element name="a">
					<xsl:attribute name="name">END_Section<xsl:value-of select="@id"/></xsl:attribute>
				</xsl:element>
			</xsl:otherwise>
			
		</xsl:choose>
	</xsl:template>
	
	<xsl:template name="SectionDetails">
		<xsl:if test="name(..) = 'Summary'">

      <!-- If the section contains keypoints at any level, build up a list with
           appropriate nesting by recursively walking the SummarySection hierarchy. -->
        <xsl:if test="count(descendant-or-self::KeyPoint) != 0">
				<table cellSpacing="0" cellPadding="0" width="100%" align="center" border="1">
					<tr>
						<td class="Summary-SummarySection-Keypoint-Title">
							<xsl:choose>
								<xsl:when test="/Summary/SummaryMetaData/SummaryLanguage != 'English'">
									Puntos importantes de esta sección
								</xsl:when>
								<xsl:otherwise>Key Points for This Section 
								</xsl:otherwise>
							</xsl:choose>
						</td>
					</tr>
					<tr>
						<td>
						<img src="/images/spacer.gif" width="1" height="5" alt="" border="0" />

              <!-- Current node is a top-level SummarySection.  Keypoints are normally
                   found one-per sub-section, and generally none-at-all at the outermost
                   level of SummarySection tags. -->
              <xsl:choose>

                <!-- Normal case, top-level section has no keypoints -->
                <xsl:when test="count(KeyPoint) = 0">
                  <xsl:apply-templates select="." mode="build-keypoint-list" />
                </xsl:when>

                <!-- Unusual case, roll-up the top-level section's keypoints. -->
                <xsl:otherwise>
                  <ul class="Summary-SummarySection-KeyPoint-UL-Dash">
                    <xsl:for-each select="KeyPoint">
                      <li class="Summary-SummarySection-KeyPoint-LI">
                        <xsl:apply-templates select="." mode="build-keypoint-list" />
                        <xsl:if test="position() = last()">
                          <!-- Current node is a KeyPoint in the for-each, so we need to go up a level
                               to pass the topmost section into the keypoint building list. -->
                          <xsl:apply-templates select=".." mode="build-keypoint-list" />
                        </xsl:if>
                      </li>
                    </xsl:for-each>
                  </ul>
                </xsl:otherwise>
              </xsl:choose>
              
						<img src="/images/spacer.gif" width="1" height="5" alt="" border="0" />
						</td>
					</tr>	
				</table>
				</xsl:if>
		</xsl:if>
		<xsl:apply-templates/>

	</xsl:template>

  <!-- Match Summary Sections, but only when we're building a list of keypoints. -->
  <xsl:template match="SummarySection" mode="build-keypoint-list">

    <!-- Current node is a summary section, does it contain subsections with keypoints? -->
    <xsl:choose>

      <!-- Has Keypoints -->
      <xsl:when test="count(SummarySection/KeyPoint) > 0">
        <ul>
          <!-- Set the bullet-point style according to the level of nesting. -->
          <xsl:attribute name="class">
            <xsl:choose>
              <xsl:when test="count(ancestor::SummarySection) = 0">Summary-SummarySection-KeyPoint-UL-Bullet</xsl:when>
              <xsl:otherwise>Summary-SummarySection-KeyPoint-UL-Dash</xsl:otherwise>
            </xsl:choose>
          </xsl:attribute>

          <!-- Roll-up the keypoints from the next level of sub-sections. -->
          <xsl:for-each select="SummarySection">
            <xsl:choose>
              <xsl:when test="count(KeyPoint) > 0">
                <li class="Summary-SummarySection-KeyPoint-LI">
                  <xsl:apply-templates select="KeyPoint" mode="build-keypoint-list" />
                  <xsl:apply-templates select="."        mode="build-keypoint-list" />
                </li>
              </xsl:when>
              <xsl:otherwise>
                <xsl:apply-templates select="." mode="build-keypoint-list" />
              </xsl:otherwise>
            </xsl:choose>
          </xsl:for-each>
        </ul>
      </xsl:when>

      <!-- No Keypoints, process keypoints in sub-sections -->
      <xsl:otherwise>
        <xsl:apply-templates select="SummarySection" mode="build-keypoint-list" />
      </xsl:otherwise>
    </xsl:choose>

  </xsl:template>

  <xsl:template match="KeyPoint" mode="build-keypoint-list">
    <xsl:element name="a">
      <xsl:attribute name="href">#Keypoint<xsl:value-of select="count(preceding::KeyPoint) + 1"/></xsl:attribute>
      <xsl:apply-templates/>
    </xsl:element>
  </xsl:template>
  
	<xsl:template match="Title">
		<xsl:if test="count(ancestor::SummarySection) = 1">
			<!--Span class="Summary-SummarySection-Title-Level1"><xsl:apply-templates/></Span-->
		</xsl:if>
		<xsl:if test="count(ancestor::SummarySection) = 2">
			<Span class="Summary-SummarySection-Title-Level2"><xsl:apply-templates/></Span>
		</xsl:if>
		<xsl:if test="count(ancestor::SummarySection) = 3">
			<Span class="Summary-SummarySection-Title-Level3"><xsl:apply-templates/></Span>
		</xsl:if>
		<xsl:if test="count(ancestor::SummarySection) &gt; 3 ">
			<Span class="Summary-SummarySection-Title-Level4"><xsl:apply-templates/></Span>
		</xsl:if>
		<xsl:if test="following-sibling::node()">
			<xsl:for-each select="following-sibling::node()">
				<xsl:choose>
					<xsl:when test="name() ='SummarySection' and position()=1"><br /><br /></xsl:when>
					<!--xsl:when test="name() ='SummarySection' and position()=2"><br /><br /></xsl:when-->
					<xsl:when test="name() ='Table' and position()=1"><br /><br /></xsl:when>
					<!--xsl:when test="name() ='Table' and position()=2"><br /><br /></xsl:when-->
				</xsl:choose>
			</xsl:for-each>
		</xsl:if>
	</xsl:template>
	
	<!-- ******************** End Summary Section Section *********************** -->
	
	
	<!-- 
		************************* Reference Section **************************** 
		Reference will be pre-rendered.
	-->

  
  <xsl:template match="SummaryRef">
    <a inlinetype="SummaryRef" objectid="{@href}">
      <xsl:copy-of select="text()"/>
    </a>
  </xsl:template>
  
  <!--
    Renders a placeholder tag structure for MediaLinks.  The MediaLink data is
    gathered during the Extract step and the tag structure replaced by the CMS,
    using the value of the objectid attribute.
  -->
	<xsl:template match="MediaLink">
    <div inlinetype="rxvariant" objectid="{@id}">
      Placeholder slot
    </div>
	</xsl:template>
  
	<xsl:template match="Reference">
		<xsl:element name="a">
			<xsl:attribute name="href">#Reference<xsl:for-each select="ancestor::SummarySection[child::ReferenceSection]"><xsl:value-of select="count(preceding-sibling::SummarySection) + 1"/>.</xsl:for-each><xsl:value-of select="@refidx"/></xsl:attribute>
			<xsl:value-of select="@refidx"/>
		</xsl:element>	
	</xsl:template>
	
	<xsl:template match="ReferenceSection">
		<xsl:element name="a">
			<xsl:attribute name="name">ReferenceSection</xsl:attribute>
		</xsl:element>
		<p>
		    <!-- When Language is English, use References. Otherwise, Biblioggrafia -->
		    <Span Class="Summary-ReferenceSection">
				<xsl:choose>
					<xsl:when test="//SummaryMetaData/SummaryLanguage != 'English'">
						Bibliografía
					</xsl:when>
					<xsl:otherwise>
						References
					</xsl:otherwise>
				</xsl:choose>
			</Span>
			<ol>
				<xsl:apply-templates/>
			</ol>
		</p>
		<xsl:element name="a">
			<xsl:attribute name="name">END_ReferenceSection</xsl:attribute>
		</xsl:element>
	</xsl:template>
	
	<xsl:template match="Citation">
		<li>			
			<xsl:element name="a">
				<xsl:attribute name="name">Reference<xsl:for-each select="ancestor::SummarySection"><xsl:value-of select="count(preceding-sibling::SummarySection) + 1"/>.</xsl:for-each><xsl:value-of select="@idx"/></xsl:attribute>
			</xsl:element>
			<xsl:apply-templates/>&#160;
			<xsl:element name="A">
				<xsl:choose>
					<xsl:when test="@ProtocolID !=''">
            <xsl:variable name="audience">
              <xsl:choose>
                <xsl:when test="/Summary/SummaryMetaData/SummaryAudience= 'Patients'">patient</xsl:when>
                <xsl:otherwise>healthprofessional</xsl:otherwise>
              </xsl:choose>
            </xsl:variable>

            <xsl:attribute name="href">/clinicaltrials/search/view?version=<xsl:value-of select="$audience"/>&amp;cdrid=<xsl:value-of select="number(substring-after(@ProtocolID,'CDR'))"/></xsl:attribute>
						<span class="Summary-Citation-PUBMED">[PDQ Clinical Trial]</span>
					</xsl:when>
					<xsl:when test="@PMID !=''">
						<xsl:attribute name="href">http://www.ncbi.nlm.nih.gov/entrez/query.fcgi?cmd=Retrieve&amp;db=PubMed&amp;list_uids=<xsl:value-of select="@PMID"/>&amp;dopt=Abstract</xsl:attribute><span class="Summary-Citation-PUBMED">[PUBMED Abstract]</span>
					</xsl:when>
				</xsl:choose>				
			</xsl:element>
			<br/><br/>
		</li>
	</xsl:template>
			
	<!-- Key point is with h4 tag-->
	<xsl:template match="KeyPoint">
		<p>
			<xsl:element name="a">
				<xsl:attribute name="name">Keypoint<xsl:value-of select="count(preceding::KeyPoint) + 1"/></xsl:attribute>
			</xsl:element>
			<Span Class="Summary-KeyPoint">
				<xsl:apply-templates/>
			</Span>
		</p>
	</xsl:template>

	<xsl:template match="ProfessionalDisclaimer">
		<xsl:element name="a">
			<xsl:attribute name="name">Disclaimer</xsl:attribute>
		</xsl:element>
		<br />
		<xsl:for-each  select="descendant::Section">
			<Span Class="Summary-ProfessionalDisclaimer-Title"><xsl:value-of select="Title"/></Span>:
			<Span Class="Summary-ProfessionalDisclaimer"><xsl:apply-templates/></Span>
		</xsl:for-each>
		<xsl:element name="a">
			<xsl:attribute name="name">END_Disclaimer</xsl:attribute>
		</xsl:element>
	</xsl:template>
	
	<xsl:template match="ProtocolRef">
    <xsl:variable name="audience">
      <xsl:choose>
        <xsl:when test="/Summary/SummaryMetaData/SummaryAudience= 'Patients'">patient</xsl:when>
        <xsl:otherwise>healthprofessional</xsl:otherwise>
      </xsl:choose>
    </xsl:variable>
    
		<xsl:element name="A">
			<xsl:attribute name="Class">Summary-ProtocolRef</xsl:attribute>
			<xsl:attribute name="href">/clinicaltrials/search/view?version=<xsl:value-of select="$audience"/>&amp;cdrid=<xsl:value-of select="number(substring-after(@href,'CDR'))"/></xsl:attribute>
			<xsl:value-of select="."/>
		</xsl:element>
		<xsl:if test="following-sibling::node()">
			<xsl:for-each select="following-sibling::node()">
				<xsl:if test="name() !='' and position()=1">&#160;</xsl:if>
			</xsl:for-each>
		</xsl:if>
	</xsl:template>
					
	<xsl:template match="LOERef">
		<a>
			<xsl:attribute name="Class">Summary-LOERef</xsl:attribute>
			<xsl:choose>
				<xsl:when test="/Summary/SummaryMetaData/SummaryLanguage != 'English'">
					<xsl:attribute name="href">/Common/PopUps/popDefinition.aspx?id=<xsl:value-of select="number(substring-after(@href,'CDR'))"/>&amp;version=HealthProfessional&amp;language=Spanish</xsl:attribute>
				</xsl:when>
				<xsl:otherwise>
					<xsl:attribute name="href">/Common/PopUps/popDefinition.aspx?id=<xsl:value-of select="number(substring-after(@href,'CDR'))"/>&amp;version=HealthProfessional&amp;language=English</xsl:attribute>
				</xsl:otherwise>
			</xsl:choose>
				
			<xsl:choose>
				<xsl:when test="/Summary/SummaryMetaData/SummaryLanguage != 'English'">
					<xsl:attribute name="onclick">javascript:popWindow('defbyid','<xsl:value-of select="@href"/>&amp;version=HealthProfessional&amp;language=Spanish'); return(false);</xsl:attribute>	
				</xsl:when>
				<xsl:otherwise>	
					<xsl:attribute name="onclick">javascript:popWindow('defbyid','<xsl:value-of select="@href"/>&amp;version=HealthProfessional&amp;language=English'); return(false);</xsl:attribute>	
				</xsl:otherwise>
			</xsl:choose>
			<xsl:value-of select="."/>
		</a>
		<xsl:if test="following-sibling::node()">
			<xsl:for-each select="following-sibling::node()">
				<xsl:if test="name() !='' and position()=1">&#160;</xsl:if>
			</xsl:for-each>
		</xsl:if>
	</xsl:template>				
					
	<xsl:template match="GlossaryTermRef">
		<a>
			<xsl:attribute name="Class">Summary-GlossaryTermRef</xsl:attribute>
			<xsl:choose>
				<xsl:when test="/Summary/SummaryMetaData/SummaryLanguage != 'English'">
					<xsl:choose>
						<xsl:when test="/Summary/SummaryMetaData/SummaryAudience != 'Patients'">
							<xsl:choose>
								<xsl:when test="/Summary/SummaryMetaData/SummaryType = 'Complementary and alternative medicine'">
									<xsl:attribute name="href">/Common/PopUps/popDefinition.aspx?id=<xsl:value-of select="number(substring-after(@href,'CDR'))"/>&amp;version=Patient&amp;language=Spanish</xsl:attribute>
								</xsl:when>
								<xsl:otherwise>
									<xsl:attribute name="href">/Common/PopUps/popDefinition.aspx?id=<xsl:value-of select="number(substring-after(@href,'CDR'))"/>&amp;version=HealthProfessional&amp;language=Spanish</xsl:attribute>
								</xsl:otherwise>
							</xsl:choose>
						</xsl:when>
						<xsl:otherwise>
							<xsl:attribute name="href">/Common/PopUps/popDefinition.aspx?id=<xsl:value-of select="number(substring-after(@href,'CDR'))"/>&amp;version=Patient&amp;language=Spanish</xsl:attribute>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:when>
				<xsl:otherwise>
					<xsl:choose>
						<xsl:when test="/Summary/SummaryMetaData/SummaryAudience != 'Patients'">
							<xsl:choose>
								<xsl:when test="/Summary/SummaryMetaData/SummaryType = 'Complementary and alternative medicine'">
									<xsl:attribute name="href">/Common/PopUps/popDefinition.aspx?id=<xsl:value-of select="number(substring-after(@href,'CDR'))"/>&amp;version=Patient&amp;language=English</xsl:attribute>
								</xsl:when>
								<xsl:otherwise>
									<xsl:attribute name="href">/Common/PopUps/popDefinition.aspx?id=<xsl:value-of select="number(substring-after(@href,'CDR'))"/>&amp;version=HealthProfessional&amp;language=English</xsl:attribute>
								</xsl:otherwise>
							</xsl:choose>
						</xsl:when>
						<xsl:otherwise>
							<xsl:attribute name="href">/Common/PopUps/popDefinition.aspx?id=<xsl:value-of select="number(substring-after(@href,'CDR'))"/>&amp;version=Patient&amp;language=English</xsl:attribute>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:otherwise>
			</xsl:choose>

			
			<xsl:choose>
				<xsl:when test="/Summary/SummaryMetaData/SummaryLanguage != 'English'">
					<xsl:choose>
						<xsl:when test="/Summary/SummaryMetaData/SummaryAudience != 'Patients'">
							<xsl:choose>
								<xsl:when test="/Summary/SummaryMetaData/SummaryType = 'Complementary and alternative medicine'">
									<xsl:attribute name="onclick">javascript:popWindow('defbyid','<xsl:value-of select="@href"/>&amp;version=Patient&amp;language=Spanish');  return(false);</xsl:attribute>
								</xsl:when>
								<xsl:otherwise>									
									<xsl:attribute name="onclick">javascript:popWindow('defbyid','<xsl:value-of select="@href"/>&amp;version=HealthProfessional&amp;language=Spanish');  return(false);</xsl:attribute>
								</xsl:otherwise>
							</xsl:choose>
						</xsl:when>
						<xsl:otherwise>
							<xsl:attribute name="onclick">javascript:popWindow('defbyid','<xsl:value-of select="@href"/>&amp;version=Patient&amp;language=Spanish');  return(false);</xsl:attribute>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:when>
				<xsl:otherwise>
					<xsl:choose>
						<xsl:when test="/Summary/SummaryMetaData/SummaryAudience != 'Patients'">
							<xsl:choose>
								<xsl:when test="/Summary/SummaryMetaData/SummaryType = 'Complementary and alternative medicine'">
									<xsl:attribute name="onclick">javascript:popWindow('defbyid','<xsl:value-of select="@href"/>&amp;version=Patient&amp;language=English');  return(false);</xsl:attribute>
								</xsl:when>
								<xsl:otherwise>									
									<xsl:attribute name="onclick">javascript:popWindow('defbyid','<xsl:value-of select="@href"/>&amp;version=HealthProfessional&amp;language=English');  return(false);</xsl:attribute>
								</xsl:otherwise>
							</xsl:choose>
						</xsl:when>
						<xsl:otherwise>
							<xsl:attribute name="onclick">javascript:popWindow('defbyid','<xsl:value-of select="@href"/>&amp;version=Patient&amp;language=English');  return(false);</xsl:attribute>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:otherwise>
			</xsl:choose>
			<xsl:value-of select="."/>
		</a>
		<xsl:if test="following-sibling::node()">
			<xsl:for-each select="following-sibling::node()">
				<xsl:if test="name() !='' and position()=1">&#160;</xsl:if>
			</xsl:for-each>
		</xsl:if>
	</xsl:template>
	
	<!-- *********************** End Content Section **************************** -->

</xsl:stylesheet>
