using nanoFramework.Device.Bluetooth;
using nanoFramework.Device.Bluetooth.GenericAttributeProfile;
using nanoFramework.Hardware.Esp32;
using nanoFramework.Runtime.Native;
using System;
using System.Diagnostics;
using System.Threading;

namespace nanoFramework_BleServer.Bluetooth
{
    public class BluetoothDevice
    {

        public ulong BluetoothAddress { get; set; }
        public BluetoothAddressType BluetoothAddressTypeType { get; set; }
    }

    internal class BleServer
    {

        private GattServiceProvider _gattServiceProvider;

        private Guid serviceUuid = new Guid("A7EEDF2C-DA87-4CB5-A9C5-5151C78B0057");
        private Guid characteristicUuid = new Guid("A7EEDF2C-DA89-4CB5-A9C5-5151C78B0057");

        public BleServer()
        {

        }

        public static BleServer Create()
        {
            return new BleServer();
        }


        public void StartAdvertising()
        {
            if (_gattServiceProvider is null)
            {
                StartBleServer();
            }

        }

        public void StopAdvertising()
        {
            if (_gattServiceProvider is not null && _gattServiceProvider.AdvertisementStatus == GattServiceProviderAdvertisementStatus.Started)
            {
                _gattServiceProvider.StopAdvertising();
                _gattServiceProvider = null;
            }
        }


        private void StartBleServer()
        {
            GattServiceProviderResult result = GattServiceProvider.Create(serviceUuid);
            if (result.Error != BluetoothError.Success)
            {
                return;
            }

            GattServiceProvider serviceProvider = result.ServiceProvider;

            // Get created Primary service from provider
            GattLocalService service = serviceProvider.Service;

           


            GattLocalCharacteristicResult characteristicResult = service.CreateCharacteristic(characteristicUuid,
                 new GattLocalCharacteristicParameters()
                 {
                     //Option 1
                     //CharacteristicProperties = GattCharacteristicProperties.Read | GattCharacteristicProperties.Write,
                     
                     //Option 2
                     CharacteristicProperties = GattCharacteristicProperties.Read | GattCharacteristicProperties.Write | GattCharacteristicProperties.Notify,
                     
                     UserDescription = "nF01 Characteristic",

                 });

            if (characteristicResult.Error != BluetoothError.Success)
            {
                // An error occurred.
                return;
            }

            //Option 1 -Event for Read and Write
            //characteristicResult.Characteristic.ReadRequested += Characteristic_ReadRequested;
            //characteristicResult.Characteristic.WriteRequested += Characteristic_WriteRequested;

            // Option 2 - Event for Read and Write with Notification
            characteristicResult.Characteristic.ReadRequested += Characteristic_ReadRequestedNotify;
            characteristicResult.Characteristic.WriteRequested+= Characteristic_WriteRequestedNotify;


            // Device Information
            DeviceInformationServiceService DifService = new DeviceInformationServiceService(
                    serviceProvider,
                    "nF-01",
                    "Model-1",
                    "989898", // no serial number
                    "v1.0",
                    SystemInfo.Version.ToString(),
                    "");

            // Battery Level (Random with every call)
            BatteryService BatService = new BatteryService(serviceProvider);

            BatService.BatteryLevel = 94;

            // Start Advertising
            serviceProvider.StartAdvertising(new GattServiceProviderAdvertisingParameters()
            {
                DeviceName = "nF-01",
                IsConnectable = true,
                IsDiscoverable = true,

            });
        }

       
        //Option 1
        private void Characteristic_ReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs ReadRequestEventArgs)
        {
            Debug.WriteLine("Read Request");
            GattReadRequest request = ReadRequestEventArgs.GetRequest();
            //sender.NotifyValue(new Buffer(new byte[] { 0x03, 0x04, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0 }));
            DataWriter dw = new DataWriter();
            dw.WriteBuffer(new Buffer(new byte[] { 0x03, 0x04, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0 }));

            request.RespondWithValue(dw.DetachBuffer());
        }

