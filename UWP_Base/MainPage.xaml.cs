﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Документацию по шаблону элемента "Пустая страница" см. по адресу http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UWP_Base
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            var v = ApplicationView.GetForCurrentView();
            v.FullScreenSystemOverlayMode = FullScreenSystemOverlayMode.Minimal;
            var b = Window.Current.Bounds;
            text.Text = $" Высота и Ширина Приложения {b.Height} {b.Width}";       
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {           
            var b = this.RenderSize;
            text.Text = $" Высота и Ширина Приложения {b.Height} {b.Width}";
        }
    }
}
