using AVFoundation;
using FM.LiveSwitch;
using FM.LiveSwitch.Cocoa;
using UIKit;
using Matroska = FM.LiveSwitch.Matroska;
using Opus = FM.LiveSwitch.Opus;
using Vp8 = FM.LiveSwitch.Vp8;
using Vp9 = FM.LiveSwitch.Vp9;
using Yuv = FM.LiveSwitch.Yuv;

namespace Chat
{
    public class LocalCameraMedia : LocalMedia<OpenGLView>
	{
		private VideoConfig _CameraConfig = new VideoConfig(640, 480, 30);
		
		// Alternatively, you can use AVCaptureSession Presets for VideoConfig
		// private VideoConfig _CameraConfig = new VideoConfig(AVCaptureSession.Preset640x480, 30);

		private AVCapturePreview _preview;

		/// <summary>
		/// Initializes a new instance of the <see cref="LocalCameraMedia"/> class.
		/// </summary>
		/// <param name="disableAudio">Whether to disable audio.</param>
		/// <param name="disableVideo">Whether to disable video.</param>
		/// <param name="aecContext">The AEC context, if using software echo cancellation.</param>
		public LocalCameraMedia(bool disableAudio, bool disableVideo, bool enableSimulcast, AecContext aecContext)
			: base(disableAudio, disableVideo, aecContext)
		{
			_preview = new AVCapturePreview();
			VideoSimulcastDisabled = !enableSimulcast;
			Initialize();
		}

		/// <summary>
		/// Creates a video source.
		/// </summary>
		/// <returns></returns>
		protected override VideoSource CreateVideoSource()
		{
			return new AVCaptureSource(_preview, _CameraConfig);

		}

		/// <summary>
		/// Creates a view sink.
		/// </summary>
		/// <returns></returns>
		protected override ViewSink<OpenGLView> CreateViewSink()
		{
			return null;
		}

		public UIKit.UIView GetView()
		{
			return _preview;
		}
	}

	public class LocalScreenMedia : LocalMedia<UIImageView>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LocalScreenMedia"/> class.
		/// </summary>
		/// <param name="disableAudio">Whether to disable audio.</param>
		/// <param name="disableVideo">Whether to disable video.</param>
		/// <param name="aecContext">The AEC context, if using software echo cancellation.</param>
		public LocalScreenMedia(bool disableAudio, bool disableVideo, bool enableSimulcast, AecContext aecContext)
			: base(disableAudio, disableVideo, aecContext)
		{
			VideoSimulcastDisabled = !enableSimulcast;
			Initialize();
		}

		/// <summary>
		/// Creates a video source.
		/// </summary>
		/// <returns></returns>
		protected override VideoSource CreateVideoSource()
		{
			return new ScreenSource(3);
		}

		/// <summary>
		/// Creates a view sink.
		/// </summary>
        protected override ViewSink<UIImageView> CreateViewSink()
		{
            return new ImageViewSink();
		}

	}

    public abstract class LocalMedia<TView> : RtcLocalMedia<TView>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LocalMedia"/> class.
		/// </summary>
		/// <param name="disableAudio">Whether to disable audio.</param>
		/// <param name="disableVideo">Whether to disable video.</param>
		/// <param name="aecContext">The AEC context, if using software echo cancellation.</param>
		public LocalMedia(bool disableAudio, bool disableVideo, AecContext aecContext)
			: base(disableAudio, disableVideo, aecContext)
		{
			AVAudioSession.SharedInstance().SetCategory(AVAudioSessionCategory.PlayAndRecord, AVAudioSessionCategoryOptions.AllowBluetooth | AVAudioSessionCategoryOptions.DefaultToSpeaker);
		}

		/// <summary>
		/// Creates an audio recorder.
		/// </summary>
		/// <param name="inputFormat">The input format.</param>
		/// <returns></returns>
		protected override AudioSink CreateAudioRecorder(AudioFormat inputFormat)
		{
			return new Matroska.AudioSink(Id + "-local-audio-" + inputFormat.Name.ToLower() + ".mkv");
		}

		/// <summary>
		/// Creates an audio source.
		/// </summary>
		/// <param name="config">The configuration.</param>
		/// <returns></returns>
		protected override AudioSource CreateAudioSource(AudioConfig config)
		{
			return new AudioUnitSource(config);
		}

		/// <summary>
		/// Creates an H.264 encoder.
		/// </summary>
		/// <returns></returns>
		protected override VideoEncoder CreateH264Encoder()
		{
			return null;
		}

		/// <summary>
		/// Creates an image converter.
		/// </summary>
		/// <param name="outputFormat">The output format.</param>
		/// <returns></returns>
		protected override VideoPipe CreateImageConverter(VideoFormat outputFormat)
		{
			return new Yuv.ImageConverter(outputFormat);
		}

		/// <summary>
		/// Creates an Opus encoder.
		/// </summary>
		/// <param name="config">The configuration.</param>
		/// <returns></returns>
		protected override AudioEncoder CreateOpusEncoder(AudioConfig config)
		{
			return new Opus.Encoder(config);
		}

		/// <summary>
		/// Creates a video recorder.
		/// </summary>
		/// <param name="inputFormat">The output format.</param>
		/// <returns></returns>
		protected override VideoSink CreateVideoRecorder(VideoFormat inputFormat)
		{
			return new Matroska.VideoSink(Id + "-local-video-" + inputFormat.Name.ToLower() + ".mkv");
		}

		/// <summary>
		/// Creates a VP8 encoder.
		/// </summary>
		/// <returns></returns>
		protected override VideoEncoder CreateVp8Encoder()
		{
			return new Vp8.Encoder();
		}

		/// <summary>
		/// Creates a VP9 encoder.
		/// </summary>
		/// <returns></returns>
		protected override VideoEncoder CreateVp9Encoder()
		{
			return new Vp9.Encoder();
		}
	}
}
