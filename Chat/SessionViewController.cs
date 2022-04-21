using CoreGraphics;
using FM.LiveSwitch;
using Foundation;
using System;
using System.Collections.Generic;
using System.Drawing;
using UIKit;

namespace Chat
{
    public partial class SessionViewController : UIViewController
    {
        //private int _LandscapeGridBump; // Used to make small grid adjustments when we rotate to landscape.

        // Used to store the UI grid's initial X value (see OnViewDiDLayoutSubviews).
        // We then use this value for laying out subviews that we want to keep the size the same for in any orientation.
        private double _InitialGrid_X = 0;

        private static string[] _Names = {
            "Aurora",
            "Argus",
            "Baker",
            "Blackrock",
            "Caledonia",
            "Coquitlam",
            "Doom",
            "Dieppe",
            "Eon",
            "Elkhorn",
            "Fairweather",
            "Finlayson",
            "Garibaldi",
            "Grouse",
            "Hoodoo",
            "Helmer",
            "Isabelle",
            "Itcha",
            "Jackpass",
            "Josephine",
            "Klinkit",
            "King Albert",
            "Lilliput",
            "Lyall",
            "Mallard",
            "Douglas",
            "Nahlin",
            "Normandy",
            "Omega",
            "One Eye",
            "Pukeashun",
            "Plinth",
            "Quadra",
            "Quartz",
            "Razerback",
            "Raleigh",
            "Sky Pilot",
            "Swannell",
            "Tatlow",
            "Thomlinson",
            "Unnecessary",
            "Upright",
            "Vista",
            "Vedder",
            "Whistler",
            "Washington",
            "Yeda",
            "Yellowhead",
            "Zoa"
    };

        public SessionViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewWillAppear(bool animated)
        {
            NavigationController.NavigationBar.Hidden = true;

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
            textChannelID.Text = new FM.LiveSwitch.Randomizer().Next(100000, 999999).ToString();

            textName.Text = _Names[new FM.LiveSwitch.Randomizer().Next(_Names.Length)];

            buttonJoin.Layer.CornerRadius = 5f;
            buttonJoin.TouchUpInside += Button_Click;

            textChannelID.ShouldReturn += (textField) =>
            {
                textChannelID.ResignFirstResponder();
                return true;
            };

            textName.ShouldReturn += (textField) =>
            {
                textName.ResignFirstResponder();
                return true;
            };

            buttonSend.Layer.CornerRadius = 5f;

            buttonSend.TouchUpInside += ButtonSend_TouchUpInside;
            textLog.Text = "";
            VideoStart.TouchUpInside += VideoStart_TouchUpInside;
            AudioStart.TouchUpInside += AudioStart_TouchUpInside;

            App.Instance.MessageReceived += Instance_MessageReceived;
            App.Instance.PeerJoined += Instance_PeerJoined;
            App.Instance.PeerLeft += Instance_PeerLeft;

            App.Instance.ClientRegistered += Instance_ClientRegistered;
            App.Instance.ClientUnregistered += Instance_ClientUnregistered;

            textSend.ShouldReturn += (textField) => {
                textSend.ResignFirstResponder();
                return true;
            };

            //ModePickerViewModel model = new ModePickerViewModel(new List<string>(Enum.GetNames(typeof(App.Modes))));
            //modePicker.Model = model;
            //modePicker.ShowSelectionIndicator = true;
            //model.ValueChanged += (sender, e) =>
            //{
            //    if (model.SelectedIndex == 2)
            //    {
            //        switchSimulcast.On = false;
            //        switchSimulcast.Enabled = false;
            //        switchAudioOnly.Enabled = true;
            //        switchReceiveOnly.Enabled = true;
            //    }
            //    else
            //    {
            //        switchSimulcast.Enabled = true;
            //    }
            // };
            // App.Instance.Mode = (App.Modes)3;
            App.Instance.JoinAsync()
                .Then((p) =>
               {
                   //var tapGestureRecognizer = new UITapGestureRecognizer((gesture) =>
                   // {
                   //     // App.Instance.UseNextVideoDevice();
                   // });
                   //tapGestureRecognizer.NumberOfTapsRequired = 2; // double-tap
                   //this.View.AddGestureRecognizer(tapGestureRecognizer);
               })
                  .Fail((p) =>
                     {
                         textLog.Text = "Cannot start local media";
                         FM.LiveSwitch.Log.Error("Cannot start local media", p);
                     });

           

            //if (!App.Instance.isRegistered)
            //{
            //    textLog.Text = "failed";
            //    FM.LiveSwitch.Log.Debug(String.Format("Disabling the sendbtn/textsend so that a message cannot be sent before registration is complete."));
            //    EnableChatUI(false);
            //}
            App.Instance.TextUILoaded = true;
            App.Instance.EmptyMessagesQueue();
        }

