using FM.LiveSwitch;
using FM.LiveSwitch.Cocoa;
using Matroska = FM.LiveSwitch.Matroska;
using Opus = FM.LiveSwitch.Opus;
using Pcma = FM.LiveSwitch.Pcma;
using Pcmu = FM.LiveSwitch.Pcmu;
using Vp8 = FM.LiveSwitch.Vp8;
using Vp9 = FM.LiveSwitch.Vp9;
using Yuv = FM.LiveSwitch.Yuv;

namespace Chat
{
    public class RemoteMedia : RtcRemoteMedia<OpenGLView>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RemoteMedia"/> class.
		/// </summary>
		/// <param name="disableAudio">if set to <c>true</c> [disable audio].</param>
		/// <param name="disableVideo">if set to <c>true</c> [disable video].</param>
		/// <param name="aecContext">The aec context.</param>
		public RemoteMedia(bool disableAudio, bool disableVideo, AecContext aecContext)
			: base(disableAudio, disableVideo, aecContext)
		{
			Initialize();
		}

		/// <summary>
		/// Creates an audio recorder.
		/// </summary>
		/// <param name="inputFormat">The input format.</param>
		/// <returns></returns>
		protected override AudioSink CreateAudioRecorder(AudioFormat inputFormat)
		{
			return new Matroska.AudioSink(Id + "-remote-audio-" + inputFormat.Name.ToLower() + ".mkv");
		}

		/// <summary>
		/// Creates an audio sink.
		/// </summary>
		/// <param name="config">The configuration.</param>
		/// <returns></returns>
		protected override AudioSink CreateAudioSink(AudioConfig config)
		{
			return new AudioUnitSink(config);
		}

		/// <summary>
		/// Creates a pcma decoder.
		/// </summary>
		/// <param name="config">The configuration.</param>
		/// <returns></returns>
		protected override AudioDecoder CreatePcmaDecoder(AudioConfig config)
		{
			return new Pcma.Decoder(config);
		}

		/// <summary>
		/// Creates a pcmu decoder.
		/// </summary>
		/// <param name="config">The configuration.</param>
		/// <returns></returns>
		protected override AudioDecoder CreatePcmuDecoder(AudioConfig config)
		{
			return new Pcmu.Decoder(config);
		}

		/// <summary>
		/// Creates an H.264 encoder.
		/// </summary>
		/// <returns></returns>
		protected override VideoDecoder CreateH264Decoder()
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
		protected override AudioDecoder CreateOpusDecoder(AudioConfig config)
		{
			return new Opus.Decoder(config);
		}

		/// <summary>
		/// Creates a video recorder.
		/// </summary>
		/// <param name="inputFormat">The output format.</param>
		/// <returns></returns>
		protected override VideoSink CreateVideoRecorder(VideoFormat inputFormat)
		{
			return new Matroska.VideoSink(Id + "-remote-video-" + inputFormat.Name.ToLower() + ".mkv");
		}

		/// <summary>
		/// Creates a view sink.
		/// </summary>
		protected override ViewSink<OpenGLView> CreateViewSink()
		{
			return new OpenGLSink();
		}

		/// <summary>
		/// Creates a VP8 encoder.
		/// </summary>
		/// <returns></returns>
		protected override VideoDecoder CreateVp8Decoder()
		{
			return new Vp8.Decoder();
		}

		/// <summary>
		/// Creates a VP9 encoder.
		/// </summary>
		/// <returns></returns>
		protected override VideoDecoder CreateVp9Decoder()
		{
			return new Vp9.Decoder();
		}
	}
}
