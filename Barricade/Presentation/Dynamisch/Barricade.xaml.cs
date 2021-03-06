﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Barricade.Presentation.Dynamisch
{
    /// <summary>
    /// Interaction logic for Barricade.xaml
    /// </summary>
    public partial class Barricade : UserControl
    {
        public Barricade(Logic.Barricade barricade)
        {
            InitializeComponent();
        }

        public void Beweeg(Point target)
        {
            var thickness = new Thickness(target.X, target.Y, 0, 0);
            var moveAnimation = new ThicknessAnimation(Margin, thickness, TimeSpan.FromMilliseconds(500))
                {
                    FillBehavior = FillBehavior.Stop
                };
            moveAnimation.Completed += (sender, args) => Margin = thickness;
            BeginAnimation(MarginProperty, moveAnimation);
        }
    }
}
