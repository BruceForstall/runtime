// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    internal sealed class DefaultMeterFactory : IMeterFactory
    {
        private readonly Dictionary<string, List<FactoryMeter>> _cachedMeters = new();
        private bool _disposed;

        public DefaultMeterFactory() { }

        public Meter Create(MeterOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            if (options.Scope is not null && !object.ReferenceEquals(options.Scope, this))
            {
                throw new InvalidOperationException(SR.InvalidScope);
            }

            Debug.Assert(options.Name is not null);

            lock (_cachedMeters)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(DefaultMeterFactory));
                }

                if (_cachedMeters.TryGetValue(options.Name, out List<FactoryMeter>? meterList))
                {
                    foreach (Meter meter in meterList)
                    {
                        if (meter.Version == options.Version && DiagnosticsHelper.CompareTags(meter.Tags as IList<KeyValuePair<string, object?>>, options.Tags))
                        {
                            return meter;
                        }
                    }
                }
                else
                {
                    meterList = new List<FactoryMeter>();
                    _cachedMeters.Add(options.Name, meterList);
                }

                object? scope = options.Scope;
                options.Scope = this;
                FactoryMeter m = new FactoryMeter(options);
                options.Scope = scope;

                meterList.Add(m);
                return m;
            }
        }

        public void Dispose()
        {
            lock (_cachedMeters)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;

                foreach (List<FactoryMeter> meterList in _cachedMeters.Values)
                {
                    foreach (FactoryMeter meter in meterList)
                    {
                        meter.Release();
                    }
                }

                _cachedMeters.Clear();
            }
        }
    }

    internal sealed class FactoryMeter : Meter
    {
        public FactoryMeter(MeterOptions options) : base(options)
        {
        }

        public void Release() => base.Dispose(true); // call the protected Dispose(bool)

        protected override void Dispose(bool disposing)
        {
            // no-op, disallow users from disposing of the meters created from the factory.
        }
    }
}
