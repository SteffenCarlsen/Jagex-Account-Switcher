using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JagexAccountSwitcher.Model;

namespace JagexAccountSwitcher.Helpers;

public static class EnumHelper
{
    public static string GetLaunchParamterFromRamLimitation(RamLimitationEnum ramLimitation)
    {
        return ramLimitation switch
        {
            RamLimitationEnum.OneGigabyte => "-Xmx1G",
            RamLimitationEnum.TwoGigabytes => "-Xmx2G",
            RamLimitationEnum.ThreeGigabytes => "-Xmx3G",
            RamLimitationEnum.FourGigabytes => "-Xmx4G",
            _ => string.Empty
        };
    }
    public static IEnumerable<EnumDescriptionItem<T>> GetEnumValuesWithDescriptions<T>() where T : struct, Enum
    {
        return Enum.GetValues<T>().Select(value => new EnumDescriptionItem<T>
        {
            Value = value,
            Description = GetEnumDescription(value)
        });
    }
    public static EnumDescriptionItem<T> GetEnumValueWithDescription<T>(T value) where T : Enum
    {
        return new EnumDescriptionItem<T>
        {
            Value = value,
            Description = GetEnumDescription(value)
        };
    }

    public static string GetEnumDescription<T>(T value) where T : Enum
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = field?.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
        return attribute?.Description ?? value.ToString();
    }
    
    public static T GetEnumValueFromDescription<T>(string description) where T : struct, Enum
    {
        return Enum.GetValues<T>().FirstOrDefault(value => GetEnumDescription(value).Equals(description, StringComparison.OrdinalIgnoreCase));
    }

    public class EnumDescriptionItem<T> where T : Enum
    {
        public T Value { get; set; }
        public string Description { get; set; }

        public override string ToString()
        {
            return Description;
        }
    }

    public static int GetEnumIndex<T>(T selectedAccountRamLimitation)
    {
        var enumValues = Enum.GetValues(typeof(T)).Cast<T>().ToList();
        return enumValues.IndexOf(selectedAccountRamLimitation);
    }
}