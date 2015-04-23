namespace HotAssembly.PersistenceProviderRepository
{
    public class ExecutionScope : global::System.IDisposable
    {
        internal static global::System.Collections.Generic.List<int> RetryableErrors = new global::System.Collections.Generic.List<int>
        {
			53, 601, 615, 913, 921, 922, 923, 924, 926, 927, 941, 955, 956, 983, 976, 978, 979, 982, 983, 1204, 1205, 1214, 1222, 1428, 35201
		};
        public global::System.Data.SqlClient.SqlTransaction Transaction
        {
            get;
            private set;
        }
        private readonly global::System.Data.SqlClient.SqlConnection _connection;
        public ExecutionScope()
        {
            this._connection = new global::System.Data.SqlClient.SqlConnection(ConnectionString);
            this._connection.Open();
            this.Transaction = _connection.BeginTransaction();
        }
        public void Commit()
        {
            if (this.Transaction != null)
                this.Transaction.Commit();
        }
        public void Rollback()
        {
            if (this.Transaction != null)
                this.Transaction.Rollback();
        }
        public void Dispose()
        {
            if (this.Transaction != null)
            {
                this.Transaction.Dispose();
            }
            if (this._connection != null && this._connection.State != System.Data.ConnectionState.Closed)
            {
                this._connection.Close();
                this._connection.Dispose();
            }
        }
        private static global::System.String _connectionString;
        public static global::System.String ConnectionString
        {
            get
            {
                global::System.Threading.LazyInitializer.EnsureInitialized(
                    ref _connectionString,
                    () => global::System.Configuration.ConfigurationManager.ConnectionStrings["HotAssembly"].ConnectionString
                );
                return _connectionString;
            }
            set
            {
                _connectionString = value;
            }
        }
    }
}
namespace HotAssembly.PersistenceProviderRepository.Executables.common
{
    public class GetBundleFileStreamData
    {
        public class ParametersCollection
        {
            public global::System.String BundleId
            {
                get;
                private set;
            }
            public global::System.String BundlePath
            {
                get;
                private set;
            }
            public global::System.Byte[] TransactionContext
            {
                get;
                private set;
            }
            public ParametersCollection(global::System.String bundleId, global::System.String bundlePath, global::System.Byte[] transactionContext)
            {
                this.BundleId = bundleId;
                this.BundlePath = bundlePath;
                this.TransactionContext = transactionContext;
            }
        }
        public ParametersCollection Parameters
        {
            get;
            private set;
        }
        public global::System.Int32 ReturnValue
        {
            get;
            private set;
        }
        public static async global::System.Threading.Tasks.Task<GetBundleFileStreamData> ExecuteAsync(global::System.String bundleId, global::System.String bundlePath, global::System.Byte[] transactionContext, global::HotAssembly.PersistenceProviderRepository.ExecutionScope executionScope = null, global::System.Int32 commandTimeout = 30)
        {
            var retValue = new GetBundleFileStreamData();
            {
                var retryCycle = 0;
                while (true)
                {
                    global::System.Data.SqlClient.SqlConnection conn = executionScope == null ? new global::System.Data.SqlClient.SqlConnection(global::HotAssembly.PersistenceProviderRepository.ExecutionScope.ConnectionString) : executionScope.Transaction.Connection;
                    try
                    {
                        if (conn.State != global::System.Data.ConnectionState.Open)
                        {
                            if (executionScope == null)
                            {
                                await conn.OpenAsync();
                            }
                            else
                            {
                                retryCycle = int.MaxValue;
                                throw new global::System.Exception("Execution Scope must have an open connection.");
                            }
                        }
                        using (global::System.Data.SqlClient.SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.CommandType = global::System.Data.CommandType.StoredProcedure;
                            if (executionScope != null && executionScope.Transaction != null)
                                cmd.Transaction = executionScope.Transaction;
                            cmd.CommandTimeout = commandTimeout;
                            cmd.CommandText = "[common].[GetBundleFileStreamData]";
                            cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@BundleId", global::System.Data.SqlDbType.NVarChar, 128, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, bundleId)
                            {
                            });
                            cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@BundlePath", global::System.Data.SqlDbType.NVarChar, -1, global::System.Data.ParameterDirection.Output, true, 0, 0, null, global::System.Data.DataRowVersion.Default, bundlePath)
                            {
                            });
                            cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@TransactionContext", global::System.Data.SqlDbType.VarBinary, -1, global::System.Data.ParameterDirection.Output, true, 0, 0, null, global::System.Data.DataRowVersion.Default, transactionContext)
                            {
                            });
                            cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@ReturnValue", global::System.Data.SqlDbType.Int, 4, global::System.Data.ParameterDirection.ReturnValue, true, 0, 0, null, global::System.Data.DataRowVersion.Default, global::System.DBNull.Value));
                            await cmd.ExecuteNonQueryAsync();
                            retValue.Parameters = new ParametersCollection(bundleId, cmd.Parameters["@BundlePath"].Value == global::System.DBNull.Value ? null : (global::System.String)cmd.Parameters["@BundlePath"].Value, cmd.Parameters["@TransactionContext"].Value == global::System.DBNull.Value ? null : (global::System.Byte[])cmd.Parameters["@TransactionContext"].Value);
                            retValue.ReturnValue = (global::System.Int32)cmd.Parameters["@ReturnValue"].Value;
                            return retValue;
                        }
                    }
                    catch (global::System.Data.SqlClient.SqlException e)
                    {
                        if (retryCycle++ > 9 || !ExecutionScope.RetryableErrors.Contains(e.Number))
                            throw;
                        global::System.Threading.Thread.Sleep(1000);
                    }
                    finally
                    {
                        if (executionScope == null && conn != null)
                        {
                            ((global::System.IDisposable)conn).Dispose();
                        }
                    }
                }
            }
        }/*end*/
        public static GetBundleFileStreamData Execute(global::System.String bundleId, global::System.String bundlePath, global::System.Byte[] transactionContext, global::HotAssembly.PersistenceProviderRepository.ExecutionScope executionScope = null, global::System.Int32 commandTimeout = 30)
        {
            var retValue = new GetBundleFileStreamData();
            {
                var retryCycle = 0;
                while (true)
                {
                    global::System.Data.SqlClient.SqlConnection conn = executionScope == null ? new global::System.Data.SqlClient.SqlConnection(global::HotAssembly.PersistenceProviderRepository.ExecutionScope.ConnectionString) : executionScope.Transaction.Connection;
                    try
                    {
                        if (conn.State != global::System.Data.ConnectionState.Open)
                        {
                            if (executionScope == null)
                            {
                                conn.Open();
                            }
                            else
                            {
                                retryCycle = int.MaxValue;
                                throw new global::System.Exception("Execution Scope must have an open connection.");
                            }
                        }
                        using (global::System.Data.SqlClient.SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.CommandType = global::System.Data.CommandType.StoredProcedure;
                            if (executionScope != null && executionScope.Transaction != null)
                                cmd.Transaction = executionScope.Transaction;
                            cmd.CommandTimeout = commandTimeout;
                            cmd.CommandText = "[common].[GetBundleFileStreamData]";
                            cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@BundleId", global::System.Data.SqlDbType.NVarChar, 128, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, bundleId)
                            {
                            });
                            cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@BundlePath", global::System.Data.SqlDbType.NVarChar, -1, global::System.Data.ParameterDirection.Output, true, 0, 0, null, global::System.Data.DataRowVersion.Default, bundlePath)
                            {
                            });
                            cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@TransactionContext", global::System.Data.SqlDbType.VarBinary, -1, global::System.Data.ParameterDirection.Output, true, 0, 0, null, global::System.Data.DataRowVersion.Default, transactionContext)
                            {
                            });
                            cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@ReturnValue", global::System.Data.SqlDbType.Int, 4, global::System.Data.ParameterDirection.ReturnValue, true, 0, 0, null, global::System.Data.DataRowVersion.Default, global::System.DBNull.Value));
                            cmd.ExecuteNonQuery();
                            retValue.Parameters = new ParametersCollection(bundleId, cmd.Parameters["@BundlePath"].Value == global::System.DBNull.Value ? null : (global::System.String)cmd.Parameters["@BundlePath"].Value, cmd.Parameters["@TransactionContext"].Value == global::System.DBNull.Value ? null : (global::System.Byte[])cmd.Parameters["@TransactionContext"].Value);
                            retValue.ReturnValue = (global::System.Int32)cmd.Parameters["@ReturnValue"].Value;
                            return retValue;
                        }
                    }
                    catch (global::System.Data.SqlClient.SqlException e)
                    {
                        if (retryCycle++ > 9 || !ExecutionScope.RetryableErrors.Contains(e.Number))
                            throw;
                        global::System.Threading.Thread.Sleep(1000);
                    }
                    finally
                    {
                        if (executionScope == null && conn != null)
                        {
                            ((global::System.IDisposable)conn).Dispose();
                        }
                    }
                }
            }
        }/*end*/
    }
    public class PrepareSaveBundleFileStreamData
    {
        public class ParametersCollection
        {
            public global::System.String BundleId
            {
                get;
                private set;
            }
            public global::System.String BundlePath
            {
                get;
                private set;
            }
            public global::System.Byte[] TransactionContext
            {
                get;
                private set;
            }
            public ParametersCollection(global::System.String bundleId, global::System.String bundlePath, global::System.Byte[] transactionContext)
            {
                this.BundleId = bundleId;
                this.BundlePath = bundlePath;
                this.TransactionContext = transactionContext;
            }
        }
        public ParametersCollection Parameters
        {
            get;
            private set;
        }
        public global::System.Int32 ReturnValue
        {
            get;
            private set;
        }
        public static async global::System.Threading.Tasks.Task<PrepareSaveBundleFileStreamData> ExecuteAsync(global::System.String bundleId, global::System.String bundlePath, global::System.Byte[] transactionContext, global::HotAssembly.PersistenceProviderRepository.ExecutionScope executionScope = null, global::System.Int32 commandTimeout = 30)
        {
            var retValue = new PrepareSaveBundleFileStreamData();
            {
                var retryCycle = 0;
                while (true)
                {
                    global::System.Data.SqlClient.SqlConnection conn = executionScope == null ? new global::System.Data.SqlClient.SqlConnection(global::HotAssembly.PersistenceProviderRepository.ExecutionScope.ConnectionString) : executionScope.Transaction.Connection;
                    try
                    {
                        if (conn.State != global::System.Data.ConnectionState.Open)
                        {
                            if (executionScope == null)
                            {
                                await conn.OpenAsync();
                            }
                            else
                            {
                                retryCycle = int.MaxValue;
                                throw new global::System.Exception("Execution Scope must have an open connection.");
                            }
                        }
                        using (global::System.Data.SqlClient.SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.CommandType = global::System.Data.CommandType.StoredProcedure;
                            if (executionScope != null && executionScope.Transaction != null)
                                cmd.Transaction = executionScope.Transaction;
                            cmd.CommandTimeout = commandTimeout;
                            cmd.CommandText = "[common].[PrepareSaveBundleFileStreamData]";
                            cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@BundleId", global::System.Data.SqlDbType.NVarChar, 128, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, bundleId)
                            {
                            });
                            cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@BundlePath", global::System.Data.SqlDbType.NVarChar, -1, global::System.Data.ParameterDirection.Output, true, 0, 0, null, global::System.Data.DataRowVersion.Default, bundlePath)
                            {
                            });
                            cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@TransactionContext", global::System.Data.SqlDbType.VarBinary, -1, global::System.Data.ParameterDirection.Output, true, 0, 0, null, global::System.Data.DataRowVersion.Default, transactionContext)
                            {
                            });
                            cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@ReturnValue", global::System.Data.SqlDbType.Int, 4, global::System.Data.ParameterDirection.ReturnValue, true, 0, 0, null, global::System.Data.DataRowVersion.Default, global::System.DBNull.Value));
                            await cmd.ExecuteNonQueryAsync();
                            retValue.Parameters = new ParametersCollection(bundleId, cmd.Parameters["@BundlePath"].Value == global::System.DBNull.Value ? null : (global::System.String)cmd.Parameters["@BundlePath"].Value, cmd.Parameters["@TransactionContext"].Value == global::System.DBNull.Value ? null : (global::System.Byte[])cmd.Parameters["@TransactionContext"].Value);
                            retValue.ReturnValue = (global::System.Int32)cmd.Parameters["@ReturnValue"].Value;
                            return retValue;
                        }
                    }
                    catch (global::System.Data.SqlClient.SqlException e)
                    {
                        if (retryCycle++ > 9 || !ExecutionScope.RetryableErrors.Contains(e.Number))
                            throw;
                        global::System.Threading.Thread.Sleep(1000);
                    }
                    finally
                    {
                        if (executionScope == null && conn != null)
                        {
                            ((global::System.IDisposable)conn).Dispose();
                        }
                    }
                }
            }
        }/*end*/
        public static PrepareSaveBundleFileStreamData Execute(global::System.String bundleId, global::System.String bundlePath, global::System.Byte[] transactionContext, global::HotAssembly.PersistenceProviderRepository.ExecutionScope executionScope = null, global::System.Int32 commandTimeout = 30)
        {
            var retValue = new PrepareSaveBundleFileStreamData();
            {
                var retryCycle = 0;
                while (true)
                {
                    global::System.Data.SqlClient.SqlConnection conn = executionScope == null ? new global::System.Data.SqlClient.SqlConnection(global::HotAssembly.PersistenceProviderRepository.ExecutionScope.ConnectionString) : executionScope.Transaction.Connection;
                    try
                    {
                        if (conn.State != global::System.Data.ConnectionState.Open)
                        {
                            if (executionScope == null)
                            {
                                conn.Open();
                            }
                            else
                            {
                                retryCycle = int.MaxValue;
                                throw new global::System.Exception("Execution Scope must have an open connection.");
                            }
                        }
                        using (global::System.Data.SqlClient.SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.CommandType = global::System.Data.CommandType.StoredProcedure;
                            if (executionScope != null && executionScope.Transaction != null)
                                cmd.Transaction = executionScope.Transaction;
                            cmd.CommandTimeout = commandTimeout;
                            cmd.CommandText = "[common].[PrepareSaveBundleFileStreamData]";
                            cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@BundleId", global::System.Data.SqlDbType.NVarChar, 128, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, bundleId)
                            {
                            });
                            cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@BundlePath", global::System.Data.SqlDbType.NVarChar, -1, global::System.Data.ParameterDirection.Output, true, 0, 0, null, global::System.Data.DataRowVersion.Default, bundlePath)
                            {
                            });
                            cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@TransactionContext", global::System.Data.SqlDbType.VarBinary, -1, global::System.Data.ParameterDirection.Output, true, 0, 0, null, global::System.Data.DataRowVersion.Default, transactionContext)
                            {
                            });
                            cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@ReturnValue", global::System.Data.SqlDbType.Int, 4, global::System.Data.ParameterDirection.ReturnValue, true, 0, 0, null, global::System.Data.DataRowVersion.Default, global::System.DBNull.Value));
                            cmd.ExecuteNonQuery();
                            retValue.Parameters = new ParametersCollection(bundleId, cmd.Parameters["@BundlePath"].Value == global::System.DBNull.Value ? null : (global::System.String)cmd.Parameters["@BundlePath"].Value, cmd.Parameters["@TransactionContext"].Value == global::System.DBNull.Value ? null : (global::System.Byte[])cmd.Parameters["@TransactionContext"].Value);
                            retValue.ReturnValue = (global::System.Int32)cmd.Parameters["@ReturnValue"].Value;
                            return retValue;
                        }
                    }
                    catch (global::System.Data.SqlClient.SqlException e)
                    {
                        if (retryCycle++ > 9 || !ExecutionScope.RetryableErrors.Contains(e.Number))
                            throw;
                        global::System.Threading.Thread.Sleep(1000);
                    }
                    finally
                    {
                        if (executionScope == null && conn != null)
                        {
                            ((global::System.IDisposable)conn).Dispose();
                        }
                    }
                }
            }
        }/*end*/
    }
}