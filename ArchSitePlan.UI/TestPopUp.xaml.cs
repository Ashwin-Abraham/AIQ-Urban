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
using System.Windows.Shapes;

namespace ArchSitePlan.UI
{
    /// <summary>
    /// Interaction logic for TestPopUp.xaml
    /// </summary>
    public partial class TestPopUp : Window
    {
        public PopUpViewModel ViewModel { get; set; }
        public TestPopUp()
        {
            InitializeComponent();
            ViewModel = new PopUpViewModel();
        }
    }
}
