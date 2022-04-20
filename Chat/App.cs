using AVFoundation;
using FM.LiveSwitch;
using FM.LiveSwitch.Cocoa.Helpers;
using System;
using System.Collections.Generic;
using UIKit;

namespace Chat
{
    public class App
    {
        public enum Modes
        {
            Mcu = 1,
            Sfu = 2,
            Peer = 3
        };
        public bool isRegistered = false;
        public event Action0 ClientRegistered;
        public event Action0 ClientUnregistered;
        public event Action1<string> PeerJoined;
        public event Action1<string> PeerLeft;
        public event EventHandler<MessageReceivedArgs> MessageReceived;
        public bool TextUILoaded = false;
        private const string _ApplicationId = "d232ae96-1be7-46cd-b1c1-2bfe3a94ad09";
        private const string _GatewayUrl = "https://cloud.liveswitch.io/";
        private string _sharedSecret = "d71784f67f674844a94d3242cafbb0f0802566d2abdb4aa7abdc6b7b5f287d09";
        // Track whether the user has decided to leave (unregister)
        // If they have not and the client gets into the Disconnected state then we attempt to reregister (reconnect) automatically.
        private bool _Unregistering = false;
        private int _ReregisterBackoff = 200;
        private int _MaxRegisterBackoff = 60000;

        private FM.LiveSwitch.Cocoa.LayoutManager _LayoutManager = null;

        private LocalMedia _LocalMedia = null;

        private Client _Client;
        private Channel _Channel;
        //private VideoLayout _VideoLayout;

        //private SfuUpstreamConnection _SfuUpstreamConnection;
        //private Dictionary<string, SfuDownstreamConnection> _SfuDownStreamConnections;
        private Dictionary<string, PeerConnection> _PeerConnections;
        private Dictionary<string, ManagedConnection> _RemoteMediaMaps;
        public Dictionary<string, bool> _RemoteAVMaps;
        public Dictionary<string, int[]> _RemoteEncodingMaps;
        public List<bool> sendEncodings;
        public List<string> encodings;
        public int receiveEncoding = 0;
        private string _DeviceId = Guid.NewGuid().ToString().Replace("-", "");
        private string _UserId = Guid.NewGuid().ToString().Replace("-", "");
       
        //private string _McuViewId = null;
        public bool _DataChannelConnected;
        private ManagedTimer _DataChannelsMessageTimer;
        private List<DataChannel> _DataChannels = new List<DataChannel>();
        private object _DataChannelLock = new object(); //synchronize data channel book-keeping (collection may be modified while trying to send messages in SendDataChannelMessage())

        // VideoViewController class
        public SessionViewController ssvideoViewController = null;

        #region Singleton
        private static App _app;
        public static App Instance
        {
            get
            {
                if (_app == null)
                {
                    _app = new App();
                }

                return _app;
            }
        }
        #endregion

        #region Properties

        public string ChannelId
        { get; set; }

        public string Name
        { get; set; }

        public bool AudioOnly
        { get; set; }

        public bool ReceiveOnly
        { get; set; }

        public bool EnableScreenShare
        { get; set; }

        public bool Simulcast
        { get; set; }

        public Modes Mode { get; set; }
        #endregion
        private List<MessageReceivedArgs> messages = new List<MessageReceivedArgs>();
        public void EmptyMessagesQueue()
        {
            if (messages.Count > 0)
            {
                foreach (var messageReceivedArgs in messages)
                {
                    MessageReceived?.Invoke(this, messageReceivedArgs);
                }
                messages.Clear();
            }
        }
        private void OnMessageReceived(MessageReceivedArgs args)
        {
            if (!TextUILoaded)
            {
                // The TextViewUI sometimes loads after connection has been established and therefore some messages are not displayed
                // We queue the messages until the UI loads
                messages.Add(args);
            }
            else if (TextUILoaded)
            {
                MessageReceived?.Invoke(this, args);
            }
        }

        private void OnPeerJoined(string peer)
        {
            PeerJoined?.Invoke(peer);
        }
        private void OnPeerLeft(string peer)
        {
            PeerLeft?.Invoke(peer);
        }

