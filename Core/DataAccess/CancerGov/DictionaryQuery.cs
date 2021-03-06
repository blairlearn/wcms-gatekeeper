﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.Common;
using System.Linq;
using System.Text;

using Microsoft.Practices.EnterpriseLibrary.Data;
using Microsoft.Practices.EnterpriseLibrary.Data.Sql;

using NCI.Data;

using GateKeeper.DocumentObjects;
using GateKeeper.DocumentObjects.Dictionary;
using GateKeeper.DataAccess.StoreProcedures;

namespace GateKeeper.DataAccess.CancerGov
{
    /// <summary>
    /// This is a helper class, used by the GlossaryTermQuery and TerminologyQuery
    /// classes to implement the shared functionality of working with the Dictionary tables.
    /// Note that this class is intended to be used as a helper object and is not intended
    /// as a subclass of DocumentQuery.  The public method names match as matter of convenience
    /// and clarity, however the method signatures are signficantly different.
    /// </summary>
    class DictionaryQuery
    {
        const string SP_SAVE_DICTIONARY_TERM = "usp_SaveDictionaryTerm";
        const string SP_PUSH_TO_PREVIEW = "usp_PushDictionaryTermToPreview";
        const string SP_PUSH_TO_LIVE = "usp_PushDictionaryTermToLive";
        const string SP_DELETE_DICTIONARY_TERM = "usp_ClearDictionaryData";

        /// <summary>
        /// Save Dictionary entries to the CDRStagingGK database.
        /// </summary>
        /// <param name="termId">The dictionary term's ID. The term ID reflects the original CDR document
        /// and is not intended to be a unique key.</param>
        /// <param name="entries">A collection of GeneralDictionaryEntry containing the dictionary entries
        /// to be saved in the database.</param>
        /// <param name="transaction">The database transaction the save operation is to be part of.</param>
        public void SaveDocument(int termId, IEnumerable<GeneralDictionaryEntry> entries, IEnumerable<TermAlias> aliases,  DbTransaction transaction)
        {
            if (transaction == null)
                throw new ArgumentException("Argument 'transaction' must not be null.");

            // It's OK for entries to be empty. It is not OK for entries to be null.
            if (entries == null)
                throw new ArgumentException("Argument 'entries' must not be null.");


            /// Verify the DbTransaction object we've been passed (generated by Enterprise Library classes)
            /// is actually a SqlTransaction.  This should always be true since we only run against MSSQL,
            /// but best to check the assumption.
            SqlTransaction sqlTransaction = transaction as SqlTransaction;
            if (sqlTransaction == null)
                throw new DatabaseAssumptionException("Unable to treat dbTransaction as SqlTransaction.");

            // Create a table to pass to the stored proc. This allows us make a single call instead
            // of callling from a loop.
            DataTable dictionary = new DataTable("dictionary");
            dictionary.Columns.Add("TermID", typeof(int));
            dictionary.Columns.Add("TermName", typeof(String));
            dictionary.Columns.Add("Dictionary", typeof(String));
            dictionary.Columns.Add("Language", typeof(String));
            dictionary.Columns.Add("Audience", typeof(String));
            dictionary.Columns.Add("ApiVers", typeof(String));
            dictionary.Columns.Add("Object", typeof(String));

            // Populate the table.
            foreach (GeneralDictionaryEntry entry in entries)
            {
                dictionary.Rows.Add(entry.TermID, entry.TermName, entry.Dictionary, entry.Language, entry.Audience, entry.ApiVersion, entry.Object);
            }

            // Create table parameter containing all aliases
            DataTable aliasList = new DataTable("aliases");
            aliasList.Columns.Add("TermID", typeof(int));
            aliasList.Columns.Add("Name", typeof(String));
            aliasList.Columns.Add("NameType", typeof(String));
            aliasList.Columns.Add("Language", typeof(String));

            // Populate the table.
            foreach (TermAlias item in aliases)
            {
                aliasList.Rows.Add(termId, item.AlternateName, item.NameType, item.Language);
            }


            SqlParameter[] parameters = new SqlParameter[]{
                new SqlParameter("@TermID", SqlDbType.Int){Value = termId},
                new SqlParameter("@Entries", SqlDbType.Structured){Value = dictionary},
                new SqlParameter("@Aliases", SqlDbType.Structured){Value = aliasList}
            };

            SqlHelper.ExecuteNonQuery(sqlTransaction, CommandType.StoredProcedure, SP_SAVE_DICTIONARY_TERM, parameters);
        }

