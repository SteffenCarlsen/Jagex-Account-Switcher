#region

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

#endregion

namespace JagexAccountSwitcher.Model;

public class AccountGroup
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#3498db"; // Default blue color
    public List<string> AccountIds { get; set; } = new();

    [JsonIgnore] public int MemberCount => AccountIds.Count;
}