using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading.Tasks;

namespace TaskbarSystemMonitor
{
    public class SystemMetrics
    {
        private readonly PerformanceCounter _cpuCounter;
        private readonly PerformanceCounter _ramAvailable;
        private readonly PerformanceCounter _diskRead;
        private readonly PerformanceCounter _diskWrite;
        private readonly PerformanceCounter _networkSent;
        private readonly PerformanceCounter _networkReceived;
        private readonly List<PerformanceCounter> _gpuCounters;

        private readonly long _totalRam;
        private readonly System.Threading.Timer _updateTimer;

        public event Action<SystemMetricsData>? MetricsUpdated;

        public SystemMetrics()
        {
            // Initialize performance counters
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ramAvailable = new PerformanceCounter("Memory", "Available MBytes");

            // Get total RAM
            _totalRam = GetTotalPhysicalMemory();

            // Initialize disk counters (using PhysicalDisk _Total)
            _diskRead = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
            _diskWrite = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");

            // Initialize network counters
            var networkInterface = GetActiveNetworkInterface();
            if (!string.IsNullOrEmpty(networkInterface))
            {
                _networkSent = new PerformanceCounter("Network Interface", "Bytes Sent/sec", networkInterface);
                _networkReceived = new PerformanceCounter("Network Interface", "Bytes Received/sec", networkInterface);
            }
            else
            {
                // Fallback to loopback interface
                _networkSent = new PerformanceCounter("Network Interface", "Bytes Sent/sec", "Loopback Pseudo-Interface 1");
                _networkReceived = new PerformanceCounter("Network Interface", "Bytes Received/sec", "Loopback Pseudo-Interface 1");
            }

            // Initialize GPU counters
            _gpuCounters = InitializeGpuCounters();

            // Start update timer with slightly longer interval for smoother updates
            _updateTimer = new System.Threading.Timer(UpdateMetrics, null, 0, 1500); // Update every 1.5 seconds
        }

        private void UpdateMetrics(object? state)
        {
            try
            {
                var metrics = new SystemMetricsData
                {
                    CpuUsage = _cpuCounter.NextValue(),
                    RamUsage = GetRamUsagePercentage(),
                    DiskRead = _diskRead.NextValue(),
                    DiskWrite = _diskWrite.NextValue(),
                    NetworkSent = _networkSent?.NextValue() ?? 0,
                    NetworkReceived = _networkReceived?.NextValue() ?? 0,
                    GpuUsage = GetGpuUsage(),
                    Timestamp = DateTime.Now
                };

                MetricsUpdated?.Invoke(metrics);
            }
            catch (Exception ex)
            {
                // Log error but continue
                Console.WriteLine($"Error updating metrics: {ex.Message}");
            }
        }

        private float GetRamUsagePercentage()
        {
            var availableRam = _ramAvailable.NextValue();
            var usedRam = _totalRam - (availableRam * 1024 * 1024); // Convert MB to bytes
            return (float)(usedRam * 100.0 / _totalRam);
        }

        private long GetTotalPhysicalMemory()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                using var collection = searcher.Get();
                foreach (ManagementObject mo in collection)
                {
                    return Convert.ToInt64(mo["TotalPhysicalMemory"]);
                }
            }
            catch
            {
                // Fallback to 8GB if unable to detect
                return 8L * 1024 * 1024 * 1024;
            }
            return 0;
        }

        private string GetActiveNetworkInterface()
        {
            try
            {
                var category = new PerformanceCounterCategory("Network Interface");
                var instanceNames = category.GetInstanceNames()
                    .Where(name => !name.Contains("Loopback") && !name.Contains("Teredo"))
                    .FirstOrDefault();

                return instanceNames ?? "";
            }
            catch
            {
                return "";
            }
        }

        private List<PerformanceCounter> InitializeGpuCounters()
        {
            var counters = new List<PerformanceCounter>();

            try
            {
                // Try to find GPU performance counters
                var category = PerformanceCounterCategory.GetCategories()
                    .FirstOrDefault(c => c.CategoryName.Contains("GPU"));

                if (category != null)
                {
                    var instanceNames = category.GetInstanceNames();
                    foreach (var instance in instanceNames.Take(1)) // Take first GPU
                    {
                        try
                        {
                            var counter = new PerformanceCounter(category.CategoryName, "% Processor Time", instance);
                            counters.Add(counter);
                        }
                        catch
                        {
                            // Continue if this counter fails
                        }
                    }
                }
            }
            catch
            {
                // GPU counters not available
            }

            return counters;
        }

        private float GetGpuUsage()
        {
            try
            {
                if (_gpuCounters.Count > 0)
                {
                    return _gpuCounters.Average(counter => counter.NextValue());
                }
            }
            catch
            {
                // Return 0 if GPU monitoring fails
            }

            return 0;
        }

        public void Dispose()
        {
            _updateTimer?.Dispose();
            _cpuCounter?.Dispose();
            _ramAvailable?.Dispose();
            _diskRead?.Dispose();
            _diskWrite?.Dispose();
            _networkSent?.Dispose();
            _networkReceived?.Dispose();

            foreach (var counter in _gpuCounters)
            {
                counter?.Dispose();
            }
        }
    }

    public class SystemMetricsData
    {
        public float CpuUsage { get; set; }
        public float RamUsage { get; set; }
        public float DiskRead { get; set; }
        public float DiskWrite { get; set; }
        public float NetworkSent { get; set; }
        public float NetworkReceived { get; set; }
        public float GpuUsage { get; set; }
        public DateTime Timestamp { get; set; }
    }
}