        private void AudioStart_TouchUpInside(object sender, EventArgs e)
        {
            //foreach (var remoteClientInfo in _Channel.RemoteClientInfos)
            //{
            //    OpenPeerOfferConnection(remoteClientInfo);
            //}
            LocalScreenMedia localScreenMedia = new LocalScreenMedia(false, true, false, null);
            this.View = (UIKit.UIView)localScreenMedia.View;
            //_LocalMedia = localScreenMedia;
            //foreach (var encoding in _LocalMedia.VideoEncodings)
            //{
            //    Log.Debug("Local encoding: " + encoding);
            //}
        }

        private void VideoStart_TouchUpInside(object sender, EventArgs e)
        {
            App.Instance.StartLocalMedia(this.View)
                  .Then((p) =>
                  {
                      if (!App.Instance.EnableScreenShare)
                      {
                          var tapGestureRecognizer = new UITapGestureRecognizer((gesture) =>
                          {
                              App.Instance.UseNextVideoDevice();
                          });
                          tapGestureRecognizer.NumberOfTapsRequired = 2; // double-tap
                          this.View.AddGestureRecognizer(tapGestureRecognizer);
                      }

                      return App.Instance.JoinAsync();
                  })
              .Fail((p) =>
              {
                  FM.LiveSwitch.Log.Error("Cannot start local media", p);
              });
        }

        private void ButtonSend_TouchUpInside(object sender, EventArgs e)
        {
            string text = textSend.Text;
            textSend.Text = string.Empty;
            textSend.ResignFirstResponder();

            if (!string.IsNullOrWhiteSpace(text))
            {
                App.Instance.WriteLine(text);
            }
        }

        private void Instance_PeerLeft(string p)
        {
            textLog.InvokeOnMainThread(() =>
            {
                textLog.Text = textLog.Text + string.Format("{0} has left.\n", p);
            });
        }

        private void Instance_MessageReceived(object sender, MessageReceivedArgs e)
        {
            WriteMessage(e.Name, e.Message);
        }

        private void Instance_PeerJoined(string p)
        {
            textLog.InvokeOnMainThread(() =>
            {
                textLog.Text = textLog.Text + string.Format("{0} has joined.\n", p);
            });
        }
    
