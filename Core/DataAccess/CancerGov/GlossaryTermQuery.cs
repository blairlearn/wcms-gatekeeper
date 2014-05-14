using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Collections;
using System.Transactions;

using Microsoft.Practices.EnterpriseLibrary.Data;
using Microsoft.Practices.EnterpriseLibrary.Data.Sql;

using GateKeeper.Common;
using GateKeeper.DocumentObjects;
using GateKeeper.DocumentObjects.GlossaryTerm;
using GateKeeper.DocumentObjects.Media;
using GateKeeper.DataAccess.StoreProcedures;
using GateKeeper.DataAccess;

namespace GateKeeper.DataAccess.CancerGov
{
    public class GlossaryTermQuery : DocumentQuery
    {

        #region Constructors

        /// <summary>
        /// Class constructor.
        /// </summary>
        public GlossaryTermQuery()
        { }

        #endregion Constructors

        #region Query for Store Procedure Calls

        /// <summary>
        /// Save glossary term document into CDR staging database
        /// </summary>
        /// <param name="glossaryDoc">
        /// Glossary Term document object
        /// </param>
        public override bool SaveDocument(Document glossaryDoc, String userID)
        {
            bool bSuccess = true;
            Database db = this.StagingDBWrapper.SetDatabase();
            DbConnection conn = this.StagingDBWrapper.EnsureConnection();
            DbTransaction transaction = conn.BeginTransaction();

            try
            {
                GlossaryTermDocument GTDocument = (GlossaryTermDocument)glossaryDoc;

                // SP: Clear extracted data
                ClearExtractedData(GTDocument.DocumentID, db, transaction);

                // SP: Save Glossary Term
                SaveGlossaryTerm(GTDocument, db, transaction, userID);

                //SP: Save Glossary Term Definition/Dictionary
                SaveDefinitions(GTDocument, db, transaction, userID);

                //SP: Save Glossary Term document data
                SaveDBDocument(GTDocument, db, transaction);

                transaction.Commit();
            }
            catch (Exception e)
            {
                bSuccess = false;
                transaction.Rollback();
                throw new Exception("Database Error: Saving glossary term document failed. Document CDRID=" + glossaryDoc.DocumentID.ToString(), e);
            }
            finally
            {
                transaction.Dispose();
                conn.Close();
                conn.Dispose();
            }

            return bSuccess;
        }

