using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;

using GateKeeper.Common;
using GateKeeper.Common.XPathKeys;
using GateKeeper.DataAccess.CancerGov;
using GateKeeper.DataAccess.GateKeeper;
using GateKeeper.DocumentObjects;
using GateKeeper.DocumentObjects.Dictionary;
using GateKeeper.DocumentObjects.Terminology;

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

        /// <summary>
        /// Extracts drug dictionary information if it exists in the document.
        /// </summary>
        /// <param name="xNav"></param>
        /// <param name="terminology"></param>
        private void ExtractDrugDictionaryItems(XmlDocument xmlDoc, TerminologyDocument terminology)
        {
            if (terminology.TermType == TerminologyType.Drug)
            {
                // Stinky code.  Add the URL of this drug's matching Drug Info Summary (if any)
                // to the XML structure.  This information should actually come from the CDR
                // and will in some pending release.
                AddRelatedDrugInfoSummary(terminology);

                try
                {
                    // Use XML deserialization to extract the meta data.
                    XmlSerializer serializer = new XmlSerializer(typeof(TerminologyMetadata));

                    // Use XML Serialization to parse the dictionary entry and extract the term details.
                    // Note that this approach is new as of the Feline release (Summer 2015) and is significantly
                    // different from the legacy extract mechanisms used elsewhere in this codebase.
                    TerminologyMetadata extractData;
                    using (TextReader reader = new StringReader(xmlDoc.OuterXml))
                    {
                        extractData = (TerminologyMetadata)serializer.Deserialize(reader);
                    }

                    //Add rows to the Dictionary and DictionaryTermAlias tables only when there is a Definition
                    if (extractData.HasDefinition)
                    {
                        GeneralDictionaryEntry dictionaryEntry = GetDictionary(extractData);

                        //already initialized to empty in the TerminologyDefinition object
                        terminology.Dictionary.Add(dictionaryEntry);

                        //Populating the TermAlias object using the pre-extracted data
                        //Since we did not want to do ReadXML and implement ISerializable in 
                        //the TerminologyMetadata class at this time
                        foreach (TerminologyOtherName otherName in terminology.OtherNames)
                        {
                            TermAlias alias = new TermAlias();
                            alias.AlternateName = otherName.Name;
                            alias.NameType = otherName.Type;
                            //For terminology documents the Language is set to English by default
                            alias.Language = Language.English.ToString();
                            terminology.TermAliasList.Add(alias);
                        }

                    }
                    
                }
                catch (Exception e)
                {
                    throw new Exception("Extraction Error: Extracting drug dictionary items failed.  Document CDRID=" + _documentID.ToString(), e);
                }
            }


        }

        /// <summary>
        /// Creates a collection of DictionaryEntry objects from the information contained in TerminologyMetadata object.
        /// </summary>
        /// <param name="metadata">A TerminologyMetadata created from a Terminology XML document.</param>
        /// <returns></returns>
        private GeneralDictionaryEntry GetDictionary(TerminologyMetadata metadata)
        {
            GeneralDictionaryEntry entry = new GeneralDictionaryEntry();
            entry.TermID = metadata.ID;
            entry.TermName = metadata.TermName;
            entry.Dictionary = metadata.Dictionary;
            entry.Language = metadata.Language;
            entry.Audience = metadata.Definition.Audience;
            entry.ApiVersion = "v1";

            return entry;
        }

        /// <summary>
        /// Stinky code.  Modifies the XML document contained in drugTerm to include a
        /// RelatedDrugInfoSummary element containing the path to the drug information
        /// summary matching this term.  If no matching drug information summary is found,
        /// the element is not added.
        /// </summary>
        /// <param name="drugTerm">Terminology document containing a term with a drug definition.</param>
        private void AddRelatedDrugInfoSummary(TerminologyDocument drugTerm)
        {
            try
            {
                TerminologyQuery query = new TerminologyQuery();
                string disPath = query.GetRelatedDrugInfoSummaryURL(drugTerm.DocumentID);

                if (!String.IsNullOrEmpty(disPath))
                {
                    XPathNavigator nav = drugTerm.Xml.CreateNavigator();
                    nav.MoveToFirstChild();
                    
                    nav.AppendChild(String.Format("<RelatedDrugInfoSummary>{0}</RelatedDrugInfoSummary>", disPath));
                }
            }
            catch (Exception ex)
            {
                // Don't let errors in the look up stop processing.
                drugTerm.WarningWriter("AddRelatedDrugInfoSummary(). Unable to lookup matching Drug Info Summary.\n" + ex.Message);
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
                ExtractDrugDictionaryItems(xmlDoc, terminology);

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
