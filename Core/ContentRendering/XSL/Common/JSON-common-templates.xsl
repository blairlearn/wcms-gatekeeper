﻿<?xml version="1.0" encoding="utf-8"?>
<!--
  Shared templates for the set of XSL files used for rendering JSON.
  
  Correct function of these templates is VERY SENSITIVE TO EXTRA CARRIAGE RETURNS.
  Be very careful when using an editor (e.g. Visual Studio) which automatically
  reformats text to a "suggested" format.
  
-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl"
>
    <xsl:output method="text" indent="yes"/>

  
  <!--
    GetNumericID - Converts an id formatted CDR000012345 to a number with no leading zeros.
    
    Args:
      cdrid - A numeric ID with the first four characters being "CDR0".
  -->
  <xsl:template name="GetNumericID">
    <xsl:param name="cdrid" />
    <!--
      translate - renders the letters to lowercase.
      substring-after - remove the leading "cdr0"
      number - convert to numeric, implictly removing leading zeros.
    -->
    <xsl:value-of select="number(substring-after(translate($cdrid,'CDR','cdr'), 'cdr0'))" />
  </xsl:template>



  <!--
    Matches the ExternalRef, LOERef, ProtocolRef, GlossaryTermRef elements, outputting the common
    xref attribute as a link.
  -->
  <xsl:template match="ExternalRef|LOERef|ProtocolRef|GlossaryTermRef"><!--
    -->&lt;a href=\"<xsl:value-of select="@xref" />\"&gt;<xsl:apply-templates select="node()" />&lt;/a&gt;<!--
--></xsl:template>

  
  <!--
    Matches the SummaryRef element.
  -->
  <xsl:template match="SummaryRef"><!--
    -->&lt;a href=\"<xsl:value-of select="@url" />\"&gt;<xsl:apply-templates select="node()" />&lt;/a&gt;<!--
--></xsl:template>


  <!--
     Match for all text nodes when processing with mode="JSON".
     A call to xsl:apply-templates with mode="JSON" will cause the
     text in a hierarchy of nodes to be output without element names.
     
     NOTE: A template with match="node()" will take priority over this one.
  -->
  <xsl:template match="text()" mode="JSON">
    <!-- Allow the regular processing for text nodes (below) to handle the element. -->
    <xsl:apply-templates select="." />
  </xsl:template>


  <!--
     Match for all Text nodes.  The text is sent through a series of substitutions
     to escape special characters.
     
     '\' is replaced with \\
     Carriage return is replaced with \r.
     Newline is replaced with \n.
     Quotation mark is replaced with \".
  -->
  <xsl:template match="text()">
    <!-- Preserve a leading space. -->
    <xsl:variable name="leadingSpace">
      <xsl:if test="starts-with(., ' ')"><xsl:text xml:space="preserve"> </xsl:text></xsl:if>
    </xsl:variable>
    
    <!-- Preserve a trailing space. -->
    <xsl:variable name="trailingSpace">
      <!-- There's no "ends-with() function, so we end using a slightly odd-looking expression
           to check for trailing spaces. -->
      <xsl:if test="string-length(.) > 0 and substring(., string-length(.)) = ' '"><xsl:text xml:space="preserve"> </xsl:text></xsl:if>
    </xsl:variable>
    
    <!-- Escape backslash in the current node ("string" param)
         and cascade the change to all subsequent replacements. -->
    <xsl:variable name="slashEscaped">
      <xsl:call-template name="Replace">
        <xsl:with-param name="string" select="." />
        <xsl:with-param name="target" select="'\'" />
        <xsl:with-param name="replace" select="'\\'" />
      </xsl:call-template>
    </xsl:variable>
    <!-- Replace Carriage return -->
    <xsl:variable name="returnEscaped">
      <xsl:call-template name="Replace">
        <xsl:with-param name="string" select="$slashEscaped" />
        <xsl:with-param name="target" select="'&#xa;'" />
        <xsl:with-param name="replace" select="'\r'" />
      </xsl:call-template>
    </xsl:variable>
    <!-- Replace Newline -->
    <xsl:variable name="newlineEscaped">
      <xsl:call-template name="Replace">
        <xsl:with-param name="string" select="$returnEscaped" />
        <xsl:with-param name="target" select="'&#xd;'" />
        <xsl:with-param name="replace" select="'\n'" />
      </xsl:call-template>
    </xsl:variable>
    <!-- Replace quotation mark -->
    <xsl:variable name="escaped">
      <xsl:call-template name="Replace">
        <xsl:with-param name="string" select="$newlineEscaped" />
        <xsl:with-param name="target" select="'&quot;'" />
        <xsl:with-param name="replace" select="'\&quot;'" />
      </xsl:call-template>
    </xsl:variable>
    <!-- Output text with possible leading/trailing spaces. -->
    <xsl:value-of select="$leadingSpace"/><xsl:value-of select="normalize-space($escaped)"/><xsl:value-of select="$trailingSpace"/>
  </xsl:template>


  <!--
    Scans through the value contained in the $string parameter and replaces
    all instances of $target with $replace.
  -->
  <xsl:template name="Replace">
    <xsl:param name="string" />
    <xsl:param name="target" />
    <xsl:param name="replace" />

    <xsl:choose>

      <!-- string contains the target -->
      <xsl:when test="contains($string, $target)">

        <!-- Everything before the target-->
        <xsl:variable name="pre" select="substring-before($string, $target)" />

        <!-- Everything after the target goes back through the template. -->
        <xsl:variable name="post">
          <xsl:call-template name="Replace">
            <xsl:with-param name="string" select="substring-after($string, $target)" />
            <xsl:with-param name="target" select="$target" />
            <xsl:with-param name="replace" select="$replace" />
          </xsl:call-template>
        </xsl:variable>

        <!-- Rebuild the string with the replacement characters. -->
        <xsl:value-of select="concat($pre, $replace, $post)"/>
        <!--<xsl:value-of select="$pre"/>-->
      </xsl:when>

      <!--Just return the string-->
      <xsl:otherwise>
        <xsl:value-of select="$string"/>
      </xsl:otherwise>
    </xsl:choose>

  </xsl:template>
  
</xsl:stylesheet>