        private void WriteMessage(string name, string message)
        {
            textLog.InvokeOnMainThread(() =>
            {
                textLog.Text = textLog.Text + string.Format("{0}: {1}\n", name, message);
            });
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

        private void Button_Click(object sender, EventArgs e)
        {
            App.Instance.ChannelId = textChannelID.Text;
            App.Instance.Name = textName.Text;
            if (!App.Instance.isRegistered)
            {
                textLog.Text = "failed";
                FM.LiveSwitch.Log.Debug(String.Format("Disabling the sendbtn/textsend so that a message cannot be sent before registration is complete."));
                EnableChatUI(false);
            }
            else { EnableChatUI(true); }

        }
        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            CGRect ScreenBounds = UIScreen.MainScreen.Bounds;

            // Programmatic layout: our grid is 30x42
            double grid_x = ScreenBounds.Width / 30;
            double grid_y = ScreenBounds.Height / 42;

            if (_InitialGrid_X == 0)
                _InitialGrid_X = grid_x; // Init once.

            // FM Logo


            // LiveSwitch Logo


            // Name
            labelName.Frame = new CGRect(grid_x * 1, grid_y * 9, grid_x * 9, grid_y * 3);
            labelName.Text = "Name";
            labelName.TextAlignment = UITextAlignment.Right;

            textName.Frame = new CGRect(grid_x * 11, grid_y * 9, grid_x * 18, grid_y * 3);
            textName.Placeholder = "Name";
            Add(textName);

            // Channel ID
            labelChannelID.Frame = new CGRect(grid_x * 1, grid_y * 12.5, grid_x * 9, grid_y * 3);
            labelChannelID.Text = "Channel ID";
            labelChannelID.TextAlignment = UITextAlignment.Right;

            textChannelID.Frame = new CGRect(grid_x * 11, grid_y * 12.5, grid_x * 18, grid_y * 3);
            textChannelID.Placeholder = "123456";
            textChannelID.KeyboardType = UIKeyboardType.Default;

            // Join row.
            buttonJoin.Frame = new CGRect(grid_x * 11, grid_y * 15, grid_x * 10, grid_y * 4);

            textSend.Frame = new CGRect(grid_x * 2, grid_y * 20, grid_x * 20, grid_y * 4); //
         //new CGRect(grid_x * 1 /* x */, grid_y * 7 /* y */, grid_x * 21 /* width */, grid_y * 4 /* height */);
            textSend.Placeholder = "Message";
            textSend.Font = UIFont.SystemFontOfSize(14f);

            buttonSend.Frame = new CGRect(grid_x * 20, grid_y * 20, grid_x * 6, grid_y * 4);

            textLog.Frame = new CGRect(grid_x * 1, grid_y * 29, grid_x * 28, grid_y * 29);
            textLog.Editable = false;
            textLog.Layer.BorderColor = new CGColor(0.8f, 0.8f, 0.8f, 1);
            textLog.Layer.BorderWidth = 1;
            textLog.Layer.CornerRadius = 5f;

            // Audio Only label/Mode label Only row
            //labelAudioOnly.Frame = new CGRect(grid_x * 1, grid_y * 16, grid_x * 9, grid_y * 3);
            //labelAudioOnly.Text = "Audio Only";
            //labelAudioOnly.TextAlignment = UITextAlignment.Center;

            //labelReceiveOnly.Frame = new CGRect(grid_x * 10, grid_y * 16, grid_x * 9, grid_y * 3);
            //labelReceiveOnly.Text = "Receive Only";
            //labelReceiveOnly.TextAlignment = UITextAlignment.Center;

            //labelScreenShare.Frame = new CGRect(grid_x * 1, grid_y * 22, grid_x * 9, grid_y * 3);
            //labelScreenShare.Text = "Screen Share";
            //labelScreenShare.TextAlignment = UITextAlignment.Center;

            //labelSimulcast.Frame = new CGRect(grid_x * 10, grid_y * 22, grid_x * 9, grid_y * 3);
            //labelSimulcast.Text = "Simulcast";
            //labelSimulcast.TextAlignment = UITextAlignment.Center;

            //labelMode.Frame = new CGRect(grid_x * 20, grid_y * 18, grid_x * 9, grid_y * 3);
            //labelMode.Text = "Mode";
            //labelMode.TextAlignment = UITextAlignment.Center;

            //// Switches and picker row. Centred under labels.
            //switchAudioOnly.Frame = new CGRect(labelAudioOnly.Frame.X + (labelAudioOnly.Frame.Width / 2) - (switchAudioOnly.Frame.Width / 2), grid_y * 19, 0, 0);
            //switchAudioOnly.HorizontalAlignment = UIControlContentHorizontalAlignment.Left;
            //switchAudioOnly.ValueChanged += AudioOnlyValueChanged;

            //switchReceiveOnly.Frame = new CGRect(labelReceiveOnly.Frame.X + (labelReceiveOnly.Frame.Width / 2) - (switchReceiveOnly.Frame.Width / 2), grid_y * 19, 0, 0);
            //switchReceiveOnly.HorizontalAlignment = UIControlContentHorizontalAlignment.Left;
            //switchReceiveOnly.ValueChanged += ReceiveOnlyValueChanged;

            //switchScreenShare.Frame = new CGRect(labelScreenShare.Frame.X + (labelScreenShare.Frame.Width / 2) - (switchScreenShare.Frame.Width / 2), grid_y * 25, 0, 0);
            //switchScreenShare.HorizontalAlignment = UIControlContentHorizontalAlignment.Left;

            //switchSimulcast.Frame = new CGRect(labelSimulcast.Frame.X + (labelSimulcast.Frame.Width / 2) - (switchSimulcast.Frame.Width / 2), grid_y * 25, 0, 0);
            //switchSimulcast.HorizontalAlignment = UIControlContentHorizontalAlignment.Left;
            //switchSimulcast.ValueChanged += SimulcastValueChanged;

            //// The `modePickerWidth` stays the same in any orientation.
            //// This way we do not run into issues with the labels for each item of the picker being off centre because the picker's view was resized.
            //var modePickerWidth = _InitialGrid_X * 9;
            //modePicker.Frame = new CGRect(labelMode.Frame.X + (labelMode.Frame.Width / 2) - (modePickerWidth / 2), grid_y * 20, modePickerWidth, (grid_y * 4) + (_LandscapeGridBump * 2));

            // Join row.
            // buttonJoin.Frame = new CGRect(grid_x * 19, grid_y * 28, grid_x * 10, grid_y * 4);

            // Phone image and label
            //var socialIconWidth = 25;
            //var socialIconHeight = socialIconWidth;
            //imagePhone.Frame = new CGRect(grid_x * 1, grid_y * 35.5 - _LandscapeGridBump, socialIconWidth, socialIconHeight);
            //imagePhone.ContentMode = UIViewContentMode.ScaleAspectFit; // Necessary so that the image fits within the frame.

            //labelPhone.Frame = new CGRect(imagePhone.Frame.X + socialIconWidth + (socialIconWidth / 2), imagePhone.Frame.Y + (socialIconHeight / 3), grid_x * 16, grid_y * 1);
            //labelPhone.Font = UIFont.SystemFontOfSize(10f);
            //labelPhone.HorizontalAlignment = UIControlContentHorizontalAlignment.Left;

            //// Email image and label
            //imageEmail.Frame = new CGRect(grid_x * 1, grid_y * 38, socialIconWidth, socialIconHeight);
            //imageEmail.ContentMode = UIViewContentMode.ScaleAspectFit; // Necessary so that the image fits within the frame.

            //labelEmail.Frame = new CGRect(imageEmail.Frame.X + socialIconWidth + (socialIconWidth / 2), imageEmail.Frame.Y + (socialIconHeight / 3), grid_x * 16, grid_y * 1);
            //labelEmail.Font = UIFont.SystemFontOfSize(10f);
            //labelEmail.HorizontalAlignment = UIControlContentHorizontalAlignment.Left;

            //// Social media images
            //imageTwitter.Frame = new CGRect(grid_x * 21, imageEmail.Frame.Y, socialIconWidth, socialIconHeight);
            //imageTwitter.ContentMode = UIViewContentMode.ScaleAspectFit; // Necessary so that the image fits within the frame.

            //imageFacebook.Frame = new CGRect(grid_x * 24, imageEmail.Frame.Y, socialIconWidth, socialIconHeight);
            //imageFacebook.ContentMode = UIViewContentMode.ScaleAspectFit; // Necessary so that the image fits within the frame.

            //imageLinkedIn.Frame = new CGRect(grid_x * 27, imageEmail.Frame.Y, socialIconWidth, socialIconHeight);
            //imageLinkedIn.ContentMode = UIViewContentMode.ScaleAspectFit; // Necessary so that the image fits within the frame.

            //CGRect ScreenBounds = UIScreen.MainScreen.Bounds;
            //double grid_x = ScreenBounds.Width / 30;
            //double grid_y = ScreenBounds.Height / 42;


        }

