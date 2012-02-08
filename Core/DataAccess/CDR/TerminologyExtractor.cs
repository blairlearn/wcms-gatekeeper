using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using GateKeeper.Common;
using GateKeeper.Common.XPathKeys;
using GateKeeper.DocumentObjects;
using GateKeeper.DocumentObjects.Terminology;
using GateKeeper.DataAccess.GateKeeper;

namespace GateKeeper.DataAccess.CDR
{
    /// <summary>
    /// Class to extract terminology object from an input XML document.
    /// </summary>
    public class TerminologyExtractor
    {
        #region Fields
        private int    _documentID = 0;
        private DocumentXPathManager xPathManager;
        #endregion
        
        #region Private Methods
          /// <summary>
        /// Method to extract misc metadata.
        /// </summary>
        /// <param name="xNav"></param>
        /// <param name="terminology"></param>
        private void ExtractMetaData(XPathNavigator xNav, TerminologyDocument terminology)
        {
            // The path is used to hold the xpath for error catching
            string path = xPathManager.GetXPath(TerminologyXPath.PerferenceName); 
            try
            {
                terminology.PreferredName = xNav.SelectSingleNode(path).Value;

                path = xPathManager.GetXPath(TerminologyXPath.DefinitionType); 
                XPathNavigator defTypeNav = xNav.SelectSingleNode(path);
                if (defTypeNav != null)
                {
                    terminology.DefinitionAudience = DocumentHelper.DetermineAudienceType(defTypeNav.Value);
                }

                // Extract optional DefinitionText element value
                path = xPathManager.GetXPath(TerminologyXPath.DefinitionText);
                XPathNavigator defTextNav = xNav.SelectSingleNode(path);
                if (defTextNav != null)
                {
                    terminology.DefinitionText = defTextNav.Value;
                    terminology.Html = defTextNav.InnerXml;
                }

                // Extract optional TermTypeName element value
                path = xPathManager.GetXPath(TerminologyXPath.TypeName);
                XPathNavigator termTypeNav = xNav.SelectSingleNode(path);
                if (termTypeNav != null)
                {
                    terminology.TermTypeName = termTypeNav.Value;
                }
            }
            catch (Exception e)
            {
                throw new Exception("Extraction Error: Extracting " + path + " failed.  Document CDRID=" + _documentID.ToString(), e);
            }
        }

        /// <summary>
        /// Method to extract other names.
        /// </summary>
        /// <param name="xNav"></param>
        /// <param name="terminology"></param>
        private void ExtractOtherNames(XPathNavigator xNav, TerminologyDocument terminology)
        {
            string path = xPathManager.GetXPath(TerminologyXPath.OtherName);
            try {
                XPathNodeIterator otherNameIter = xNav.Select(path);
                while (otherNameIter.MoveNext())
                {
                    terminology.OtherNames.Add(
                        new TerminologyOtherName(
                        otherNameIter.Current.SelectSingleNode(xPathManager.GetXPath(TerminologyXPath.OtherTermName)).Value.Trim(),
                        otherNameIter.Current.SelectSingleNode(xPathManager.GetXPath(TerminologyXPath.OtherNameType)).Value.Trim()));
                }
            }
            catch (Exception e)
            {
                throw new Exception("Extraction Error: Extracting " + path + " failed.  Document CDRID=" + _documentID.ToString(), e);
            }
        }