        private App()
        {
            AudioOnly = false;
            ReceiveOnly = false;

            // Log to the console.
            Log.Provider = new ConsoleLogProvider(LogLevel.Debug);

            
            _PeerConnections = new Dictionary<string, PeerConnection>();
            _RemoteMediaMaps = new Dictionary<string, ManagedConnection>();
            _RemoteAVMaps = new Dictionary<string, bool>();
            _RemoteEncodingMaps = new Dictionary<string, int[]>();
            sendEncodings = new List<bool>();
            encodings = new List<string>();
        }

        public void WriteLine(string message)
        {
            if (_Channel != null) // If the registration has not completed, the "_Clannel will be null. So we want to send Messages only after registration.
            {
                _Channel.SendMessage(message);
            }
        }

        public Future<FM.LiveSwitch.LocalMedia> StartLocalMedia(UIKit.UIView container)
        {
            Promise<FM.LiveSwitch.LocalMedia> promise = new Promise<FM.LiveSwitch.LocalMedia>();

            Utilities.DispatchSync(() =>
            {
                // Set up the layout manager.
                _LayoutManager = new FM.LiveSwitch.Cocoa.LayoutManager(container);
            });

            if (ReceiveOnly)
            {
                promise.Resolve(null);
            }
            else
            {
                UIKit.UIView localView;//= ((LocalCameraMedia)_LocalMedia).GetView();

                // Set up the local media.
                if (EnableScreenShare)
                {
                    LocalScreenMedia localScreenMedia = new LocalScreenMedia(false, AudioOnly, Simulcast, null);
                    localView = (UIKit.UIView)localScreenMedia.View;
                    _LocalMedia = localScreenMedia;
                    foreach (var encoding in _LocalMedia.VideoEncodings)
                    {
                        Log.Debug("Local encoding: " + encoding);
                    }
                }
                else
                {
                    _LocalMedia = new LocalCameraMedia(false, AudioOnly, Simulcast, null);
                    localView = ((LocalCameraMedia)_LocalMedia).GetView();
                }

                // Add the local preview to the layout.
                if (localView != null)
                {
                    Utilities.DispatchSync(() =>
                    {
                        _LayoutManager.SetLocalView(localView);
                    });
                    UILongPressGestureRecognizer longPress = new UILongPressGestureRecognizer(action: this.LongPressLocal);
                    longPress.MinimumPressDuration = 0.5;
                    longPress.DelaysTouchesBegan = true;
                    localView.AddGestureRecognizer(longPress);
                    ExtractSendEncodings(_LocalMedia.VideoEncodings);
                }

                // Start the local media.
                return _LocalMedia.Start().Then(new Action1<FM.LiveSwitch.LocalMedia>((localMedia) =>
                {
                    promise.Resolve(null);
                }), new Action1<Exception>((ex) =>
                {
                    promise.Reject(ex);
                }));
            }

            return promise;
        }

        public Future<FM.LiveSwitch.LocalMedia> StopLocalMedia()
        {
            return Promise<FM.LiveSwitch.LocalMedia>.WrapPromise<FM.LiveSwitch.LocalMedia>(() =>
            {
                if (_LocalMedia == null)
                {
                    return Promise<FM.LiveSwitch.LocalMedia>.ResolveNow<FM.LiveSwitch.LocalMedia>(null);
                }

                // Stop the local media.
                return _LocalMedia.Stop();
            }).Then((o) =>
            {
                Utilities.DispatchSync(() =>
                {
                    // Tear down the layout manager.
                    var lm = _LayoutManager;
                    if (lm != null)
                    {
                        lm.RemoveRemoteViews();
                        lm.UnsetLocalView();
                        _LayoutManager = null;
                    }

                    // Tear down the local media.
                    if (_LocalMedia != null)
                    {
                        // TODO: follow up on why this == crash
                        //_LocalMedia.Destroy();
                        _LocalMedia = null;
                       // videoViewController = null;
                    }
                });
            });
        }

