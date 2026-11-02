using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RevitScaffoldingTool.ViewModels
{
    public partial class BayRowModel : ObservableObject
    {
        [ObservableProperty] private int _stt;
        [ObservableProperty] private string _elementId = string.Empty;
        [ObservableProperty] private string _bayList = string.Empty;
        [ObservableProperty] private string _connectCase = "TH 1.1";
        [ObservableProperty] private double _length = 0;
        [ObservableProperty] private double _width = 1200;
        [ObservableProperty] private List<double> _bayLengths = new List<double>();

        // Tọa độ hình học và góc xoay hướng tuyến
        [ObservableProperty] private double _startX = 0;
        [ObservableProperty] private double _startY = 0;
        [ObservableProperty] private double _startZ = 0;
        [ObservableProperty] private double _endX = 0;
        [ObservableProperty] private double _endY = 0;
        [ObservableProperty] private double _endZ = 0;
        [ObservableProperty] private double _angleDeg = 0;
    }
}
