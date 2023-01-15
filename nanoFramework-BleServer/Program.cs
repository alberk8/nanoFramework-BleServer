using nanoFramework.Hardware.Esp32;
using nanoFramework_BleServer.Bluetooth;
using System.Diagnostics;
using System.Threading;

namespace nanoFramework_BleServer
{


    public class Program
    {

      

        public static void Main()
        {
            PrintMemory("Start");
            Thread.Sleep(2_000);
            Debug.WriteLine("Starting  BLE");

            // This is non blocking
            var ble = BleServer.Create();
            // The Service will write
            ble.StartAdvertising();


            PrintMemory("\nBLE Started");

            Thread.Sleep(Timeout.Infinite);


        }


        public static void PrintMemory(string msg)
        {
            NativeMemory.GetMemoryInfo(NativeMemory.MemoryType.Internal, out uint totalSize, out uint totalFree, out uint largestFree);
            Debug.WriteLine($"{msg} -> Internal Mem:  Total Internal: {totalSize} Free: {totalFree} Largest: {largestFree}");
            Debug.WriteLine($"nF Mem:  {nanoFramework.Runtime.Native.GC.Run(false)}");
        }


    }
}
