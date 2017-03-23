using System;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using HotAssembly.PersistenceProviderRepository;

namespace HotAssembly
{
    public class SqlPersistenceProvider : IPersistenceProvider
    {


        public SqlPersistenceProvider(string connectionString = null)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return;

            ExecutionScope.ConnectionString = connectionString;
        }

        public void GetBundle(string bundleId, string destinationPath)
        {
            var getBundle = PersistenceProviderRepository.Executables.common.GetBundle.Execute(bundleId, null);
            File.WriteAllBytes(destinationPath, getBundle.Parameters.Bundle);
        }

        public void PersistBundle(string bundleId, string sourcePath)
        {

            var data = File.ReadAllBytes(sourcePath);
            PersistenceProviderRepository.Executables.common.SaveBundle.Execute(bundleId, data);
        }
    }
}