        /// <summary>
        /// Retrieve a brief summary of a definition.
        /// </summary>
        /// <param name="cdrID">The ID of the GlossaryTerm to retrieve</param>
        /// <returns>A GlossaryTermSimple object containing the requested GlossaryTerm document.</returns>
        public GlossaryTermSimple GetGlossaryTerm(int cdrID)
        {
            Database db = StagingDBWrapper.SetDatabase();
            GlossaryTermSimple term = null;

            // Get document data
            IDataReader reader = null;
            try
            {
                string spGetData = SPGlossaryTerm.SP_GET_GLOSSARY_TERM;
                using (DbCommand getCommand = db.GetStoredProcCommand(spGetData))
                {
                    getCommand.CommandType = CommandType.StoredProcedure;
                    db.AddInParameter(getCommand, "@GlossaryTermID", DbType.Int32, cdrID);

                    DataSet ds = db.ExecuteDataSet(getCommand);

                    reader = db.ExecuteReader(getCommand);
                    if (reader.Read())
                    {
                        // Get the Term names and pronunciation
                        String name = reader["TermName"].ToString();
                        String spanishName = reader["SpanishTermName"].ToString();
                        String pronunciation = reader["TermPronunciation"].ToString();

                        term = new GlossaryTermSimple(cdrID, name, spanishName, pronunciation);

                        // Get as many definitions as exist.
                        reader.NextResult();
                        while (reader.Read())
                        {
                            // Retreive fields
                            String tmpAudience = reader["Audience"].ToString();
                            String tmpLanguage = reader["Language"].ToString();
                            String definitionText = reader["DefinitionText"].ToString();
                            String definitionHtml = reader["DefinitionHTML"].ToString();
                            String mediaHtml = reader["MediaHTML"].ToString();
                            String audioHtml = reader["AudioMediaHTML"].ToString();
                            String relatedLinksHtml = reader["RelatedInformationHtml"].ToString();

                            // Convert the two tempoaries into strong types.
                            Language language = ConvertEnum<Language>.Convert(tmpLanguage);

                            // Audience is stored as either "Patient" or "Health professional".  The latter can't be converted
                            // as an enum, so we end up with a string compare.
                            AudienceType audience;
                            if (String.Compare(tmpAudience, "Patient", true) == 0)
                                audience = AudienceType.Patient;
                            else if (String.Compare(tmpAudience, "Health professional", true) == 0)
                                audience = AudienceType.HealthProfessional;
                            else
                                throw new Exception("Don't know how to convert audience type: '" + tmpAudience + "'");

                            GlossaryTermSimpleDefinition definition =
                                new GlossaryTermSimpleDefinition(audience, language, definitionText, definitionHtml, mediaHtml, audioHtml, relatedLinksHtml);
                            term.DefinitionList.Add(definition);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error: Retrieveing GlossaryTerm data from CDR staging database failed. Document CDRID=" + cdrID, ex);
            }
            finally
            {
                reader.Close();
                reader.Dispose();
            }

            return term;
        }

        /// <summary>
        /// Delete glossary term document in database
        /// </summary>
        /// <param name="documentID"></param>
        /// <param name="dbName">database name</param>
        /// <param name="userID"></param>
        public override void DeleteDocument(Document glossaryDoc, ContentDatabase databaseName, string userID)
        {
            try
            {
                Database db;
                DbConnection conn;
                switch (databaseName)
                {
                    case ContentDatabase.Staging:
                        db = this.StagingDBWrapper.SetDatabase();
                        conn = this.StagingDBWrapper.EnsureConnection();
                        break;
                    case ContentDatabase.Preview:
                        db = this.PreviewDBWrapper.SetDatabase();
                        conn = this.PreviewDBWrapper.EnsureConnection();
                        break;
                    case ContentDatabase.Live:
                        db = this.LiveDBWrapper.SetDatabase();
                        conn = this.LiveDBWrapper.EnsureConnection();
                        break;
                    default:
                        throw new Exception("Database Error: Invalid database name. DatabaseName = " + databaseName.ToString());
                }
                DbTransaction transaction = conn.BeginTransaction();

                try
                {
                    // SP: Clear extracted data
                    ClearExtractedData(glossaryDoc.DocumentID, db, transaction);

                    // SP: Clear document
                    ClearDocument(glossaryDoc.DocumentID, db, transaction, databaseName.ToString());
                    transaction.Commit();
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    throw new Exception("Database Error: Deleting glossary term document data in " + databaseName.ToString() + " database failed. Document CDRID=" + glossaryDoc.DocumentID.ToString(), e);
                }
                finally
                {
                    transaction.Dispose();
                    conn.Close();
                    conn.Dispose();
                }
            }
            catch (Exception e)
            {
                throw new Exception("Database Error: Deleting glossary term document data in " + databaseName.ToString() + " database failed. Document CDRID=" + glossaryDoc.DocumentID.ToString(), e);
            }
        }

        /// <summary>
        /// Push glossary term document into preview database
        /// </summary>
        /// <param name="documentID">
        /// <param name="userID">
        /// </param>
        public override void PushDocumentToPreview(Document glossaryDoc, string userID)
        {
            Database db = this.StagingDBWrapper.SetDatabase();
            DbConnection conn = this.StagingDBWrapper.EnsureConnection();
            DbTransaction transaction = conn.BeginTransaction();

            try
            {
                try
                {
                    // SP: Call push glossary term document
                    string spPushData = SPGlossaryTerm.SP_PUSH_GT_DOCUMENT_DATA;
                    using (DbCommand pushCommand = db.GetStoredProcCommand(spPushData))
                    {
                        pushCommand.CommandType = CommandType.StoredProcedure;
                        db.AddInParameter(pushCommand, "@DocumentID", DbType.Int32, glossaryDoc.DocumentID);
                        db.AddInParameter(pushCommand, "@UpdateUserID", DbType.String, userID);
                        db.ExecuteNonQuery(pushCommand, transaction);
                        transaction.Commit();
                    }
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    throw new Exception("Database Error: Store procedure dbo.usp_PushExtractedGlossaryData failed.", e);
                }
                finally
                {
                    transaction.Dispose();
                }

                // SP: Call push document 
                PushDocument(glossaryDoc.DocumentID, db, ContentDatabase.Preview.ToString());

            }
            catch (Exception e)
            {
                throw new Exception("Database Error: Pushing glossary term document data to preview database failed. Document CDRID=" + glossaryDoc.DocumentID.ToString(), e);
            }
            finally
            {
                conn.Close();
                conn.Dispose();
            }
        }

        /// <summary>
        /// Push glossary term document into live database
        /// </summary>
        /// <param name="documentID">
        /// <param name="userID">
        /// </param>
        public override void PushDocumentToLive(Document glossaryDoc, string userID)
        {
            Database db = this.PreviewDBWrapper.SetDatabase();
            DbConnection conn = this.PreviewDBWrapper.EnsureConnection();
            DbTransaction transaction = conn.BeginTransaction();

            try
            {
                // Rollback the change only if push glossary term sp failed.
                try
                {
                    // SP: Call push glossary term document
                    string spPushData = SPGlossaryTerm.SP_PUSH_GT_DOCUMENT_DATA;
                    using (DbCommand pushCommand = db.GetStoredProcCommand(spPushData))
                    {
                        pushCommand.CommandType = CommandType.StoredProcedure;
                        db.AddInParameter(pushCommand, "@DocumentID", DbType.Int32, glossaryDoc.DocumentID);
                        db.AddInParameter(pushCommand, "@UpdateUserID", DbType.String, userID);
                        db.ExecuteNonQuery(pushCommand, transaction);
                        transaction.Commit();
                    }
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    throw new Exception("Database Error: Store procedure dbo.usp_PushExtractedGlossaryData failed", e);
                }

                // SP: Call Push document
                PushDocument(glossaryDoc.DocumentID, db, ContentDatabase.Live.ToString());
            }
            catch (Exception e)
            {
                throw new Exception("Database Error: Pushing glossary term document data to live database failed. Document CDRID=" + glossaryDoc.DocumentID.ToString(), e);
            }
            finally
            {
                transaction.Dispose();
                conn.Close();
                conn.Dispose();
            }
        }

        #endregion

        #region Private methods
        /// <summary>
        /// Call store procedure to clear existing glossary term data in database
        /// </summary>
        /// <param name="documentID"></param
        /// <param name="db"></param>
        /// <param name="transaction"></param>
        /// <param name="userID"></param>
        /// <returns></returns>
        private void ClearExtractedData(int documentID, Database db, DbTransaction transaction)
        {
            try
            {
                string spClearExtractedData = SPGlossaryTerm.SP_CLEAR_GLOSSARY_DATA;
                using (DbCommand clearCommand = db.GetStoredProcCommand(spClearExtractedData))
                {
                    clearCommand.CommandType = CommandType.StoredProcedure;
                    db.AddInParameter(clearCommand, "@DocumentID", DbType.Int32, documentID);
                    db.ExecuteNonQuery(clearCommand, transaction);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Database Error: Clearing glossary term data failed. Document CDRID=" + documentID.ToString(), e);
            }
        }

        /// <summary>
        /// Call store procedure to saving glossary term data
        /// </summary>
        /// <param name="GTDocument"></param
        /// <param name="db"></param>
        /// <param name="transaction"></param>
        /// <param name="userID"></param>
        /// <returns></returns>
        private void SaveGlossaryTerm(GlossaryTermDocument GTDocument, Database db, DbTransaction transaction, string userID)
        {
            try
            {
                string spSaveGlossaryTerm = SPGlossaryTerm.SP_SAVE_GLOSSARY_TERM;
                using (DbCommand saveGTCommand = db.GetStoredProcCommand(spSaveGlossaryTerm))
                {
                    saveGTCommand.CommandType = CommandType.StoredProcedure;
                    db.AddInParameter(saveGTCommand, "@GlossaryTermID", DbType.Int32, GTDocument.DocumentID);
                    db.AddInParameter(saveGTCommand, "@UpdateUserID", DbType.String, userID);
                    bool spanishTerm = false;
                    foreach (Language language in GTDocument.GlossaryTermTranslationMap.Keys)
                    {
                        GlossaryTermTranslation trans = GTDocument.GlossaryTermTranslationMap[language];
                        if (language.Equals(Language.English))
                        {
                            if (trans.TermName.Trim().Length > 0)
                                db.AddInParameter(saveGTCommand, "@TermName", DbType.String, trans.TermName.Trim());
                            else
                                db.AddInParameter(saveGTCommand, "@TermName", DbType.String, null);

                            if (trans.Pronounciation.Trim().Length > 0)
                                db.AddInParameter(saveGTCommand, "@TermPronunciation", DbType.String, trans.Pronounciation.Trim());
                            else
                                db.AddInParameter(saveGTCommand, "@TermPronunciation", DbType.String, null);
                        }
                        else if (language.Equals(Language.Spanish))
                        {
                            spanishTerm = true;
                            db.AddInParameter(saveGTCommand, "@SpanishTermName", DbType.String, trans.TermName.Trim());
                        }
                    }
                    if (!spanishTerm)
                        db.AddInParameter(saveGTCommand, "@SpanishTermName", DbType.String, null);
                    db.ExecuteNonQuery(saveGTCommand, transaction);
                }

            }
            catch (Exception e)
            {
                throw new Exception("Database Error: Saving data to GlossaryTerm table failed. Document CDRID=" + GTDocument.DocumentID.ToString(), e);
            }
        }

        /// <summary>
        /// Call store procedure to save glossary term definition
        /// </summary>
        /// <param name="GTDocument"></param
        /// <param name="db"></param>
        /// <param name="transaction"></param>
        /// <param name="userID"></param>
        /// <returns></returns>
        private void SaveDefinitions(GlossaryTermDocument GTDocument, Database db, DbTransaction transaction, string userID)
        {
            try
            {
                //SP: Save Glossary Term Definition: English/Spanish
                //SP: Save Glossary Term Definition Audience: Patient, Health professional
                //SP: Save Glossary Term Definition Dictionary: Cancer.Gov
                foreach (Language language in GTDocument.GlossaryTermTranslationMap.Keys)
                {
                    GlossaryTermTranslation trans = GTDocument.GlossaryTermTranslationMap[language];
                    string relatedInformationHtml = String.Empty;

                    #region GlossaryTerm Definition
                    foreach (GlossaryTermDefinition gtDef in trans.DefinitionList)
                    {
                        // Each definition has a discrete audience.
                        AudienceType currAudience = gtDef.AudienceTypeList[0];

                        // If a pronunciation is associated with this translation, find the markup to save.
                        string audioMediaHTML = trans.GetAudioMarkup();

                        // If an image is associated with this definition, find the values to store in the DB.
                        // Otherwise, store non-NULL "blank" values.
                        string imageMediaHTML = trans.GetImageMarkup(currAudience);
                        List<string> imageCaptions = trans.GetImageCaptionColl(currAudience);
                        List<int> imageMediaIDs = trans.GetImageIDColl(currAudience);


                        //Save Glossary Term Definition
                        String spGlossaryTermDefinition = SPGlossaryTerm.SP_SAVE_GT_DEFINITION;
                        int definitionID = 0;
                        using (DbCommand spDefCommand = db.GetStoredProcCommand(spGlossaryTermDefinition))
                        {
                            spDefCommand.CommandType = CommandType.StoredProcedure;
                            db.AddInParameter(spDefCommand, "@GlossaryTermID", DbType.Int32, GTDocument.DocumentID);
                            db.AddInParameter(spDefCommand, "@Language", DbType.String, language.ToString().Trim());
                            db.AddInParameter(spDefCommand, "@UpdateUserID", DbType.String, userID);
                            db.AddInParameter(spDefCommand, "@MediaHTML", DbType.String, imageMediaHTML);
                            db.AddInParameter(spDefCommand, "@AudioMediaHTML", DbType.String, audioMediaHTML.Trim());
                            db.AddInParameter(spDefCommand, "@RelatedInformationHTML", DbType.String, gtDef.RelatedInformationHTML.Trim());
                            db.AddInParameter(spDefCommand, "@DefinitionText", DbType.String, gtDef.Text.Trim());

                            // THIS IS WRONG!!!!
                            // The business rules allow for multiple images to be referenced from a single definition,
                            // however the current database schema and front-end code don't support multiple.
                            // At present, the MediaCaption and MediaId are only used by CancerTypeHomePage on the mobile site.
                            // This is recorded in OCEPROJECT-756
                            db.AddInParameter(spDefCommand, "@MediaCaption", DbType.String,
                                (imageCaptions.Count != 0) ? imageCaptions[0] : string.Empty);
                            db.AddInParameter(spDefCommand, "@MediaID", DbType.Int32,
                                (imageMediaIDs.Count != 0) ? imageMediaIDs[0] : 0);

                            // Replace summaryref with prettyURL
                            string html = gtDef.Html.Trim();
                            //TODO: Fix SummaryRef tags in Glossary Terms.
                            if (html.Contains("<SummaryRef"))
                            {
                                BuildSummaryRefLink(ref html, 1);
                            }
                            db.AddInParameter(spDefCommand, "@DefinitionHTML", DbType.String, html);
                            db.AddOutParameter(spDefCommand, "@GlossaryTermDefinitionID", DbType.Int32, 1);
                            db.ExecuteNonQuery(spDefCommand, transaction);
                            definitionID = (int)db.GetParameterValue(spDefCommand, "@GlossaryTermDefinitionID");
                        }

                        if (definitionID > 0)
                        {
                            // Save Glossary Term Definition Audience
                            foreach (AudienceType at in gtDef.AudienceTypeList)
                            {
                                String spAudience = SPGlossaryTerm.SP_SAVE_GT_DEFINITION_AUDI;
                                using (DbCommand spAudiCommand = db.GetStoredProcCommand(spAudience))
                                {
                                    spAudiCommand.CommandType = CommandType.StoredProcedure;
                                    db.AddInParameter(spAudiCommand, "@GlossaryTermDefinitionID", DbType.Int32, definitionID);
                                    db.AddInParameter(spAudiCommand, "@Audience", DbType.String, DocumentHelper.GetAudienceDBString(at).Trim());
                                    db.AddInParameter(spAudiCommand, "@UpdateUserID", DbType.String, userID);
                                    db.ExecuteNonQuery(spAudiCommand, transaction);
                                }
                            }

                            // Save Glossary Term Definition Dictionary Name
                            foreach (String dic in gtDef.DictionaryNameList)
                            {
                                String spDictionaryName = SPGlossaryTerm.SP_SAVE_GT_DEFINITION_DIC;
                                using (DbCommand spDicCommand = db.GetStoredProcCommand(spDictionaryName))
                                {
                                    spDicCommand.CommandType = CommandType.StoredProcedure;
                                    db.AddInParameter(spDicCommand, "@GlossaryTermDefinitionID", DbType.Int32, definitionID);
                                    db.AddInParameter(spDicCommand, "@Dictionary", DbType.String, dic.Trim());
                                    db.AddInParameter(spDicCommand, "@UpdateUserID", DbType.String, userID);
                                    db.ExecuteNonQuery(spDicCommand, transaction);
                                }
                            }
                        }
                    } // End definition list loop 
                    #endregion
                    
                } // End language map loop

            }
            catch (Exception e)
            {
                throw new Exception("Database Error: Saving glossary term definition failed. Document CDRID=" + GTDocument.DocumentID.ToString(), e);
            }
        }

        #endregion

    }
}
