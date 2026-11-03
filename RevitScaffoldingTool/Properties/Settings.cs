using System.Configuration;

namespace RevitScaffoldingTool.Properties
{
    internal sealed class Settings : ApplicationSettingsBase
    {
        private static readonly Settings DefaultInstance = (Settings)Synchronized(new Settings());

        public static Settings Default => DefaultInstance;

        [UserScopedSetting]
        [DefaultSettingValue("0")]
        public double ElevationZ
        {
            get => (double)this[nameof(ElevationZ)];
            set => this[nameof(ElevationZ)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("250")]
        public double JackHeight
        {
            get => (double)this[nameof(JackHeight)];
            set => this[nameof(JackHeight)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("0")]
        public double Post1Height
        {
            get => (double)this[nameof(Post1Height)];
            set => this[nameof(Post1Height)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("0")]
        public double Post2Height
        {
            get => (double)this[nameof(Post2Height)];
            set => this[nameof(Post2Height)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("0")]
        public int Post3Count
        {
            get => (int)this[nameof(Post3Count)];
            set => this[nameof(Post3Count)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("0")]
        public double Post3Height
        {
            get => (double)this[nameof(Post3Height)];
            set => this[nameof(Post3Height)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("0")]
        public double Post4Height
        {
            get => (double)this[nameof(Post4Height)];
            set => this[nameof(Post4Height)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("0")]
        public double Post5Height
        {
            get => (double)this[nameof(Post5Height)];
            set => this[nameof(Post5Height)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("1")]
        public int StoryCount
        {
            get => (int)this[nameof(StoryCount)];
            set => this[nameof(StoryCount)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("False")]
        public bool HasTopTransom
        {
            get => (bool)this[nameof(HasTopTransom)];
            set => this[nameof(HasTopTransom)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("False")]
        public bool IsFromData
        {
            get => (bool)this[nameof(IsFromData)];
            set => this[nameof(IsFromData)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("1800")]
        public double ButtonL1
        {
            get => (double)this[nameof(ButtonL1)];
            set => this[nameof(ButtonL1)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("1800")]
        public double ButtonL2
        {
            get => (double)this[nameof(ButtonL2)];
            set => this[nameof(ButtonL2)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("1200")]
        public double ButtonW
        {
            get => (double)this[nameof(ButtonW)];
            set => this[nameof(ButtonW)] = value;
        }
    }
}