        // Generate a register token.
        // WARNING: do NOT do this here!
        // Tokens should be generated by a secure server that
        // has authenticated your user identity and is authorized
        // to allow you to register with the LiveSwitch server.
        private string GenerateToken(ChannelClaim[] ChannelClaims)
        {
            return Token.GenerateClientRegisterToken(
                _ApplicationId,
                _Client.UserId,
                _Client.DeviceId,
                _Client.Id,
                null,
                ChannelClaims,
                _sharedSecret
            );
        }

        public Future<object> JoinAsync()
        {
            // Create a client to manage the channel.
            _Client = new Client(_GatewayUrl, _ApplicationId, _UserId, _DeviceId);
            string ChannelId = "Patient";
            ChannelClaim[] ChannelClaims = new ChannelClaim[] { new ChannelClaim(ChannelId) };

            _Client.Tag = App.Modes.Peer.ToString();
            _Client.UserAlias = Name;

            string Token = GenerateToken(ChannelClaims);

            _Client.OnStateChange += (client) =>
            {
                if (client.State == ClientState.Registering)
                {
                    Log.Debug("client is registering");
                }
                else if (client.State == ClientState.Registered)
                {
                    Log.Debug("client is registered");
                }
                else if (client.State == ClientState.Unregistering)
                {
                    Log.Debug("client is unregistering");
                }
                else if (client.State == ClientState.Unregistered)
                {
                    Log.Debug("client is unregistered");

                    // Client has failed for some reason:
                    // We do not need to `c.closeAll()` as the client handled this for us as part of unregistering.
                    if (!_Unregistering)
                    {
                        // Back off our reregister attempts as they continue to fail to avoid runaway process.
                        ManagedThread.Sleep(_ReregisterBackoff);
                        if (_ReregisterBackoff < _MaxRegisterBackoff)
                        {
                            _ReregisterBackoff += _ReregisterBackoff;
                        }

                        // ReRegister
                        Token = GenerateToken(ChannelClaims); // ensure token has not expired
                        _Client.Register(Token)
                        .Then<object>(channels =>
                        {
                            _ReregisterBackoff = 200; // reset for next time
                            return OnClientRegistered(channels);
                        })
                        .Fail((e) =>
                        {
                            Log.Error("Failed to register with Gateway.", e);
                        });

                    }
                }
            };

            return _Client.Register(Token)
            .Then(channels =>
            {
                return OnClientRegistered(channels);
            })
            .Fail((e) =>
            {
                Log.Error("Failed to register with Gateway.", e);
            });
        }

        private Future<object> OnClientRegistered(Channel[] channels)
        {
            if (channels == null)
            {
                return null;
            }

            _Channel = channels[0];

            _Channel.OnRemoteClientJoin += (remoteClientInfo) =>
            {
                Log.Info(string.Format("Remote client joined the channel (client ID: {0}, device ID: {1}, user ID: {2}, tag: {3}).", remoteClientInfo.Id, remoteClientInfo.DeviceId, remoteClientInfo.UserId, remoteClientInfo.Tag));

                string n = !string.IsNullOrEmpty(remoteClientInfo.UserAlias) ? remoteClientInfo.UserAlias : remoteClientInfo.UserId;
                OnPeerJoined(n);
            };

            _Channel.OnRemoteClientLeave += (remoteClientInfo) =>
            {
                string n = !string.IsNullOrEmpty(remoteClientInfo.UserAlias) ? remoteClientInfo.UserAlias : remoteClientInfo.UserId;
                OnPeerLeft(n);
                Log.Info(string.Format("Remote client left the channel (client ID: {0}, device ID: {1}, user ID: {2}, tag: {3}).", remoteClientInfo.Id, remoteClientInfo.DeviceId, remoteClientInfo.UserId, remoteClientInfo.Tag));
            };

            //_Channel.OnRemoteUpstreamConnectionOpen += (remoteConnectionInfo) =>
            //{
            //    Log.Info(string.Format("Remote client opened upstream connection (connection ID: {0}, client Id: {1}, device ID: {2}, user ID: {3}, tag: {4}).", remoteConnectionInfo.Id, remoteConnectionInfo.ClientId, remoteConnectionInfo.DeviceId, remoteConnectionInfo.UserId, remoteConnectionInfo.Tag));

            //    if (Mode == Modes.Sfu)
            //    {
            //        OpenSfuDownstreamConnection(remoteConnectionInfo);
            //    }
            //};

            _Channel.OnRemoteUpstreamConnectionClose += (remoteConnectionInfo) =>
            {
                Log.Info(string.Format("Remote client closed upstream connection (connection ID: {0}, client Id: {1}, device ID: {2}, user ID: {3}, tag: {4}).", remoteConnectionInfo.Id, remoteConnectionInfo.ClientId, remoteConnectionInfo.DeviceId, remoteConnectionInfo.UserId, remoteConnectionInfo.Tag));
            };

            _Channel.OnPeerConnectionOffer += (peerConnectionOffer) =>
            {
                OpenPeerAnswerConnection(peerConnectionOffer);
            };

            _Channel.OnMessage += (clientInfo, message) =>
            {
                string n = !string.IsNullOrEmpty(clientInfo.UserAlias) ? clientInfo.UserAlias : clientInfo.UserId;
                OnMessageReceived(new MessageReceivedArgs(n, message));
            };

            if (Mode == Modes.Peer)
            {
                foreach (var remoteClientInfo in _Channel.RemoteClientInfos)
                {
                    OpenPeerOfferConnection(remoteClientInfo);
                }
            }
            isRegistered = true;
            ClientRegistered?.Invoke(); // This may not invoke anything if TextViewController has not loaded.

            return Promise<object>.ResolveNow<object>(null);

        }

