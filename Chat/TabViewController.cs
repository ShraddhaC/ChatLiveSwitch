using System;
using UIKit;

namespace Chat
{
    public partial class TabViewController :  UITabBarController
	{
		public TabViewController(IntPtr handle) : base(handle)
		{
		}

		public override void ViewWillAppear(bool animated)
		{
			this.NavigationController.NavigationBar.Hidden = false;

			base.ViewWillAppear(animated);
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();
            if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
            {
                var property_OverrideUserInterfaceStyle = typeof(UIViewController).GetProperty("OverrideUserInterfaceStyle");
                var type_UIUserInterfaceStyle = Type.GetType("UIKit.UIUserInterfaceStyle, Xamarin.IOS");
                if (property_OverrideUserInterfaceStyle != null && type_UIUserInterfaceStyle != null)
                {
                    var Enum_UIUserInterfaceStyle = type_UIUserInterfaceStyle.GetEnumValues();
                    //Set to Light
                    property_OverrideUserInterfaceStyle.SetValue(this, Enum_UIUserInterfaceStyle.GetValue(1));
                }
            }
            App.Instance.StartLocalMedia(ViewControllers[0].View)
               .Then((p) =>
                {
                    if (!App.Instance.EnableScreenShare)
                    {
                        var tapGestureRecognizer = new UITapGestureRecognizer((gesture) =>
                        {
                            App.Instance.UseNextVideoDevice();
                        });
                        tapGestureRecognizer.NumberOfTapsRequired = 2; // double-tap
                        ViewControllers[0].View.AddGestureRecognizer(tapGestureRecognizer);
                    }

                    return App.Instance.JoinAsync();
			    })
               .Fail((p) =>
                { 
                    FM.LiveSwitch.Log.Error("Cannot start local media", p);
                }); 
        }

		public override void DidReceiveMemoryWarning()
		{
			base.DidReceiveMemoryWarning();
			// Release any cached data, images, etc that aren't in use.
		}

		public override void ViewWillDisappear(bool animated)
		{
            var p = App.Instance.LeaveAsync();
            if (p != null)
            {
                p.Fail((ex) =>
                {
                    FM.LiveSwitch.Log.Error("Failed to leave conference.", ex);
                });
            }

            App.Instance.StopLocalMedia()
            .Fail((ex) =>
            {
                FM.LiveSwitch.Log.Error("Failed to stop local media.", ex);
            });

            base.ViewWillDisappear(animated);
		}
	}
}
