using System;
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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GameTracking
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            DataContext = this;

            InitializeComponent();

            Sheets = new SheetsAccess();
        }
        
        public SheetsAccess Sheets
        {
            get { return (SheetsAccess)GetValue(SheetsProperty); }
            set { SetValue(SheetsProperty, value); }
        }
        public static DependencyProperty SheetsProperty = DependencyProperty.Register("Sheets", typeof(SheetsAccess), typeof(MainWindow), null);
        
    }
}