        public Future<object> LeaveAsync()
        {
            if (_DataChannelsMessageTimer != null)
            {
                // Need to stop sending messages on data channel before we
                // unregister client
                _DataChannelsMessageTimer.Stop();
                _DataChannelsMessageTimer = null;
            }

            if (_Client != null)
            {
                _Unregistering = true;
                return _Client.Unregister().Then((obj) =>
                {
                    _Unregistering = false;
                })
                 .Then((obj) =>
                 {
                     isRegistered = false;
                     ClientUnregistered?.Invoke();
                     _DataChannelConnected = false;
                 }).Fail((Exception p) =>
                 {
                     Log.Debug(String.Format("Failed to unregister client"));
                 });
            }
            else
            {
                return null;
            }
        }
        private PeerConnection OpenPeerOfferConnection(ClientInfo remoteClientInfo, String tag = null)
        {
            RemoteMedia remoteMedia = null;

            Utilities.DispatchSync(() =>
            {
                remoteMedia = new RemoteMedia(false, AudioOnly, null);
                _LayoutManager.AddRemoteView(remoteMedia.Id, remoteMedia.View);
                UIView remoteView = remoteMedia.View;
                if (remoteView != null)
                {
                    remoteView.AccessibilityIdentifier = remoteMedia.Id;
                    UILongPressGestureRecognizer longPress = new UILongPressGestureRecognizer(action: this.LongPressRemote);
                    longPress.MinimumPressDuration = 0.5;
                    longPress.DelaysTouchesBegan = true;
                    remoteView.AddGestureRecognizer(longPress);
                }
            });

            PeerConnection connection;
            VideoStream videoStream = null;
            AudioStream audioStream = new AudioStream(_LocalMedia, remoteMedia);
            if (!AudioOnly)
            {
                videoStream = new VideoStream(_LocalMedia, remoteMedia);
            }

            //Please note that DataStreams can also be added to Peer-to-peer connections.
            //Nevertheless, since peer connections do not connect to the media server, there may arise
            //incompatibilities with the peers that do not support DataStream (e.g. Microsoft Edge browser:
            //https://developer.microsoft.com/en-us/microsoft-edge/platform/status/rtcdatachannels/?filter=f3f0000bf&search=rtc&q=data%20channels).
            //For a solution around this issue and complete documentation visit:
            //https://help.frozenmountain.com/docs/liveswitch1/working-with-datachannels

            connection = _Channel.CreatePeerConnection(remoteClientInfo, audioStream, videoStream);

            _PeerConnections.Add(connection.Id, connection);
            _RemoteMediaMaps.Add(remoteMedia.Id, connection);

            // Tag the connection (optional).
            if (tag != null)
            {
                connection.Tag = tag;
            }

            /*
            Embedded TURN servers are used by default.  For more information refer to:
            https://help.frozenmountain.com/docs/liveswitch/server/advanced-topics#TURNintheMediaServer
            */
            connection.OnStateChange += (conn) =>
            {
                Log.Info(string.Format("{0}: Peer connection state is {1}.", conn.Id, conn.State.ToString()));

                if (conn.State == ConnectionState.Closing || conn.State == ConnectionState.Failing)
                {
                    if (conn.RemoteRejected)
                    {
                        Log.Info(string.Format("{0}: Remote peer rejected the connection.", conn.Id));
                    }
                    else if (conn.RemoteClosed)
                    {
                        Log.Info(string.Format("{0}: Remote peer closed the connection.", conn.Id));
                    }
                    var lm = _LayoutManager;
                    if (lm != null)
                    {
                        lm.RemoveRemoteView(remoteMedia.Id);
                    }

                    // https://forums.developer.apple.com/thread/22133
                    // https://trac.pjsip.org/repos/ticket/1697
                    // Workaround to fix reduced volume issue after the teardown of audio unit.
                    AVAudioSession.SharedInstance().SetCategory(AVAudioSessionCategory.Record);
                    remoteMedia.Destroy();
                    AVAudioSession.SharedInstance().SetCategory(AVAudioSessionCategory.PlayAndRecord, AVAudioSessionCategoryOptions.AllowBluetooth | AVAudioSessionCategoryOptions.DefaultToSpeaker);
                    _PeerConnections.Remove(conn.Id);
                    _RemoteMediaMaps.Remove(remoteMedia.Id);
                    LogConnectionState(conn, "Peer");
                }
                else if (conn.State == ConnectionState.Failed)
                {
                    // Note: no need to close the connection as it's done for us.
                    OpenPeerOfferConnection(remoteClientInfo, tag);
                    LogConnectionState(conn, "Peer");
                }
                else if (((PeerConnection)conn).State == ConnectionState.Connected)
                {
                    LogConnectionState(conn, "Peer");
                }
            };

            connection.Open();
            return connection;
        }

