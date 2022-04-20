using CoreGraphics;
using System;
using UIKit;

namespace Chat
{
	public partial class TextViewController : UIViewController
	{
        public TextViewController(IntPtr handle) : base(handle)
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
            buttonSend.Layer.CornerRadius = 5f;

			buttonSend.TouchUpInside += Button_Click;
			textLog.Text = "";

			App.Instance.MessageReceived += Instance_MessageReceived;
			App.Instance.PeerJoined += Instance_PeerJoined;
			App.Instance.PeerLeft += Instance_PeerLeft;

            App.Instance.ClientRegistered  += Instance_ClientRegistered;
            App.Instance.ClientUnregistered += Instance_ClientUnregistered;

            textSend.ShouldReturn += (textField) => {
                textSend.ResignFirstResponder();
                return true;
            };

            // If the UI is created before joinAsyc is not fully executed, then we need to make sure that client has been not been registered if we want to disable the UI
            if(!App.Instance.isRegistered)             {
                FM.LiveSwitch.Log.Debug(String.Format("Disabling the sendbtn/textsend so that a message cannot be sent before registration is complete."));
                EnableChatUI(false);
            }
            App.Instance.TextUILoaded = true;
            App.Instance.EmptyMessagesQueue();
        }
        public void Instance_ClientRegistered()
        {
            EnableChatUI(true);
        }

        public void Instance_ClientUnregistered()
        {
            EnableChatUI(false);
            App.Instance.TextUILoaded = false;
        }

        private void EnableChatUI(bool enable)
        {
            textSend.InvokeOnMainThread(() =>
            {
                textSend.UserInteractionEnabled = enable;
            });
            buttonSend.InvokeOnMainThread(() =>
            {
                buttonSend.UserInteractionEnabled = enable;
            });
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            CGRect ScreenBounds = UIScreen.MainScreen.Bounds;

            // Programmatic layout: our grid is 30x42
            double grid_x = ScreenBounds.Width / 30;
            double grid_y = ScreenBounds.Height / 42;

            textSend.Frame = new CGRect(grid_x * 1 /* x */, grid_y * 7 /* y */, grid_x * 21 /* width */, grid_y * 4 /* height */);
            textSend.Placeholder = "Message";
            textSend.Font = UIFont.SystemFontOfSize(14f);

            buttonSend.Frame = new CGRect(grid_x * 23, grid_y * 7, grid_x * 6, grid_y * 4);

            textLog.Frame = new CGRect(grid_x * 1, grid_y * 13, grid_x * 28, grid_y * 23);
            textLog.Editable = false;
            textLog.Layer.BorderColor = new CGColor(0.8f, 0.8f, 0.8f, 1);
            textLog.Layer.BorderWidth = 1;
            textLog.Layer.CornerRadius = 5f;
        }

        private void Instance_PeerLeft(string p)
		{
			textLog.InvokeOnMainThread(() =>
			{
				textLog.Text = textLog.Text + string.Format("{0} has left.\n", p);
			});
		}

		private void Instance_PeerJoined(string p)
		{
			textLog.InvokeOnMainThread(() =>
			{
				textLog.Text = textLog.Text + string.Format("{0} has joined.\n", p);
			});
		}

		private void Instance_MessageReceived(object sender, MessageReceivedArgs e)
		{
			WriteMessage(e.Name, e.Message);
		}

		private void WriteMessage(string name, string message)
		{
			textLog.InvokeOnMainThread(() =>
			{
				textLog.Text = textLog.Text + string.Format("{0}: {1}\n", name, message);
			});
		}

		private void Button_Click(object sender, EventArgs e)
		{
			string text = textSend.Text;
			textSend.Text = string.Empty;
            textSend.ResignFirstResponder();

            if (!string.IsNullOrWhiteSpace(text))
			{
				App.Instance.WriteLine(text);
			}
		}
	}
}