        public override void WillRotate(UIInterfaceOrientation toInterfaceOrientation, double duration)
        {
            // Set the `LandscapeGridBump` depending on toInterfaceOrientation.
            // When ViewDidLayoutSubviews gets called this will serve to tweak layout as needed.
            if (toInterfaceOrientation == UIInterfaceOrientation.LandscapeRight || toInterfaceOrientation == UIInterfaceOrientation.LandscapeLeft)
            {
                //_LandscapeGridBump = 10;
            }
            else
            {
               // _LandscapeGridBump = 0;
            }
        }

        //public override bool ShouldPerformSegue(string segueIdentifier, NSObject sender)
        //{
        //    //if (segueIdentifier == "SegueToChat")
        //    //{
        //    //    App.Instance.ChannelId = textChannelID.Text;
        //    //    App.Instance.Name = textName.Text;
        //    //    //            App.Instance.EnableScreenShare = switchScreenShare.On;
        //    //    //App.Instance.AudioOnly = switchAudioOnly.On;
        //    //    //App.Instance.ReceiveOnly = switchReceiveOnly.On;
        //    //    //            App.Instance.Simulcast = switchSimulcast.On;
        //    //    App.Instance.Mode = App.Modes.Peer;
        //    //}
        //    //return base.ShouldPerformSegue(segueIdentifier, sender);
        //}