        private PeerConnection OpenPeerAnswerConnection(PeerConnectionOffer peerConnectionOffer, string tag = null)
        {
            RemoteMedia remoteMedia = null;

            Utilities.DispatchSync(() =>
            {
                var disableRemoteVideo = AudioOnly;
                if (peerConnectionOffer.HasVideo)
                {
                    /*
                     * The remote client is offering audio AND video => they are expecting a VideoStream in the connection.
                     * To create this connection successfully we must include a VideoStream, even though we may have chosen to be in audio only mode.
                     * In this case we simply set the VideoStream's direction to inactive.
                    */
                    disableRemoteVideo = false;
                }
                remoteMedia = new RemoteMedia(false, disableRemoteVideo, null);
                _LayoutManager.AddRemoteView(remoteMedia.Id, remoteMedia.View);
                UIView remoteView = remoteMedia.View;
                if (remoteView != null)
                {
                    remoteView.AccessibilityIdentifier = remoteMedia.Id;
                    UILongPressGestureRecognizer longPress = new UILongPressGestureRecognizer(action: this.LongPressRemote);
                    longPress.MinimumPressDuration = 0.5;
                    longPress.DelaysTouchesBegan = true;
                    remoteView.AddGestureRecognizer(longPress);
                }
            });

            PeerConnection connection;

            VideoStream videoStream = null;
            AudioStream audioStream = null;

            if (peerConnectionOffer.HasAudio)
            {
                audioStream = new AudioStream(_LocalMedia, remoteMedia);
            }
            if (peerConnectionOffer.HasVideo)
            {
                videoStream = new VideoStream(_LocalMedia, remoteMedia);
                if (AudioOnly)
                {
                    videoStream.LocalDirection = StreamDirection.Inactive;
                }
            }

            //Please note that DataStreams can also be added to Peer-to-peer connections.
            //Nevertheless, since peer connections do not connect to the media server, there may arise
            //incompatibilities with the peers that do not support DataStream (e.g. Microsoft Edge browser:
            //https://developer.microsoft.com/en-us/microsoft-edge/platform/status/rtcdatachannels/?filter=f3f0000bf&search=rtc&q=data%20channels).
            //For a solution around this issue and complete documentation visit:
            //https://help.frozenmountain.com/docs/liveswitch1/working-with-datachannels

            connection = _Channel.CreatePeerConnection(peerConnectionOffer, audioStream, videoStream);

            /*
            Embedded TURN servers are used by default.  For more information refer to:
            https://help.frozenmountain.com/docs/liveswitch/server/advanced-topics#TURNintheMediaServer
            */

            _PeerConnections.Add(connection.Id, connection);
            _RemoteMediaMaps.Add(remoteMedia.Id, connection);

            // Tag the connection (optional).
            if (tag != null)
            {
                connection.Tag = tag;
            }

            connection.OnStateChange += (conn) =>
            {
                Log.Info(string.Format("{0}: Peer connection state is {1}.", conn.Id, conn.State.ToString()));

                if (conn.State == ConnectionState.Closing || conn.State == ConnectionState.Failing)
                {
                    if (conn.RemoteClosed)
                    {
                        Log.Info(string.Format("{0}: Remote peer closed the connection.", conn.Id));
                    }

                    var lm = _LayoutManager;
                    if (lm != null)
                    {
                        lm.RemoveRemoteView(remoteMedia.Id);
                    }

                    // https://forums.developer.apple.com/thread/22133
                    // https://trac.pjsip.org/repos/ticket/1697
                    // Workaround to fix reduced volume issue after the teardown of audio unit.
                    AVAudioSession.SharedInstance().SetCategory(AVAudioSessionCategory.Record);
                    remoteMedia.Destroy();
                    AVAudioSession.SharedInstance().SetCategory(AVAudioSessionCategory.PlayAndRecord, AVAudioSessionCategoryOptions.AllowBluetooth | AVAudioSessionCategoryOptions.DefaultToSpeaker);
                    _PeerConnections.Remove(conn.Id);
                    _RemoteMediaMaps.Remove(remoteMedia.Id);
                    LogConnectionState(conn, "Peer");
                }
                else if (conn.State == ConnectionState.Failed)
                {
                    // Note: no need to close the connection as it's done for us.
                    // Note: do not offer a new answer here. Let the offerer reoffer and then we answer normally.
                    LogConnectionState(conn, "Peer");
                }
                else if (conn.State == ConnectionState.Connected)
                {
                    LogConnectionState(conn, "Peer");
                }
            };

            connection.Open();
            return connection;
        }

    

