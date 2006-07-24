using MediaPortal;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;


namespace MediaPortal
{
    public partial class Welcome : Page
    {
        DoubleAnimation _fadeOut;
        public Welcome(ResourceDictionary dict)
        {
            InitializeComponent();
            this.Resources = dict;
            this.MouseDown += new System.Windows.Input.MouseButtonEventHandler(Welcome_MouseDown);
        }

        void Welcome_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _fadeOut = new DoubleAnimation(0.0f, new Duration(new TimeSpan(0, 0, 0, 0, 500)));
            _fadeOut.Completed += new EventHandler(_fadeOut_Completed);
            this.BeginAnimation(Page.OpacityProperty, _fadeOut);
          
        }

        void _fadeOut_Completed(object sender, EventArgs e)
        {
            Core coreWindow = (Core)Application.Current.MainWindow; 
            if (coreWindow != null)
                coreWindow.LoadHome();
            
        }

     }
}