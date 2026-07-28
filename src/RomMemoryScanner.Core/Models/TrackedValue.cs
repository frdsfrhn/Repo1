using System.Text.Json.Serialization;

namespace RomMemoryScanner.Core.Models;

/// <summary>
/// A single labeled memory location within a game's console-address space (FR-2.5, FR-3.1, §6).
/// Addresses are stored in "console space" (the addresses humans and cheat databases use, e.g.
/// SNES 0x7E0000-0x7FFFFF) — never in host-process space, which is emulator/session-specific.
/// </summary>
public sealed class TrackedValue
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>Console-space address, e.g. 0x7E0DB0 for SNES WRAM.</summary>
    [JsonPropertyName("console_address")]
    public uint ConsoleAddress { get; set; }

    [JsonPropertyName("data_type")]
    public DataType DataType { get; set; } = DataType.U8;

    [JsonPropertyName("byte_order")]
    public ByteOrder ByteOrder { get; set; } = ByteOrder.LittleEndian;

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}
