// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;

namespace Chat
{
    [Register ("SessionViewController")]
    partial class SessionViewController
    {
        [Outlet]
        UIKit.UIButton buttonJoin { get; set; }
        [Outlet]
        UIKit.UIButton buttonSend { get; set; }

        [Outlet]
        UIKit.UIImageView imageEmail { get; set; }


        [Outlet]
        UIKit.UIButton imageFacebook { get; set; }


        [Outlet]
        UIKit.UIButton imageLinkedIn { get; set; }


        [Outlet]
        UIKit.UIImageView imagePhone { get; set; }


        [Outlet]
        UIKit.UIButton imageTwitter { get; set; }


        [Outlet]
        UIKit.UIButton labelEmail { get; set; }


        [Outlet]
        UIKit.UIButton labelPhone { get; set; }


        [Outlet]
        UIKit.UIImageView Logo { get; set; }


        [Outlet]
        UIKit.UIImageView liveswitchLogo { get; set; }


        [Outlet]
        UIKit.UISwitch switchAudioOnly { get; set; }


        [Outlet]
        UIKit.UISwitch switchReceiveOnly { get; set; }

        [Outlet]
        UIKit.UISwitch switchSimulcast { get; set; }

        [Outlet]
        UIKit.UITextView textLog { get; set; }
        [Outlet]
        UIKit.UITextField textName { get; set; }


        [Outlet]
        UIKit.UITextField textSend { get; set; }
        [Outlet]
        UIKit.UIButton VideoStart { get; set; }
        [Outlet]
        UIKit.UIButton AudioStart { get; set; }
        [Outlet]
        UIKit.UITextField textChannelID { get; set; }


        [Outlet]
        UIKit.UILabel labelChannelID { get; set; }


        [Outlet]
        UIKit.UILabel labelName { get; set; }


        [Outlet]
        UIKit.UILabel labelMode { get; set; }


        [Outlet]
        UIKit.UIPickerView modePicker { get; set; }


        [Outlet]
        UIKit.UILabel labelAudioOnly { get; set; }


        [Outlet]
        UIKit.UILabel labelReceiveOnly { get; set; }

        [Outlet]
        UIKit.UILabel labelSimulcast { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel labelScreenShare { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISwitch switchScreenShare { get; set; }

        void ReleaseDesignerOutlets ()
        {
            if (labelScreenShare != null) {
                labelScreenShare.Dispose ();
                labelScreenShare = null;
            }

            if (switchScreenShare != null) {
                switchScreenShare.Dispose ();
                switchScreenShare = null;
            }
        }
    }
}