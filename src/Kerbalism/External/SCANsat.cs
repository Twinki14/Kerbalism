using System;
using System.Reflection;

namespace Kerbalism.External
{
    public static class SCANsat
    {
        // reflection type of SCANUtils static class in SCANsat assembly, if present
        private static readonly Type SCANUtils;
        private static readonly MethodInfo RegisterSensor;
        private static readonly MethodInfo UnregisterSensor;
        private static readonly MethodInfo GetCoverage;

        static SCANsat()
        {
            foreach (var a in AssemblyLoader.loadedAssemblies)
            {
                if (a.name != "SCANsat")
                {
                    continue;
                }

                SCANUtils = a.assembly.GetType("SCANsat.SCANUtil");
                RegisterSensor = SCANUtils.GetMethod("registerSensorExternal");
                UnregisterSensor = SCANUtils.GetMethod("unregisterSensorExternal");
                GetCoverage = SCANUtils.GetMethod("GetCoverage");
                break;
            }
        }

        // interrupt scanning of a SCANsat module
        // - v: vessel that own the module
        // - m: protomodule of a SCANsat or a resource scanner
        // - p: prefab of the part owning the module
        public static bool StopScanner(Vessel v, ProtoPartModuleSnapshot m, Part partPrefab)
        {
            return SCANUtils != null && (bool) UnregisterSensor.Invoke(null, new object[] {v, m, partPrefab});
        }

        // resume scanning of a SCANsat module
        // - v: vessel that own the module
        // - m: protomodule of a SCANsat or a resource scanner
        // - p: prefab of the part owning the module
        public static bool ResumeScanner(Vessel v, ProtoPartModuleSnapshot m, Part partPrefab)
        {
            return SCANUtils != null && (bool) RegisterSensor.Invoke(null, new object[] {v, m, partPrefab});
        }

        // return the scanning coverage for a given sensor type on a give body
        // - sensor_type: the sensor type
        // - body: the body in question
        public static double Coverage(int sensorType, CelestialBody body)
        {
            if (SCANUtils == null)
            {
                return 0;
            }

            return (double) GetCoverage.Invoke(null, new object[] {sensorType, body});
        }

        public static bool IsScanning(PartModule scanner) => Lib.ReflectionValue<bool>(scanner, "scanning");

        public static void StopScan(PartModule scanner) => Lib.ReflectionCall(scanner, "stopScan");

        public static void StartScan(PartModule scanner) => Lib.ReflectionCall(scanner, "startScan");
    }
}
