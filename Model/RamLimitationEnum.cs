using System.ComponentModel;

namespace JagexAccountSwitcher.Model;

public enum RamLimitationEnum
{
    [Description("Default")]
    Default = 0,
    [Description("1GB")]
    OneGigabyte = 1,
    [Description("2GB")]
    TwoGigabytes = 2,
    [Description("3GB")]
    ThreeGigabytes = 3,
    [Description("4GB")]
    FourGigabytes = 4,
}