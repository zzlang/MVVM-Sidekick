﻿using MVVMSidekick.Views;
using Samples.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Samples
{
    public partial class MainPage : MVVMControl
    {
        public MainPage():base(null)
        {
            InitializeComponent();
        }

        public MainPage(Index_Model model)
            : base(model)
        {
            InitializeComponent();
        }
    }
}
