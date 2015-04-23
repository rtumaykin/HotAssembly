using System;
using System.Data.SqlTypes;
using System.IO;
using HotAssembly.PersistenceProviderRepository;

namespace HotAssembly
{
    public class SqlPersistenceProvider : IPersistenceProvider
    {
        public bool GetBundle(string bundleId, string destinationPath)
        {
            try
            {
                using (var scope = new ExecutionScope())
                {
                    var result = PersistenceProviderRepository.Executables.common.GetBundleFileStreamData
                        .Execute(
                            bundleId,
                            null, null, scope);

                    var inputPath = result.Parameters.BundlePath;

                    using (
                        var sqlFileStream = new SqlFileStream(inputPath,
                            result.Parameters.TransactionContext, FileAccess.Read, FileOptions.SequentialScan, 0))
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
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool PersistBundle(string bundleId, string sourcePath)
        {
            try
            {
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
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