        private void LogConnectionState(ManagedConnection conn, string connectionType)
        {
            var streams = "";
            var streamCount = 0;
            if (conn.AudioStream != null)
            {
                streamCount++;
                streams = "audio";
            }
            if (conn.DataStream != null)
            {
                if (streams.Length > 0)
                {
                    streams += "/";
                }
                streamCount++;
                streams += "data";
            }
            if (conn.VideoStream != null)
            {
                if (streams.Length > 0)
                {
                    streams += "/";
                }
                streamCount++;
                streams += "video";
            }
            if (streamCount > 1)
            {
                streams += " streams.";
            }
            else
            {
                streams += " stream.";
            }

            if (conn.State == ConnectionState.Connected)
            {
                OnMessageReceived(new MessageReceivedArgs("System", connectionType + " connection connected with " + streams));
            }
            else if (conn.State == ConnectionState.Closing)
            {
                OnMessageReceived(new MessageReceivedArgs("System", connectionType + " connection closing for " + streams));
            }
            else if (conn.State == ConnectionState.Failing)
            {
                var eventString = connectionType + " connection failing for " + streams;

                if (conn.Error != null)
                {
                    eventString += conn.Error.GetDescription();
                }
                OnMessageReceived(new MessageReceivedArgs("System", eventString));
            }
            else if (conn.State == ConnectionState.Closed)
            {
                OnMessageReceived(new MessageReceivedArgs("System", connectionType + " connection closed for " + streams));
            }
            else if (conn.State == ConnectionState.Failed)
            {
                OnMessageReceived(new MessageReceivedArgs("System", connectionType + " connection failed for " + streams));
            }

        }

