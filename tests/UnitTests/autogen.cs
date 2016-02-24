namespace ns_d66a1cde125240c598ebf7dacde1e8d4
{
    public class RemoteInstantiator_d66a1cde125240c598ebf7dacde1e8d4 : System.MarshalByRefObject
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<System.Guid, HotAssembly.IComputer> _instances = new System.Collections.Concurrent.ConcurrentDictionary<System.Guid, HotAssembly.IComputer>();
        public System.Guid CreateInstance(params object[] args)
        {
            var instanceHandle = System.Guid.NewGuid();
            if (_instances.TryAdd(instanceHandle, new HotAssembly.Computer.Computer()))
            {
                return instanceHandle;
            }
            return System.Guid.Empty;
        }
        public void DisposeInstance(System.Guid instanceHandle)
        {
            HotAssembly.IComputer instance;
            _instances.TryRemove(instanceHandle, out instance);
        }
        public System.String GetAppDomain(System.Guid instanceHandle)
        {
            HotAssembly.IComputer instance;
            if (_instances.TryGetValue(instanceHandle, out instance))
            {
                return instance.GetAppDomain();
            }
            return default(System.String);
        }
        public System.String ToString(System.Guid instanceHandle)
        {
            HotAssembly.IComputer instance;
            if (_instances.TryGetValue(instanceHandle, out instance))
            {
                return instance.ToString();
            }
            return default(System.String);
        }
        public System.Boolean Equals(System.Guid instanceHandle, System.Object obj)
        {
            HotAssembly.IComputer instance;
            if (_instances.TryGetValue(instanceHandle, out instance))
            {
                return instance.Equals(obj);
            }
            return default(System.Boolean);
        }
        public System.Int32 GetHashCode(System.Guid instanceHandle)
        {
            HotAssembly.IComputer instance;
            if (_instances.TryGetValue(instanceHandle, out instance))
            {
                return instance.GetHashCode();
            }
            return default(System.Int32);
        }
        public System.Type GetType(System.Guid instanceHandle)
        {
            HotAssembly.IComputer instance;
            if (_instances.TryGetValue(instanceHandle, out instance))
            {
                return instance.GetType();
            }
            return default(System.Type);
        }
    }
    [System.Serializable]
    public class LocalProxy_d66a1cde125240c598ebf7dacde1e8d4 : HotAssembly.IComputer, System.IDisposable
    {
        private readonly RemoteInstantiator_d66a1cde125240c598ebf7dacde1e8d4 _remoteInstantiator;
        private readonly System.Guid _remoteInstanceHandle;
        public LocalProxy_d66a1cde125240c598ebf7dacde1e8d4(RemoteInstantiator_d66a1cde125240c598ebf7dacde1e8d4 remoteInstantiator, System.Guid remoteInstanceHandle)
        {
            _remoteInstantiator = remoteInstantiator;
            _remoteInstanceHandle = remoteInstanceHandle;
        }
        public void Dispose()
        {
            _remoteInstantiator?.DisposeInstance(_remoteInstanceHandle);
        }
        public System.String GetAppDomain()
        {
            return _remoteInstantiator.GetAppDomain(_remoteInstanceHandle);
        }
        public override System.String ToString()
        {
            return _remoteInstantiator.ToString(_remoteInstanceHandle);
        }
        public System.Boolean Equals(System.Object obj)
        {
            return _remoteInstantiator.Equals(_remoteInstanceHandle, obj);
        }
        public System.Int32 GetHashCode()
        {
            return _remoteInstantiator.GetHashCode(_remoteInstanceHandle);
        }
        public System.Type GetType()
        {
            return _remoteInstantiator.GetType(_remoteInstanceHandle);
        }
    }
    [System.Serializable]
    public class LocalInstantiator_d66a1cde125240c598ebf7dacde1e8d4 : HotAssembly.IInstantiator<HotAssembly.IComputer>
    {
        private readonly RemoteInstantiator_d66a1cde125240c598ebf7dacde1e8d4 _remoteInstantiator;
        public LocalInstantiator_d66a1cde125240c598ebf7dacde1e8d4(RemoteInstantiator_d66a1cde125240c598ebf7dacde1e8d4 remoteInstantiator)
        {
            _remoteInstantiator = remoteInstantiator;

        }
        public HotAssembly.IComputer Instantiate(params object[] args)
        {
            var remoteInstanceHandle = _remoteInstantiator.CreateInstance(args);
            var instance = new LocalProxy_d66a1cde125240c598ebf7dacde1e8d4(_remoteInstantiator, remoteInstanceHandle);
            return instance;
        }
    }
}
