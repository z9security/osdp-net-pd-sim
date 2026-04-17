using System.Collections;
using System.Collections.Concurrent;
using OSDP.Net;
using OSDP.Net.Model;
using OSDP.Net.Model.CommandData;
using OSDP.Net.Model.ReplyData;
using ReplyDeviceCapabilities = OSDP.Net.Model.ReplyData.DeviceCapabilities;
using ReplyLocalStatus = OSDP.Net.Model.ReplyData.LocalStatus;

namespace OsdpPdSim;

public class SimDevice : Device
{
    private readonly ConcurrentQueue<ReceivedCommand> _receivedCommands = new();
    private volatile bool[] _inputStatuses = [false, false];

    public SimDevice(byte address, ILoggerFactory loggerFactory) : base(
        new DeviceConfiguration(new ClientIdentification([0x00, 0x00, 0x00], 1))
        {
            Address = address
        },
        loggerFactory)
    {
    }

    public IReadOnlyCollection<ReceivedCommand> ReceivedCommands => _receivedCommands.ToArray();

    public void ClearCommands() => _receivedCommands.Clear();

    public void SetInputStatuses(bool[] statuses)
    {
        _inputStatuses = statuses;
    }

    public void QueueCardRead(int cardNumber, int bitCount, byte readerNumber)
    {
        var data = EncodeCardNumber(cardNumber, bitCount);
        EnqueuePollReply(new RawCardData(readerNumber, FormatCode.NotSpecified, new BitArray(data)));
    }

    public void QueueWiegand26CardRead(int facilityCode, int cardNumber, byte readerNumber)
    {
        var bits = new BitArray(26);

        // Bits 1-8: facility code (MSB at bit 1)
        for (int i = 0; i < 8; i++)
            bits[1 + i] = ((facilityCode >> (7 - i)) & 1) == 1;

        // Bits 9-24: card number (MSB at bit 9)
        for (int i = 0; i < 16; i++)
            bits[9 + i] = ((cardNumber >> (15 - i)) & 1) == 1;

        // Bit 0: even parity over bits 1-12
        int count1 = 0;
        for (int i = 1; i <= 12; i++)
            if (bits[i]) count1++;
        bits[0] = (count1 % 2) != 0;

        // Bit 25: odd parity over bits 13-24
        int count2 = 0;
        for (int i = 13; i <= 24; i++)
            if (bits[i]) count2++;
        bits[25] = (count2 % 2) == 0;

        EnqueuePollReply(new RawCardData(readerNumber, FormatCode.NotSpecified, bits));
    }

    public void QueueKeypad(string digits, byte readerNumber)
    {
        EnqueuePollReply(new KeypadData(readerNumber, digits));
    }

    protected override PayloadData HandleIdReport()
    {
        return new DeviceIdentification(
            vendorCode: [0x00, 0x00, 0x01],
            modelNumber: 0x01,
            version: 0x01,
            serialNumber: 1001,
            firmwareMajor: 1,
            firmwareMinor: 0,
            firmwareBuild: 1
        );
    }

    protected override PayloadData HandleDeviceCapabilities()
    {
        return new ReplyDeviceCapabilities(
        [
            new DeviceCapability(CapabilityFunction.ContactStatusMonitoring, 2, 2),
            new DeviceCapability(CapabilityFunction.CardDataFormat, 1, 0),
            new DeviceCapability(CapabilityFunction.OutputControl, 1, 2),
            new DeviceCapability(CapabilityFunction.ReaderLEDControl, 1, 1),
            new DeviceCapability(CapabilityFunction.ReaderAudibleOutput, 1, 1),
            new DeviceCapability(CapabilityFunction.ReaderTextOutput, 1, 2)
        ]);
    }

    protected override PayloadData HandleInputStatusReport()
    {
        var statuses = _inputStatuses;
        return new InputStatus(statuses.Select(s =>
            s ? InputStatusValue.Active : InputStatusValue.Inactive).ToArray());
    }

    protected override PayloadData HandleLocalStatusReport()
    {
        return new ReplyLocalStatus(tamper: false, powerFailure: false);
    }

    protected override PayloadData HandleOutputStatusReport()
    {
        return new OutputStatus([false, false]);
    }

    protected override PayloadData HandleOutputControl(OutputControls commandPayload)
    {
        _receivedCommands.Enqueue(new ReceivedCommand("osdp_OUT", DateTime.UtcNow,
            "OutputControl received"));
        return new OutputStatus([false, false]);
    }

    protected override PayloadData HandleReaderLEDControl(ReaderLedControls commandPayload)
    {
        _receivedCommands.Enqueue(new ReceivedCommand("osdp_LED", DateTime.UtcNow,
            "LED control received"));
        return new Ack();
    }

    protected override PayloadData HandleBuzzerControl(ReaderBuzzerControl commandPayload)
    {
        _receivedCommands.Enqueue(new ReceivedCommand("osdp_BUZ", DateTime.UtcNow,
            "Buzzer control received"));
        return new Ack();
    }

    private static byte[] EncodeCardNumber(int cardNumber, int bitCount)
    {
        int byteCount = (bitCount + 7) / 8;
        var data = new byte[byteCount];
        for (int i = byteCount - 1; i >= 0 && cardNumber > 0; i--)
        {
            data[i] = (byte)(cardNumber & 0xFF);
            cardNumber >>= 8;
        }
        return data;
    }
}

public record ReceivedCommand(string Type, DateTime Timestamp, string Data);
