using System.Text.Json;
using NUnit.Framework;

namespace RightKeyboard.NUnit;

public sealed class SettingsIpcProtocolTests
{
    [Test]
    public void Request_RoundTrip_PreservesMutation()
    {
        SettingsRequest request = new(
            SettingsIpcProtocol.Version,
            SettingsIpcProtocol.SaveAction,
            "device:1",
            "Teclado externo",
            1234,
            false);

        string json = JsonSerializer.Serialize(request, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        SettingsRequest? restored = JsonSerializer.Deserialize<SettingsRequest>(
            json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.That(restored, Is.EqualTo(request));
    }

    [Test]
    public void Response_RoundTrip_PreservesSnapshot()
    {
        SettingsSnapshot snapshot = new(
            SettingsIpcProtocol.Version,
            [new SettingsDevice("device:1", "Alias", "Detectado", "ID", DateTimeOffset.UnixEpoch, true, false, 1234)],
            [new SettingsLayout(1234, "español (Chile)", "Latin American")]);
        SettingsResponse response = new(true, null, snapshot);

        string json = JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        SettingsResponse? restored = JsonSerializer.Deserialize<SettingsResponse>(
            json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Multiple(() =>
        {
            Assert.That(restored?.Success, Is.True);
            Assert.That(restored?.Snapshot?.Devices.Single().DisplayName, Is.EqualTo("Alias"));
            Assert.That(restored?.Snapshot?.Layouts.Single().Name, Is.EqualTo("español (Chile) / Latin American"));
        });
    }
}