        private DataChannel PrepareDataChannel()
        {
            var dc = new DataChannel("data")
            {
                OnReceive = (e) => {
                    //Display info indicating that a message has been receieved only once.
                    if (!_DataChannelConnected)
                    {
                        if (e.DataString != null)
                        {
                            OnMessageReceived(new MessageReceivedArgs("System", string.Format("Data channel connection established. Received test message from server: {0}", e.DataString)));
                        }
                        _DataChannelConnected = true;
                    }
                }
            };
            dc.OnStateChange += (e) =>
            {
                if (e.State == DataChannelState.Connected)
                {
                    if (_DataChannelsMessageTimer == null)
                    {
                        //Send a "Hello world!" to everyone once a second.
                        _DataChannelsMessageTimer = new ManagedTimer(1000, SendDataChannelMessage);
                        _DataChannelsMessageTimer.Start();
                    }
                }
            };

            return dc;
        }

        private void SendDataChannelMessage()
        {
            DataChannel[] dcs;
            lock (_DataChannelLock)
            {
                dcs = _DataChannels.ToArray();
            }
            foreach (var channel in dcs)
            {
                channel.SendDataString("Hello world!");
            }
        }

        private bool UsingFrontVideoDevice = true;
        public void UseNextVideoDevice()
        {
            if (_LocalMedia != null && _LocalMedia.VideoSource != null)
            {
                _LocalMedia.ChangeVideoSourceInput(UsingFrontVideoDevice ?
                    ((FM.LiveSwitch.Cocoa.AVCaptureSource)_LocalMedia.VideoSource).BackInput :
                    ((FM.LiveSwitch.Cocoa.AVCaptureSource)_LocalMedia.VideoSource).FrontInput);

                UsingFrontVideoDevice = !UsingFrontVideoDevice;
            }
        }

        public void ToggleSendEncoding()
        {
            var encodings = _LocalMedia.VideoEncodings;
            if (encodings != null && encodings.Length > 1)
            {
                for (var i = 0; i < encodings.Length; i++)
                {
                    encodings[i].Deactivated = !sendEncodings[i];
                }
            }
            _LocalMedia.VideoEncodings = encodings;
        }

        public void ToggleReceiveEncoding(string id)
        {
            //var connection = _SfuDownStreamConnections[id];
            //var encodings = connection.RemoteConnectionInfo.VideoStream.SendEncodings;
            //if (encodings != null && encodings.Length > 1 && receiveEncoding < encodings.Length)
            //{
            //    var encoding = encodings[receiveEncoding];
            //    var config = connection.Config;
            //    config.RemoteVideoEncoding = encodings[receiveEncoding];
            //    connection.Update(config).Then((__) =>
            //    {
            //        Log.Debug("Set remote encoding for connection " + connection.Id + ": " + encoding.ToString());
            //    }).Fail((ex) =>
            //    {
            //        Log.Error("Could not set remote encoding for connection " + connection.Id + ": " + encoding.ToString());
            //    });
            //}
            // update _RemoteEncodingMaps
            foreach (KeyValuePair<string, int[]> entry in _RemoteEncodingMaps)
            {
                if (entry.Key.Contains(id))
                {
                    if (entry.Value[0] == receiveEncoding)
                    {
                        _RemoteEncodingMaps[entry.Key][1] = 1;
                    }
                    else
                    {
                        _RemoteEncodingMaps[entry.Key][1] = 0;
                    }
                }
            }
        }

        public void ToggleLocalDisableVideo()
        {
            ConnectionConfig config = null;
   
            foreach (var peerConnection in _PeerConnections.Values)
            {
                config = peerConnection.Config;
                config.LocalVideoDisabled = !config.LocalVideoDisabled;
                peerConnection.Update(config);
            }
            ssvideoViewController.disableVideoTitle = config.LocalVideoDisabled ? "Enable Video" : "Disable Video";
        }

        public void ToggleRemoteDisableVideo(string id)
        {
            var downstream = _RemoteMediaMaps[id];
            var config = downstream.Config;
            config.RemoteVideoDisabled = !config.RemoteVideoDisabled;
            downstream.Update(config);
            _RemoteAVMaps[id + "DisableVideo"] = config.RemoteVideoDisabled;
        }

