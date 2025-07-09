#region

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using JagexAccountSwitcher.Model;

#endregion

namespace JagexAccountSwitcher.Services;

public class GroupService
{
    private readonly string _groupsFilePath;
    private List<AccountGroup> _groups = new();

    public GroupService(string configDirectory)
    {
        _groupsFilePath = Path.Combine(configDirectory, "groups.json");
        LoadGroups();
    }

    public IReadOnlyList<AccountGroup> Groups => _groups.AsReadOnly();

    public void LoadGroups()
    {
        if (File.Exists(_groupsFilePath))
        {
            var json = File.ReadAllText(_groupsFilePath);
            _groups = JsonSerializer.Deserialize<List<AccountGroup>>(json) ?? new List<AccountGroup>();
        }
    }

    public void SaveGroups()
    {
        var json = JsonSerializer.Serialize(_groups);
        File.WriteAllText(_groupsFilePath, json);
    }

    public void AddGroup(AccountGroup group)
    {
        _groups.Add(group);
        SaveGroups();
    }

    public void UpdateGroup(AccountGroup group)
    {
        var index = _groups.FindIndex(g => g.Id == group.Id);
        if (index >= 0)
        {
            _groups[index] = group;
            SaveGroups();
        }
    }

    public void DeleteGroup(string groupId)
    {
        _groups.RemoveAll(g => g.Id == groupId);
        SaveGroups();
    }
}