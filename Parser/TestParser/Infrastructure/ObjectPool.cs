using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using TestParser.Sys;

namespace TestParser.Infrastructure
{
    public class ObjectPool<T> : IDisposable
    {
        private readonly ConcurrentDictionary<Guid, int> _useCounter;
        private readonly ConcurrentDictionary<Guid, PolledObject<T>> _objectInUse;
        private readonly ConcurrentDictionary<Guid, PolledObject<T>> _failed;
        private readonly string _name;
        private readonly int _max;
        private readonly Func<T> _init;
        private readonly Action<T> _release;
        private readonly HybridLock _locker;

        private volatile int _index;

        public ObjectPool(string name, Func<T> init, Action<T> release, int max = 1)
        {
            Guard.ArgumentNotEmpty(name, nameof(name));
            Guard.ArgumentNotNull(init, nameof(init));
            Guard.ArgumentNotNull(release, nameof(release));

            _name = name;

            _max = max;

            _init = init;

            _release = release;

            _useCounter = new ConcurrentDictionary<Guid, int>();

            _objectInUse = new ConcurrentDictionary<Guid, PolledObject<T>>();

            _failed = new ConcurrentDictionary<Guid, PolledObject<T>>();

            _locker = new HybridLock();
        }

        public bool GetFromPool(out PolledObject<T> obj)
        {
            obj = null;

            bool success = false;
            bool needCreate = false;
            do
            {
                if (needCreate || _objectInUse.IsEmpty)
                {
                    _index++;

                    if (_index > _max)
                    {
                        _index--;
                        break;
                    }

                    obj = new PolledObject<T>
                    {
                        Id = Guid.NewGuid(),
                        Instance = _init()
                    };

                    if (_objectInUse.TryAdd(obj.Id, obj))
                    {
                        Log.Information("Create new {0}", _name);

                        success = true;
                    }
                }
                else
                {
                    foreach (var item in _objectInUse)
                    {
                        if (!_failed.ContainsKey(item.Key))
                        {
                            obj = item.Value;

                            success = true;

                            break;
                        }
                    }
                }

                if (success)
                {
                    _useCounter.AddOrUpdate(obj.Id, 1, (key, existing) => existing + 1);
                }
                else
                {
                    needCreate = true;

                    Log.Information("Need to create new {0}", _name);
                }
            }
            while (!success);

            return success;
        }

        public void ReleaseToPool(PolledObject<T> obj)
        {
            if (obj != null)
            {
                _useCounter.AddOrUpdate(obj.Id, 0, (key, existing) => existing - 1);
            }

            DisposeFailedObjects();
        }

        public void CloseObject(PolledObject<T> obj)
        {
            if (obj == null)
            {
                return;
            }

            _failed.TryAdd(obj.Id, obj);
        }

        public void Dispose()
        {
            _locker.Dispose();
        }

        private void DisposeFailedObjects()
        {
            _locker.Enter();

            try
            {
                var forDispose = new HashSet<Guid>();
                foreach (var item in _failed)
                {
                    if (_useCounter.TryGetValue(item.Key, out int count) && (count == 0))
                    {
                        _useCounter.TryRemove(item.Key, out int tmp);

                        forDispose.Add(item.Key);
                    }
                }

                foreach (var item in forDispose)
                {
                    if (_failed.TryRemove(item, out PolledObject<T> remove))
                    {
                        Log.Information("Dispose {0}", _name);

                        try
                        {
                            _release(remove.Instance);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "There was an exception while release object {0} from pool", remove.Id);
                        }
                        finally
                        {
                            _index--;
                        }
                        remove.Instance = default(T);

                        if (!_objectInUse.TryRemove(item, out PolledObject<T> inUse))
                        {
                            Log.Warning("Can't remove object from pool");
                        }
                    }
                    else
                    {
                        Log.Warning("Can't dispose object from pool");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "There was an exception while disposing failed objects from pool");
            }
            finally
            {
                _locker.Leave();
            }
        }
    }
}
