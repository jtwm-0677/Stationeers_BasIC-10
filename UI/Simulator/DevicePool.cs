using System;
using System.Collections.Generic;
using System.Linq;
using BasicToMips.Data;
using BasicToMips.Simulator;

namespace BasicToMips.UI.Simulator
{
    /// <summary>
    /// Manages pools of virtual devices grouped by prefab hash for batch operations (lb/sb).
    /// </summary>
    public class DevicePool
    {
        private readonly Dictionary<int, List<VirtualDevice>> _devicesByHash = new();

        /// <summary>
        /// Adds a device to the pool, grouping it by its prefab hash.
        /// Called when a DEVICE is registered in the DeviceAliasRegistry.
        /// </summary>
        /// <param name="device">The virtual device to add</param>
        public void AddDevice(VirtualDevice device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            int hash = DeviceDatabase.CalculateHash(device.PrefabName);

            if (!_devicesByHash.ContainsKey(hash))
            {
                _devicesByHash[hash] = new List<VirtualDevice>();
            }

            _devicesByHash[hash].Add(device);
        }

        /// <summary>
        /// Gets all devices matching a given prefab hash.
        /// </summary>
        /// <param name="prefabHash">The prefab hash to search for</param>
        /// <returns>Enumerable of matching devices (empty if none found)</returns>
        public IEnumerable<VirtualDevice> GetDevicesByHash(int prefabHash)
        {
            if (_devicesByHash.TryGetValue(prefabHash, out var devices))
            {
                return devices;
            }
            return Enumerable.Empty<VirtualDevice>();
        }

        /// <summary>
        /// Performs a batch read operation (lb instruction).
        /// Aggregates property values from all devices matching the prefab hash.
        /// </summary>
        /// <param name="prefabHash">The prefab hash identifying the device type</param>
        /// <param name="property">The property name to read</param>
        /// <param name="mode">Aggregation mode (Average, Sum, Minimum, Maximum)</param>
        /// <returns>The aggregated value, or 0 if no devices found</returns>
        public double BatchRead(int prefabHash, string property, BatchMode mode)
        {
            var devices = GetDevicesByHash(prefabHash).ToList();

            if (devices.Count == 0)
                return 0;

            var values = devices
                .Select(d => d.GetProperty(property))
                .Where(v => !double.IsNaN(v)) // Filter out NaN values
                .ToList();

            if (values.Count == 0)
                return 0;

            return mode switch
            {
                BatchMode.Average => values.Average(),
                BatchMode.Sum => values.Sum(),
                BatchMode.Minimum => values.Min(),
                BatchMode.Maximum => values.Max(),
                _ => values.Average() // Default to average
            };
        }

        /// <summary>
        /// Performs a batch write operation (sb instruction).
        /// Writes a value to the specified property on all devices matching the prefab hash.
        /// </summary>
        /// <param name="prefabHash">The prefab hash identifying the device type</param>
        /// <param name="property">The property name to write</param>
        /// <param name="value">The value to write</param>
        public void BatchWrite(int prefabHash, string property, double value)
        {
            var devices = GetDevicesByHash(prefabHash);

            foreach (var device in devices)
            {
                device.SetProperty(property, value);
            }
        }

        /// <summary>
        /// Clears all devices from the pool.
        /// Called when resetting the simulator or loading a new program.
        /// </summary>
        public void Clear()
        {
            _devicesByHash.Clear();
        }

        /// <summary>
        /// Gets the total number of devices in the pool.
        /// </summary>
        public int TotalDeviceCount => _devicesByHash.Values.Sum(list => list.Count);

        /// <summary>
        /// Gets the number of unique device types (prefab hashes) in the pool.
        /// </summary>
        public int UniqueDeviceTypeCount => _devicesByHash.Count;

        /// <summary>
        /// Gets all device type hashes currently in the pool.
        /// </summary>
        public IEnumerable<int> GetAllDeviceHashes()
        {
            return _devicesByHash.Keys;
        }

        /// <summary>
        /// Gets the count of devices for a specific prefab hash.
        /// </summary>
        /// <param name="prefabHash">The prefab hash to check</param>
        /// <returns>Number of devices with this hash</returns>
        public int GetDeviceCount(int prefabHash)
        {
            if (_devicesByHash.TryGetValue(prefabHash, out var devices))
            {
                return devices.Count;
            }
            return 0;
        }

        /// <summary>
        /// Removes a specific device from the pool.
        /// </summary>
        /// <param name="device">The device to remove</param>
        /// <returns>True if device was found and removed, false otherwise</returns>
        public bool RemoveDevice(VirtualDevice device)
        {
            if (device == null)
                return false;

            int hash = DeviceDatabase.CalculateHash(device.PrefabName);

            if (_devicesByHash.TryGetValue(hash, out var devices))
            {
                bool removed = devices.Remove(device);

                // Clean up empty lists
                if (devices.Count == 0)
                {
                    _devicesByHash.Remove(hash);
                }

                return removed;
            }

            return false;
        }
    }

    /// <summary>
    /// Aggregation modes for batch read operations.
    /// Matches Stationeers IC10 batch modes.
    /// </summary>
    public enum BatchMode
    {
        /// <summary>
        /// Average of all values (default for lb)
        /// </summary>
        Average = 0,

        /// <summary>
        /// Sum of all values
        /// </summary>
        Sum = 1,

        /// <summary>
        /// Minimum value
        /// </summary>
        Minimum = 2,

        /// <summary>
        /// Maximum value
        /// </summary>
        Maximum = 3
    }
}