        private void AudioOnlyValueChanged(object sender, EventArgs e)
        {
            if (switchAudioOnly.On)
            {
                switchSimulcast.On = false;
                switchSimulcast.Enabled = false;
            }
            else if (!switchReceiveOnly.On)
            {
                switchSimulcast.Enabled = true;
            }
        }

        private void ReceiveOnlyValueChanged(object sender, EventArgs e)
        {
            if (switchReceiveOnly.On)
            {
                switchSimulcast.On = false;
                switchSimulcast.Enabled = false;
            }
            else if (!switchAudioOnly.On)
            {
                switchSimulcast.Enabled = true;
            }
        }

        //private void SimulcastValueChanged(object sender, EventArgs e)
        //{
        //    if (switchSimulcast.On)
        //    {
        //        switchAudioOnly.On = false;
        //        switchReceiveOnly.On = false;
        //        switchAudioOnly.Enabled = false;
        //        switchReceiveOnly.Enabled = false;
        //    }
        //    else
        //    {
        //        switchAudioOnly.Enabled = true;
        //        switchReceiveOnly.Enabled = true;
        //    }
        //}

        #region video
        public string muteAudioTitle = "Mute Audio";
        public string muteVideoTitle = "Mute Video";
        public string disableAudioTitle = "Disable Audio";
        public string disableVideoTitle = "Disable Video";
        public void ShowLocalContextMenu()
        {
            var alertController = UIAlertController.Create("Local", null, UIAlertControllerStyle.Alert);
            var muteAudio = UIAlertAction.Create(muteAudioTitle, UIAlertActionStyle.Default, alerAction =>
            { App.Instance.ToggleMuteAudio(); });
            var muteVideo = UIAlertAction.Create(muteVideoTitle, UIAlertActionStyle.Default, alerAction =>
            { App.Instance.ToggleMuteVideo(); });
            var disableAudio = UIAlertAction.Create(disableAudioTitle, UIAlertActionStyle.Default, alerAction =>
            { App.Instance.ToggleLocalDisableAudio(); });
            var disableVideo = UIAlertAction.Create(disableVideoTitle, UIAlertActionStyle.Default, alerAction =>
            { App.Instance.ToggleLocalDisableVideo(); });
            var encoding = UIAlertAction.Create("Send Encoding", UIAlertActionStyle.Default, alertAction =>
            { ShowEncodings(null); });
            alertController.AddAction(muteAudio);
            alertController.AddAction(muteVideo);
            alertController.AddAction(disableAudio);
            alertController.AddAction(disableVideo);
            if (App.Instance.encodings != null && App.Instance.encodings.Count > 1)
            {
                alertController.AddAction(encoding);
            }
            var cancel = UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, null);
            alertController.AddAction(cancel);
            this.PresentViewController(alertController, true, null);
        }   

