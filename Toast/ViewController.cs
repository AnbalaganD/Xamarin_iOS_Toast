using System;

using UIKit;

namespace Toast
{
    public partial class ViewController : UIViewController
    {
        protected ViewController(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        partial void OnShowToastClicked(UIButton sender)
        {
            Toast.Shared.Show(messageTextField.Text);
        }
    }
}
