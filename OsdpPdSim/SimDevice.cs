using System.Collections;
using System.Collections.Concurrent;
using OSDP.Net;
using OSDP.Net.Model;
using OSDP.Net.Model.CommandData;
using OSDP.Net.Model.ReplyData;
using ReplyDeviceCapabilities = OSDP.Net.Model.ReplyData.DeviceCapabilities;

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

    public void QueueKeypad(string digits, byte readerNumber)
    {
        EnqueuePollReply(new KeypadData(readerNumber, digits));
    }

    protected override PayloadData HandleIdReport()
    {
        return base.HandleIdReport();
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
        return base.HandleReaderLEDControl(commandPayload);
    }

    protected override PayloadData HandleBuzzerControl(ReaderBuzzerControl commandPayload)
    {
        _receivedCommands.Enqueue(new ReceivedCommand("osdp_BUZ", DateTime.UtcNow,
            "Buzzer control received"));
        return base.HandleBuzzerControl(commandPayload);
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
