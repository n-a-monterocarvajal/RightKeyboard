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
        SettingsResponse response = new(true, null, snapshot, new SettingsActivity(7, "device:1"));

        string json = JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        SettingsResponse? restored = JsonSerializer.Deserialize<SettingsResponse>(
            json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Multiple(() =>
        {
            Assert.That(restored?.Success, Is.True);
            Assert.That(restored?.Snapshot?.Devices.Single().DisplayName, Is.EqualTo("Alias"));
            Assert.That(restored?.Snapshot?.Layouts.Single().Name, Is.EqualTo("español (Chile) / Latin American"));
            Assert.That(restored?.Activity, Is.EqualTo(new SettingsActivity(7, "device:1")));
        });
    }

    [Test]
    public void Request_RoundTrip_PreservesImportFields()
    {
        SettingsRequest request = new(
            SettingsIpcProtocol.Version,
            SettingsIpcProtocol.ImportApplyAction,
            FilePath: @"C:\ruta con espacios\preferencias.json",
            Replace: true);

        string json = JsonSerializer.Serialize(request, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        SettingsRequest? restored = JsonSerializer.Deserialize<SettingsRequest>(
            json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.That(restored, Is.EqualTo(request));
    }

    [Test]
    public void Response_RoundTrip_PreservesImportPreview()
    {
        SettingsResponse response = new(
            true,
            null,
            null,
            ImportPreview: new SettingsImportPreview(3, ["No se encontró la distribución de 'device:9'."]));

        string json = JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        SettingsResponse? restored = JsonSerializer.Deserialize<SettingsResponse>(
            json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Multiple(() =>
        {
            Assert.That(restored?.ImportPreview?.DeviceCount, Is.EqualTo(3));
            Assert.That(restored?.ImportPreview?.Warnings.Single(), Does.Contain("device:9"));
        });
    }
}
