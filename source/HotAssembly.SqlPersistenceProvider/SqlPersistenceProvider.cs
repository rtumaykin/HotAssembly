using System;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using HotAssembly.PersistenceProviderRepository;

namespace HotAssembly
{
    public class SqlPersistenceProvider : IPersistenceProvider
    {
        private enum SqlAuthenticationType
        {
            Sql,
            Windows
        };

        private SqlAuthenticationType AuthenticationType { get; }

        public SqlPersistenceProvider(string connectionString = null)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return;

            ExecutionScope.ConnectionString = connectionString;
            AuthenticationType = new SqlConnectionStringBuilder(connectionString).IntegratedSecurity
                ? SqlAuthenticationType.Windows
                : SqlAuthenticationType.Sql;
        }

        public void GetBundle(string bundleId, string destinationPath)
        {
            switch (AuthenticationType)
            {
                case SqlAuthenticationType.Sql:
                    var getBundle = PersistenceProviderRepository.Executables.common.GetBundle.Execute(bundleId, null);
                    File.WriteAllBytes(destinationPath, getBundle.Parameters.Bundle);
                    break;

                case SqlAuthenticationType.Windows:
                    using (var scope = new ExecutionScope())
                    {
                        var getBundleFileStreamData = PersistenceProviderRepository.Executables.common.GetBundleFileStreamData
                            .Execute(
                                bundleId,
                                null, null, scope);

                        var inputPath = getBundleFileStreamData.Parameters.BundlePath;

                        using (
                            var sqlFileStream = new SqlFileStream(inputPath,
                                getBundleFileStreamData.Parameters.TransactionContext, FileAccess.Read, FileOptions.SequentialScan, 0))
                        {
                            using (
                                var localFileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write)
                                )
                            {
                                const int bufferSize = 4096;
                                var buffer = new byte[bufferSize];

                                var bytesRead = sqlFileStream.Read(buffer, 0, bufferSize);
                                while (bytesRead > 0)
                                {
                                    localFileStream.Write(buffer, 0, bytesRead);
                                    localFileStream.Flush();

                                    bytesRead = sqlFileStream.Read(buffer, 0, bufferSize);
                                }

                                localFileStream.Close();
                            }

                            sqlFileStream.Close();
                        }
                        scope.Commit();
                    }
                    break;
            }
        }

        public void PersistBundle(string bundleId, string sourcePath)
        {
            switch (AuthenticationType)
            {
                case SqlAuthenticationType.Sql:
                    var data = File.ReadAllBytes(sourcePath);
                    PersistenceProviderRepository.Executables.common.SaveBundle.Execute(bundleId, data);
                    break;

                case SqlAuthenticationType.Windows:
                    using (var scope = new ExecutionScope())
                    {
                        var result = PersistenceProviderRepository.Executables.common.PrepareSaveBundleFileStreamData
                            .Execute(
                                bundleId,
                                null, null, scope);

                        var outputPath = result.Parameters.BundlePath;

                        using (var localFileStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read))
                        {
                            using (
                                var sqlFileStream = new SqlFileStream(outputPath,
                                    result.Parameters.TransactionContext, FileAccess.Write, FileOptions.SequentialScan,
                                    0))
                            {
                                const int bufferSize = 4096;
                                var buffer = new byte[bufferSize];

                                var bytesRead = localFileStream.Read(buffer, 0, bufferSize);
                                while (bytesRead > 0)
                                {
                                    sqlFileStream.Write(buffer, 0, bytesRead);
                                    sqlFileStream.Flush();

                                    bytesRead = localFileStream.Read(buffer, 0, bufferSize);
                                }

                                sqlFileStream.Close();
                            }
                            localFileStream.Close();
                        }

                        scope.Commit();
                    }
                    break;
            }
        }
    }
}
