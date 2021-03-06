using System;
using System.Collections;
using System.Text;

namespace GateKeeper.DataAccess
{
    public class DbBaseQuery : IDisposable
    {
        protected DataAccessManager dbManager = new DataAccessManager();

        #region Public Properties
        /// <summary>
        /// Return CDRStaging Database Wrapper
        /// </summary>
        protected DbWrapper StagingDBWrapper
        {
            get
            {
                return dbManager.GetDatabaseWrapper(ContentDatabase.Staging.ToString());
            }
        }

        /// <summary>
        /// Return GateKeeper Database Wrapper
        /// </summary>
        protected DbWrapper GateKeeperDBWrapper
        {
            get
            {
                return dbManager.GetDatabaseWrapper(ContentDatabase.GateKeeper.ToString());
            }
        }

        /// <summary>
        /// Return Preview Database Wrapper
        /// </summary>
        protected DbWrapper PreviewDBWrapper
        {
            get
            {
                return dbManager.GetDatabaseWrapper(ContentDatabase.Preview.ToString());
            }
        }

        /// <summary>
        /// Return Live Database Wrapper
        /// </summary>
        protected DbWrapper LiveDBWrapper
        {
            get
            {
                return dbManager.GetDatabaseWrapper(ContentDatabase.Live.ToString());
            }
        }
        #endregion

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Free managed resources only.
                if (dbManager != null)
                {
                    dbManager.Dispose();
                    dbManager = null;
                }
            }
        }

        #endregion
    }
}
