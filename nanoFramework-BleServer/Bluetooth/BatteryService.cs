using nanoFramework.Device.Bluetooth;
using nanoFramework.Device.Bluetooth.GenericAttributeProfile;
using System;
using System.Diagnostics;

namespace nanoFramework_BleServer.Bluetooth
{
    public class BatteryService
    {
        private readonly GattLocalService _batteryService;
        private readonly GattLocalCharacteristic _batteryLevelCharacteristic;
        private byte _batteryLevel;

        public BatteryService(GattServiceProvider provider)
        {
            // Add new Battery service to provider
            _batteryService = provider.AddService(GattServiceUuids.Battery);


            GattLocalCharacteristicResult result =
                _batteryService.CreateCharacteristic(GattCharacteristicUuids.BatteryLevel, new GattLocalCharacteristicParameters()
                {
                    UserDescription = "Battery level %",
                    CharacteristicProperties = GattCharacteristicProperties.Read | GattCharacteristicProperties.Notify,
                    //ReadProtectionLevel = GattProtectionLevel.EncryptionRequired
                });

            _batteryLevelCharacteristic = result.Characteristic;
            _batteryLevelCharacteristic.ReadRequested += BatteryLevelCharacteristic_ReadRequested;

            // Set default values
            BatteryLevel = 100;

            DeviceInError = false;
        }

        /// <summary>
        /// Get or Set current battery level.
        /// </summary>
        public byte BatteryLevel
        {
            get => _batteryLevel;
            set
            {
                if (_batteryLevel != value)
                {
                    _batteryLevelCharacteristic.NotifyValue(GetBatteryLevel());

                }
                _batteryLevel = value;
            }
        }

        /// <summary>
        /// Set if Battery not connected or error reading battery level.
        /// </summary>
        public bool DeviceInError { get; set; }

        /// <summary>
        /// Read event handler.
        /// </summary>
        /// <param name="sender">GattLocalCharacteristic sender</param>
        /// <param name="ReadRequestEventArgs">Request args</param>
        private void BatteryLevelCharacteristic_ReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs ReadRequestEventArgs)
        {
            GattReadRequest request = ReadRequestEventArgs.GetRequest();

            if (DeviceInError)
            {
                request.RespondWithProtocolError((byte)BluetoothError.DeviceNotConnected);
            }
            else
            {
                Debug.WriteLine("Read Request");
                Random rand = new Random();
                Int32 val = rand.Next(100);

                BatteryLevel = (byte)val;
                request.RespondWithValue(GetBatteryLevel());
            }
        }

        private Buffer GetBatteryLevel()
        {
            DataWriter writer = new DataWriter();
            writer.WriteByte(BatteryLevel);
            return writer.DetachBuffer();
        }
    }
}
