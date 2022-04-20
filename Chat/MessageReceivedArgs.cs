using System;
namespace Chat
{
	public class MessageReceivedArgs : EventArgs
	{
		public string Name { get; private set; }
		public string Message { get; private set; }

		public MessageReceivedArgs(string name, string message)
		{
			this.Name = name;
			this.Message = message;
		}
	}
}