        /// <summary>
        /// Delete the dictioanry entries corresponding to a given CDR document.
        /// </summary>
        /// <param name="termId">The dictionary term's ID. The term ID reflects the original CDR document
        /// and is not intended to be a unique key.</param>
        /// <param name="transaction">The database transaction the save operation is to be part of.</param>
        public void DeleteDocument(int termId, DbTransaction transaction)
        {
            if (transaction == null)
                throw new ArgumentException("Argument 'transaction' must not be null.");

            // Verify the DbTransaction object we've been passed (generated by Enterprise Library classes)
            // is actually a SqlTransaction.  This should always be true since we only run against MSSQL,
            // but best to check the assumption.
            SqlTransaction sqlTransaction = transaction as SqlTransaction;
            if (sqlTransaction == null)
                throw new DatabaseAssumptionException("Unable to treat dbTransaction as SqlTransaction.");

            SqlParameter[] parameters = new SqlParameter[]{
                new SqlParameter("@TermID", SqlDbType.Int){Value = termId}
            };

            int rc = SqlHelper.ExecuteNonQuery(sqlTransaction, CommandType.StoredProcedure, SP_DELETE_DICTIONARY_TERM, parameters);
        }

        /// <summary>
        /// Copy dictionary entries from the Staging database to Preview.
        /// </summary>
        /// <param name="termId">The dictionary term's ID. The term ID reflects the original CDR document
        /// and is not intended to be a unique key.</param>
        /// <param name="transaction">The database transaction the save operation is to be part of.</param>
        public void PushDocumentToPreview(int termId, DbTransaction transaction)
        {
            if (transaction == null)
                throw new ArgumentException("Argument 'transaction' must not be null.");

            /// Verify the DbTransaction object we've been passed (generated by Enterprise Library classes)
            /// is actually a SqlTransaction.  This should always be true since we only run against MSSQL,
            /// but best to check the assumption.
            SqlTransaction sqlTransaction = transaction as SqlTransaction;
            if (sqlTransaction == null)
                throw new DatabaseAssumptionException("Unable to treat dbTransaction as SqlTransaction.");

            SqlParameter[] parameters = new SqlParameter[]{
                new SqlParameter("@TermID", SqlDbType.Int){Value = termId}
            };

            int rc = SqlHelper.ExecuteNonQuery(sqlTransaction, CommandType.StoredProcedure, SP_PUSH_TO_PREVIEW, parameters);
        }

        /// <summary>
        /// Copy dictionary entries from the Preview database to Live.
        /// </summary>
        /// <param name="termId">The dictionary term's ID. The term ID reflects the original CDR document
        /// and is not intended to be a unique key.</param>
        /// <param name="transaction">The database transaction the save operation is to be part of.</param>
        public void PushDocumentToLive(int termId, DbTransaction transaction)
        {
            if (transaction == null)
                throw new ArgumentException("Argument 'transaction' must not be null.");

            /// Verify the DbTransaction object we've been passed (generated by Enterprise Library classes)
            /// is actually a SqlTransaction.  This should always be true since we only run against MSSQL,
            /// but best to check the assumption.
            SqlTransaction sqlTransaction = transaction as SqlTransaction;
            if (sqlTransaction == null)
                throw new DatabaseAssumptionException("Unable to treat dbTransaction as SqlTransaction.");

            SqlParameter[] parameters = new SqlParameter[]{
                new SqlParameter("@TermID", SqlDbType.Int){Value = termId}
            };

            int rc = SqlHelper.ExecuteNonQuery(sqlTransaction, CommandType.StoredProcedure, SP_PUSH_TO_LIVE, parameters);
        }
    }
}
