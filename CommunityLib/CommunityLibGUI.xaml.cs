namespace CommunityLib
{
    /// <summary>
    /// Interaction logic for CommunityLibGUI.xaml
    /// </summary>
    public partial class CommunityLibGUI
    {
        public CommunityLibGUI()
        {
            InitializeComponent();
            DataContext = CommunityLibSettings.Instance;
        }
    }
}
