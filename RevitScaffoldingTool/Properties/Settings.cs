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
        [DefaultSettingValue("True")]
        public bool IsFromData
        {
            get => (bool)this[nameof(IsFromData)];
            set => this[nameof(IsFromData)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("1800,1800")]
        public string ButtonL0
        {
            get => (string)this[nameof(ButtonL0)];
            set => this[nameof(ButtonL0)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("1800,1200")]
        public string ButtonL1
        {
            get => (string)this[nameof(ButtonL1)];
            set => this[nameof(ButtonL1)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("1800,1800")]
        public string ButtonL2
        {
            get => (string)this[nameof(ButtonL2)];
            set => this[nameof(ButtonL2)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("1800,1200")]
        public string ButtonL3
        {
            get => (string)this[nameof(ButtonL3)];
            set => this[nameof(ButtonL3)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("1800,1800")]
        public string ButtonL4
        {
            get => (string)this[nameof(ButtonL4)];
            set => this[nameof(ButtonL4)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("1800,2x1800")]
        public string ButtonL5
        {
            get => (string)this[nameof(ButtonL5)];
            set => this[nameof(ButtonL5)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("1800,2x1800")]
        public string ButtonL6
        {
            get => (string)this[nameof(ButtonL6)];
            set => this[nameof(ButtonL6)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("1200")]
        public double ButtonW
        {
            get => (double)this[nameof(ButtonW)];
            set => this[nameof(ButtonW)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("True")]
        public bool CheckCase11
        {
            get => (bool)this[nameof(CheckCase11)];
            set => this[nameof(CheckCase11)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("True")]
        public bool CheckCase12
        {
            get => (bool)this[nameof(CheckCase12)];
            set => this[nameof(CheckCase12)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("True")]
        public bool CheckCase13
        {
            get => (bool)this[nameof(CheckCase13)];
            set => this[nameof(CheckCase13)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("True")]
        public bool CheckCase14
        {
            get => (bool)this[nameof(CheckCase14)];
            set => this[nameof(CheckCase14)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("True")]
        public bool CheckCase21
        {
            get => (bool)this[nameof(CheckCase21)];
            set => this[nameof(CheckCase21)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("True")]
        public bool CheckCase22
        {
            get => (bool)this[nameof(CheckCase22)];
            set => this[nameof(CheckCase22)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("True")]
        public bool CheckCase31
        {
            get => (bool)this[nameof(CheckCase31)];
            set => this[nameof(CheckCase31)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("True")]
        public bool CheckCase32
        {
            get => (bool)this[nameof(CheckCase32)];
            set => this[nameof(CheckCase32)] = value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("True")]
        public bool CheckCase33
        {
            get => (bool)this[nameof(CheckCase33)];
            set => this[nameof(CheckCase33)] = value;
        }
    }
}