        public void ToggleLocalDisableAudio()
        {
            ConnectionConfig config = null;
           
            foreach (var peerConnection in _PeerConnections.Values)
            {
                config = peerConnection.Config;
                config.LocalAudioDisabled = !config.LocalAudioDisabled;
                peerConnection.Update(config);
            }
            ssvideoViewController.disableAudioTitle = config.LocalAudioDisabled ? "Enable Audio" : "Disable Audio";
        }

        public void ToggleRemoteDisableAudio(string id)
        {
            var downstream = _RemoteMediaMaps[id];
            var config = downstream.Config;
            config.RemoteAudioDisabled = !config.RemoteAudioDisabled;
            downstream.Update(config);
            _RemoteAVMaps[id + "DisableAudio"] = config.RemoteAudioDisabled;
        }

        public void ToggleMuteVideo()
        {
            ConnectionConfig config = null;
           
            foreach (var peerConnection in _PeerConnections.Values)
            {
                config = peerConnection.Config;
                config.LocalVideoMuted = !config.LocalVideoMuted;
                peerConnection.Update(config);
            }
            ssvideoViewController.muteVideoTitle = config.LocalVideoMuted ? "Unmute Video" : "Mute Video";
        }

        public void ToggleMuteAudio()
        {
            ConnectionConfig config = null;
            
            foreach (var peerConnection in _PeerConnections.Values)
            {
                config = peerConnection.Config;
                config.LocalAudioMuted = !config.LocalAudioMuted;
                peerConnection.Update(config);
            }
            ssvideoViewController.muteAudioTitle = config.LocalAudioMuted ? "Unmute Audio" : "Mute Audio";
        }

        public void ExtractSendEncodings(VideoEncodingConfig[] encodings)
        {
            this.encodings.Clear();
            this.sendEncodings.Clear();
            for (var i = 0; i < encodings.Length; i++)
            {
                var encoding = encodings[i].ToString();
                var index = encoding.IndexOf("Bitrate");
                if (index != -1 && encoding.Length - index > 28)
                {
                    encoding = encoding.Substring(index, 28);
                }
                this.encodings.Add(encoding);
                this.sendEncodings.Add(true);
            }
        }

        public void InitRemoteMaps(string id, EncodingInfo[] encodings)
        {
            _RemoteAVMaps.Add(id + "DisableAudio", false);
            _RemoteAVMaps.Add(id + "DisableVideo", false);
            for (var i = 0; i < encodings.Length; i++)
            {
                var encoding = encodings[i].ToString();
                var index = encoding.IndexOf("Bitrate");
                if (index != -1 && encoding.Length - index > 28)
                {
                    encoding = encoding.Substring(index, 28);
                }
                var key = id + encoding;
                if (i == 0)
                {
                    _RemoteEncodingMaps.Add(key, new int[] { i, 1 });
                }
                else
                {
                    _RemoteEncodingMaps.Add(key, new int[] { i, 0 });
                }
            }
        }

        public void ClearRemoteMaps(string id)
        {
            foreach (var key in _RemoteEncodingMaps.Keys)
            {
                if (key.Contains(id))
                {
                    _RemoteEncodingMaps.Remove(key);
                }
            }
            foreach (var key in _RemoteAVMaps.Keys)
            {
                if (key.Contains(id))
                {
                    _RemoteEncodingMaps.Remove(key);
                }
            }
        }


        private void LongPressLocal(UILongPressGestureRecognizer sender)
        {
            if (sender.State == UIGestureRecognizerState.Began)
            {
                ssvideoViewController.ShowLocalContextMenu();
            }
        }

        private void LongPressRemote(UILongPressGestureRecognizer sender)
        {
            string id = sender.View.AccessibilityIdentifier;
            // update receiveEncoding for the current selected remote session
            foreach (KeyValuePair<string, int[]> entry in _RemoteEncodingMaps)
            {
                if (entry.Key.Contains(id))
                {
                    if (entry.Value[1] == 1)
                    {
                        receiveEncoding = entry.Value[0];
                    }
                }
            }
            if (sender.State == UIGestureRecognizerState.Began)
            {
                ssvideoViewController.ShowRemoteContextMenu(id);
            }
        }
    }
}