        /// <summary>
        /// Method to extract semantic types.
        /// </summary>
        /// <param name="xNav"></param>
        /// <param name="terminology"></param>
        private void ExtractSemanticTypes(XPathNavigator xNav, TerminologyDocument terminology)
        {
            string path = xPathManager.GetXPath(TerminologyXPath.SemanticType);
            try {
                XPathNodeIterator semanticTypeIter = xNav.Select(path);
                while (semanticTypeIter.MoveNext())
                {
                    string semanticTypeName = semanticTypeIter.Current.Value;
                    string tempIDString = DocumentHelper.GetAttribute(semanticTypeIter.Current, xPathManager.GetXPath(TerminologyXPath.SemanticTypeRef));
                    int semanticTypeID = 0;
                    if (tempIDString.Trim().Length > 0)
                    {
                        if (!Int32.TryParse(CDRHelper.ExtractCDRID(tempIDString), out semanticTypeID))
                            throw new Exception("Extraction Error: " + path + "/@" +  xPathManager.GetXPath(TerminologyXPath.SemanticTypeRef) + " should be a valid CDRID. CurrentValue=" + tempIDString + ". Document CDRID= " + _documentID.ToString());
                    }
                    terminology.SemanticTypes.Add(new TermSemanticType(semanticTypeName, semanticTypeID));
                }

                // Determine term type from the semantic type
                if (terminology.SemanticTypes.Count > 0)
                {
                     if (terminology.SemanticTypes[0].Name.Trim().ToLower() == "cancer stage")
                    {
                        terminology.TermType = TerminologyType.Menu;
                    }
                    else if (terminology.SemanticTypes[0].Name.Trim().ToLower() == "drug/agent")
                    {
                        terminology.TermType = TerminologyType.Drug;
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Extraction Error: Extracting " + path + " failed.  Document CDRID=" + _documentID.ToString(), e);
            }
        }

        /// <summary>
        /// Extracts menu information if it exists in the document.
        /// </summary>
        /// <param name="xNav"></param>
        /// <param name="terminology"></param>
        private void ExtractMenus(XPathNavigator xNav, TerminologyDocument terminology)
        {
            string path = xPathManager.GetXPath(TerminologyXPath.MenuItem);
            try {
                XPathNodeIterator menuItemIter = xNav.Select(path);
                while (menuItemIter.MoveNext())
                {
                    string sortName = DocumentHelper.GetAttribute(menuItemIter.Current, xPathManager.GetXPath(TerminologyXPath.MenuSortOrder));

                    // Get optional display name element value
                    string displayName = string.Empty;
                    XPathNavigator displayNameNav = menuItemIter.Current.SelectSingleNode(xPathManager.GetXPath(TerminologyXPath.MenuDisplayName));
                    if (displayNameNav != null)
                    {
                        displayName = displayNameNav.Value;
                    }
             

                    TerminologyMenu termMenu = new TerminologyMenu(displayName, sortName);

                    // Get optional MenuParent
                    // CDRMenu can contain multiple parent IDs or none at all                     

                    XPathNodeIterator xnTmp = menuItemIter.Current.Select(xPathManager.GetXPath(TerminologyXPath.MenuParent));
                    while (xnTmp.MoveNext())
                    {
                        string tempIDString = DocumentHelper.GetAttribute(xnTmp.Current, xPathManager.GetXPath(TerminologyXPath.MenuRef));
                        if (tempIDString.Trim().Length > 0)
                        {
                            int menuParentID;
                            if (Int32.TryParse(CDRHelper.ExtractCDRID(tempIDString), out menuParentID))
                                termMenu.MenuParentIDList.Add(menuParentID);
                            else
                                throw new Exception("Extraction Error: " + xPathManager.GetXPath(TerminologyXPath.MenuParent) + "/@" + xPathManager.GetXPath(TerminologyXPath.MenuRef) +" should be a valid CDRID. CurrentValue=" + tempIDString + ". Document CDRID= " + _documentID.ToString());
                        }
                    }


                    terminology.Menus.Add(termMenu);

                }
            }
            catch (Exception e)
            {
                throw new Exception("Extraction Error: Extracting " + path + " failed.  Document CDRID=" + _documentID.ToString(), e);
            }
        }

        #endregion Private Methods

        #region Public Methods

        /// <summary>
        /// Modifies the document XML, so that subsequent processing is based on
        /// ideal input.
        /// </summary>
        /// <param name="xmlDoc"></param>
        public void PrepareXml(XmlDocument xmlDoc)
        {
            // TODO: Add code to "prepare" xml (fix problems with data) 
        }

        /// <summary>
        /// Method to extract a terminology document from the input XML document.
        /// </summary>
        /// <param name="xmlDoc"></param>
        /// <param name="terminology"></param>
        public void Extract(XmlDocument xmlDoc, TerminologyDocument terminology, DocumentXPathManager xPath)
        {
           try{
                XPathNavigator xNav = xmlDoc.CreateNavigator();

                // Extract the CDR ID...
                xNav.MoveToFirstChild();
                if (CDRHelper.ExtractCDRID(xNav, xPath.GetXPath(CommonXPath.CDRID), out _documentID))
                {
                    terminology.DocumentID = _documentID;
                }
                else
                {
                    throw new Exception("Extraction Error: Failed to extract document (CDR) ID in terminology document!");
                }

                DocumentHelper.CopyXml(xmlDoc, terminology);

               // Set document type to be terminology
                terminology.DocumentType = DocumentType.Terminology;

                // Get terminology xpath
                xPathManager = xPath;

                ExtractMetaData(xNav, terminology);
                ExtractOtherNames(xNav, terminology);
                ExtractSemanticTypes(xNav, terminology);
                ExtractMenus(xNav, terminology);

                DocumentHelper.ExtractDates(xNav, terminology, xPathManager.GetXPath(CommonXPath.LastModifiedDate), xPathManager.GetXPath(CommonXPath.FirstPublishedDate));
            }
            catch (Exception e)
            {
                throw new Exception("Extraction Error: Failed to extract terminology document", e);
            }
        }

        #endregion Public Methods
    }
}