        //Option 1
        private void Characteristic_WriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs WriteRequestEventArgs)
        {
            GattWriteRequest request = WriteRequestEventArgs.GetRequest();
            var read = request.Value;
            Debug.WriteLine("Value Size: " + read.Length);
            DataReader dataReader = DataReader.FromBuffer(read);

            byte[] readByte = new byte[read.Length];

            dataReader.ReadBytes(readByte);
            Console.WriteLine("Read Byte: " + readByte.Length);

            switch (readByte[1])
            {
                case 0x25:
                    byte[] sendByte = new byte[8];
                    Random random = new Random();
                    random.NextBytes(sendByte);
                    sendByte[0] = 0x21;
                    sendByte[1] = 0x25;
                    sender.NotifyValue(new Buffer(sendByte));
                    break;
                case 0x50:
                    //This shuts down the ESP32 for a random number of seconds as defined by GetSleepTime.
                    sender.NotifyValue(new Buffer(new byte[] { 0x21, 0x50, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0 }));

                    //_gattServiceProvider.StopAdvertising();
                    request.Respond();
                    Thread.Sleep(100);
                    var time = GetSleepTime(5, 15);
                    Console.WriteLine($"Stop and Sleep for {time.TotalSeconds} seconds");
                    Sleep.EnableWakeupByTimer(time);
                    Sleep.StartDeepSleep();
                    break;
            }
            request.Respond();
        }

        //Option 2
        private void Characteristic_ReadRequestedNotify(GattLocalCharacteristic sender, GattReadRequestedEventArgs ReadRequestEventArgs)
        {
            Debug.WriteLine("Read Request");
            
            sender.NotifyValue(new Buffer(new byte[] { 0x03, 0x04, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0 }));
           
        }

        // Option 2
        private void Characteristic_WriteRequestedNotify(GattLocalCharacteristic sender, GattWriteRequestedEventArgs WriteRequestEventArgs)
        {
            Debug.WriteLine("Write Request");
            GattWriteRequest request = WriteRequestEventArgs.GetRequest();
            var read = request.Value;

            Debug.WriteLine("Value Size: " + read.Length);
            DataReader dataReader = DataReader.FromBuffer(read);

            byte[] readByte = new byte[read.Length];

            dataReader.ReadBytes(readByte);
            Debug.WriteLine("Read Byte: " + readByte.Length);

            switch (readByte[1])
            {
                case 0x25:
                    byte[] sendByte = new byte[8];
                    Random random = new Random();
                    random.NextBytes(sendByte);
                    sendByte[0] = 0x21;
                    sendByte[1] = 0x25;
                    sender.NotifyValue(new Buffer(sendByte));
                    break;
                case 0x50:
                    //This shuts down the ESP32 for a random number of seconds as defined by GetSleepTime.
                    sender.NotifyValue(new Buffer(new byte[] { 0x21, 0x50, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0 }));
               
                    Thread.Sleep(100);
                    var time = GetSleepTime(5, 15);
                    Debug.WriteLine($"Stop and Sleep for {time.TotalSeconds} seconds");
                    Sleep.EnableWakeupByTimer(time);
                    Sleep.StartDeepSleep();
                    break;
            }

           
        }

        /// <summary>
        /// Get Sleep Time
        /// </summary>
        /// <param name="minSec">Minimum Sleep Seconds</param>
        /// <param name="maxSec">Maximum Sleep Seconds</param>
        /// <returns>TimeSpan</returns>
        private static TimeSpan GetSleepTime(int minSec, int maxSec)
        {
            int sec = 0;
            Random random = new Random();
            for (int i = 0; i < 10; i++)
            {
                sec = random.Next(maxSec);
                if (sec >= minSec)
                {
                    break;
                }
            }
            return TimeSpan.FromSeconds(sec);

        }


        private class DeviceInformationServiceService
        {
            private readonly GattLocalService _deviceInformationService;

            /// <summary>
            /// Create a new Device Information Service on Provider using supplied string.
            /// If a string is null the Characteristic will not be included in service.
            /// </summary>
            /// <param name="provider"></param>
            /// <param name="Manufacturer"></param>
            /// <param name="ModelNumber"></param>
            /// <param name="SerialNumber"></param>
            /// <param name="HardwareRevision"></param>
            /// <param name="FirmwareRevision"></param>
            /// <param name="SoftwareRevision"></param>
            public DeviceInformationServiceService(
                GattServiceProvider provider,
                string Manufacturer,
                string ModelNumber = null,
                string SerialNumber = null,
                string HardwareRevision = null,
                string FirmwareRevision = null,
                string SoftwareRevision = null
                )
            {
                // Add new Device Information Service to provider
                _deviceInformationService = provider.AddService(GattServiceUuids.DeviceInformation);

                CreateReadStaticCharacteristic(GattCharacteristicUuids.ManufacturerNameString, Manufacturer);
                CreateReadStaticCharacteristic(GattCharacteristicUuids.ModelNumberString, ModelNumber);
                CreateReadStaticCharacteristic(GattCharacteristicUuids.SerialNumberString, SerialNumber);
                CreateReadStaticCharacteristic(GattCharacteristicUuids.HardwareRevisionString, HardwareRevision);
                CreateReadStaticCharacteristic(GattCharacteristicUuids.FirmwareRevisionString, FirmwareRevision);
                CreateReadStaticCharacteristic(GattCharacteristicUuids.SoftwareRevisionString, SoftwareRevision);
            }

            /// <summary>
            /// Create static Characteristic if not null.
            /// </summary>
            /// <param name="Uuid">Characteristic UUID</param>
            /// <param name="data">string data or null</param>
            private void CreateReadStaticCharacteristic(Guid Uuid, String data)
            {
                if (data != null)
                {
                    // Create data buffer
                    DataWriter writer = new DataWriter();
                    writer.WriteString(data);

                    _deviceInformationService.CreateCharacteristic(Uuid, new GattLocalCharacteristicParameters()
                    {
                        CharacteristicProperties = GattCharacteristicProperties.Read,
                        StaticValue = writer.DetachBuffer()
                    });
                }
            }
        }

    }
}