        public void ShowRemoteContextMenu(string id)
        {
            var alertController = UIAlertController.Create("Remote", null, UIAlertControllerStyle.Alert);
            var disableAudio = UIAlertAction.Create(App.Instance._RemoteAVMaps[id + "DisableAudio"] ? "Enable Audio" : "Disable Audio",
                UIAlertActionStyle.Default, alerAction => { App.Instance.ToggleRemoteDisableAudio(id); });
            var disableVideo = UIAlertAction.Create(App.Instance._RemoteAVMaps[id + "DisableVideo"] ? "Enable Video" : "Disable Video",
                UIAlertActionStyle.Default, alerAction => { App.Instance.ToggleRemoteDisableVideo(id); });
            var encoding = UIAlertAction.Create("Receive Encoding", UIAlertActionStyle.Default, alertAction =>
            { ShowEncodings(id); });
            alertController.AddAction(disableAudio);
            alertController.AddAction(disableVideo);

            int encodingCount = 0;
            foreach (var key in App.Instance._RemoteEncodingMaps.Keys)
            {
                if (key.Contains(id)) encodingCount++;
            }

            if (encodingCount > 1)
            {
                alertController.AddAction(encoding);
            }

            var cancel = UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, null);
            alertController.AddAction(cancel);
            this.PresentViewController(alertController, true, null);
        }

        private void ShowEncodings(string id)
        {
            UIAlertController alertController;
            UIAlertAction okAction;

            var tableViewController = new UITableViewController();

            if (id == null)
            {
                alertController = UIAlertController.Create("Send Encoding", null, UIAlertControllerStyle.Alert);
                okAction = UIAlertAction.Create("OK", UIAlertActionStyle.Default, alertAction =>
                { App.Instance.ToggleSendEncoding(); });

                tableViewController.PreferredContentSize = new CGSize(width: 272, height: 44 * App.Instance.encodings.Count);
                tableViewController.TableView.Source = new TableViewSource(App.Instance.encodings);
            }
            else
            {
                alertController = UIAlertController.Create("Receive Encoding", null, UIAlertControllerStyle.Alert);
                okAction = UIAlertAction.Create("OK", UIAlertActionStyle.Default, alertAction =>
                { App.Instance.ToggleReceiveEncoding(id); });

                List<string> encodings = new List<string>();
                foreach (var key in App.Instance._RemoteEncodingMaps.Keys)
                {
                    if (key.Contains(id))
                    {
                        encodings.Add(key.Replace(id, ""));
                    }
                }

                tableViewController.PreferredContentSize = new CGSize(width: 272, height: 44 * encodings.Count);
                tableViewController.TableView.Source = new TableViewSource(encodings);
            }
            alertController.AddAction(okAction);

            tableViewController.TableView.BackgroundColor = UIColor.Clear;
            tableViewController.TableView.SeparatorColor = UIColor.Clear;

            tableViewController.TableView.AllowsMultipleSelection = id == null ? true : false;
            alertController.SetValueForKey(tableViewController, new NSString("contentViewController"));
            this.PresentViewController(alertController, true, null);
        }
        #endregion
    }
    public class ModePickerViewModel : UIPickerViewModel
    {
        private List<string> _myItems;
        protected int selectedIndex = 0;

        public ModePickerViewModel(List<string> items)
        {
            _myItems = items;
        }

        public event EventHandler<EventArgs> ValueChanged;

        public string SelectedItem
        {
            get { return _myItems[selectedIndex]; }
        }

        public int SelectedIndex
        {
            get { return selectedIndex; }
        }

        public override nint GetComponentCount(UIPickerView picker)
        {
            return 1;
        }

        public override nint GetRowsInComponent(UIPickerView picker, nint component)
        {
            return _myItems.Count;
        }

        public override string GetTitle(UIPickerView picker, nint row, nint component)
        {
            return _myItems[(int)row];
        }

        public override void Selected(UIPickerView picker, nint row, nint component)
        {
            selectedIndex = (int)row;
            if (ValueChanged != null)
            {
                ValueChanged(this, new EventArgs());
            }
        }

        public override UIView GetView(UIPickerView pickerView, nint row, nint component, UIView view)
        {

            UILabel lbl = new UILabel(new RectangleF(0, 0, 130f, 40f));
            lbl.TextColor = UIColor.Black;
            lbl.Font = UIFont.SystemFontOfSize(18f);
            lbl.TextAlignment = UITextAlignment.Center;
            lbl.Text = _myItems[(int)row];

            return lbl;
        }
    }
}
