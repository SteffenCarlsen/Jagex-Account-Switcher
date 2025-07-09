#region

using System.Collections.Generic;
using System.Text.Json.Serialization;
using JagexAccountSwitcher.Model;

#endregion

namespace JagexAccountSwitcher.Converters;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(UserSettings))]
[JsonSerializable(typeof(RunescapeAccount))]
[JsonSerializable(typeof(List<RunescapeAccount>))]
public partial class AppJsonContext : JsonSerializerContext
{
}