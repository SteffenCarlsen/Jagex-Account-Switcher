using System.Collections.Generic;
using JagexAccountSwitcher.Model;

namespace JagexAccountSwitcher.Converters;

using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(UserSettings))]
[JsonSerializable(typeof(RunescapeAccount))]
[JsonSerializable(typeof(List<RunescapeAccount>))]
public partial class AppJsonContext : JsonSerializerContext
{
}